namespace ScaffoldX.App.Services;

/// <summary>
/// 视频帧提取服务接口，用于从视频文件中提取图像帧。
/// </summary>
public interface IVideoFrameService
{
    /// <summary>
    /// 获取视频文件的基本信息。
    /// </summary>
    /// <param name="videoPath">视频文件路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>视频元信息。</returns>
    Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default);

    /// <summary>
    /// 从视频文件中按指定帧率提取图像帧。
    /// </summary>
    /// <param name="videoPath">视频文件路径。</param>
    /// <param name="outputDir">帧图像输出目录。</param>
    /// <param name="fps">提取帧率（每秒提取的帧数，默认 1.0）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>提取的帧图像文件路径列表。</returns>
    Task<IReadOnlyList<string>> ExtractFramesAsync(string videoPath, string outputDir, double fps = 1.0, CancellationToken ct = default);
}

/// <summary>
/// 视频元信息。
/// </summary>
/// <param name="Duration">视频时长（秒）。</param>
/// <param name="Width">视频宽度（像素）。</param>
/// <param name="Height">视频高度（像素）。</param>
/// <param name="Fps">视频帧率。</param>
/// <param name="TotalFrames">总帧数（估算）。</param>
public record VideoInfo(double Duration, int Width, int Height, double Fps, int TotalFrames);
