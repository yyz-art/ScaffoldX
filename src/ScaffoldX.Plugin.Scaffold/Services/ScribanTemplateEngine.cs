using Scriban;
using Scriban.Runtime;

namespace ScaffoldX.Plugin.Scaffold.Services;

public class ScribanTemplateEngine : ITemplateEngine
{
    public string Render(string template, Dictionary<string, object> variables)
    {
        var parsed = Template.Parse(template);

        if (parsed.HasErrors)
        {
            var errors = string.Join("; ", parsed.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"模板解析失败：{errors}");
        }

        var scriptObject = new ScriptObject();
        foreach (var kv in variables)
        {
            scriptObject.Add(kv.Key, kv.Value);
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return parsed.Render(context);
    }
}
