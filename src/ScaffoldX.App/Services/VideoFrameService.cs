using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// 视频帧提取服务实现，通过调用 ffprobe/ffmpeg 从视频文件中提取图像帧。
/// </summary>
public class VideoFrameService : IVideoFrameService
{
    private readonly ILogger _logger = Log.ForContext<VideoFrameService>();

    /// <summary>
    /// 获取视频文件的基本信息，通过 ffprobe 解析 JSON 输出。
    /// </summary>
    /// <param name="videoPath">视频文件路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>视频元信息。</returns>
    public async Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
            throw new ArgumentException("视频文件路径不能为空。", nameof(videoPath));

        if (!File.Exists(videoPath))
            throw new FileNotFoundException("视频文件不存在", videoPath);

        EnsureFfprobeAvailable();

        var arguments = $"-v quiet -print_format json -show_format -show_streams \"{videoPath}\"";

        var (exitCode, stdout, stderr) = await RunProcessAsync("ffprobe", arguments, ct);

        if (exitCode != 0)
            throw new InvalidOperationException($"ffprobe 执行失败 (exit code {exitCode}): {stderr}");

        return ParseVideoInfo(stdout);
    }

    /// <summary>
    /// 从视频文件中按指定帧率提取图像帧，通过调用 ffmpeg 实现。
    /// </summary>
    /// <param name="videoPath">视频文件路径。</param>
    /// <param name="outputDir">帧图像输出目录。</param>
    /// <param name="fps">提取帧率（每秒提取的帧数，默认 1.0）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>提取的帧图像文件路径列表。</returns>
    public async Task<IReadOnlyList<string>> ExtractFramesAsync(
        string videoPath, string outputDir, double fps = 1.0, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(videoPath))
            throw new ArgumentException("视频文件路径不能为空。", nameof(videoPath));

        if (!File.Exists(videoPath))
            throw new FileNotFoundException("视频文件不存在", videoPath);

        if (fps <= 0)
            throw new ArgumentOutOfRangeException(nameof(fps), "帧率必须大于 0");

        EnsureFfmpegAvailable();

        Directory.CreateDirectory(outputDir);

        var outputPattern = Path.Combine(outputDir, "frame_%06d.jpg");
        var arguments = $"-i \"{videoPath}\" -vf fps={fps:F4} -qscale:v 2 -y \"{outputPattern}\"";

        _logger.Information("开始从视频提取帧: {Path}, fps={Fps}, output={Output}", videoPath, fps, outputDir);

        var (exitCode, _, stderr) = await RunProcessAsync("ffmpeg", arguments, ct);

        if (exitCode != 0)
            throw new InvalidOperationException($"ffmpeg 帧提取失败 (exit code {exitCode}): {stderr}");

        var frameFiles = Directory.EnumerateFiles(outputDir, "frame_*.jpg")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        _logger.Information("帧提取完成: {Count} 帧已保存到 {Output}", frameFiles.Count, outputDir);

        return frameFiles;
    }

    /// <summary>
    /// 解析 ffprobe 的 JSON 输出，提取视频元信息。
    /// </summary>
    private static VideoInfo ParseVideoInfo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var duration = 0.0;
        if (root.TryGetProperty("format", out var format) &&
            format.TryGetProperty("duration", out var durationElement) &&
            durationElement.ValueKind == JsonValueKind.String &&
            double.TryParse(durationElement.GetString(), CultureInfo.InvariantCulture, out var d))
        {
            duration = d;
        }

        var width = 0;
        var height = 0;
        var fps = 0.0;
        var totalFrames = 0;

        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var stream in streams.EnumerateArray())
            {
                var codecType = stream.TryGetProperty("codec_type", out var ct) ? ct.GetString() : null;
                if (codecType != "video") continue;

                if (stream.TryGetProperty("width", out var w))
                    width = w.GetInt32();
                if (stream.TryGetProperty("height", out var h))
                    height = h.GetInt32();

                // 解析帧率，格式如 "30/1" 或 "30000/1001"
                if (stream.TryGetProperty("r_frame_rate", out var rFps) &&
                    rFps.ValueKind == JsonValueKind.String)
                {
                    fps = ParseFraction(rFps.GetString()!);
                }

                // 尝试读取 nb_frames
                if (stream.TryGetProperty("nb_frames", out var nbFrames) &&
                    nbFrames.ValueKind == JsonValueKind.String &&
                    int.TryParse(nbFrames.GetString(), out var nf))
                {
                    totalFrames = nf;
                }

                break;
            }
        }

        // 如果 nb_frames 不可用，通过 duration * fps 估算
        if (totalFrames == 0 && duration > 0 && fps > 0)
        {
            totalFrames = (int)(duration * fps);
        }

        return new VideoInfo(duration, width, height, fps, totalFrames);
    }

    /// <summary>
    /// 解析分数格式的字符串（如 "30/1"）为浮点数值。
    /// </summary>
    private static double ParseFraction(string value)
    {
        var parts = value.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], CultureInfo.InvariantCulture, out var numerator) &&
            double.TryParse(parts[1], CultureInfo.InvariantCulture, out var denominator) &&
            denominator != 0)
        {
            return numerator / denominator;
        }

        if (double.TryParse(value, CultureInfo.InvariantCulture, out var direct))
            return direct;

        return 0;
    }

    /// <summary>
    /// 验证 ffmpeg 是否可用，不可用时抛出明确异常。
    /// </summary>
    private static void EnsureFfmpegAvailable()
    {
        if (!IsCommandAvailable("ffmpeg"))
        {
            throw new InvalidOperationException(
                "未检测到 ffmpeg，请先安装 ffmpeg 并将其添加到系统 PATH 环境变量。" +
                "下载地址: https://ffmpeg.org/download.html");
        }
    }

    /// <summary>
    /// 验证 ffprobe 是否可用，不可用时抛出明确异常。
    /// </summary>
    private static void EnsureFfprobeAvailable()
    {
        if (!IsCommandAvailable("ffprobe"))
        {
            throw new InvalidOperationException(
                "未检测到 ffprobe，请先安装 ffmpeg（包含 ffprobe）并将其添加到系统 PATH 环境变量。" +
                "下载地址: https://ffmpeg.org/download.html");
        }
    }

    /// <summary>
    /// 检查指定命令是否在系统 PATH 中可用。
    /// </summary>
    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var (exitCode, _, _) = RunProcessSync(command, "-version", TimeSpan.FromSeconds(5));
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 异步运行外部进程，返回退出码、标准输出和标准错误。
    /// </summary>
    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
        string fileName, string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    /// <summary>
    /// 同步运行外部进程（用于可用性检查）。
    /// </summary>
    private static (int ExitCode, string Stdout, string Stderr) RunProcessSync(
        string fileName, string arguments, TimeSpan timeout)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            process.Kill();
            throw new TimeoutException($"进程 {fileName} 执行超时");
        }

        return (process.ExitCode, stdout, stderr);
    }
}
