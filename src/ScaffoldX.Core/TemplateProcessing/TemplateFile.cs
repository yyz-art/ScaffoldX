namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 表示一个模板文件，包含模板内容和输出路径模板。
/// </summary>
public class TemplateFile
{
    /// <summary>模板的逻辑名称，通常对应嵌入资源的文件名（不含扩展名）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Scriban 模板原始内容。</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 输出路径模板，支持 Scriban 变量，如
    /// <c>src/{{project_name}}.Core/IPlugin.cs</c>。
    /// </summary>
    public string OutputPathTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 模板所属分类：<c>Common</c>、<c>Collection</c>、<c>Vision</c>、<c>System</c>。
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>是否为必须生成的文件；为 <c>false</c> 时由配置条件决定是否渲染。</summary>
    public bool IsRequired { get; set; } = true;
}
