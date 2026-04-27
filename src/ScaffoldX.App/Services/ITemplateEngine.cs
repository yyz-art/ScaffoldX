namespace ScaffoldX.App.Services;

/// <summary>
/// 模板引擎契约，负责将模板字符串与变量上下文渲染为最终文本。
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// 使用给定的变量字典渲染 Scriban 模板字符串。
    /// </summary>
    /// <param name="template">Scriban 格式的模板字符串。</param>
    /// <param name="variables">模板变量字典，键为变量名，值为任意对象。</param>
    /// <returns>渲染后的文本内容。</returns>
    /// <exception cref="InvalidOperationException">模板解析或渲染失败时抛出。</exception>
    string Render(string template, Dictionary<string, object> variables);
}
