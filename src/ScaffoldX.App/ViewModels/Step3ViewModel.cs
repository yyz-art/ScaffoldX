using Prism.Mvvm;

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
/// 包含采集类、视觉类、系统类三套配置属性及联动逻辑。
/// </summary>
public class Step3ViewModel : BindableBase
{
    private string _projectType = string.Empty;

    // ── 采集类字段 ────────────────────────────────────────────────────────────
    private bool _enableSimulationDriver = true;
    private string _defaultPLCIp = "192.168.1.1";
    private int _defaultPLCPort = 102;
    private int _s7Rack;
    private int _s7Slot = 1;
    private string _opcUaEndpoint = "opc.tcp://localhost:4840";

    // ── 视觉类字段 ────────────────────────────────────────────────────────────
    private string _cameraBrand = "海康";
    private string _modelType = "Classification";
    private string _modelPath = string.Empty;
    private bool _enablePipeline = true;

    // ── 系统类字段 ────────────────────────────────────────────────────────────
    private bool _enableLoginWindow = true;
    private bool _forcePasswordChange;

    /// <summary>
    /// 初始化步骤三 ViewModel，构建驱动和模块选项列表。
    /// </summary>
    public Step3ViewModel()
    {
        DriverOptions = new List<DriverOption>
        {
            new() { Key = "S7Net",     DisplayName = "西门子 S7（S7Net）",   IsSelected = false },
            new() { Key = "ModbusTcp", DisplayName = "Modbus TCP",           IsSelected = false },
            new() { Key = "OpcUa",     DisplayName = "OPC UA",               IsSelected = false },
            new() { Key = "Mitsubishi",DisplayName = "三菱 MC 协议",          IsSelected = false },
            new() { Key = "Omron",     DisplayName = "欧姆龙 FINS",           IsSelected = false },
        };

        // 监听 S7Net 选中状态，联动显示机架槽号
        foreach (var d in DriverOptions)
            d.PropertyChanged += (_, _) => RaisePropertyChanged(nameof(IsS7Selected));

        ModuleOptions = new List<ModuleOption>
        {
            new() { Key = "UserManagement", DisplayName = "用户管理",   IsSelected = true  },
            new() { Key = "RolePermission", DisplayName = "角色权限",   IsSelected = true  },
            new() { Key = "SystemLog",      DisplayName = "审计日志",   IsSelected = false },
            new() { Key = "ThemeSwitcher",  DisplayName = "主题切换",   IsSelected = false },
        };

        CameraBrands = new List<string> { "海康", "大华", "Basler", "其他" };
        ModelTypes   = new List<string> { "Classification", "Detection", "Segmentation" };
    }

    // ── 通用 ──────────────────────────────────────────────────────────────────

    /// <summary>当前项目类型，控制显示哪个配置面板。</summary>
    public string ProjectType
    {
        get => _projectType;
        private set
        {
            if (SetProperty(ref _projectType, value))
            {
                RaisePropertyChanged(nameof(IsCollection));
                RaisePropertyChanged(nameof(IsVision));
                RaisePropertyChanged(nameof(IsSystem));
            }
        }
    }

    /// <summary>是否为采集类项目。</summary>
    public bool IsCollection => ProjectType == "Collection";

    /// <summary>是否为视觉类项目。</summary>
    public bool IsVision => ProjectType == "Vision";

    /// <summary>是否为系统类项目。</summary>
    public bool IsSystem => ProjectType == "System";

    // ── 采集类属性 ────────────────────────────────────────────────────────────

    /// <summary>驱动选项列表（CheckBox 绑定）。</summary>
    public List<DriverOption> DriverOptions { get; }

    /// <summary>是否选中了 S7Net 驱动（联动显示机架槽号）。</summary>
    public bool IsS7Selected => DriverOptions.Any(d => d.Key == "S7Net" && d.IsSelected);

