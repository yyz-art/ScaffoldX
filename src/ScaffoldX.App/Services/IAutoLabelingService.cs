using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// 自动标注服务契约，提供基于 ONNX 模型的目标检测和自动标注功能。
/// </summary>
public interface IAutoLabelingService
{
    /// <summary>模型是否已加载。</summary>
    bool IsModelLoaded { get; }

    /// <summary>已加载模型的文件路径。</summary>
    string? LoadedModelPath { get; }

    /// <summary>已加载模型的类别名称列表。</summary>
    IReadOnlyList<string> ClassNames { get; }

    /// <summary>
    /// 加载 ONNX 检测模型。
    /// </summary>
    /// <param name="modelPath">ONNX 模型文件路径。</param>
    /// <param name="classesFilePath">类别名称文件路径（classes.txt），可选。</param>
    /// <param name="classNames">直接指定类别名称列表，可选（优先于 classesFilePath）。</param>
    Task LoadModelAsync(string modelPath, string? classesFilePath = null, IEnumerable<string>? classNames = null);

    /// <summary>
    /// 卸载当前模型，释放资源。
    /// </summary>
    void UnloadModel();

    /// <summary>
    /// 对单张图像执行目标检测，返回归一化坐标的边界框列表。
    /// </summary>
    /// <param name="imagePath">图像文件路径。</param>
    /// <param name="confidenceThreshold">置信度阈值（0-1）。</param>
    /// <returns>检测到的边界框列表（归一化 YOLO 格式）。</returns>
    Task<List<BoundingBoxAnnotation>> DetectAsync(string imagePath, float confidenceThreshold = 0.5f);

    /// <summary>
    /// 对多张图像执行批量检测。
    /// </summary>
    /// <param name="imagePaths">图像文件路径列表。</param>
    /// <param name="confidenceThreshold">置信度阈值（0-1）。</param>
    /// <param name="progress">进度回调（当前索引, 总数）。</param>
    /// <returns>每张图像的检测结果，键为图像路径。</returns>
    Task<Dictionary<string, List<BoundingBoxAnnotation>>> DetectBatchAsync(
        IEnumerable<string> imagePaths,
        float confidenceThreshold = 0.5f,
        IProgress<(int current, int total)>? progress = null);
}
