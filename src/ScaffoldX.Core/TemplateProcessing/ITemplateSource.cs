namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 模板文件加载源的抽象。
/// </summary>
public interface ITemplateSource
{
    /// <summary>
    /// 从源中异步加载所有模板文件。
    /// </summary>
    /// <returns>加载的模板文件集合。</returns>
    Task<IReadOnlyList<TemplateFile>> LoadTemplatesAsync();
}
