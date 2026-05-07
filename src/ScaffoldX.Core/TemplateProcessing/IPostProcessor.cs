using ScaffoldX.Core.Models;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 对 Scriban 渲染后的文件内容执行后处理。
/// </summary>
public interface IPostProcessor
{
    /// <summary>
    /// 对渲染后的文件内容执行全量后处理流水线。
    /// </summary>
    string Process(string content, string outputPath, ProjectConfig config);
}
