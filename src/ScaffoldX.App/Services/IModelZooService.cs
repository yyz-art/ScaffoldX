namespace ScaffoldX.App.Services;

/// <summary>
/// 模型仓库服务契约，提供可用模型查询、下载和本地缓存管理能力。
/// </summary>
public interface IModelZooService
{
    /// <summary>
    /// 获取所有可用模型的信息列表。
    /// </summary>
    /// <returns>模型信息的只读列表。</returns>
    IReadOnlyList<ModelInfo> GetAvailableModels();

    /// <summary>
    /// 异步下载指定模型到本地缓存目录，支持进度回调和取消。
    /// </summary>
    /// <param name="modelId">模型标识符（如 "yolov8n"）。</param>
    /// <param name="progress">下载进度回调（0.0–1.0）；可为 null。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>下载完成后本地模型文件的绝对路径。</returns>
    Task<string> DownloadModelAsync(string modelId, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// 获取指定模型的本地缓存路径；若未下载则返回 null。
    /// </summary>
    /// <param name="modelId">模型标识符。</param>
    /// <returns>本地文件路径或 null。</returns>
    string? GetModelPath(string modelId);

    /// <summary>
    /// 判断指定模型是否已下载到本地缓存。
    /// </summary>
    /// <param name="modelId">模型标识符。</param>
    /// <returns>已下载返回 true；否则返回 false。</returns>
    bool IsModelDownloaded(string modelId);
}

/// <summary>
/// 模型元数据记录，描述模型的基本信息和下载来源。
/// </summary>
/// <param name="Id">模型唯一标识符（如 "yolov8n"）。</param>
/// <param name="Name">显示名称（如 "YOLOv8 Nano"）。</param>
/// <param name="Description">模型简要描述。</param>
/// <param name="SizeBytes">模型文件大小（字节）。</param>
/// <param name="DownloadUrl">模型下载地址。</param>
/// <param name="Category">模型类别（如 "Detection"、"Segmentation"）。</param>
/// <param name="Backend">推理后端（如 "TorchSharp"、"ONNX"）。</param>
public record ModelInfo(string Id, string Name, string Description, long SizeBytes, string DownloadUrl, string Category, string Backend = "TorchSharp");
