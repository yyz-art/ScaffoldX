using ScaffoldX.Core.Models;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 将 <see cref="ProjectConfig"/> 转换为 Scriban 模板变量字典。
/// </summary>
public interface IVariableResolver
{
    /// <summary>
    /// 根据项目配置构建完整的 Scriban 变量上下文字典。
    /// </summary>
    Dictionary<string, object> BuildVariableContext(ProjectConfig config);

    /// <summary>
    /// 将任意字符串转换为 PascalCase。
    /// </summary>
    string ToPascalCase(string input);
}
