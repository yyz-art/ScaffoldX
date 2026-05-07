using System.Drawing;
using System.IO;
using ScaffoldX.App.Models;
using ScaffoldX.Core.Vision;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// <see cref="IAutoLabelingService"/> 的实现，基于 TorchSharp + SAM 3 提供自动标注能力。
/// </summary>
public class AutoLabelingService : IAutoLabelingService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<AutoLabelingService>();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly Func<ISam3SegmentationEngine> _engineFactory;
    private ISam3SegmentationEngine? _sam3;
    private ImageEmbedding? _cachedEmbedding;
    private string? _cachedEmbeddingPath;

    public AutoLabelingService(Func<ISam3SegmentationEngine> engineFactory)
    {
        _engineFactory = engineFactory;
    }

    /// <inheritdoc/>
    public bool IsModelLoaded => _sam3?.IsLoaded == true;

    /// <inheritdoc/>
    public string? LoadedModelPath { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<string> ClassNames => Array.Empty<string>();

    /// <inheritdoc/>
    public AutoLabelingMode CurrentMode { get; private set; } = AutoLabelingMode.Detection;

    /// <inheritdoc/>
    public async Task LoadModelAsync(string modelPath, string? classesFilePath = null, IEnumerable<string>? classNames = null)
    {
        UnloadModel();

        // modelPath can be a directory (SAM3) or a file path (legacy)
        var modelDir = Directory.Exists(modelPath) ? modelPath : Path.GetDirectoryName(modelPath) ?? string.Empty;

        _sam3 = _engineFactory();
        await _sam3.LoadModelAsync(modelDir);
        LoadedModelPath = modelDir;
        CurrentMode = AutoLabelingMode.Detection;

        _logger.Information("检测模型已加载: {ModelDir}", modelDir);
    }

    /// <inheritdoc/>
    public async Task LoadSam3ModelAsync(string modelDirectory, CancellationToken ct = default)
    {
        UnloadModel();

        _sam3 = _engineFactory();
        await _sam3.LoadModelAsync(modelDirectory, ct);
        LoadedModelPath = modelDirectory;
        CurrentMode = AutoLabelingMode.Segmentation;

        _logger.Information("SAM 3 分割模型已加载: {ModelDir}", modelDirectory);
    }

    /// <inheritdoc/>
    public void UnloadModel()
    {
        _cacheLock.Wait();
        try
        {
            _cachedEmbedding?.Dispose();
            _cachedEmbedding = null;
            _cachedEmbeddingPath = null;
        }
        finally
        {
            _cacheLock.Release();
        }

        _sam3?.Dispose();
        _sam3 = null;
        LoadedModelPath = null;
        CurrentMode = AutoLabelingMode.Detection;
    }

    /// <inheritdoc/>
    public async Task<List<BoundingBoxAnnotation>> DetectAsync(string imagePath, float confidenceThreshold = 0.5f)
    {
        if (_sam3 == null || !_sam3.IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModelAsync。");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("图像文件不存在", imagePath);

        using var image = new Bitmap(imagePath);

        // 使用 SAM 3 的自动分割获取掩码，然后转换为边界框
        await _cacheLock.WaitAsync();
        ImageEmbedding embedding;
        try
        {
            _cachedEmbedding?.Dispose();
            _cachedEmbedding = await _sam3.EncodeImageAsync(image);
            _cachedEmbeddingPath = imagePath;
            embedding = _cachedEmbedding;
        }
        finally
        {
            _cacheLock.Release();
        }

        var results = await _sam3.SegmentByTextAsync(embedding, new[] { "object" }, confidenceThreshold);

        return results.Select(r => SegmentationToBBox(r, image.Width, image.Height)).ToList();
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<BoundingBoxAnnotation>>> DetectBatchAsync(
        IEnumerable<string> imagePaths,
        float confidenceThreshold = 0.5f,
        IProgress<(int current, int total)>? progress = null)
    {
        if (_sam3 == null || !_sam3.IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModelAsync。");

        var paths = imagePaths.ToList();
        var results = new Dictionary<string, List<BoundingBoxAnnotation>>();

        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            progress?.Report((i + 1, paths.Count));

            try
            {
                if (!File.Exists(path))
                {
                    _logger.Warning("图像文件不存在，跳过: {Path}", path);
                    results[path] = new List<BoundingBoxAnnotation>();
                    continue;
                }

                results[path] = await DetectAsync(path, confidenceThreshold);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "自动标注失败: {Path}", path);
                results[path] = new List<BoundingBoxAnnotation>();
            }
        }

        _logger.Information("批量自动标注完成: {Count} 张图像", paths.Count);
        return results;
    }

    /// <inheritdoc/>
    public async Task<List<SegmentationAnnotation>> SegmentByTextAsync(
        string imagePath, IEnumerable<string> textPrompts, float threshold = 0.5f, CancellationToken ct = default)
    {
        if (_sam3 == null || !_sam3.IsLoaded)
            throw new InvalidOperationException("SAM 3 模型未加载。");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("图像文件不存在", imagePath);

        using var image = new Bitmap(imagePath);

        await _cacheLock.WaitAsync();
        ImageEmbedding embedding;
        try
        {
            _cachedEmbedding?.Dispose();
            _cachedEmbedding = await _sam3.EncodeImageAsync(image, ct);
            _cachedEmbeddingPath = imagePath;
            embedding = _cachedEmbedding;
        }
        finally
        {
            _cacheLock.Release();
        }

        var results = await _sam3.SegmentByTextAsync(embedding, textPrompts, threshold, ct);

        return results.Select(r => ToSegmentationAnnotation(r)).ToList();
    }

    /// <inheritdoc/>
    public async Task<SegmentationAnnotation> SegmentByPointsAsync(
        string imagePath, IEnumerable<PointF> positivePoints, IEnumerable<PointF> negativePoints, CancellationToken ct = default)
    {
        if (_sam3 == null || !_sam3.IsLoaded)
            throw new InvalidOperationException("SAM 3 模型未加载。");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("图像文件不存在", imagePath);

        using var image = new Bitmap(imagePath);

        await _cacheLock.WaitAsync();
        ImageEmbedding embedding;
        try
        {
            if (_cachedEmbedding == null || _cachedEmbeddingPath != imagePath)
            {
                _cachedEmbedding?.Dispose();
                _cachedEmbedding = await _sam3.EncodeImageAsync(image, ct);
                _cachedEmbeddingPath = imagePath;
            }
            embedding = _cachedEmbedding!;
        }
        finally
        {
            _cacheLock.Release();
        }

        var result = await _sam3.SegmentByPointsAsync(embedding, positivePoints, negativePoints, ct);
        return ToSegmentationAnnotation(result);
    }

    /// <inheritdoc/>
    public async Task<List<SegmentationAnnotation>> SegmentByReferenceAsync(
        string imagePath, string referenceImagePath, float threshold = 0.5f, CancellationToken ct = default)
    {
        if (_sam3 == null || !_sam3.IsLoaded)
            throw new InvalidOperationException("SAM 3 模型未加载。");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("图像文件不存在", imagePath);
        if (!File.Exists(referenceImagePath))
            throw new FileNotFoundException("参考图像不存在", referenceImagePath);

        using var image = new Bitmap(imagePath);
        using var refImage = new Bitmap(referenceImagePath);
        var results = await _sam3.SegmentByReferenceAsync(image, refImage, threshold, ct);

        return results.Select(r => ToSegmentationAnnotation(r)).ToList();
    }

    private static BoundingBoxAnnotation SegmentationToBBox(SegmentationResult result, int imageWidth, int imageHeight)
    {
        var bb = result.BoundingBox;
        return new BoundingBoxAnnotation
        {
            ClassIndex = 0,
            ClassName = result.Label,
            CenterX = Math.Round((bb.X + bb.Width / 2) / imageWidth, 6),
            CenterY = Math.Round((bb.Y + bb.Height / 2) / imageHeight, 6),
            Width = Math.Round(bb.Width / imageWidth, 6),
            Height = Math.Round(bb.Height / imageHeight, 6)
        };
    }

    private static SegmentationAnnotation ToSegmentationAnnotation(SegmentationResult result)
    {
        return new SegmentationAnnotation
        {
            ClassIndex = 0,
            ClassName = result.Label,
            Confidence = result.Confidence,
            Polygon = result.ContourPoints,
            Mask = result.Mask
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        UnloadModel();
        _cacheLock.Dispose();
    }
}
