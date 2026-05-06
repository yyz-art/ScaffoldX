using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TorchSharp;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// TorchSharp 推理引擎基类，提供基于 libtorch 的模型加载和推理通用实现。
/// </summary>
public abstract class InferenceEngineBase : IDisposable
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    /// <summary>模型文件路径。</summary>
    protected string ModelPath { get; private set; } = string.Empty;

    /// <summary>模型是否已加载。</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>模型输入宽度。</summary>
    public int InputWidth { get; protected set; } = 640;

    /// <summary>模型输入高度。</summary>
    public int InputHeight { get; protected set; } = 640;

    /// <summary>
    /// 加载模型。
    /// </summary>
    /// <param name="modelPath">模型文件路径。</param>
    public virtual void LoadModel(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("模型文件不存在", modelPath);

        _loadLock.Wait();
        try
        {
            ModelPath = modelPath;
            LoadModelInternal(modelPath);
            IsLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// 异步加载模型。使用信号量防止并发加载。
    /// </summary>
    /// <param name="modelPath">模型文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public virtual async Task LoadModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("模型文件不存在", modelPath);

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            ModelPath = modelPath;
            await Task.Run(() => LoadModelInternal(modelPath), cancellationToken);
            IsLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// 执行推理。
    /// </summary>
    /// <param name="image">输入图像。</param>
    /// <returns>推理结果列表。</returns>
    public virtual List<InferenceResult> Run(Bitmap image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (!IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModel 方法");

        using var tensor = PreprocessToTensor(image);
        using var outputs = RunModelInference(tensor);
        return PostprocessResults(outputs, image.Width, image.Height);
    }

    /// <summary>
    /// 异步执行推理。
    /// </summary>
    /// <param name="image">输入图像。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>推理结果列表。</returns>
    public virtual async Task<List<InferenceResult>> RunAsync(Bitmap image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (!IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModel 方法");

        return await Task.Run(() =>
        {
            using var tensor = PreprocessToTensor(image);
            using var outputs = RunModelInference(tensor);
            return PostprocessResults(outputs, image.Width, image.Height);
        }, cancellationToken);
    }

    /// <summary>
    /// 将图像缩放到指定尺寸，使用高质量双三次插值。
    /// </summary>
    protected static Bitmap ResizeImage(Bitmap image, int width, int height)
    {
        var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        using (var graphics = Graphics.FromImage(result))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, new Rectangle(0, 0, width, height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        return result;
    }

    /// <summary>
    /// 将 Bitmap 转换为 CHW 格式的 TorchSharp Tensor（归一化到 [0, 1]）。
    /// </summary>
    protected static torch.Tensor BitmapToTensor(Bitmap image, int targetWidth, int targetHeight)
    {
        using var resized = ResizeImage(image, targetWidth, targetHeight);
        var data = resized.LockBits(
            new Rectangle(0, 0, resized.Width, resized.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            var buffer = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            var floatData = new float[3 * targetHeight * targetWidth];
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    var pixelOffset = y * data.Stride + x * 3;
                    var ch0Offset = y * targetWidth + x;
                    var ch1Offset = targetHeight * targetWidth + ch0Offset;
                    var ch2Offset = 2 * targetHeight * targetWidth + ch0Offset;

                    floatData[ch0Offset] = buffer[pixelOffset + 2] / 255f; // R
                    floatData[ch1Offset] = buffer[pixelOffset + 1] / 255f; // G
                    floatData[ch2Offset] = buffer[pixelOffset] / 255f;     // B
                }
            }

            return torch.tensor(floatData, dtype: torch.ScalarType.Float32)
                .reshape(new long[] { 1, 3, targetHeight, targetWidth });
        }
        finally
        {
            resized.UnlockBits(data);
        }
    }

    /// <summary>
    /// 根据中心坐标和尺寸创建检测结果，执行坐标缩放和边界裁剪。
    /// </summary>
    protected static InferenceResult CreateDetectionResult(
        float cx, float cy, float w, float h,
        int maxClassIndex, float maxScore,
        int originalWidth, int originalHeight,
        float scaleX, float scaleY,
        IReadOnlyList<string> classNames)
    {
        var x = (cx - w / 2) * scaleX;
        var y = (cy - h / 2) * scaleY;
        var width = w * scaleX;
        var height = h * scaleY;

        x = Math.Max(0, x);
        y = Math.Max(0, y);
        width = Math.Max(0, Math.Min(width, originalWidth - x));
        height = Math.Max(0, Math.Min(height, originalHeight - y));

        var className = maxClassIndex < classNames.Count
            ? classNames[maxClassIndex]
            : $"class_{maxClassIndex}";

        return new InferenceResult
        {
            ClassIndex = maxClassIndex,
            ClassName = className,
            Confidence = maxScore,
            BoundingBox = new RectangleF(x, y, width, height)
        };
    }

    // ── 抽象方法（子类必须实现） ──────────────────────────────────────────────

    /// <summary>
    /// 加载模型的内部实现。
    /// </summary>
    protected abstract void LoadModelInternal(string modelPath);

    /// <summary>
    /// 图像预处理，返回 TorchSharp Tensor。
    /// </summary>
    protected abstract torch.Tensor PreprocessToTensor(Bitmap image);

    /// <summary>
    /// 执行模型推理。
    /// </summary>
    protected abstract torch.Tensor RunModelInference(torch.Tensor input);

    /// <summary>
    /// 后处理模型输出，转换为推理结果。
    /// </summary>
    protected abstract List<InferenceResult> PostprocessResults(torch.Tensor outputs, int originalWidth, int originalHeight);

    /// <summary>
    /// 释放资源。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的内部实现。
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadLock.Dispose();
        }
    }
}

/// <summary>
/// 推理结果。
/// </summary>
public class InferenceResult
{
    /// <summary>类别索引。</summary>
    public int ClassIndex { get; set; }

    /// <summary>类别名称。</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>置信度（0-1）。</summary>
    public float Confidence { get; set; }

    /// <summary>边界框（像素坐标）。</summary>
    public RectangleF BoundingBox { get; set; }

    /// <summary>掩码数据（分割模型使用）。</summary>
    public float[]? Mask { get; set; }
}

/// <summary>
/// 目标检测推理结果。
/// </summary>
public class DetectionResult : InferenceResult
{
    // 当前与 InferenceResult 相同，预留扩展
}

/// <summary>
/// 分类推理结果。
/// </summary>
public class ClassificationResult : InferenceResult
{
    /// <summary>类别概率分布。</summary>
    public float[] Probabilities { get; set; } = Array.Empty<float>();
}

// SegmentationResult 已移至 Sam3Segmentor.cs（与 ISam3SegmentationEngine 配套）
