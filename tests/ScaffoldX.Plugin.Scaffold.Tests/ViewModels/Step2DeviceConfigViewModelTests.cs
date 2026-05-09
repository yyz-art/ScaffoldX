using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step2DeviceConfigViewModelTests
{
    private readonly IValidationService _validationService = new ValidationService();

    private Step2DeviceConfigViewModel CreateViewModel()
    {
        return new Step2DeviceConfigViewModel(_validationService);
    }

    #region Constructor & Default Values

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = CreateViewModel();

        // PLC 默认值
        Assert.False(vm.HasSiemensS7);
        Assert.Equal("192.168.1.10", vm.S7Ip);
        Assert.Equal(102, vm.S7Port);
        Assert.Equal(0, vm.S7Rack);
        Assert.Equal(1, vm.S7Slot);

        Assert.False(vm.HasMitsubishiMc);
        Assert.Equal("192.168.1.11", vm.McIp);
        Assert.Equal(5000, vm.McPort);
        Assert.Equal(1, vm.McStation);

        Assert.False(vm.HasModbusTcp);
        Assert.Equal("192.168.1.12", vm.ModbusIp);
        Assert.Equal(502, vm.ModbusPort);
        Assert.Equal(1, vm.ModbusSlaveId);

        Assert.False(vm.HasOpcUa);
        Assert.Equal("opc.tcp://192.168.1.13:4840", vm.OpcUaEndpoint);

        // 相机默认值
        Assert.False(vm.HasHikVision);
        Assert.Equal("192.168.1.100", vm.HikIp);
        Assert.Equal(8000, vm.HikPort);
        Assert.Equal("admin", vm.HikUsername);
        Assert.Equal("admin123", vm.HikPassword);

        Assert.False(vm.HasDaHua);
        Assert.Equal("192.168.1.101", vm.DaHuaIp);
        Assert.Equal(37777, vm.DaHuaPort);
        Assert.Equal("admin", vm.DaHuaUsername);
        Assert.Equal("admin123", vm.DaHuaPassword);
    }

    #endregion

    #region PLC Properties

    [Fact]
    public void HasSiemensS7_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HasSiemensS7 = true;

        Assert.True(vm.HasSiemensS7);
    }

    [Fact]
    public void S7Ip_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.S7Ip = "192.168.0.1";

        Assert.Equal("192.168.0.1", vm.S7Ip);
    }

    [Fact]
    public void S7Port_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.S7Port = 2000;

        Assert.Equal(2000, vm.S7Port);
    }

    [Fact]
    public void S7Rack_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.S7Rack = 1;

        Assert.Equal(1, vm.S7Rack);
    }

    [Fact]
    public void S7Slot_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.S7Slot = 2;

        Assert.Equal(2, vm.S7Slot);
    }

    [Fact]
    public void HasMitsubishiMc_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HasMitsubishiMc = true;

        Assert.True(vm.HasMitsubishiMc);
    }

    [Fact]
    public void McIp_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.McIp = "192.168.0.2";

        Assert.Equal("192.168.0.2", vm.McIp);
    }

    [Fact]
    public void McPort_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.McPort = 6000;

        Assert.Equal(6000, vm.McPort);
    }

    [Fact]
    public void McStation_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.McStation = 2;

        Assert.Equal(2, vm.McStation);
    }

    [Fact]
    public void HasModbusTcp_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HasModbusTcp = true;

        Assert.True(vm.HasModbusTcp);
    }

    [Fact]
    public void ModbusIp_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.ModbusIp = "192.168.0.3";

        Assert.Equal("192.168.0.3", vm.ModbusIp);
    }

    [Fact]
    public void ModbusPort_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.ModbusPort = 8080;

        Assert.Equal(8080, vm.ModbusPort);
    }

    [Fact]
    public void ModbusSlaveId_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.ModbusSlaveId = 5;

        Assert.Equal(5, vm.ModbusSlaveId);
    }

    [Fact]
    public void HasOpcUa_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HasOpcUa = true;

        Assert.True(vm.HasOpcUa);
    }

    [Fact]
    public void OpcUaEndpoint_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.OpcUaEndpoint = "opc.tcp://192.168.0.10:4840";

        Assert.Equal("opc.tcp://192.168.0.10:4840", vm.OpcUaEndpoint);
    }

    #endregion

    #region Camera Properties

    [Fact]
    public void HasHikVision_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HasHikVision = true;

        Assert.True(vm.HasHikVision);
    }

    [Fact]
    public void HikIp_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HikIp = "192.168.0.50";

        Assert.Equal("192.168.0.50", vm.HikIp);
    }

    [Fact]
    public void HikPort_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HikPort = 9000;

        Assert.Equal(9000, vm.HikPort);
    }

    [Fact]
    public void HikUsername_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HikUsername = "user";

        Assert.Equal("user", vm.HikUsername);
    }

    [Fact]
    public void HikPassword_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HikPassword = "password123";

        Assert.Equal("password123", vm.HikPassword);
    }

    [Fact]
    public void HasDaHua_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.HasDaHua = true;

        Assert.True(vm.HasDaHua);
    }

    [Fact]
    public void DaHuaIp_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.DaHuaIp = "192.168.0.51";

        Assert.Equal("192.168.0.51", vm.DaHuaIp);
    }

    [Fact]
    public void DaHuaPort_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.DaHuaPort = 38888;

        Assert.Equal(38888, vm.DaHuaPort);
    }

    [Fact]
    public void DaHuaUsername_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.DaHuaUsername = "operator";

        Assert.Equal("operator", vm.DaHuaUsername);
    }

    [Fact]
    public void DaHuaPassword_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.DaHuaPassword = "securepass";

        Assert.Equal("securepass", vm.DaHuaPassword);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void SetProjectType_Collection_集合可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.True(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_Vision_视觉可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.False(vm.IsCollectionVisible);
        Assert.True(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_System_系统可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.False(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.True(vm.IsSystemVisible);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsValid_Collection无设备_返回False()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.False(vm.IsValid);
    }

    [Fact]
    public void IsValid_Collection有SiemensS7_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);
        vm.HasSiemensS7 = true;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Collection有MitsubishiMc_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);
        vm.HasMitsubishiMc = true;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Collection有ModbusTcp_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);
        vm.HasModbusTcp = true;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Collection有OpcUa_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);
        vm.HasOpcUa = true;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Vision无相机_返回False()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.False(vm.IsValid);
    }

    [Fact]
    public void IsValid_Vision有HikVision_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);
        vm.HasHikVision = true;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_Vision有DaHua_返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);
        vm.HasDaHua = true;

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_System_始终返回True()
    {
        var vm = CreateViewModel();
        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_无项目类型_返回False()
    {
        var vm = CreateViewModel();
        // 不设置项目类型，默认为None

        Assert.False(vm.IsValid);
    }

    #endregion

    #region Property Changed Tests

    [Fact]
    public void PropertyChanged_PLC属性变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.HasSiemensS7 = true;
        vm.S7Ip = "192.168.0.1";
        vm.S7Port = 2000;

        Assert.Contains(nameof(vm.HasSiemensS7), propertyChangedEvents);
        Assert.Contains(nameof(vm.S7Ip), propertyChangedEvents);
        Assert.Contains(nameof(vm.S7Port), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_相机属性变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.HasHikVision = true;
        vm.HikIp = "192.168.0.50";
        vm.HikPort = 9000;

        Assert.Contains(nameof(vm.HasHikVision), propertyChangedEvents);
        Assert.Contains(nameof(vm.HikIp), propertyChangedEvents);
        Assert.Contains(nameof(vm.HikPort), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_SetProjectType_触发可见性通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.Contains(nameof(vm.IsCollectionVisible), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsVisionVisible), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsSystemVisible), propertyChangedEvents);
    }

    #endregion
}
