using System.Drawing;
using System.IO;
using ScaffoldX.App.Models;
using ScaffoldX.Core.Vision;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// <see cref="IAutoLabelingService"/> 的实现，基于 <see cref="OnnxDetector"/> 提供自动标注能力。
/// </summary>
public class AutoLabelingService : IAutoLabelingService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<AutoLabelingService>();
    private OnnxDetector? _detector;

    /// <inheritdoc/>
    public bool IsModelLoaded => _detector?.IsLoaded == true;

    /// <inheritdoc/>
    public string? LoadedModelPath { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<string> ClassNames => _detector?.ClassNames ?? Array.Empty<string>();

    /// <inheritdoc/>
    public async Task LoadModelAsync(string modelPath, string? classesFilePath = null, IEnumerable<string>? classNames = null)
    {
        UnloadModel();

        _detector = new OnnxDetector();

        if (classNames != null)
        {
            _detector.SetClassNames(classNames);
        }
        else if (!string.IsNullOrEmpty(classesFilePath) && File.Exists(classesFilePath))
        {
            _detector.LoadClassNames(classesFilePath!);
        }

        await _detector.LoadModelAsync(modelPath);
        LoadedModelPath = modelPath;

        _logger.Information("自动标注模型已加载: {ModelPath}, 类别数: {ClassCount}",
            modelPath, _detector.ClassNames.Count);
    }

    /// <inheritdoc/>
    public void UnloadModel()
    {
        _detector?.Dispose();
        _detector = null;
        LoadedModelPath = null;
    }

    /// <inheritdoc/>
    public async Task<List<BoundingBoxAnnotation>> DetectAsync(string imagePath, float confidenceThreshold = 0.5f)
    {
        if (_detector == null || !_detector.IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModelAsync。");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("图像文件不存在", imagePath);

        _detector.ConfidenceThreshold = confidenceThreshold;

        using var image = new Bitmap(imagePath);
        var results = await _detector.RunAsync(image);

        return results.Select(r => ToBoundingBoxAnnotation(r, image.Width, image.Height)).ToList();
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, List<BoundingBoxAnnotation>>> DetectBatchAsync(
        IEnumerable<string> imagePaths,
        float confidenceThreshold = 0.5f,
        IProgress<(int current, int total)>? progress = null)
    {
        if (_detector == null || !_detector.IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModelAsync。");

        _detector.ConfidenceThreshold = confidenceThreshold;

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

                using var image = new Bitmap(path);
                var detections = await _detector.RunAsync(image);
                results[path] = detections
                    .Select(r => ToBoundingBoxAnnotation(r, image.Width, image.Height))
                    .ToList();
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

    /// <summary>
    /// 将推理结果转换为归一化坐标的边界框标注。
    /// </summary>
    private static BoundingBoxAnnotation ToBoundingBoxAnnotation(InferenceResult result, int imageWidth, int imageHeight)
    {
        var bb = result.BoundingBox;

        // 像素坐标 → 归一化坐标（YOLO 格式：center_x, center_y, width, height）
        var centerX = (bb.X + bb.Width / 2) / imageWidth;
        var centerY = (bb.Y + bb.Height / 2) / imageHeight;
        var width = bb.Width / imageWidth;
        var height = bb.Height / imageHeight;

        return new BoundingBoxAnnotation
        {
            ClassIndex = result.ClassIndex,
            ClassName = result.ClassName,
            CenterX = Math.Round(centerX, 6),
            CenterY = Math.Round(centerY, 6),
            Width = Math.Round(width, 6),
            Height = Math.Round(height, 6)
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        UnloadModel();
    }
}
