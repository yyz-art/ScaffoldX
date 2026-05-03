using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// YOLO 训练服务接口，提供模型训练、验证和导出功能。
/// </summary>
public interface IYoloTrainingService
{
    /// <summary>
    /// 检查训练环境是否就绪（Python、Ultralytics 等）。
    /// </summary>
    /// <returns>环境检查结果。</returns>
    Task<EnvironmentCheckResult> CheckEnvironmentAsync();

    /// <summary>
    /// 安装训练依赖（ultralytics 包）。
    /// </summary>
    /// <param name="progress">进度回调。</param>
    /// <returns>安装是否成功。</returns>
    Task<bool> InstallDependenciesAsync(IProgress<string> progress);

    /// <summary>
    /// 开始训练 YOLO 模型。
    /// </summary>
    /// <param name="config">训练配置。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>训练结果。</returns>
    Task<TrainingResult> TrainAsync(
        YoloTrainingConfig config,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 从指定的检查点恢复训练。
    /// </summary>
    /// <param name="config">训练配置。</param>
    /// <param name="resumeFromPath">恢复训练的 .pt 检查点路径。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>训练结果。</returns>
    Task<TrainingResult> ResumeTrainAsync(
        YoloTrainingConfig config,
        string resumeFromPath,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证模型性能。
    /// </summary>
    /// <param name="modelPath">模型路径。</param>
    /// <param name="datasetPath">数据集路径。</param>
    /// <returns>验证结果。</returns>
    Task<ModelValidationResult> ValidateAsync(string modelPath, string datasetPath);

    /// <summary>
    /// 导出模型为 ONNX 格式。
    /// </summary>
    /// <param name="modelPath">PyTorch 模型路径。</param>
    /// <param name="outputPath">ONNX 输出路径。</param>
    /// <param name="imageSize">输入图像尺寸。</param>
    /// <returns>导出是否成功。</returns>
    Task<bool> ExportToOnnxAsync(string modelPath, string outputPath, int imageSize = 640);

    /// <summary>
    /// 获取可用的预训练模型列表。
    /// </summary>
    /// <returns>预训练模型列表。</returns>
    IReadOnlyList<PretrainedModel> GetAvailableModels();

    /// <summary>
    /// 下载预训练模型。
    /// </summary>
    /// <param name="modelName">模型名称。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>下载是否成功。</returns>
    Task<bool> DownloadModelAsync(string modelName, IProgress<string> progress);
}

/// <summary>
/// 环境检查结果。
/// </summary>
public class EnvironmentCheckResult
{
    /// <summary>Python 是否安装。</summary>
    public bool PythonInstalled { get; set; }

    /// <summary>Python 版本。</summary>
    public string PythonVersion { get; set; } = string.Empty;

    /// <summary>Ultralytics 是否安装。</summary>
    public bool UltralyticsInstalled { get; set; }

    /// <summary>Ultralytics 版本。</summary>
    public string UltralyticsVersion { get; set; } = string.Empty;

    /// <summary>PyTorch 是否安装。</summary>
    public bool PyTorchInstalled { get; set; }

    /// <summary>CUDA 是否可用。</summary>
    public bool CudaAvailable { get; set; }

    /// <summary>CUDA 版本。</summary>
    public string CudaVersion { get; set; } = string.Empty;

    /// <summary>是否所有依赖都就绪。</summary>
    public bool IsReady => PythonInstalled && UltralyticsInstalled && PyTorchInstalled;

    /// <summary>错误信息。</summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// 模型验证结果。
/// </summary>
public class ModelValidationResult
{
    /// <summary>mAP@0.5。</summary>
    public double Map50 { get; set; }

    /// <summary>mAP@0.5:0.95。</summary>
    public double Map50_95 { get; set; }

    /// <summary>精确率。</summary>
    public double Precision { get; set; }

    /// <summary>召回率。</summary>
    public double Recall { get; set; }

    /// <summary>推理速度（毫秒/张）。</summary>
    public double InferenceSpeed { get; set; }
}

/// <summary>
/// 预训练模型信息。
/// </summary>
public class PretrainedModel
{
    /// <summary>模型名称（如 yolov8n.pt）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>模型描述。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>模型大小（MB）。</summary>
    public double SizeMB { get; set; }

    /// <summary>mAP@0.5:0.95（COCO 数据集）。</summary>
    public double Map50_95 { get; set; }

    /// <summary>推理速度（毫秒/张，GPU）。</summary>
    public double GpuSpeed { get; set; }

    /// <summary>是否已下载。</summary>
    public bool IsDownloaded { get; set; }
}
