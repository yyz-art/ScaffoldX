using System.Reflection;
using ScaffoldX.Core.Models;
using Serilog;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 管理所有模板文件的注册与查询。
/// 从 ScaffoldX.Templates 程序集的嵌入资源中加载 .stpl 文件。
/// </summary>
public class TemplateRegistry
{
    private readonly ILogger _logger = Log.ForContext<TemplateRegistry>();
    private readonly List<TemplateFile> _templates = new();

    // ── 公共 API ──────────────────────────────────────────────────────────

    /// <summary>
    /// 从 ScaffoldX.Templates 程序集的嵌入资源异步加载所有 .stpl 模板文件。
    /// 资源名称格式：ScaffoldX.Templates.<Category>.<Name>.stpl
    /// </summary>
    /// <returns>成功加载的模板数量。</returns>
    /// <exception cref="InvalidOperationException">找不到 ScaffoldX.Templates 程序集时抛出。</exception>
    public async Task<int> LoadFromAssemblyAsync()
    {
        _templates.Clear();

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
                _templates.Add(template);
                _logger.Debug("已加载模板: {Name} (Category={Category})", template.Name, template.Category);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载模板资源失败: {ResourceName}", resourceName);
                throw;
            }
        }

        _logger.Information("共加载 {Count} 个模板", _templates.Count);
        return _templates.Count;
    }

    /// <summary>
    /// 根据项目配置返回需要渲染的模板列表。
    /// 按 Category 和条件过滤（如 EnableSiemensS7 才包含 S7 驱动模板）。
    /// </summary>
    /// <param name="config">用户在向导中填写的项目配置。</param>
    /// <returns>过滤后的模板列表。</returns>
    public IReadOnlyList<TemplateFile> GetTemplatesForConfig(ProjectConfig config)
    {
        var result = new List<TemplateFile>();

        foreach (var template in _templates)
        {
            if (ShouldInclude(template, config))
            {
                result.Add(template);
            }
        }

        _logger.Debug("配置过滤后共 {Count} 个模板需要渲染", result.Count);
        return result;
    }

    /// <summary>
    /// 返回所有已注册的模板（不过滤）。
    /// </summary>
    public IReadOnlyList<TemplateFile> GetAllTemplates() => _templates.AsReadOnly();

    // ── 私有方法 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 从程序集嵌入资源流中读取单个模板文件。
    /// 资源名称格式：ScaffoldX.Templates.Category.FileName.stpl
    /// </summary>
    private static async Task<TemplateFile> LoadTemplateFromResourceAsync(
        Assembly assembly,
        string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"无法打开嵌入资源流: {resourceName}");

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // 解析资源名称：ScaffoldX.Templates.<Category>.<FileName>.stpl
        // 去掉前缀 "ScaffoldX.Templates."
        var relativeName = resourceName["ScaffoldX.Templates.".Length..];
        var parts = relativeName.Split('.');

        // parts 最后一个是 "stpl"，倒数第二个是文件名（不含扩展名），其余是目录/分类
        string category;
        string name;

        if (parts.Length >= 3)
        {
            // 取第一段作为 Category，最后两段是 name.stpl
            category = parts[0];
            name = string.Join(".", parts[1..^1]); // 去掉 "stpl"
        }
        else
        {
            category = "Common";
            name = parts[0];
        }

        // 从模板内容第一行读取输出路径模板（约定：首行以 ##OUTPUT: 开头）
        var outputPathTemplate = ExtractOutputPathTemplate(ref content, name);

        // 从模板内容读取 IsRequired 标记（约定：##REQUIRED: true/false）
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

    /// <summary>
    /// 从模板内容中提取并移除 ##OUTPUT: 指令行，返回输出路径模板字符串。
    /// 若不存在该指令，则返回基于模板名称的默认路径。
    /// </summary>
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
                // 移除该指令行
                content = string.Join('\n', lines.Where((_, idx) => idx != i));
                return outputPath;
            }
        }

        // 默认：使用模板名称作为相对路径
        return templateName.Replace('.', '/') + ".cs";
    }

    /// <summary>
    /// 从模板内容中提取并移除 ##REQUIRED: 指令行，返回是否必须生成。
    /// 若不存在该指令，默认返回 true。
    /// </summary>
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

    /// <summary>
    /// 根据模板分类和项目配置判断是否应包含该模板。
    /// </summary>
    private static bool ShouldInclude(TemplateFile template, ProjectConfig config)
    {
        if (template.IsRequired)
        {
            return true;
        }

        return template.Category switch
        {
            "Collection" => ShouldIncludeCollectionTemplate(template, config),
            "Vision"     => config.EnableVision,
            "System"     => ShouldIncludeSystemTemplate(template, config),
            _            => true, // Common 及未知分类默认包含
        };
    }

    /// <summary>
    /// 判断采集类模板是否应包含，依据模板名称中的驱动关键字匹配配置。
    /// </summary>
    private static bool ShouldIncludeCollectionTemplate(TemplateFile template, ProjectConfig config)
    {
        var name = template.Name.ToUpperInvariant();

        if (name.Contains("S7") || name.Contains("SIEMENS"))
        {
            return config.EnableSiemensS7;
        }

        if (name.Contains("MODBUS"))
        {
            return config.EnableModbusTcp;
        }

        if (name.Contains("OPCUA") || name.Contains("OPC_UA") || name.Contains("OPC-UA"))
        {
            return config.EnableOpcUa;
        }

        if (name.Contains("MITSUBISHI") || name.Contains("MC"))
        {
            return config.EnableMitsubishiMc;
        }

        if (name.Contains("OMRON") || name.Contains("FINS"))
        {
            return config.EnableOmronFins;
        }

        // 采集类通用模板：只要启用了任意驱动就包含
        return config.EnableSiemensS7
            || config.EnableModbusTcp
            || config.EnableOpcUa
            || config.EnableMitsubishiMc
            || config.EnableOmronFins;
    }

    /// <summary>
    /// 判断系统类模板是否应包含，依据模板名称中的功能关键字匹配配置。
    /// </summary>
    private static bool ShouldIncludeSystemTemplate(TemplateFile template, ProjectConfig config)
    {
        var name = template.Name.ToUpperInvariant();

        if (name.Contains("USER"))
        {
            return config.EnableUserManagement;
        }

        if (name.Contains("ROLE") || name.Contains("PERMISSION"))
        {
            return config.EnableRolePermission;
        }

        if (name.Contains("LOG") || name.Contains("AUDIT") || name.Contains("SYSTEMLOG"))
        {
            return config.EnableSystemLog;
        }

        if (name.Contains("THEME") || name.Contains("SWITCHER"))
        {
            return config.EnableThemeSwitcher;
        }

        // 系统核心模板（UserRole、IMenuModule 等）：任一系统模块启用时包含
        return config.EnableUserManagement
            || config.EnableRolePermission
            || config.EnableSystemLog
            || config.EnableThemeSwitcher;
    }
}
