using System.Drawing;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// 推理引擎基类，提供 ONNX 模型加载和推理的通用实现。
/// </summary>
public abstract class InferenceEngineBase : IDisposable
{
    /// <summary>模型文件路径。</summary>
    protected string ModelPath { get; private set; } = string.Empty;

    /// <summary>模型是否已加载。</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>模型输入宽度。</summary>
    public int InputWidth { get; protected set; } = 640;

    /// <summary>模型输入高度。</summary>
    public int InputHeight { get; protected set; } = 640;

    /// <summary>模型输入名称。</summary>
    public string InputName { get; protected set; } = "images";

    /// <summary>模型输出名称。</summary>
    public string[] OutputNames { get; protected set; } = Array.Empty<string>();

    /// <summary>
    /// 加载 ONNX 模型。
    /// </summary>
    /// <param name="modelPath">模型文件路径。</param>
    public virtual void LoadModel(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("模型文件不存在", modelPath);

        ModelPath = modelPath;
        LoadModelInternal(modelPath);
        IsLoaded = true;
    }

    /// <summary>
    /// 异步加载 ONNX 模型。
    /// </summary>
    /// <param name="modelPath">模型文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public virtual async Task LoadModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(modelPath))
            throw new FileNotFoundException("模型文件不存在", modelPath);

        ModelPath = modelPath;
        await Task.Run(() => LoadModelInternal(modelPath), cancellationToken);
        IsLoaded = true;
    }

    /// <summary>
    /// 执行推理。
    /// </summary>
    /// <param name="image">输入图像。</param>
    /// <returns>推理结果列表。</returns>
    public virtual List<InferenceResult> Run(Bitmap image)
    {
        if (!IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModel 方法");

        var preprocessed = Preprocess(image);
        var outputs = RunInference(preprocessed);
        return Postprocess(outputs, image.Width, image.Height);
    }

    /// <summary>
    /// 异步执行推理。
    /// </summary>
    /// <param name="image">输入图像。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>推理结果列表。</returns>
    public virtual async Task<List<InferenceResult>> RunAsync(Bitmap image, CancellationToken cancellationToken = default)
    {
        if (!IsLoaded)
            throw new InvalidOperationException("模型未加载，请先调用 LoadModel 方法");

        return await Task.Run(() =>
        {
            var preprocessed = Preprocess(image);
            var outputs = RunInference(preprocessed);
            return Postprocess(outputs, image.Width, image.Height);
        }, cancellationToken);
    }

    /// <summary>
    /// 释放资源。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // ── 抽象方法（子类必须实现） ──────────────────────────────────────────────

    /// <summary>
    /// 加载模型的内部实现。
    /// </summary>
    /// <param name="modelPath">模型文件路径。</param>
    protected abstract void LoadModelInternal(string modelPath);

    /// <summary>
    /// 图像预处理。
    /// </summary>
    /// <param name="image">原始图像。</param>
    /// <returns>预处理后的数据（归一化、resize 等）。</returns>
    protected abstract float[] Preprocess(Bitmap image);

    /// <summary>
    /// 执行模型推理。
    /// </summary>
    /// <param name="input">预处理后的输入数据。</param>
    /// <returns>模型输出数据。</returns>
    protected abstract float[][] RunInference(float[] input);

    /// <summary>
    /// 后处理模型输出，转换为推理结果。
    /// </summary>
    /// <param name="outputs">模型输出数据。</param>
    /// <param name="originalWidth">原始图像宽度。</param>
    /// <param name="originalHeight">原始图像高度。</param>
    /// <returns>推理结果列表。</returns>
    protected abstract List<InferenceResult> Postprocess(float[][] outputs, int originalWidth, int originalHeight);

    /// <summary>
    /// 释放资源的内部实现。
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源。</param>
    protected virtual void Dispose(bool disposing)
    {
        // 子类可以重写此方法来释放资源
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

/// <summary>
/// 分割推理结果。
/// </summary>
public class SegmentationResult : InferenceResult
{
    /// <summary>分割掩码（与原图同尺寸）。</summary>
    public byte[,] SegmentationMask { get; set; } = new byte[0, 0];
}
