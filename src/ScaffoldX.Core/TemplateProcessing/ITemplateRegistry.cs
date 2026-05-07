using ScaffoldX.Core.Models;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 管理所有模板文件的注册与查询。
/// </summary>
public interface ITemplateRegistry
{
    /// <summary>
    /// 异步加载所有模板文件。
    /// </summary>
    /// <returns>成功加载的模板数量。</returns>
    Task<int> LoadTemplatesAsync();

    /// <summary>
    /// 根据项目配置返回需要渲染的模板列表。
    /// </summary>
    IReadOnlyList<TemplateFile> GetTemplatesForConfig(ProjectConfig config);

    /// <summary>
    /// 返回所有已注册的模板（不过滤）。
    /// </summary>
    IReadOnlyList<TemplateFile> GetAllTemplates();
}
