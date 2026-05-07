using System.Drawing;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// 自动标注服务契约，提供基于检测模型和 SAM 3 分割模型的自动标注功能。
/// </summary>
public interface IAutoLabelingService
{
    /// <summary>模型是否已加载。</summary>
    bool IsModelLoaded { get; }

    /// <summary>已加载模型的文件路径。</summary>
    string? LoadedModelPath { get; }

    /// <summary>已加载模型的类别名称列表。</summary>
    IReadOnlyList<string> ClassNames { get; }

    /// <summary>当前自动标注模式。</summary>
    AutoLabelingMode CurrentMode { get; }

    /// <summary>
    /// 加载检测模型。modelPath 为模型目录（包含 encoder.pt 等）或单个 .pt 文件路径。
    /// </summary>
    Task LoadModelAsync(string modelPath, string? classesFilePath = null, IEnumerable<string>? classNames = null);

    /// <summary>
    /// 加载 SAM 3 分割模型。
    /// </summary>
    /// <param name="modelDirectory">SAM 3 模型目录（需包含 encoder.pt, text_encoder.pt, decoder.pt）。</param>
    Task LoadSam3ModelAsync(string modelDirectory, CancellationToken ct = default);

    /// <summary>
    /// 卸载当前模型，释放资源。
    /// </summary>
    void UnloadModel();

    /// <summary>
    /// 对单张图像执行目标检测，返回归一化坐标的边界框列表。
    /// </summary>
    Task<List<BoundingBoxAnnotation>> DetectAsync(string imagePath, float confidenceThreshold = 0.5f);

    /// <summary>
    /// 对多张图像执行批量检测。
    /// </summary>
    Task<Dictionary<string, List<BoundingBoxAnnotation>>> DetectBatchAsync(
        IEnumerable<string> imagePaths,
        float confidenceThreshold = 0.5f,
        IProgress<(int current, int total)>? progress = null);

    /// <summary>
    /// 使用 SAM 3 文本提示进行分割标注。
    /// </summary>
    Task<List<SegmentationAnnotation>> SegmentByTextAsync(
        string imagePath, IEnumerable<string> textPrompts, float threshold = 0.5f, CancellationToken ct = default);

    /// <summary>
    /// 使用 SAM 3 点提示进行分割标注。
    /// </summary>
    Task<SegmentationAnnotation> SegmentByPointsAsync(
        string imagePath, IEnumerable<PointF> positivePoints, IEnumerable<PointF> negativePoints, CancellationToken ct = default);

    /// <summary>
    /// 使用 SAM 3 参考图进行分割标注。
    /// </summary>
    Task<List<SegmentationAnnotation>> SegmentByReferenceAsync(
        string imagePath, string referenceImagePath, float threshold = 0.5f, CancellationToken ct = default);
}
