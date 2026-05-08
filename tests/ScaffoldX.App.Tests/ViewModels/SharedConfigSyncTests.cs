using FluentAssertions;
using ScaffoldX.App.ViewModels;
using ScaffoldX.Core.Models;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// 验证各步骤 ViewModel 直接读写共享 ProjectConfig，无需手动字段同步。
/// </summary>
public class SharedConfigSyncTests
{
    // ── Step1：SelectedProjectType 直接写入 SharedConfig ────────────────────────

    [Fact]
    public void Step1_SelectedProjectType_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step1ViewModel(config);

        vm.SelectTypeCommand.Execute("Vision");

        config.ProjectType.Should().Be("Vision");
        vm.SelectedProjectType.Should().Be("Vision");
    }

    [Fact]
    public void Step1_Reset_ClearsConfig()
    {
        var config = new ProjectConfig { ProjectType = "Collection" };
        var vm = new Step1ViewModel(config);

        vm.Reset();

        config.ProjectType.Should().BeEmpty();
        vm.SelectedProjectType.Should().BeEmpty();
    }

    // ── Step2：所有字段直接写入 SharedConfig ────────────────────────────────────

    [Fact]
    public void Step2_ProjectName_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step2ViewModel(new FakeValidationService(), config);

        vm.ProjectName = "MyProject";

        config.ProjectName.Should().Be("MyProject");
    }

    [Fact]
    public void Step2_OutputPath_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step2ViewModel(new FakeValidationService(), config);

        vm.OutputPath = @"C:\Projects";

        config.OutputDirectory.Should().Be(@"C:\Projects");
    }

    [Fact]
    public void Step2_UIFramework_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step2ViewModel(new FakeValidationService(), config);

        vm.UIFramework = "Avalonia";

        config.UIFramework.Should().Be("Avalonia");
    }

    [Fact]
    public void Step2_DotNetVersion_SetsTargetFramework()
    {
        var config = new ProjectConfig { UIFramework = "WPF" };
        var vm = new Step2ViewModel(new FakeValidationService(), config);

        vm.DotNetVersion = ".NET 6";

        config.TargetFramework.Should().Be("net6.0-windows");
    }

    [Fact]
    public void Step2_Description_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step2ViewModel(new FakeValidationService(), config);

        vm.ProjectDescription = "Test description";

        config.Description.Should().Be("Test description");
    }

    [Fact]
    public void Step2_Reset_ClearsConfig()
    {
        var config = new ProjectConfig
        {
            ProjectName = "Old",
            OutputDirectory = @"C:\Old",
            NamespacePrefix = "Old",
            UIFramework = "Avalonia"
        };
        var vm = new Step2ViewModel(new FakeValidationService(), config);

        vm.Reset();

        config.ProjectName.Should().BeEmpty();
        config.OutputDirectory.Should().BeEmpty();
        config.NamespacePrefix.Should().BeEmpty();
        config.UIFramework.Should().Be("WPF");
    }

    // ── Step3：所有子 VM 直接写入 SharedConfig ─────────────────────────────────

    [Fact]
    public void Step3_Collection_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step3ViewModel(config);

        vm.DefaultPLCIp = "10.0.0.1";
        vm.DefaultPLCPort = 502;
        vm.S7Rack = 2;
        vm.S7Slot = 3;
        vm.OpcUaEndpoint = "opc.tcp://remote:4840";
        vm.EnableSimulationDriver = false;

        config.DefaultPLCIp.Should().Be("10.0.0.1");
        config.DefaultPLCPort.Should().Be(502);
        config.S7Rack.Should().Be(2);
        config.S7Slot.Should().Be(3);
        config.OpcUaEndpoint.Should().Be("opc.tcp://remote:4840");
        config.EnableSimulationDriver.Should().BeFalse();
    }

    [Fact]
    public void Step3_DriverOptions_SyncToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step3ViewModel(config);

        vm.DriverOptions.First(d => d.Key == "S7Net").IsSelected = true;
        vm.DriverOptions.First(d => d.Key == "ModbusTcp").IsSelected = true;

        config.EnableSiemensS7.Should().BeTrue();
        config.EnableModbusTcp.Should().BeTrue();
        config.EnableOpcUa.Should().BeFalse();
    }

    [Fact]
    public void Step3_Vision_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step3ViewModel(config);

        vm.CameraBrand = "Basler";
        vm.ModelType = "Detection";
        vm.ModelPath = "/models/yolo.onnx";
        vm.EnablePipeline = false;

        config.CameraBrand.Should().Be("Basler");
        config.ModelType.Should().Be("Detection");
        config.ModelPath.Should().Be("/models/yolo.onnx");
        config.EnablePipeline.Should().BeFalse();
    }

    [Fact]
    public void Step3_System_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step3ViewModel(config);

        vm.EnableLoginWindow = false;
        vm.ForcePasswordChange = true;

        config.EnableLoginWindow.Should().BeFalse();
        config.ForcePasswordChange.Should().BeTrue();
    }

    [Fact]
    public void Step3_ModuleOptions_SyncToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step3ViewModel(config);

        vm.ModuleOptions.First(m => m.Key == "SystemLog").IsSelected = true;
        vm.ModuleOptions.First(m => m.Key == "ThemeSwitcher").IsSelected = true;

        config.EnableSystemLog.Should().BeTrue();
        config.EnableThemeSwitcher.Should().BeTrue();
    }

    [Fact]
    public void Step3_NavigationStyle_WritesDirectlyToConfig()
    {
        var config = new ProjectConfig();
        var vm = new Step3ViewModel(config);

        vm.NavigationStyle = "TopNav";
        vm.DefaultTheme = "LightModern";
        vm.DefaultLanguage = "en-US";

        config.NavigationStyle.Should().Be("TopNav");
        config.DefaultTheme.Should().Be("LightModern");
        config.DefaultLanguage.Should().Be("en-US");
    }

    // ── 端到端：共享配置在所有步骤间一致 ────────────────────────────────────────

    [Fact]
    public void SharedConfig_AllStepsReadWriteSameInstance()
    {
        var config = new ProjectConfig();
        var step1 = new Step1ViewModel(config);
        var step2 = new Step2ViewModel(new FakeValidationService(), config);
        var step3 = new Step3ViewModel(config);

        // Step1 选择类型
        step1.SelectTypeCommand.Execute("Collection");

        // Step2 填写基础信息
        step2.ProjectName = "TestApp";
        step2.OutputPath = @"C:\Output";
        step2.UIFramework = "WPF";
        step2.DotNetVersion = ".NET 8";

        // Step3 配置驱动
        step3.DriverOptions.First(d => d.Key == "S7Net").IsSelected = true;
        step3.DefaultPLCIp = "10.0.0.1";

        // 验证所有写入都到达同一个 config
        config.ProjectType.Should().Be("Collection");
        config.ProjectName.Should().Be("TestApp");
        config.OutputDirectory.Should().Be(@"C:\Output");
        config.UIFramework.Should().Be("WPF");
        config.TargetFramework.Should().Be("net8.0-windows");
        config.EnableSiemensS7.Should().BeTrue();
        config.DefaultPLCIp.Should().Be("10.0.0.1");
    }

    [Fact]
    public void Step3_Initialize_RebindsToNewConfig()
    {
        var config1 = new ProjectConfig { CameraBrand = "海康" };
        var config2 = new ProjectConfig { CameraBrand = "Basler" };
        var vm = new Step3ViewModel(config1);

        vm.CameraBrand.Should().Be("海康");

        vm.Initialize(config2);

        vm.CameraBrand.Should().Be("Basler");
        vm.Vision.CameraBrand.Should().Be("Basler");

        // 修改应写入新 config
        vm.CameraBrand = "Cognex";
        config2.CameraBrand.Should().Be("Cognex");
        config1.CameraBrand.Should().Be("海康");
    }
}

/// <summary>
/// 最小化验证服务实现，用于测试 Step2ViewModel。
/// </summary>
internal class FakeValidationService : ScaffoldX.App.Services.IValidationService
{
    public ScaffoldX.App.Services.ValidationResult ValidateProjectName(string name)
        => new(true);

    public ScaffoldX.App.Services.ValidationResult ValidateOutputPath(string path, string projectName)
        => new(true);

    public ScaffoldX.App.Services.ValidationResult ValidateIpAddress(string ip)
        => new(true);

    public ScaffoldX.App.Services.ValidationResult ValidatePort(int port)
        => new(true);

    public string ToPascalCase(string input)
        => input;
}
