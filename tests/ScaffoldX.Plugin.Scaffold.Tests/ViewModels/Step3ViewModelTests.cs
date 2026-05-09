using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step3ViewModelTests
{
    private Step3ViewModel CreateViewModel()
    {
        return new Step3ViewModel();
    }

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = CreateViewModel();

        Assert.False(vm.HasSiemensS7);
        Assert.False(vm.HasMitsubishiQ);
        Assert.False(vm.HasModbusTcp);
        Assert.False(vm.HasOpcUa);
        Assert.False(vm.HasHikVision);
        Assert.False(vm.HasDaHua);
        Assert.False(vm.HasYoloDetection);
        Assert.False(vm.HasSam3Segmentation);
        Assert.False(vm.HasUserManagement);
        Assert.False(vm.HasThemeSwitcher);
    }

    [Fact]
    public void HasAnyDriver_无驱动_返回False()
    {
        var vm = CreateViewModel();
        Assert.False(vm.HasAnyDriver);
    }

    [Fact]
    public void HasAnyDriver_有西门子S7_返回True()
    {
        var vm = CreateViewModel();
        vm.HasSiemensS7 = true;
        Assert.True(vm.HasAnyDriver);
    }

    [Fact]
    public void HasAnyDriver_有三菱MC_返回True()
    {
        var vm = CreateViewModel();
        vm.HasMitsubishiQ = true;
        Assert.True(vm.HasAnyDriver);
    }

    [Fact]
    public void HasAnyDriver_有ModbusTcp_返回True()
    {
        var vm = CreateViewModel();
        vm.HasModbusTcp = true;
        Assert.True(vm.HasAnyDriver);
    }

    [Fact]
    public void HasAnyDriver_有OpcUa_返回True()
    {
        var vm = CreateViewModel();
        vm.HasOpcUa = true;
        Assert.True(vm.HasAnyDriver);
    }

    [Fact]
    public void HasAnyVision_无视觉模块_返回False()
    {
        var vm = CreateViewModel();
        Assert.False(vm.HasAnyVision);
    }

    [Fact]
    public void HasAnyVision_有海康相机_返回True()
    {
        var vm = CreateViewModel();
        vm.HasHikVision = true;
        Assert.True(vm.HasAnyVision);
    }

    [Fact]
    public void HasAnyVision_有大华相机_返回True()
    {
        var vm = CreateViewModel();
        vm.HasDaHua = true;
        Assert.True(vm.HasAnyVision);
    }

    [Fact]
    public void HasAnyVision_有YOLO检测_返回True()
    {
        var vm = CreateViewModel();
        vm.HasYoloDetection = true;
        Assert.True(vm.HasAnyVision);
    }

    [Fact]
    public void HasAnyVision_有SAM3分割_返回True()
    {
        var vm = CreateViewModel();
        vm.HasSam3Segmentation = true;
        Assert.True(vm.HasAnyVision);
    }

    [Fact]
    public void HasAnySystemModule_无系统模块_返回False()
    {
        var vm = CreateViewModel();
        Assert.False(vm.HasAnySystemModule);
    }

    [Fact]
    public void HasAnySystemModule_有用户管理_返回True()
    {
        var vm = CreateViewModel();
        vm.HasUserManagement = true;
        Assert.True(vm.HasAnySystemModule);
    }

    [Fact]
    public void HasAnySystemModule_有主题切换_返回True()
    {
        var vm = CreateViewModel();
        vm.HasThemeSwitcher = true;
        Assert.True(vm.HasAnySystemModule);
    }

    [Fact]
    public void 属性变更通知_正确触发()
    {
        var vm = CreateViewModel();
        var propertyChangedCount = 0;
        vm.PropertyChanged += (_, _) => propertyChangedCount++;

        vm.HasSiemensS7 = true;
        vm.HasHikVision = true;
        vm.HasUserManagement = true;

        Assert.True(propertyChangedCount >= 3);
    }
}
