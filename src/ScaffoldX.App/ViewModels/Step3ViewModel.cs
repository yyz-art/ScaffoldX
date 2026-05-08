using Prism.Mvvm;
using ScaffoldX.Core.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 驱动选项数据模型，用于步骤三采集类配置的 CheckBox 列表。
/// </summary>
public class DriverOption : BindableBase
{
    private bool _isSelected;

    /// <summary>驱动标识符，如 "S7Net"、"ModbusTcp"、"OpcUa"。</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>驱动显示名称。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>是否已选中。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>
/// 功能模块选项数据模型，用于步骤三系统类配置的 CheckBox 列表。
/// </summary>
public class ModuleOption : BindableBase
{
    private bool _isSelected;

    /// <summary>模块标识符，如 "UserManagement"、"RolePermission"、"AuditLog"。</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>模块显示名称。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>是否已选中。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

/// <summary>
/// 步骤三 ViewModel：专项配置，根据项目类型展示不同的配置面板。
/// 内部拆分为三个子 ViewModel（Collection / Vision / System），
/// 所有属性直接读写共享的 ProjectConfig，无需手动字段拷贝。
/// </summary>
public class Step3ViewModel : BindableBase
{
    /// <summary>
    /// 初始化步骤三 ViewModel，创建三个子配置 ViewModel。
    /// </summary>
    public Step3ViewModel() : this(new ProjectConfig
    {
        EnableSimulationDriver = true,
        DefaultPLCIp = "192.168.1.1",
        DefaultPLCPort = 102,
        S7Slot = 1,
        OpcUaEndpoint = "opc.tcp://localhost:4840",
        CameraBrand = "海康",
        ModelType = "Classification",
        EnablePipeline = true,
        EnableLoginWindow = true,
        EnableUserManagement = true,
        EnableRolePermission = true,
        NavigationStyle = "LeftSidebar",
        DefaultTheme = "IndustrialDark",
        DefaultLanguage = "zh-CN"
    }) { }

    /// <summary>
    /// 初始化步骤三 ViewModel，绑定到指定 ProjectConfig。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public Step3ViewModel(ProjectConfig config)
    {
        Config = config;
        Collection = new CollectionConfigViewModel(config);
        Vision = new VisionConfigViewModel(config);
        System = new SystemConfigViewModel(config);

        NavigationStyleOptions = new List<string> { "LeftSidebar", "TopNav" };
        DefaultThemeOptions = new List<string> { "IndustrialDark", "LightModern" };
        DefaultLanguageOptions = new List<string> { "zh-CN", "en-US" };
    }

    // ── 子 ViewModel ───────────────────────────────────────────────────────────

    /// <summary>关联的项目配置对象，所有属性直接读写此对象。</summary>
    public ProjectConfig Config { get; private set; }

    /// <summary>
    /// 初始化或重新绑定到指定 ProjectConfig，并同步所有子 ViewModel。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public void Initialize(ProjectConfig config)
    {
        Config = config;
        Collection.Initialize(config);
        Vision.Initialize(config);
        System.Initialize(config);

        RaisePropertyChanged(nameof(ProjectType));
        RaisePropertyChanged(nameof(NavigationStyle));
        RaisePropertyChanged(nameof(DefaultTheme));
        RaisePropertyChanged(nameof(DefaultLanguage));
    }

    /// <summary>采集类配置子 ViewModel。</summary>
    public CollectionConfigViewModel Collection { get; }

    /// <summary>视觉类配置子 ViewModel。</summary>
    public VisionConfigViewModel Vision { get; }

    /// <summary>系统类配置子 ViewModel。</summary>
    public SystemConfigViewModel System { get; }

    // ── 通用 ──────────────────────────────────────────────────────────────────

    /// <summary>当前项目类型，控制显示哪个配置面板。</summary>
    public string ProjectType
    {
        get => Config.ProjectType;
        private set
        {
            Config.ProjectType = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsCollection));
            RaisePropertyChanged(nameof(IsVision));
            RaisePropertyChanged(nameof(IsSystem));
        }
    }

    /// <summary>是否为采集类项目。</summary>
    public bool IsCollection => ProjectType == "Collection";

    /// <summary>是否为视觉类项目。</summary>
    public bool IsVision => ProjectType == "Vision";

    /// <summary>是否为系统类项目。</summary>
    public bool IsSystem => ProjectType == "System";

    // ── 采集类属性（委托到 Collection） ────────────────────────────────────────

    /// <summary>驱动选项列表（CheckBox 绑定）。</summary>
    public List<DriverOption> DriverOptions => Collection.DriverOptions;

    /// <summary>是否选中了 S7Net 驱动（联动显示机架槽号）。</summary>
    public bool IsS7Selected => Collection.IsS7Selected;

    /// <summary>是否生成仿真驱动。</summary>
    public bool EnableSimulationDriver
    {
        get => Collection.EnableSimulationDriver;
        set => Collection.EnableSimulationDriver = value;
    }

