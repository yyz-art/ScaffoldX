using ScaffoldX.Core.Models;

namespace ScaffoldX.Core.TemplateProcessing;

/// <summary>
/// 将 <see cref="ProjectConfig"/> 转换为 Scriban 模板变量字典。
/// 变量键采用 PascalCase，与 .stpl 模板中的占位符一致。
/// </summary>
public class VariableResolver : IVariableResolver
{
    /// <summary>
    /// 根据项目配置构建完整的 Scriban 变量上下文字典。
    /// </summary>
    /// <param name="config">用户在向导中填写的项目配置。</param>
    /// <returns>可直接传入 Scriban ScriptObject 的键值字典。</returns>
    public Dictionary<string, object> BuildVariableContext(ProjectConfig config)
    {
        var namespacePrefix = string.IsNullOrWhiteSpace(config.NamespacePrefix)
            ? ToPascalCase(config.ProjectName)
            : config.NamespacePrefix;

        var projectNamePascal = ToPascalCase(config.ProjectName);

        var ctx = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // ── 脚手架元数据 ──────────────────────────────────────────────────
        ctx["ScaffoldXVersion"] = "1.0.0";
        ctx["GeneratedAt"]      = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // ── 基础变量 ──────────────────────────────────────────────────────
        ctx["ProjectName"]         = projectNamePascal;
        ctx["NamespacePrefix"]     = namespacePrefix;
        ctx["TargetFramework"]     = config.TargetFramework;
        ctx["TargetFrameworkShort"] = config.TargetFramework.Replace("-windows", "");
        ctx["UIFramework"]         = config.UIFramework;
        ctx["Author"]              = config.Author;
        ctx["Company"]             = config.Company;
        ctx["Description"]         = config.Description;
        ctx["ProjectDescription"]  = config.Description;
        ctx["DatabaseType"]        = config.DatabaseType;
        ctx["Year"]                = DateTime.Now.Year.ToString();
        ctx["ProjectType"]         = config.ProjectType;
        ctx["AppTitle"]            = projectNamePascal;
        ctx["AppVersion"]          = "1.0.0";

        ctx["IsWPF"]      = config.UIFramework.Equals("WPF", StringComparison.OrdinalIgnoreCase);
        ctx["IsAvalonia"] = config.UIFramework.Equals("Avalonia", StringComparison.OrdinalIgnoreCase);

        // ── 采集类变量 ────────────────────────────────────────────────────
        ctx["EnableSiemensS7"]    = config.EnableSiemensS7;
        ctx["EnableModbusTcp"]    = config.EnableModbusTcp;
        ctx["EnableOpcUa"]        = config.EnableOpcUa;
        ctx["EnableMitsubishiMc"] = config.EnableMitsubishiMc;
        ctx["EnableOmronFins"]    = config.EnableOmronFins;
        ctx["HasAnyCollection"]   = config.EnableSiemensS7
            || config.EnableModbusTcp
            || config.EnableOpcUa
            || config.EnableMitsubishiMc
            || config.EnableOmronFins;

        // ── 视觉类变量 ────────────────────────────────────────────────────
        ctx["EnableVision"]    = config.EnableVision;
        ctx["CameraBrand"]     = config.CameraBrand;
        ctx["ModelType"]       = config.ModelType;
        ctx["CameraBrandPascal"] = ToPascalCase(config.CameraBrand);
        ctx["ModelTypePascal"]   = ToPascalCase(config.ModelType);

        // ── 系统类变量 ────────────────────────────────────────────────────
        ctx["EnableUserManagement"]  = config.EnableUserManagement;
        ctx["EnableRolePermission"]  = config.EnableRolePermission;
        ctx["EnableSystemLog"]       = config.EnableSystemLog;
        ctx["EnableThemeSwitcher"]   = config.EnableThemeSwitcher;

        // ── 采集扩展变量 ──────────────────────────────────────────────────
        ctx["EnableSimulationDriver"] = config.EnableSimulationDriver;
        ctx["DefaultPLCIp"]          = config.DefaultPLCIp;
        ctx["DefaultPLCPort"]        = config.DefaultPLCPort;
        ctx["S7Rack"]                = config.S7Rack;
        ctx["S7Slot"]                = config.S7Slot;
        ctx["OpcUaEndpoint"]         = config.OpcUaEndpoint;

        // ── 视觉扩展变量 ──────────────────────────────────────────────────
        ctx["EnablePipeline"] = config.EnablePipeline;
        ctx["ModelPath"]      = config.ModelPath;

        // ── UI/导航变量 ──────────────────────────────────────────────────────
        ctx["NavigationStyle"]    = config.NavigationStyle;
        ctx["DefaultTheme"]       = config.DefaultTheme;
        ctx["DefaultLanguage"]    = config.DefaultLanguage;
        ctx["EnableLocalization"] = config.EnableLocalization;

        // ── 系统扩展变量 ──────────────────────────────────────────────────
        ctx["EnableLoginWindow"]     = config.EnableLoginWindow;
        ctx["EnableCrossPlatform"]   = config.EnableCrossPlatform;
        ctx["ForcePasswordChange"]   = config.ForcePasswordChange;

        // ── 模块列表变量（供模板 for 循环使用）────────────────────────────
        var selectedModules = new List<string>();
        if (config.EnableUserManagement) selectedModules.Add("UserManagement");
        if (config.EnableRolePermission) selectedModules.Add("RolePermission");
        if (config.EnableSystemLog)      selectedModules.Add("SystemLog");
        if (config.EnableThemeSwitcher)  selectedModules.Add("ThemeSwitcher");
        ctx["SelectedModules"] = selectedModules;

        // ── XAML 文件名变量 ───────────────────────────────────────────────
        var isAvalonia = config.UIFramework.Equals("Avalonia", StringComparison.OrdinalIgnoreCase);

        ctx["XamlExt"]    = isAvalonia ? "axaml" : "xaml";
        ctx["XamlCodeBehindExt"] = isAvalonia ? ".axaml.cs" : ".xaml.cs";
        ctx["XamlNs"]     = isAvalonia
            ? "xmlns=\"https://github.com/avaloniaui\""
            : "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"";
        ctx["XamlXNs"]    = "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";
        ctx["WindowBaseClass"]   = "Window";
        ctx["UserControlBase"]   = "UserControl";

        // ── 派生便利变量 ──────────────────────────────────────────────────
        ctx["SolutionName"]  = projectNamePascal;
        ctx["RootNamespace"] = namespacePrefix;
        ctx["AssemblyName"]  = projectNamePascal;

        return ctx;
    }

    /// <summary>
    /// 将任意字符串转换为 PascalCase。
    /// 按下划线 (_)、连字符 (-) 和空格分割，每段首字母大写，其余保持原样。
    /// 若输入为空或空白，返回空字符串。
    /// </summary>
    /// <param name="input">待转换的原始字符串。</param>
    /// <returns>PascalCase 格式的字符串。</returns>
    public string ToPascalCase(string input)
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
                var remainder = segment[1..];
                if (remainder == remainder.ToUpperInvariant())
                {
                    result.Append(remainder.ToLowerInvariant());
                }
                else
                {
                    result.Append(remainder);
                }
            }
        }

        return result.ToString();
    }
}
