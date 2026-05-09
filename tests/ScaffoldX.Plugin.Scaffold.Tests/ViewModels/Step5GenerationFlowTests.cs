using ScaffoldX.Abstractions.Config;
using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step5GenerationFlowTests
{
    private readonly IValidationService _validationService = new ValidationService();

    private class CapturingProjectGenerator : IProjectGenerator
    {
        public ConfigRegistry? CapturedConfig { get; private set; }
        public int GenerateCallCount { get; private set; }

        public Task<GenerationResult> GenerateAsync(ConfigRegistry configRegistry, IProgress<GenerationProgress>? progress = null)
        {
            CapturedConfig = configRegistry;
            GenerateCallCount++;
            return Task.FromResult(GenerationResult.Ok(Path.GetTempPath(), 5, TimeSpan.FromSeconds(1)));
        }
    }

    [Fact]
    public async Task ExecuteGenerateAsync_Collection项目_ConfigRegistry包含ScaffoldSection()
    {
        var generator = new CapturingProjectGenerator();
        var vm = CreateCollectionViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.NotNull(generator.CapturedConfig);
        var scaffoldSection = generator.CapturedConfig.GetSection("Scaffold");
        Assert.NotNull(scaffoldSection);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_Collection项目_ConfigRegistry包含CollectionSection()
    {
        var generator = new CapturingProjectGenerator();
        var vm = CreateCollectionViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.NotNull(generator.CapturedConfig);
        var collectionSection = generator.CapturedConfig.GetSection("Scaffold.Collection");
        Assert.NotNull(collectionSection);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_Collection项目_项目名称正确()
    {
        var generator = new CapturingProjectGenerator();
        var vm = CreateCollectionViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.NotNull(generator.CapturedConfig);
        var scaffoldSection = generator.CapturedConfig.GetSection("Scaffold") as ScaffoldConfigSection;
        Assert.NotNull(scaffoldSection);
        Assert.Equal("TestCollectionProject", scaffoldSection.ProjectName);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_Collection项目_项目类型正确()
    {
        var generator = new CapturingProjectGenerator();
        var vm = CreateCollectionViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.NotNull(generator.CapturedConfig);
        var scaffoldSection = generator.CapturedConfig.GetSection("Scaffold") as ScaffoldConfigSection;
        Assert.NotNull(scaffoldSection);
        Assert.Equal("Collection", scaffoldSection.ProjectType);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_Vision项目_ConfigRegistry包含VisionSection()
    {
        var generator = new CapturingProjectGenerator();
        var vm = CreateVisionViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.NotNull(generator.CapturedConfig);
        var visionSection = generator.CapturedConfig.GetSection("Scaffold.Vision");
        Assert.NotNull(visionSection);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_System项目_ConfigRegistry包含SystemSection()
    {
        var generator = new CapturingProjectGenerator();
        var vm = CreateSystemViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.NotNull(generator.CapturedConfig);
        var systemSection = generator.CapturedConfig.GetSection("Scaffold.System");
        Assert.NotNull(systemSection);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_生成器只调用一次()
    {
        var generator = new CapturingProjectGenerator();
        var vm = CreateCollectionViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.Equal(1, generator.GenerateCallCount);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_无生成器_显示错误()
    {
        var vm = new Step5ConfirmViewModel();

        await vm.ExecuteGenerateAsync();

        Assert.True(vm.ShowGenerationResult);
        Assert.False(vm.IsSuccess);
    }

    private Step5ConfirmViewModel CreateCollectionViewModel(CapturingProjectGenerator generator)
    {
        var step1 = new Step1ViewModel(_validationService)
        {
            ProjectName = "TestCollectionProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.Collection.ToString()
        };
        var step2 = new Step2DeviceConfigViewModel(_validationService);
        step2.SetProjectType(ProjectTypeCategory.Collection);
        step2.HasSiemensS7 = true;
        step2.HasModbusTcp = true;

        var step3 = new Step3FunctionConfigViewModel();
        step3.SetProjectType(ProjectTypeCategory.Collection);

        var step4 = new Step4SystemConfigViewModel();
        step4.SetProjectType(ProjectTypeCategory.Collection);

        return new Step5ConfirmViewModel(generator, step1, step2, step3, step4);
    }

    private Step5ConfirmViewModel CreateVisionViewModel(CapturingProjectGenerator generator)
    {
        var step1 = new Step1ViewModel(_validationService)
        {
            ProjectName = "TestVisionProject",
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

        return new Step5ConfirmViewModel(generator, step1, step2, step3, step4);
    }

    private Step5ConfirmViewModel CreateSystemViewModel(CapturingProjectGenerator generator)
    {
        var step1 = new Step1ViewModel(_validationService)
        {
            ProjectName = "TestSystemProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.System.ToString()
        };
        var step2 = new Step2DeviceConfigViewModel(_validationService);
        step2.SetProjectType(ProjectTypeCategory.System);

        var step3 = new Step3FunctionConfigViewModel();
        step3.SetProjectType(ProjectTypeCategory.System);

        var step4 = new Step4SystemConfigViewModel();
        step4.SetProjectType(ProjectTypeCategory.System);

        return new Step5ConfirmViewModel(generator, step1, step2, step3, step4);
    }
}
