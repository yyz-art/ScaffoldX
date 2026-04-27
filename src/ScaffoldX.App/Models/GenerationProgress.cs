namespace ScaffoldX.App.Models;

/// <summary>
/// 项目生成过程中的进度快照，通过 <see cref="IProgress{T}"/> 回调传递给 UI 层。
/// </summary>
public class GenerationProgress
{
    /// <summary>当前步骤的人类可读描述。</summary>
    public string Message { get; }

    /// <summary>完成百分比，范围 0–100。</summary>
    public int Percent { get; }

    /// <summary>
    /// 初始化 <see cref="GenerationProgress"/> 实例。
    /// </summary>
    /// <param name="message">步骤描述文本。</param>
    /// <param name="percent">完成百分比（0–100）。</param>
    public GenerationProgress(string message, int percent)
    {
        Message = message;
        Percent = percent;
    }
}
