namespace ScaffoldX.App.Models;

/// <summary>
/// 项目生成操作的最终结果，包含成功/失败状态、输出路径、文件数量及耗时。
/// </summary>
public class GenerationResult
{
    /// <summary>生成是否成功。</summary>
    public bool Success { get; set; }

    /// <summary>生成输出目录的绝对路径；失败时为空字符串。</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>本次生成写入的文件总数；失败时为 0。</summary>
    public int FileCount { get; set; }

    /// <summary>失败时的错误描述；成功时为空字符串。</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>从开始到完成（或失败）所经过的时间。</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>
    /// 构造一个表示成功的 <see cref="GenerationResult"/>。
    /// </summary>
    /// <param name="outputPath">生成输出目录路径。</param>
    /// <param name="fileCount">写入的文件数量。</param>
    /// <param name="elapsed">生成耗时。</param>
    /// <returns>成功结果实例。</returns>
    public static GenerationResult Ok(string outputPath, int fileCount, TimeSpan elapsed)
    {
        return new GenerationResult
        {
            Success = true,
            OutputPath = outputPath,
            FileCount = fileCount,
            Elapsed = elapsed,
            ErrorMessage = string.Empty
        };
    }

    /// <summary>
    /// 构造一个表示失败的 <see cref="GenerationResult"/>。
    /// </summary>
    /// <param name="error">错误描述信息。</param>
    /// <returns>失败结果实例。</returns>
    public static GenerationResult Fail(string error)
    {
        return new GenerationResult
        {
            Success = false,
            ErrorMessage = error,
            OutputPath = string.Empty,
            FileCount = 0,
            Elapsed = TimeSpan.Zero
        };
    }
}
