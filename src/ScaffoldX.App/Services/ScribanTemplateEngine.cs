using Scriban;
using Scriban.Runtime;

namespace ScaffoldX.App.Services;

/// <summary>
/// 基于 Scriban 5.x 的 <see cref="ITemplateEngine"/> 实现。
/// 每次调用 <see cref="Render"/> 时解析并渲染模板，线程安全。
/// </summary>
public class ScribanTemplateEngine : ITemplateEngine
{
    /// <summary>
    /// 使用给定的变量字典渲染 Scriban 模板字符串。
    /// </summary>
    /// <param name="template">Scriban 格式的模板字符串。</param>
    /// <param name="variables">模板变量字典，键为变量名，值为任意对象。</param>
    /// <returns>渲染后的文本内容。</returns>
    /// <exception cref="InvalidOperationException">模板解析或渲染失败时抛出。</exception>
    public string Render(string template, Dictionary<string, object> variables)
    {
        Template parsed = Template.Parse(template);

        if (parsed.HasErrors)
        {
            string errors = string.Join("; ", parsed.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"模板解析失败：{errors}");
        }

        var scriptObject = new ScriptObject();
        foreach (KeyValuePair<string, object> kv in variables)
        {
            scriptObject.Add(kv.Key, kv.Value);
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        string result = parsed.Render(context);

        return result;
    }
}
