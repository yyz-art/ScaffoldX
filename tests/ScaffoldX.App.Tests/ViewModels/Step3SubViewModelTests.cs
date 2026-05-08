using FluentAssertions;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// 验证 Step3ViewModel 拆分后的三个子 ViewModel 存在且委托正确。
/// </summary>
public class Step3SubViewModelTests
{
    // ── 子 VM 存在性 ───────────────────────────────────────────────────────────

    [Fact]
    public void Step3ViewModel_ShouldExposeCollectionSubViewModel()
    {
        var vm = new Step3ViewModel();
        vm.Collection.Should().NotBeNull();
        vm.Collection.Should().BeOfType<CollectionConfigViewModel>();
    }

    [Fact]
    public void Step3ViewModel_ShouldExposeVisionSubViewModel()
    {
        var vm = new Step3ViewModel();
        vm.Vision.Should().NotBeNull();
        vm.Vision.Should().BeOfType<VisionConfigViewModel>();
    }

    [Fact]
    public void Step3ViewModel_ShouldExposeSystemSubViewModel()
    {
        var vm = new Step3ViewModel();
        vm.System.Should().NotBeNull();
        vm.System.Should().BeOfType<SystemConfigViewModel>();
    }

    // ── 采集类委托 ─────────────────────────────────────────────────────────────