    /// <summary>默认 PLC IP 地址。</summary>
    public string DefaultPLCIp
    {
        get => Collection.DefaultPLCIp;
        set => Collection.DefaultPLCIp = value;
    }

    /// <summary>默认 PLC 端口。</summary>
    public int DefaultPLCPort
    {
        get => Collection.DefaultPLCPort;
        set => Collection.DefaultPLCPort = value;
    }

    /// <summary>S7 协议机架号。</summary>
    public int S7Rack
    {
        get => Collection.S7Rack;
        set => Collection.S7Rack = value;
    }

    /// <summary>S7 协议槽号。</summary>
    public int S7Slot
    {
        get => Collection.S7Slot;
        set => Collection.S7Slot = value;
    }

    /// <summary>OPC UA 服务端端点 URL。</summary>
    public string OpcUaEndpoint
    {
        get => Collection.OpcUaEndpoint;
        set => Collection.OpcUaEndpoint = value;
    }

    // ── 视觉类属性（委托到 Vision） ────────────────────────────────────────────

    /// <summary>相机品牌列表。</summary>
    public List<string> CameraBrands => Vision.CameraBrands;

    /// <summary>模型类型列表。</summary>
    public List<string> ModelTypes => Vision.ModelTypes;

    /// <summary>选中的相机品牌。</summary>
    public string CameraBrand
    {
        get => Vision.CameraBrand;
        set => Vision.CameraBrand = value;
    }

    /// <summary>选中的模型类型。</summary>
    public string ModelType
    {
        get => Vision.ModelType;
        set => Vision.ModelType = value;
    }

    /// <summary>推理模型文件路径。</summary>
    public string ModelPath
    {
        get => Vision.ModelPath;
        set => Vision.ModelPath = value;
    }

    /// <summary>是否启用图像处理 Pipeline。</summary>
    public bool EnablePipeline
    {
        get => Vision.EnablePipeline;
        set => Vision.EnablePipeline = value;
    }

    // ── UI/导航属性 ──────────────────────────────────────────────────────────

    /// <summary>导航样式选项列表。</summary>
    public List<string> NavigationStyleOptions { get; }

    /// <summary>默认主题选项列表。</summary>
    public List<string> DefaultThemeOptions { get; }

    /// <summary>默认语言选项列表。</summary>
    public List<string> DefaultLanguageOptions { get; }

    /// <summary>选中的导航样式。</summary>
    public string NavigationStyle
    {
        get => Config.NavigationStyle;
        set
        {
            Config.NavigationStyle = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>选中的默认主题。</summary>
    public string DefaultTheme
    {
        get => Config.DefaultTheme;
        set
        {
            Config.DefaultTheme = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>选中的默认语言。</summary>
    public string DefaultLanguage
    {
        get => Config.DefaultLanguage;
        set
        {
            Config.DefaultLanguage = value;
            RaisePropertyChanged();
        }
    }

    // ── 系统类属性（委托到 System） ────────────────────────────────────────────

    /// <summary>功能模块选项列表（CheckBox 绑定）。</summary>
    public List<ModuleOption> ModuleOptions => System.ModuleOptions;

    /// <summary>是否生成独立登录窗口。</summary>
    public bool EnableLoginWindow
    {
        get => System.EnableLoginWindow;
        set => System.EnableLoginWindow = value;
    }

    /// <summary>是否强制首次登录修改密码。</summary>
    public bool ForcePasswordChange
    {
        get => System.ForcePasswordChange;
        set => System.ForcePasswordChange = value;
    }

    // ── 公共方法 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 根据项目类型切换显示面板（由主 ViewModel 在步骤一完成后调用）。
    /// </summary>
    /// <param name="projectType">项目类型字符串。</param>
    public void ApplyProjectType(string projectType)
    {
        ProjectType = projectType;

        // 采集类：Modbus 默认端口 502
        if (projectType == "Collection")
        {
            var modbus = DriverOptions.FirstOrDefault(d => d.Key == "ModbusTcp");
            if (modbus?.IsSelected == true)
                DefaultPLCPort = 502;
        }
    }

    /// <summary>
    /// 获取已选驱动的 Key 列表。
    /// </summary>
    /// <returns>已选驱动标识符列表。</returns>
    public List<string> GetSelectedDrivers()
        => Collection.GetSelectedDrivers();

    /// <summary>
    /// 获取已选功能模块的 Key 列表。
    /// </summary>
    /// <returns>已选模块标识符列表。</returns>
    public List<string> GetSelectedModules()
        => System.GetSelectedModules();

    /// <summary>
    /// 重置所有配置到默认值（新建项目时调用）。
    /// </summary>
    public void Reset()
    {
        ProjectType = string.Empty;
        NavigationStyle = "LeftSidebar";
        DefaultTheme = "IndustrialDark";
        DefaultLanguage = "zh-CN";

        Collection.Reset();
        Vision.Reset();
        System.Reset();
    }
}
