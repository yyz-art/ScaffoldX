using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ScaffoldX.Plugin.Scaffold.Services;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public sealed class Step2DeviceConfigViewModel : INotifyPropertyChanged
{
    private readonly IValidationService _validationService;
    private ProjectTypeCategory _projectType;

    // PLC 配置
    private bool _hasSiemensS7;
    private string _s7Ip = "192.168.1.10";
    private int _s7Port = 102;
    private int _s7Rack;
    private int _s7Slot = 1;

    private bool _hasMitsubishiMc;
    private string _mcIp = "192.168.1.11";
    private int _mcPort = 5000;
    private int _mcStation = 1;

    private bool _hasModbusTcp;
    private string _modbusIp = "192.168.1.12";
    private int _modbusPort = 502;
    private int _modbusSlaveId = 1;

    private bool _hasOpcUa;
    private string _opcUaEndpoint = "opc.tcp://192.168.1.13:4840";

    // 相机配置
    private bool _hasHikVision;
    private string _hikIp = "192.168.1.100";
    private int _hikPort = 8000;
    private string _hikUsername = "admin";
    private string _hikPassword = "admin123";

    private bool _hasDaHua;
    private string _daHuaIp = "192.168.1.101";
    private int _daHuaPort = 37777;
    private string _daHuaUsername = "admin";
    private string _daHuaPassword = "admin123";

    public Step2DeviceConfigViewModel(IValidationService validationService)
    {
        _validationService = validationService;
    }

    public void SetProjectType(ProjectTypeCategory type)
    {
        _projectType = type;
        OnPropertyChanged(nameof(IsCollectionVisible));
        OnPropertyChanged(nameof(IsVisionVisible));
        OnPropertyChanged(nameof(IsSystemVisible));
    }

    // Visibility
    public bool IsCollectionVisible => _projectType == ProjectTypeCategory.Collection;
    public bool IsVisionVisible => _projectType == ProjectTypeCategory.Vision;
    public bool IsSystemVisible => _projectType == ProjectTypeCategory.System;

    #region PLC Properties

    public bool HasSiemensS7
    {
        get => _hasSiemensS7;
        set { _hasSiemensS7 = value; OnPropertyChanged(); }
    }

    public string S7Ip
    {
        get => _s7Ip;
        set { _s7Ip = value; OnPropertyChanged(); }
    }

    public int S7Port
    {
        get => _s7Port;
        set { _s7Port = value; OnPropertyChanged(); }
    }

    public int S7Rack
    {
        get => _s7Rack;
        set { _s7Rack = value; OnPropertyChanged(); }
    }

    public int S7Slot
    {
        get => _s7Slot;
        set { _s7Slot = value; OnPropertyChanged(); }
    }

    public bool HasMitsubishiMc
    {
        get => _hasMitsubishiMc;
        set { _hasMitsubishiMc = value; OnPropertyChanged(); }
    }

    public string McIp
    {
        get => _mcIp;
        set { _mcIp = value; OnPropertyChanged(); }
    }

    public int McPort
    {
        get => _mcPort;
        set { _mcPort = value; OnPropertyChanged(); }
    }

    public int McStation
    {
        get => _mcStation;
        set { _mcStation = value; OnPropertyChanged(); }
    }

    public bool HasModbusTcp
    {
        get => _hasModbusTcp;
        set { _hasModbusTcp = value; OnPropertyChanged(); }
    }

    public string ModbusIp
    {
        get => _modbusIp;
        set { _modbusIp = value; OnPropertyChanged(); }
    }

    public int ModbusPort
    {
        get => _modbusPort;
        set { _modbusPort = value; OnPropertyChanged(); }
    }

    public int ModbusSlaveId
    {
        get => _modbusSlaveId;
        set { _modbusSlaveId = value; OnPropertyChanged(); }
    }

    public bool HasOpcUa
    {
        get => _hasOpcUa;
        set { _hasOpcUa = value; OnPropertyChanged(); }
    }

    public string OpcUaEndpoint
    {
        get => _opcUaEndpoint;
        set { _opcUaEndpoint = value; OnPropertyChanged(); }
    }

    #endregion

    #region Camera Properties

    public bool HasHikVision
    {
        get => _hasHikVision;
        set { _hasHikVision = value; OnPropertyChanged(); }
    }

    public string HikIp
    {
        get => _hikIp;
        set { _hikIp = value; OnPropertyChanged(); }
    }

    public int HikPort
    {
        get => _hikPort;
        set { _hikPort = value; OnPropertyChanged(); }
    }

    public string HikUsername
    {
        get => _hikUsername;
        set { _hikUsername = value; OnPropertyChanged(); }
    }

    public string HikPassword
    {
        get => _hikPassword;
        set { _hikPassword = value; OnPropertyChanged(); }
    }

    public bool HasDaHua
    {
        get => _hasDaHua;
        set { _hasDaHua = value; OnPropertyChanged(); }
    }

    public string DaHuaIp
    {
        get => _daHuaIp;
        set { _daHuaIp = value; OnPropertyChanged(); }
    }

    public int DaHuaPort
    {
        get => _daHuaPort;
        set { _daHuaPort = value; OnPropertyChanged(); }
    }

    public string DaHuaUsername
    {
        get => _daHuaUsername;
        set { _daHuaUsername = value; OnPropertyChanged(); }
    }

    public string DaHuaPassword
    {
        get => _daHuaPassword;
        set { _daHuaPassword = value; OnPropertyChanged(); }
    }

    #endregion

    public bool IsValid => _projectType switch
    {
        ProjectTypeCategory.Collection => HasSiemensS7 || HasMitsubishiMc || HasModbusTcp || HasOpcUa,
        ProjectTypeCategory.Vision => HasHikVision || HasDaHua,
        ProjectTypeCategory.System => true, // 系统类型不需要设备配置
        _ => false
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
