namespace ScaffoldX.Core.Models;

/// <summary>
/// 保存向导收集的所有用户配置，贯穿整个代码生成流程。
/// </summary>
public class ProjectConfig
{
    // ── 基础信息 ──────────────────────────────────────────────────────────

    /// <summary>项目类型："Collection"（采集）| "Vision"（视觉）| "System"（系统）。</summary>
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>项目名称，用于生成命名空间、程序集名称和目录名。</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>命名空间前缀，默认与 ProjectName 相同，可由用户覆盖。</summary>
    public string NamespacePrefix { get; set; } = string.Empty;

    /// <summary>目标框架，如 "net8.0-windows"。</summary>
    public string TargetFramework { get; set; } = "net8.0-windows";

    /// <summary>UI 框架：WPF 或 Avalonia。</summary>
    public string UIFramework { get; set; } = "WPF";

    /// <summary>输出根目录，生成的解决方案将放置于此目录下。</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>作者名称，写入 AssemblyInfo 和 README。</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>公司名称，写入 AssemblyInfo。</summary>
    public string Company { get; set; } = string.Empty;

    /// <summary>项目描述，写入 README。</summary>
    public string Description { get; set; } = string.Empty;

    // ── 采集类选项 ────────────────────────────────────────────────────────

    /// <summary>是否启用西门子 S7 PLC 驱动。</summary>
    public bool EnableSiemensS7 { get; set; }

    /// <summary>是否启用 Modbus TCP 驱动。</summary>
    public bool EnableModbusTcp { get; set; }

    /// <summary>是否启用 OPC-UA 驱动。</summary>
    public bool EnableOpcUa { get; set; }

    /// <summary>是否启用 Mitsubishi MC 协议驱动。</summary>
    public bool EnableMitsubishiMc { get; set; }

    /// <summary>是否启用 Omron FINS 驱动。</summary>
    public bool EnableOmronFins { get; set; }

    // ── 视觉类选项 ────────────────────────────────────────────────────────

    /// <summary>是否启用视觉模块。</summary>
    public bool EnableVision { get; set; }

    /// <summary>相机品牌，如 "Basler"、"Hikvision"、"Cognex"。</summary>
    public string CameraBrand { get; set; } = string.Empty;

    /// <summary>视觉模型类型，如 "Classification"、"Detection"、"OCR"。</summary>
    public string ModelType { get; set; } = string.Empty;

    // ── 系统类选项 ────────────────────────────────────────────────────────

    /// <summary>是否启用用户管理模块。</summary>
    public bool EnableUserManagement { get; set; }

    /// <summary>是否启用角色权限模块。</summary>
    public bool EnableRolePermission { get; set; }

    /// <summary>是否启用审计日志模块。</summary>
    public bool EnableSystemLog { get; set; }

    /// <summary>是否启用主题切换模块。</summary>
    public bool EnableThemeSwitcher { get; set; }

    /// <summary>数据库类型，如 "SQLite"、"SQLServer"、"MySQL"。</summary>
    public string DatabaseType { get; set; } = "SQLite";

    // ── 采集扩展选项 ──────────────────────────────────────────────────────

    /// <summary>是否生成仿真驱动。</summary>
    public bool EnableSimulationDriver { get; set; } = true;

    /// <summary>默认 PLC IP 地址。</summary>
    public string DefaultPLCIp { get; set; } = "192.168.1.1";

    /// <summary>默认 PLC 端口。</summary>
    public int DefaultPLCPort { get; set; } = 102;

    /// <summary>S7 机架号。</summary>
    public int S7Rack { get; set; } = 0;

    /// <summary>S7 槽号。</summary>
    public int S7Slot { get; set; } = 1;

    /// <summary>OPC-UA 端点 URL。</summary>
    public string OpcUaEndpoint { get; set; } = "opc.tcp://localhost:4840";

    // ── 视觉扩展选项 ──────────────────────────────────────────────────────

