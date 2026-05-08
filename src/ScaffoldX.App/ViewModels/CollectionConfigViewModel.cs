using Prism.Mvvm;
using ScaffoldX.Core.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 采集类配置 ViewModel：PLC IP、S7 机架/槽号、OPC UA 端点、驱动选项。
/// 属性直接读写 ProjectConfig，无需手动字段拷贝。
/// </summary>
public class CollectionConfigViewModel : BindableBase
{
    /// <summary>
    /// 初始化采集类配置，构建驱动选项列表。
    /// </summary>
    public CollectionConfigViewModel() : this(new ProjectConfig()) { }

    /// <summary>
    /// 初始化采集类配置，绑定到指定 ProjectConfig。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public CollectionConfigViewModel(ProjectConfig config)
    {
        Config = config;

        DriverOptions = new List<DriverOption>
        {
            new() { Key = "S7Net",      DisplayName = "西门子 S7（S7Net）",   IsSelected = config.EnableSiemensS7 },
            new() { Key = "ModbusTcp",  DisplayName = "Modbus TCP",           IsSelected = config.EnableModbusTcp },
            new() { Key = "OpcUa",      DisplayName = "OPC UA",               IsSelected = config.EnableOpcUa },
            new() { Key = "Mitsubishi", DisplayName = "三菱 MC 协议",          IsSelected = config.EnableMitsubishiMc },
            new() { Key = "Omron",      DisplayName = "欧姆龙 FINS",           IsSelected = config.EnableOmronFins },
        };

        foreach (var d in DriverOptions)
        {
            d.PropertyChanged += (_, _) =>
            {
                RaisePropertyChanged(nameof(IsS7Selected));
                SyncDriverToConfig(d);
            };
        }
    }

    // ── 属性 ────────────────────────────────────────────────────────────────────

    /// <summary>关联的项目配置对象，所有属性直接读写此对象。</summary>
    public ProjectConfig Config { get; private set; }

    /// <summary>
    /// 初始化或重新绑定到指定 ProjectConfig，并同步 UI 状态。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public void Initialize(ProjectConfig config)
    {
        Config = config;
        SyncDriverOptionsFromConfig();
        RaisePropertyChanged(nameof(EnableSimulationDriver));
        RaisePropertyChanged(nameof(DefaultPLCIp));
        RaisePropertyChanged(nameof(DefaultPLCPort));
        RaisePropertyChanged(nameof(S7Rack));
        RaisePropertyChanged(nameof(S7Slot));
        RaisePropertyChanged(nameof(OpcUaEndpoint));
    }

    /// <summary>驱动选项列表（CheckBox 绑定）。</summary>
    public List<DriverOption> DriverOptions { get; }

    /// <summary>是否选中了 S7Net 驱动（联动显示机架槽号）。</summary>
    public bool IsS7Selected => Config.EnableSiemensS7;

    /// <summary>是否生成仿真驱动。</summary>
    public bool EnableSimulationDriver
    {
        get => Config.EnableSimulationDriver;
        set
        {
            Config.EnableSimulationDriver = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>默认 PLC IP 地址。</summary>
    public string DefaultPLCIp
    {
        get => Config.DefaultPLCIp;
        set
        {
            Config.DefaultPLCIp = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>默认 PLC 端口。</summary>
    public int DefaultPLCPort
    {
        get => Config.DefaultPLCPort;
        set
        {
            Config.DefaultPLCPort = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>S7 协议机架号。</summary>
    public int S7Rack
    {
        get => Config.S7Rack;
        set
        {
            Config.S7Rack = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>S7 协议槽号。</summary>
    public int S7Slot
    {
        get => Config.S7Slot;
        set
        {
            Config.S7Slot = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>OPC UA 服务端端点 URL。</summary>
    public string OpcUaEndpoint
    {
        get => Config.OpcUaEndpoint;
        set
        {
            Config.OpcUaEndpoint = value;
            RaisePropertyChanged();
        }
    }

    // ── 方法 ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取已选驱动的 Key 列表。
    /// </summary>
    public List<string> GetSelectedDrivers()
        => DriverOptions.Where(d => d.IsSelected).Select(d => d.Key).ToList();

    /// <summary>
    /// 重置采集类配置到默认值。
    /// </summary>
    public void Reset()
    {
        EnableSimulationDriver = true;
        DefaultPLCIp = "192.168.1.1";
        DefaultPLCPort = 102;
        S7Rack = 0;
        S7Slot = 1;
        OpcUaEndpoint = "opc.tcp://localhost:4840";
        foreach (var d in DriverOptions) d.IsSelected = false;
    }

    // ── 内部同步 ────────────────────────────────────────────────────────────────

    private void SyncDriverToConfig(DriverOption driver)
    {
        var configKey = driver.Key switch
        {
            "S7Net" => "S7Net",
            "ModbusTcp" => "ModbusTcp",
            "OpcUa" => "OpcUa",
            "Mitsubishi" => "MitsubishiMc",
            "Omron" => "OmronFins",
            _ => null
        };
        if (configKey is not null)
            Config.SetDriver(configKey, driver.IsSelected);
    }

    private void SyncDriverOptionsFromConfig()
    {
        foreach (var d in DriverOptions)
        {
            d.IsSelected = d.Key switch
            {
                "S7Net" => Config.EnableSiemensS7,
                "ModbusTcp" => Config.EnableModbusTcp,
                "OpcUa" => Config.EnableOpcUa,
                "Mitsubishi" => Config.EnableMitsubishiMc,
                "Omron" => Config.EnableOmronFins,
                _ => d.IsSelected
            };
        }
    }
}