    /// <summary>是否生成仿真驱动。</summary>
    public bool EnableSimulationDriver
    {
        get => _enableSimulationDriver;
        set => SetProperty(ref _enableSimulationDriver, value);
    }

    /// <summary>默认 PLC IP 地址。</summary>
    public string DefaultPLCIp
    {
        get => _defaultPLCIp;
        set => SetProperty(ref _defaultPLCIp, value);
    }

    /// <summary>默认 PLC 端口。</summary>
    public int DefaultPLCPort
    {
        get => _defaultPLCPort;
        set => SetProperty(ref _defaultPLCPort, value);
    }

    /// <summary>S7 协议机架号。</summary>
    public int S7Rack
    {
        get => _s7Rack;
        set => SetProperty(ref _s7Rack, value);
    }

    /// <summary>S7 协议槽号。</summary>
    public int S7Slot
    {
        get => _s7Slot;
        set => SetProperty(ref _s7Slot, value);
    }

    /// <summary>OPC UA 服务端端点 URL。</summary>
    public string OpcUaEndpoint
    {
        get => _opcUaEndpoint;
        set => SetProperty(ref _opcUaEndpoint, value);
    }

    // ── 视觉类属性 ────────────────────────────────────────────────────────────

    /// <summary>相机品牌列表。</summary>
    public List<string> CameraBrands { get; }

    /// <summary>模型类型列表。</summary>
    public List<string> ModelTypes { get; }

    /// <summary>选中的相机品牌。</summary>
    public string CameraBrand
    {
        get => _cameraBrand;
        set => SetProperty(ref _cameraBrand, value);
    }

    /// <summary>选中的模型类型。</summary>
    public string ModelType
    {
        get => _modelType;
        set => SetProperty(ref _modelType, value);
    }

    /// <summary>推理模型文件路径。</summary>
    public string ModelPath
    {
        get => _modelPath;
        set => SetProperty(ref _modelPath, value);
    }

    /// <summary>是否启用图像处理 Pipeline。</summary>
    public bool EnablePipeline
    {
        get => _enablePipeline;
        set => SetProperty(ref _enablePipeline, value);
    }

    // ── 系统类属性 ────────────────────────────────────────────────────────────

    /// <summary>功能模块选项列表（CheckBox 绑定）。</summary>
    public List<ModuleOption> ModuleOptions { get; }

    /// <summary>是否生成独立登录窗口。</summary>
    public bool EnableLoginWindow
    {
        get => _enableLoginWindow;
        set => SetProperty(ref _enableLoginWindow, value);
    }

    /// <summary>是否强制首次登录修改密码。</summary>
    public bool ForcePasswordChange
    {
        get => _forcePasswordChange;
        set => SetProperty(ref _forcePasswordChange, value);
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
        => DriverOptions.Where(d => d.IsSelected).Select(d => d.Key).ToList();

    /// <summary>
    /// 获取已选功能模块的 Key 列表。
    /// </summary>
    /// <returns>已选模块标识符列表。</returns>
    public List<string> GetSelectedModules()
        => ModuleOptions.Where(m => m.IsSelected).Select(m => m.Key).ToList();

    /// <summary>
    /// 重置所有配置到默认值（新建项目时调用）。
    /// </summary>
    public void Reset()
    {
        ProjectType = string.Empty;
        EnableSimulationDriver = true;
        DefaultPLCIp = "192.168.1.1";
        DefaultPLCPort = 102;
        S7Rack = 0;
        S7Slot = 1;
        OpcUaEndpoint = "opc.tcp://localhost:4840";
        CameraBrand = "海康";
        ModelType = "Classification";
        ModelPath = string.Empty;
        EnablePipeline = true;
        EnableLoginWindow = true;
        ForcePasswordChange = false;

        foreach (var d in DriverOptions) d.IsSelected = false;
        foreach (var m in ModuleOptions)
            m.IsSelected = m.Key is "UserManagement" or "RolePermission";
    }
}
