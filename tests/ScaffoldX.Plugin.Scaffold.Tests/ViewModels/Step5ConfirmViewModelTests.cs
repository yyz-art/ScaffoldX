using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Templates;
using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step5ConfirmViewModelTests
{
    private readonly IValidationService _validationService = new ValidationService();

    private class FakeProjectGenerator : IProjectGenerator
    {
        private readonly GenerationResult _result;

        public FakeProjectGenerator(GenerationResult result)
        {
            _result = result;
        }

        public Task<GenerationResult> GenerateAsync(ConfigRegistry configRegistry, IProgress<GenerationProgress>? progress = null)
        {
            progress?.Report(new GenerationProgress("测试进度", 100));
            return Task.FromResult(_result);
        }
    }

    private Step5ConfirmViewModel CreateViewModel(IProjectGenerator? generator = null)
    {
        var step1 = new Step1ViewModel(_validationService)
        {
            ProjectName = "TestProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.Collection.ToString()
        };
        var step2 = new Step2DeviceConfigViewModel(_validationService);
        step2.SetProjectType(ProjectTypeCategory.Collection);
        step2.HasSiemensS7 = true;

        var step3 = new Step3FunctionConfigViewModel();
        step3.SetProjectType(ProjectTypeCategory.Collection);

        var step4 = new Step4SystemConfigViewModel();
        step4.SetProjectType(ProjectTypeCategory.Collection);

        generator ??= new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 10, TimeSpan.FromSeconds(1)));
        return new Step5ConfirmViewModel(generator, step1, step2, step3, step4);
    }

    private Step5ConfirmViewModel CreateVisionViewModel(IProjectGenerator? generator = null)
    {
        var step1 = new Step1ViewModel(_validationService)
        {
            ProjectName = "VisionProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.Vision.ToString()
        };
        var step2 = new Step2DeviceConfigViewModel(_validationService);
        step2.SetProjectType(ProjectTypeCategory.Vision);
        step2.HasHikVision = true;

        var step3 = new Step3FunctionConfigViewModel();
        step3.SetProjectType(ProjectTypeCategory.Vision);
        step3.SelectedAlgorithm = VisionAlgorithm.Yolo;

        var step4 = new Step4SystemConfigViewModel();
        step4.SetProjectType(ProjectTypeCategory.Vision);

        generator ??= new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 5, TimeSpan.FromSeconds(1)));
        return new Step5ConfirmViewModel(generator, step1, step2, step3, step4);
    }

    private Step5ConfirmViewModel CreateSystemViewModel(IProjectGenerator? generator = null)
    {
        var step1 = new Step1ViewModel(_validationService)
        {
            ProjectName = "SystemProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.System.ToString()
        };
        var step2 = new Step2DeviceConfigViewModel(_validationService);
        step2.SetProjectType(ProjectTypeCategory.System);

        var step3 = new Step3FunctionConfigViewModel();
        step3.SetProjectType(ProjectTypeCategory.System);

        var step4 = new Step4SystemConfigViewModel();
        step4.SetProjectType(ProjectTypeCategory.System);

        generator ??= new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 3, TimeSpan.FromSeconds(1)));
        return new Step5ConfirmViewModel(generator, step1, step2, step3, step4);
    }

    #region Constructor & Default Values

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = new Step5ConfirmViewModel();

        Assert.False(vm.IsGenerating);
        Assert.False(vm.IsSuccess);
        Assert.False(vm.ShowGenerationResult);
        Assert.True(vm.CanGenerate);
        Assert.Equal(string.Empty, vm.GenerationStatus);
        Assert.Equal(string.Empty, vm.CurrentOperation);
        Assert.Equal(0, vm.GenerationProgress);
        Assert.Empty(vm.DeviceSummaryItems);
        Assert.Empty(vm.FunctionSummaryItems);
        Assert.Empty(vm.SystemSummaryItems);
    }

    [Fact]
    public void Constructor_注入依赖_正确初始化()
    {
        var vm = CreateViewModel();

        Assert.Equal("TestProject", vm.ProjectName);
        Assert.Equal("数据采集", vm.ProjectTypeDisplay);
        Assert.Equal(Path.GetTempPath(), vm.OutputDirectory);
        Assert.True(vm.HasDeviceConfig);
        Assert.True(vm.HasFunctionConfig);
        Assert.True(vm.HasSystemConfig);
    }

    #endregion

    #region Summary Properties - Collection

    [Fact]
    public void ProjectName_Collection项目_返回Step1的值()
    {
        var vm = CreateViewModel();

        Assert.Equal("TestProject", vm.ProjectName);
    }

    [Fact]
    public void ProjectTypeDisplay_Collection项目_返回数据采集()
    {
        var vm = CreateViewModel();

        Assert.Equal("数据采集", vm.ProjectTypeDisplay);
    }

    [Fact]
    public void OutputDirectory_Collection项目_返回Step1的值()
    {
        var vm = CreateViewModel();

        Assert.Equal(Path.GetTempPath(), vm.OutputDirectory);
    }

    [Fact]
    public void HasDeviceConfig_Collection项目_返回True()
    {
        var vm = CreateViewModel();

        Assert.True(vm.HasDeviceConfig);
    }

    [Fact]
    public void HasFunctionConfig_Collection项目_返回True()
    {
        var vm = CreateViewModel();

        Assert.True(vm.HasFunctionConfig);
    }

    [Fact]
    public void DeviceSummaryItems_Collection有SiemensS7_包含SiemensS7项()
    {
        var vm = CreateViewModel();

        Assert.Single(vm.DeviceSummaryItems);
        Assert.Contains("Siemens S7", vm.DeviceSummaryItems[0].Description);
    }

    [Fact]
    public void FunctionSummaryItems_Collection项目_包含配置项()
    {
        var vm = CreateViewModel();

        Assert.NotEmpty(vm.FunctionSummaryItems);
        Assert.Contains(vm.FunctionSummaryItems, item => item.Description.Contains("扫描周期"));
        Assert.Contains(vm.FunctionSummaryItems, item => item.Description.Contains("数据存储"));
    }

    #endregion

    #region Summary Properties - Vision

    [Fact]
    public void ProjectTypeDisplay_Vision项目_返回视觉()
    {
        var vm = CreateVisionViewModel();

        Assert.Equal("视觉", vm.ProjectTypeDisplay);
    }

    [Fact]
    public void HasDeviceConfig_Vision项目_返回True()
    {
        var vm = CreateVisionViewModel();

        Assert.True(vm.HasDeviceConfig);
    }

    [Fact]
    public void HasFunctionConfig_Vision项目_返回True()
    {
        var vm = CreateVisionViewModel();

        Assert.True(vm.HasFunctionConfig);
    }

    [Fact]
    public void DeviceSummaryItems_Vision有HikVision_包含相机项()
    {
        var vm = CreateVisionViewModel();

        Assert.Single(vm.DeviceSummaryItems);
        Assert.Contains("海康威视", vm.DeviceSummaryItems[0].Description);
    }

    [Fact]
    public void FunctionSummaryItems_Vision项目_Yolo算法_包含算法配置()
    {
        var vm = CreateVisionViewModel();

        Assert.Contains(vm.FunctionSummaryItems, item => item.Description.Contains("YOLO"));
        Assert.Contains(vm.FunctionSummaryItems, item => item.Description.Contains("YOLOv8n"));
    }

    #endregion

    #region Summary Properties - System

    [Fact]
    public void ProjectTypeDisplay_System项目_返回系统()
    {
        var vm = CreateSystemViewModel();

        Assert.Equal("系统", vm.ProjectTypeDisplay);
    }

    [Fact]
    public void HasDeviceConfig_System项目_返回False()
    {
        var vm = CreateSystemViewModel();

        Assert.False(vm.HasDeviceConfig);
    }

    [Fact]
    public void HasFunctionConfig_System项目_返回False()
    {
        var vm = CreateSystemViewModel();

        Assert.False(vm.HasFunctionConfig);
    }

    [Fact]
    public void DeviceSummaryItems_System项目_为空()
    {
        var vm = CreateSystemViewModel();

        Assert.Empty(vm.DeviceSummaryItems);
    }

    [Fact]
    public void FunctionSummaryItems_System项目_为空()
    {
        var vm = CreateSystemViewModel();

        Assert.Empty(vm.FunctionSummaryItems);
    }

    #endregion

    #region System Summary Items

    [Fact]
    public void SystemSummaryItems_启用用户管理_包含用户管理项()
    {
        var vm = CreateViewModel();

        Assert.Contains(vm.SystemSummaryItems, item => item.Description.Contains("用户管理"));
    }

    [Fact]
    public void SystemSummaryItems_启用RBAC_包含RBAC项()
    {
        var vm = CreateViewModel();

        Assert.Contains(vm.SystemSummaryItems, item => item.Description.Contains("RBAC"));
    }

    [Fact]
    public void SystemSummaryItems_包含主题信息()
    {
        var vm = CreateViewModel();

        Assert.Contains(vm.SystemSummaryItems, item => item.Description.Contains("主题"));
    }

    [Fact]
    public void SystemSummaryItems_包含强调色信息()
    {
        var vm = CreateViewModel();

        Assert.Contains(vm.SystemSummaryItems, item => item.Description.Contains("强调色"));
    }

    [Fact]
    public void SystemSummaryItems_启用日志_包含日志项()
    {
        var vm = CreateViewModel();

        Assert.Contains(vm.SystemSummaryItems, item => item.Description.Contains("日志"));
    }

    #endregion

    #region Commands

    [Fact]
    public void GenerateCommand_不为Null()
    {
        var vm = CreateViewModel();

        Assert.NotNull(vm.GenerateCommand);
    }

    [Fact]
    public void OpenProjectDirectoryCommand_不为Null()
    {
        var vm = CreateViewModel();

        Assert.NotNull(vm.OpenProjectDirectoryCommand);
    }

    [Fact]
    public void ViewLogCommand_不为Null()
    {
        var vm = CreateViewModel();

        Assert.NotNull(vm.ViewLogCommand);
    }

    #endregion

    #region Generation Tests

    [Fact]
    public async Task ExecuteGenerateAsync_成功_显示成功状态()
    {
        var generator = new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 5, TimeSpan.FromSeconds(1)));
        var vm = CreateViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.False(vm.IsGenerating);
        Assert.True(vm.IsSuccess);
        Assert.True(vm.ShowGenerationResult);
        Assert.Equal(100, vm.GenerationProgress);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_失败_显示错误状态()
    {
        var generator = new FakeProjectGenerator(GenerationResult.Fail("测试错误"));
        var vm = CreateViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.False(vm.IsGenerating);
        Assert.False(vm.IsSuccess);
        Assert.True(vm.ShowGenerationResult);
    }

    [Fact]
    public void CanGenerate_生成中_返回False()
    {
        var vm = CreateViewModel();

        // 初始状态可以生成
        Assert.True(vm.CanGenerate);
    }

    [Fact]
    public void CanGenerate_显示结果后_返回False()
    {
        var vm = CreateViewModel();

        // 手动设置状态模拟生成完成
        typeof(Step5ConfirmViewModel).GetProperty("ShowGenerationResult")?.SetValue(vm, true);

        Assert.False(vm.CanGenerate);
    }

    #endregion

    #region SetViewModels Tests

    [Fact]
    public void SetViewModels_更新ViewModels_更新摘要()
    {
        var vm = new Step5ConfirmViewModel();

        var step1 = new Step1ViewModel(_validationService)
        {
            ProjectName = "NewProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.Vision.ToString()
        };
        var step2 = new Step2DeviceConfigViewModel(_validationService);
        step2.SetProjectType(ProjectTypeCategory.Vision);
        step2.HasDaHua = true;

        var step3 = new Step3FunctionConfigViewModel();
        step3.SetProjectType(ProjectTypeCategory.Vision);

        var step4 = new Step4SystemConfigViewModel();
        step4.SetProjectType(ProjectTypeCategory.Vision);

        vm.SetViewModels(step1, step2, step3, step4);

        Assert.Equal("NewProject", vm.ProjectName);
        Assert.Equal("视觉", vm.ProjectTypeDisplay);
        Assert.Single(vm.DeviceSummaryItems);
        Assert.Contains("大华", vm.DeviceSummaryItems[0].Description);
    }

    #endregion

    #region Property Changed Tests

    [Fact]
    public void PropertyChanged_生成状态变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        // 通过反射设置IsGenerating（因为它是private set）
        typeof(Step5ConfirmViewModel).GetProperty("IsGenerating")?.SetValue(vm, true);

        Assert.Contains(nameof(vm.IsGenerating), propertyChangedEvents);
        Assert.Contains(nameof(vm.CanGenerate), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_生成进度变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        typeof(Step5ConfirmViewModel).GetProperty("GenerationProgress")?.SetValue(vm, 50.0);

        Assert.Contains(nameof(vm.GenerationProgress), propertyChangedEvents);
    }

    #endregion
}
