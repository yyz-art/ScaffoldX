using ScaffoldX.Core.Models;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 将 <see cref="ProjectConfig"/> 转换为 Scriban 模板变量字典。
/// 实现 PRD §10.4 中的 BuildVariableContext 逻辑。
/// </summary>
public static class VariableResolver
{
    /// <summary>
    /// 根据项目配置构建完整的 Scriban 变量上下文字典。
    /// 所有键均为 snake_case，与 Scriban 默认成员访问风格一致。
    /// </summary>
    /// <param name="config">用户在向导中填写的项目配置。</param>
    /// <returns>可直接传入 Scriban ScriptObject 的键值字典。</returns>
    public static Dictionary<string, object> BuildVariableContext(ProjectConfig config)
    {
        var namespacePrefix = string.IsNullOrWhiteSpace(config.NamespacePrefix)
            ? ToPascalCase(config.ProjectName)
            : config.NamespacePrefix;

        var projectNamePascal = ToPascalCase(config.ProjectName);

        var ctx = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // ── 基础变量 ──────────────────────────────────────────────────────
        ctx["project_name"]       = projectNamePascal;
        ctx["namespace_prefix"]   = namespacePrefix;
        ctx["target_framework"]   = config.TargetFramework;
        ctx["ui_framework"]       = config.UIFramework;
        ctx["author"]             = config.Author;
        ctx["company"]            = config.Company;
        ctx["description"]        = config.Description;
        ctx["database_type"]      = config.DatabaseType;
        ctx["year"]               = DateTime.Now.Year.ToString();

        // ── 采集类变量 ────────────────────────────────────────────────────
        ctx["enable_siemens_s7"]    = config.EnableSiemensS7;
        ctx["enable_modbus_tcp"]    = config.EnableModbusTcp;
        ctx["enable_opc_ua"]        = config.EnableOpcUa;
        ctx["enable_mitsubishi_mc"] = config.EnableMitsubishiMc;
        ctx["enable_omron_fins"]    = config.EnableOmronFins;

        // 是否启用了任意采集驱动
        ctx["has_any_collection"] = config.EnableSiemensS7
            || config.EnableModbusTcp
            || config.EnableOpcUa
            || config.EnableMitsubishiMc
            || config.EnableOmronFins;

        // ── 视觉类变量 ────────────────────────────────────────────────────
        ctx["enable_vision"] = config.EnableVision;
        ctx["camera_brand"]  = config.CameraBrand;
        ctx["model_type"]    = config.ModelType;

        ctx["camera_brand_pascal"] = ToPascalCase(config.CameraBrand);
        ctx["model_type_pascal"]   = ToPascalCase(config.ModelType);

        // ── 系统类变量 ────────────────────────────────────────────────────
        ctx["enable_user_management"]  = config.EnableUserManagement;
        ctx["enable_alarm_management"] = config.EnableAlarmManagement;
        ctx["enable_data_logging"]     = config.EnableDataLogging;
        ctx["enable_reporting"]        = config.EnableReporting;

        // ── XAML 文件名变量 ───────────────────────────────────────────────
        var isAvalonia = config.UIFramework.Equals("Avalonia", StringComparison.OrdinalIgnoreCase);

        // XAML 文件扩展名（WPF 和 Avalonia 均为 .axaml 或 .xaml）
        ctx["xaml_ext"] = isAvalonia ? "axaml" : "xaml";

        // XAML 命名空间声明前缀
        ctx["xaml_ns"] = isAvalonia
            ? "xmlns=\"https://github.com/avaloniaui\""
            : "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"";

        // XAML x: 命名空间
        ctx["xaml_x_ns"] = "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

        // 代码隐藏基类
        ctx["window_base_class"] = isAvalonia ? "Window" : "Window";
        ctx["user_control_base"] = isAvalonia ? "UserControl" : "UserControl";

        // ── 派生便利变量 ──────────────────────────────────────────────────
        ctx["solution_name"]    = projectNamePascal;
        ctx["root_namespace"]   = namespacePrefix;
        ctx["assembly_name"]    = projectNamePascal;

        return ctx;
    }

    /// <summary>
    /// 将任意字符串转换为 PascalCase。
    /// 按下划线 (_)、连字符 (-) 和空格分割，每段首字母大写，其余保持原样。
    /// 若输入为空或空白，返回空字符串。
    /// </summary>
    /// <param name="input">待转换的原始字符串。</param>
    /// <returns>PascalCase 格式的字符串。</returns>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var segments = input.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Text.StringBuilder(input.Length);

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                continue;
            }

            result.Append(char.ToUpperInvariant(segment[0]));

            if (segment.Length > 1)
            {
                result.Append(segment[1..]);
            }
        }

        return result.ToString();
    }
}
