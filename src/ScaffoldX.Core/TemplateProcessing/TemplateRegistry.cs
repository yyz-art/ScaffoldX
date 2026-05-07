using ScaffoldX.Core.Models;
using Serilog;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 管理所有模板文件的注册与查询。
/// 通过 <see cref="ITemplateSource"/> 加载模板文件。
/// </summary>
public class TemplateRegistry : ITemplateRegistry
{
    private readonly ILogger _logger = Log.ForContext<TemplateRegistry>();
    private readonly ITemplateSource _templateSource;
    private readonly List<TemplateFile> _templates = new();

    /// <summary>
    /// 初始化模板注册表。
    /// </summary>
    /// <param name="templateSource">模板加载源。</param>
    public TemplateRegistry(ITemplateSource templateSource)
    {
        _templateSource = templateSource;
    }

    // ── 公共 API ──────────────────────────────────────────────────────────

    /// <summary>
    /// 从模板源异步加载所有模板文件。
    /// </summary>
    /// <returns>成功加载的模板数量。</returns>
    public async Task<int> LoadTemplatesAsync()
    {
        _templates.Clear();

        var loaded = await _templateSource.LoadTemplatesAsync();
        _templates.AddRange(loaded);

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
