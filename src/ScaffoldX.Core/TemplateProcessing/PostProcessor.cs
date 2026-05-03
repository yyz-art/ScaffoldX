using ScaffoldX.Core.Models;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 对 Scriban 渲染后的文件内容执行后处理，包括行尾规范化、XML 实体还原和尾部空白清理。
/// </summary>
public static class PostProcessor
{
    /// <summary>
    /// 对渲染后的文件内容执行全量后处理流水线：
    /// 行尾规范化 → XML/XAML 实体还原 → 尾部空白清理 → 确保末尾换行。
    /// </summary>
    /// <param name="content">Scriban 渲染后的原始文本内容。</param>
    /// <param name="outputPath">目标文件路径，用于判断文件类型以决定处理策略。</param>
    /// <param name="config">项目配置，供扩展处理逻辑使用。</param>
    /// <returns>后处理完成的文本内容。</returns>
    public static string Process(string content, string outputPath, ProjectConfig config)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        content = NormalizeLineEndings(content);

        if (IsXmlLikeFile(outputPath))
        {
            content = RestoreXmlEntities(content);
        }

        content = TrimTrailingWhitespace(content);
        content = EnsureTrailingNewline(content);

        return content;
    }

    /// <summary>
    /// 将所有 \r\n 和孤立 \r 统一转换为 \n（LF）。
    /// </summary>
    /// <param name="content">待处理的文本内容。</param>
    /// <returns>行尾统一为 LF 的文本。</returns>
    public static string NormalizeLineEndings(string content)
    {
        return content
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");
    }

    /// <summary>
    /// 将模板中为避免解析冲突而写成 HTML 实体的字符还原为原始字符。
    /// 适用于 XAML / XML 文件中需要输出字面量 &amp;、&lt;、&gt;、"、&apos; 的场景。
    /// </summary>
    /// <param name="content">包含 HTML 实体的文本内容。</param>
    /// <returns>实体还原后的文本内容。</returns>
    public static string RestoreXmlEntities(string content)
    {
        // 注意：& 必须最后处理，避免二次替换
        return content
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'")
            .Replace("&amp;", "&");
    }

    /// <summary>
    /// 移除每行末尾的多余空白字符（空格和制表符）。
    /// </summary>
    /// <param name="content">待处理的文本内容（行尾应已统一为 \n）。</param>
    /// <returns>每行末尾空白已清除的文本。</returns>
    public static string TrimTrailingWhitespace(string content)
    {
        string[] lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// 确保文件以单个换行符结尾（POSIX 规范）。
    /// </summary>
    /// <param name="content">待处理的文本内容。</param>
    /// <returns>以 \n 结尾的文本内容。</returns>
    public static string EnsureTrailingNewline(string content)
    {
        if (!content.EndsWith('\n'))
        {
            return content + '\n';
        }

        return content;
    }

    /// <summary>
    /// 根据文件扩展名判断是否为 XML 类文件（.xaml、.axaml、.xml、.csproj、.props、.targets）。
    /// </summary>
    /// <param name="outputPath">目标文件路径。</param>
    /// <returns>若为 XML 类文件则返回 true。</returns>
    private static bool IsXmlLikeFile(string outputPath)
    {
        string ext = Path.GetExtension(outputPath).ToLowerInvariant();
        return ext is ".xaml" or ".axaml" or ".xml" or ".csproj" or ".props" or ".targets";
    }
}
