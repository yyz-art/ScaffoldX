using System.IO;
using System.Net.Http;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// 模型仓库服务实现，管理 YOLO 模型的注册、下载和本地缓存。
/// </summary>
public class ModelZooService : IModelZooService
{
    private readonly ILogger _logger = Log.ForContext<ModelZooService>();
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly string _baseUrl;
    private readonly IReadOnlyList<ModelInfo> _builtInModels;

    /// <summary>
    /// 初始化模型仓库服务，构建内置模型列表并确保缓存目录存在。
    /// </summary>
    /// <param name="httpClient">用于下载模型的 HttpClient 实例。</param>
    /// <param name="baseUrl">模型下载基地址，默认为 GitHub ultralytics releases。</param>
    public ModelZooService(HttpClient? httpClient = null, string? baseUrl = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _baseUrl = baseUrl ?? "https://github.com/ultralytics/assets/releases/download/v8.3.0";
        _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");

        if (!Directory.Exists(_cacheDirectory))
            Directory.CreateDirectory(_cacheDirectory);

        _builtInModels = new List<ModelInfo>
        {
            // Detection models
            new("yolov8n", "YOLOv8 Nano", "超轻量检测模型，适合边缘部署", 6_244_608, $"{_baseUrl}/yolov8n.pt", "Detection"),
            new("yolov8s", "YOLOv8 Small", "小型检测模型，速度与精度平衡", 22_585_600, $"{_baseUrl}/yolov8s.pt", "Detection"),
            new("yolov8m", "YOLOv8 Medium", "中型检测模型，较高精度", 52_124_800, $"{_baseUrl}/yolov8m.pt", "Detection"),
            new("yolov8l", "YOLOv8 Large", "大型检测模型，高精度", 87_700_480, $"{_baseUrl}/yolov8l.pt", "Detection"),
            new("yolov8x", "YOLOv8 Extra Large", "最大检测模型，最高精度", 131_660_800, $"{_baseUrl}/yolov8x.pt", "Detection"),

            // Segmentation models
            new("yolov8n-seg", "YOLOv8 Nano Seg", "超轻量分割模型", 6_749_696, $"{_baseUrl}/yolov8n-seg.pt", "Segmentation"),
            new("yolov8s-seg", "YOLOv8 Small Seg", "小型分割模型", 23_592_960, $"{_baseUrl}/yolov8s-seg.pt", "Segmentation"),
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelInfo> GetAvailableModels() => _builtInModels;

    /// <inheritdoc />
    public async Task<string> DownloadModelAsync(string modelId, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var model = _builtInModels.FirstOrDefault(m => m.Id == modelId)
            ?? throw new ArgumentException($"未知的模型 ID: {modelId}", nameof(modelId));

        var localPath = GetLocalPath(modelId);

        if (File.Exists(localPath))
        {
            _logger.Information("模型已存在，跳过下载: {ModelId} -> {Path}", modelId, localPath);
            progress?.Report(1.0);
            return localPath;
        }

        _logger.Information("开始下载模型: {ModelId} ({Size} bytes) from {Url}", modelId, model.SizeBytes, model.DownloadUrl);

        using var response = await _httpClient.GetAsync(model.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? model.SizeBytes;
        var tempPath = localPath + ".tmp";

        try
        {
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;
                progress?.Report((double)totalRead / totalBytes);
            }

            await fileStream.FlushAsync(ct);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }

        File.Move(tempPath, localPath, overwrite: true);
        _logger.Information("模型下载完成: {ModelId} -> {Path}", modelId, localPath);

        return localPath;
    }

    /// <inheritdoc />
    public string? GetModelPath(string modelId)
    {
        var localPath = GetLocalPath(modelId);
        return File.Exists(localPath) ? localPath : null;
    }

    /// <inheritdoc />
    public bool IsModelDownloaded(string modelId) => File.Exists(GetLocalPath(modelId));

    /// <summary>
    /// 获取模型的本地缓存文件路径。
    /// </summary>
    /// <param name="modelId">模型标识符。</param>
    /// <returns>本地文件的绝对路径。</returns>
    private string GetLocalPath(string modelId) => Path.Combine(_cacheDirectory, $"{modelId}.pt");
}
