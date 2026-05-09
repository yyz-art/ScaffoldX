using ScaffoldX.Abstractions.Config;
using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step2VisibilityIntegrationTests
{
    private readonly IValidationService _validationService = new ValidationService();

    [Fact]
    public void SetProjectType_Collection_PLC和相机可见性正确()
    {
        var vm = new Step2DeviceConfigViewModel(_validationService);
        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.True(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_Vision_PLC和相机可见性正确()
    {
        var vm = new Step2DeviceConfigViewModel(_validationService);
        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.False(vm.IsCollectionVisible);
        Assert.True(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_System_PLC和相机可见性正确()
    {
        var vm = new Step2DeviceConfigViewModel(_validationService);
        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.False(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.True(vm.IsSystemVisible);
    }
}

public class Step3VisibilityIntegrationTests
{
    [Fact]
    public void SetProjectType_Collection_可见性正确()
    {
        var vm = new Step3FunctionConfigViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.True(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_Vision_可见性正确()
    {
        var vm = new Step3FunctionConfigViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.False(vm.IsCollectionVisible);
        Assert.True(vm.IsVisionVisible);
        Assert.False(vm.IsSystemVisible);
    }

    [Fact]
    public void SetProjectType_System_可见性正确()
    {
        var vm = new Step3FunctionConfigViewModel();
        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.False(vm.IsCollectionVisible);
        Assert.False(vm.IsVisionVisible);
        Assert.True(vm.IsSystemVisible);
    }
}

public class Step4VisibilityIntegrationTests
{
    [Fact]
    public void SetProjectType_Collection_所有系统配置可见()
    {
        var vm = new Step4SystemConfigViewModel();
        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.True(vm.IsUserManagementVisible);
        Assert.True(vm.IsLoggingVisible);
        Assert.True(vm.IsThemeVisible);
    }

    [Fact]
    public void SetProjectType_Vision_所有系统配置可见()
    {
        var vm = new Step4SystemConfigViewModel();
        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.True(vm.IsUserManagementVisible);
        Assert.True(vm.IsLoggingVisible);
        Assert.True(vm.IsThemeVisible);
    }

    [Fact]
    public void SetProjectType_System_所有系统配置可见()
    {
        var vm = new Step4SystemConfigViewModel();
        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.True(vm.IsUserManagementVisible);
        Assert.True(vm.IsLoggingVisible);
        Assert.True(vm.IsThemeVisible);
    }
}