    [Fact]
    public void DriverOptions_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.DriverOptions.Should().BeSameAs(vm.Collection.DriverOptions);
    }

    [Fact]
    public void EnableSimulationDriver_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.EnableSimulationDriver = false;
        vm.Collection.EnableSimulationDriver.Should().BeFalse();
        vm.EnableSimulationDriver.Should().BeFalse();
    }

    [Fact]
    public void DefaultPLCIp_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.DefaultPLCIp = "10.0.0.1";
        vm.Collection.DefaultPLCIp.Should().Be("10.0.0.1");
    }

    [Fact]
    public void DefaultPLCPort_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.DefaultPLCPort = 502;
        vm.Collection.DefaultPLCPort.Should().Be(502);
    }

    [Fact]
    public void S7Rack_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.S7Rack = 2;
        vm.Collection.S7Rack.Should().Be(2);
    }

    [Fact]
    public void S7Slot_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.S7Slot = 3;
        vm.Collection.S7Slot.Should().Be(3);
    }

    [Fact]
    public void OpcUaEndpoint_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.OpcUaEndpoint = "opc.tcp://remote:4840";
        vm.Collection.OpcUaEndpoint.Should().Be("opc.tcp://remote:4840");
    }

    [Fact]
    public void IsS7Selected_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.IsS7Selected.Should().BeFalse();
        vm.DriverOptions.First(d => d.Key == "S7Net").IsSelected = true;
        vm.IsS7Selected.Should().BeTrue();
    }

    [Fact]
    public void GetSelectedDrivers_ShouldDelegateToCollection()
    {
        var vm = new Step3ViewModel();
        vm.DriverOptions.First(d => d.Key == "S7Net").IsSelected = true;
        vm.GetSelectedDrivers().Should().Contain("S7Net");
    }

    // ── 视觉类委托 ─────────────────────────────────────────────────────────────

    [Fact]
    public void CameraBrands_ShouldDelegateToVision()
    {
        var vm = new Step3ViewModel();
        vm.CameraBrands.Should().BeSameAs(vm.Vision.CameraBrands);
    }

    [Fact]
    public void ModelTypes_ShouldDelegateToVision()
    {
        var vm = new Step3ViewModel();
        vm.ModelTypes.Should().BeSameAs(vm.Vision.ModelTypes);
    }

    [Fact]
    public void CameraBrand_ShouldDelegateToVision()
    {
        var vm = new Step3ViewModel();
        vm.CameraBrand = "Basler";
        vm.Vision.CameraBrand.Should().Be("Basler");
    }

    [Fact]
    public void ModelType_ShouldDelegateToVision()
    {
        var vm = new Step3ViewModel();
        vm.ModelType = "Detection";
        vm.Vision.ModelType.Should().Be("Detection");
    }

    [Fact]
    public void ModelPath_ShouldDelegateToVision()
    {
        var vm = new Step3ViewModel();
        vm.ModelPath = "/models/yolo.onnx";
        vm.Vision.ModelPath.Should().Be("/models/yolo.onnx");
    }

    [Fact]
    public void EnablePipeline_ShouldDelegateToVision()
    {
        var vm = new Step3ViewModel();
        vm.EnablePipeline = false;
        vm.Vision.EnablePipeline.Should().BeFalse();
    }

    // ── 系统类委托 ─────────────────────────────────────────────────────────────

    [Fact]
    public void ModuleOptions_ShouldDelegateToSystem()
    {
        var vm = new Step3ViewModel();
        vm.ModuleOptions.Should().BeSameAs(vm.System.ModuleOptions);
    }

    [Fact]
    public void EnableLoginWindow_ShouldDelegateToSystem()
    {
        var vm = new Step3ViewModel();
        vm.EnableLoginWindow = false;
        vm.System.EnableLoginWindow.Should().BeFalse();
    }

    [Fact]
    public void ForcePasswordChange_ShouldDelegateToSystem()
    {
        var vm = new Step3ViewModel();
        vm.ForcePasswordChange = true;
        vm.System.ForcePasswordChange.Should().BeTrue();
    }

    [Fact]
    public void GetSelectedModules_ShouldDelegateToSystem()
    {
        var vm = new Step3ViewModel();
        vm.GetSelectedModules().Should().Contain("UserManagement");
    }

    // ── Reset 委托 ─────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ShouldDelegateToAllSubViewModels()
    {
        var vm = new Step3ViewModel();
        vm.DefaultPLCIp = "10.0.0.1";
        vm.CameraBrand = "Basler";
        vm.EnableLoginWindow = false;

        vm.Reset();

        vm.Collection.DefaultPLCIp.Should().Be("192.168.1.1");
        vm.Vision.CameraBrand.Should().Be("海康");
        vm.System.EnableLoginWindow.Should().BeTrue();
    }

    [Fact]
    public void Reset_ShouldClearProjectType()
    {
        var vm = new Step3ViewModel();
        vm.ApplyProjectType("Collection");
        vm.Reset();
        vm.ProjectType.Should().BeEmpty();
    }

    // ── 子 VM 独立测试 ─────────────────────────────────────────────────────────

    [Fact]
    public void CollectionConfigViewModel_DefaultValues()
    {
        var vm = new CollectionConfigViewModel();
        vm.DefaultPLCIp.Should().Be("192.168.1.1");
        vm.DefaultPLCPort.Should().Be(102);
        vm.S7Rack.Should().Be(0);
        vm.S7Slot.Should().Be(1);
        vm.OpcUaEndpoint.Should().Be("opc.tcp://localhost:4840");
        vm.EnableSimulationDriver.Should().BeTrue();
        vm.DriverOptions.Should().HaveCount(5);
    }

    [Fact]
    public void VisionConfigViewModel_DefaultValues()
    {
        var vm = new VisionConfigViewModel();
        vm.CameraBrand.Should().Be("海康");
        vm.ModelType.Should().Be("Classification");
        vm.ModelPath.Should().BeEmpty();
        vm.EnablePipeline.Should().BeTrue();
        vm.CameraBrands.Should().HaveCount(4);
        vm.ModelTypes.Should().HaveCount(3);
    }

    [Fact]
    public void SystemConfigViewModel_DefaultValues()
    {
        var vm = new SystemConfigViewModel();
        vm.EnableLoginWindow.Should().BeTrue();
        vm.ForcePasswordChange.Should().BeFalse();
        vm.ModuleOptions.Should().HaveCount(4);
    }

    [Fact]
    public void CollectionConfigViewModel_GetSelectedDrivers_EmptyByDefault()
    {
        var vm = new CollectionConfigViewModel();
        vm.GetSelectedDrivers().Should().BeEmpty();
    }

    [Fact]
    public void SystemConfigViewModel_GetSelectedModules_ShouldContainDefaults()
    {
        var vm = new SystemConfigViewModel();
        var modules = vm.GetSelectedModules();
        modules.Should().Contain("UserManagement");
        modules.Should().Contain("RolePermission");
        modules.Should().NotContain("SystemLog");
    }
}