    /// <summary>是否启用图像处理 Pipeline。</summary>
    public bool EnablePipeline { get; set; }

    /// <summary>推理模型文件路径。</summary>
    public string ModelPath { get; set; } = string.Empty;

    // ── UI/导航选项 ──────────────────────────────────────────────────────

    /// <summary>导航样式："LeftSidebar"（左侧边栏）| "TopNav"（顶部导航）。</summary>
    public string NavigationStyle { get; set; } = "LeftSidebar";

    /// <summary>默认主题："IndustrialDark"（工业深色）| "LightModern"（现代浅色）。</summary>
    public string DefaultTheme { get; set; } = "IndustrialDark";

    /// <summary>默认语言，如 "zh-CN"、"en-US"。</summary>
    public string DefaultLanguage { get; set; } = "zh-CN";

    /// <summary>是否启用中英国际化（生成 .resx 资源文件和 ILocalizationService）。</summary>
    public bool EnableLocalization { get; set; }

    // ── 系统扩展选项 ──────────────────────────────────────────────────────

    /// <summary>是否生成独立登录窗口。</summary>
    public bool EnableLoginWindow { get; set; }

    /// <summary>是否启用跨平台支持。</summary>
    public bool EnableCrossPlatform { get; set; }

    /// <summary>是否强制首次登录修改密码。</summary>
    public bool ForcePasswordChange { get; set; }

    // ── 其他 ──────────────────────────────────────────────────────────────

    /// <summary>是否生成 Git 仓库初始化文件（.gitignore 等）。</summary>
    public bool InitGitRepository { get; set; } = true;

    /// <summary>是否生成发布脚本（publish.bat / publish.sh）。</summary>
    public bool GeneratePublishScripts { get; set; } = true;

    // ── 便利方法 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 设置指定驱动的启用状态。
    /// </summary>
    public void SetDriver(string driverName, bool enabled)
    {
        switch (driverName)
        {
            case "S7Net":        EnableSiemensS7 = enabled; break;
            case "ModbusTcp":    EnableModbusTcp = enabled; break;
            case "OpcUa":        EnableOpcUa = enabled; break;
            case "MitsubishiMc": EnableMitsubishiMc = enabled; break;
            case "OmronFins":    EnableOmronFins = enabled; break;
        }
    }

    /// <summary>
    /// 设置导航样式。
    /// </summary>
    public void SetNavigationStyle(string style)
    {
        NavigationStyle = style;
    }

    /// <summary>
    /// 设置指定系统模块的启用状态。
    /// </summary>
    public void SetModule(string moduleName, bool enabled)
    {
        switch (moduleName)
        {
            case "UserManagement": EnableUserManagement = enabled; break;
            case "RolePermission": EnableRolePermission = enabled; break;
            case "SystemLog":      EnableSystemLog = enabled; break;
            case "ThemeSwitcher":  EnableThemeSwitcher = enabled; break;
        }
    }

    /// <summary>
    /// 获取已启用的驱动名称列表。
    /// </summary>
    public List<string> GetSelectedDrivers()
    {
        var drivers = new List<string>();
        if (EnableSiemensS7)    drivers.Add("S7Net");
        if (EnableModbusTcp)    drivers.Add("ModbusTcp");
        if (EnableOpcUa)        drivers.Add("OpcUa");
        if (EnableMitsubishiMc) drivers.Add("MitsubishiMc");
        if (EnableOmronFins)    drivers.Add("OmronFins");
        return drivers;
    }

    /// <summary>
    /// 获取已启用的系统模块名称列表。
    /// </summary>
    public List<string> GetSelectedModules()
    {
        var modules = new List<string>();
        if (EnableUserManagement) modules.Add("UserManagement");
        if (EnableRolePermission) modules.Add("RolePermission");
        if (EnableSystemLog)      modules.Add("SystemLog");
        if (EnableThemeSwitcher)  modules.Add("ThemeSwitcher");
        return modules;
    }
}
