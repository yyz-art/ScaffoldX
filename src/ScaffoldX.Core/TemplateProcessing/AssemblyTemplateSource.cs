using System.Reflection;
using Serilog;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 从 ScaffoldX.Templates 程序集的嵌入资源中加载 .stpl 模板文件。
/// </summary>
public class AssemblyTemplateSource : ITemplateSource
{
    private readonly ILogger _logger = Log.ForContext<AssemblyTemplateSource>();

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateFile>> LoadTemplatesAsync()
    {
        var templates = new List<TemplateFile>();

        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ScaffoldX.Templates")
            ?? Assembly.Load("ScaffoldX.Templates");

        if (assembly is null)
        {
            throw new InvalidOperationException("无法加载 ScaffoldX.Templates 程序集。");
        }

        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".stpl", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        _logger.Information("发现 {Count} 个 .stpl 嵌入资源", resourceNames.Length);

        foreach (var resourceName in resourceNames)
        {
            try
            {
                var template = await LoadTemplateFromResourceAsync(assembly, resourceName);
                templates.Add(template);
                _logger.Debug("已加载模板: {Name} (Category={Category})", template.Name, template.Category);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载模板资源失败: {ResourceName}", resourceName);
                throw;
            }
        }

        _logger.Information("共加载 {Count} 个模板", templates.Count);
        return templates;
    }

    private static async Task<TemplateFile> LoadTemplateFromResourceAsync(
        Assembly assembly,
        string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"无法打开嵌入资源流: {resourceName}");

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        var relativeName = resourceName["ScaffoldX.Templates.".Length..];
        var parts = relativeName.Split('.');

        string category;
        string name;

        if (parts.Length >= 3)
        {
            category = parts[0];
            name = string.Join(".", parts[1..^1]);
        }
        else
        {
            category = "Common";
            name = parts[0];
        }

        var outputPathTemplate = ExtractOutputPathTemplate(ref content, name);
        var isRequired = ExtractIsRequired(ref content);

        return new TemplateFile
        {
            Name = name,
            Content = content,
            OutputPathTemplate = outputPathTemplate,
            Category = category,
            IsRequired = isRequired,
        };
    }

    private static string ExtractOutputPathTemplate(ref string content, string templateName)
    {
        const string prefix = "##OUTPUT:";
        var lines = content.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var outputPath = trimmed[prefix.Length..].Trim();
                content = string.Join('\n', lines.Where((_, idx) => idx != i));
                return outputPath;
            }
        }

        return templateName.Replace('.', '/') + ".cs";
    }

    private static bool ExtractIsRequired(ref string content)
    {
        const string prefix = "##REQUIRED:";
        var lines = content.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed[prefix.Length..].Trim();
                content = string.Join('\n', lines.Where((_, idx) => idx != i));
                return !value.Equals("false", StringComparison.OrdinalIgnoreCase);
            }
        }

        return true;
    }
}
