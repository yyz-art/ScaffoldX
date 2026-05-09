using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Templates;
using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step4ViewModelTests
{
    private class FakeProjectGenerator : IProjectGenerator
    {
        private readonly GenerationResult _result;

        public FakeProjectGenerator(GenerationResult result)
        {
            _result = result;
        }

        public Task<GenerationResult> GenerateAsync(ConfigRegistry configRegistry, IProgress<GenerationProgress>? progress = null)
        {
            progress?.Report(new GenerationProgress("测试进度", 50));
            return Task.FromResult(_result);
        }
    }

    private Step4ViewModel CreateViewModel(IProjectGenerator? generator = null)
    {
        var step1 = new Step1ViewModel(new ValidationService())
        {
            ProjectName = "TestProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = "Collection"
        };
        var step2 = new Step2ViewModel
        {
            Author = "TestAuthor",
            Company = "TestCompany"
        };
        var step3 = new Step3ViewModel
        {
            HasSiemensS7 = true,
            HasHikVision = true,
            HasUserManagement = true
        };

        generator ??= new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 10, TimeSpan.FromSeconds(1)));
        return new Step4ViewModel(generator, step1, step2, step3);
    }

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsGenerating);
        Assert.Equal(0, vm.ProgressPercent);
        Assert.Equal(string.Empty, vm.ProgressMessage);
        Assert.Equal(string.Empty, vm.ResultMessage);
    }

    [Fact]
    public void SummaryProjectName_返回Step1的值()
    {
        var vm = CreateViewModel();
        Assert.Equal("TestProject", vm.SummaryProjectName);
    }

    [Fact]
    public void SummaryProjectType_返回Step1的值()
    {
        var vm = CreateViewModel();
        Assert.Equal("Collection", vm.SummaryProjectType);
    }

    [Fact]
    public void SummaryAuthor_返回Step2的值()
    {
        var vm = CreateViewModel();
        Assert.Equal("TestAuthor", vm.SummaryAuthor);
    }

    [Fact]
    public void SummaryTargetFramework_固定值()
    {
        var vm = CreateViewModel();
        Assert.Equal("net10.0-windows", vm.SummaryTargetFramework);
    }

    [Fact]
    public void SummaryDriversText_有驱动_返回驱动列表()
    {
        var vm = CreateViewModel();
        Assert.Contains("西门子 S7", vm.SummaryDriversText);
    }

    [Fact]
    public void SummaryDriversText_无驱动_返回无()
    {
        var step1 = new Step1ViewModel(new ValidationService())
        {
            ProjectName = "TestProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = "Collection"
        };
        var step2 = new Step2ViewModel();
        var step3 = new Step3ViewModel(); // 无驱动
        var generator = new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 0, TimeSpan.Zero));
        var vm = new Step4ViewModel(generator, step1, step2, step3);

        Assert.Equal("无", vm.SummaryDriversText);
    }

    [Fact]
    public void SummaryVisionText_有视觉模块_返回模块列表()
    {
        var vm = CreateViewModel();
        Assert.Contains("海康相机", vm.SummaryVisionText);
    }

    [Fact]
    public void SummarySystemText_有系统模块_返回模块列表()
    {
        var vm = CreateViewModel();
        Assert.Contains("用户管理", vm.SummarySystemText);
    }

    [Fact]
    public void CanGenerate_Step1无效_返回False()
    {
        var step1 = new Step1ViewModel(new ValidationService())
        {
            ProjectName = "", // 无效
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = "Collection"
        };
        var step2 = new Step2ViewModel();
        var step3 = new Step3ViewModel();
        var generator = new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 0, TimeSpan.Zero));
        var vm = new Step4ViewModel(generator, step1, step2, step3);

        Assert.False(vm.CanGenerate);
    }

    [Fact]
    public void CanGenerate_初始状态_根据Step1有效性返回()
    {
        var vm = CreateViewModel();
        // Step1 设置了有效的项目名和类型，所以 CanGenerate 应该为 true
        Assert.True(vm.CanGenerate);
    }

    [Fact]
    public void GenerateCommand_不为Null()
    {
        var vm = CreateViewModel();
        Assert.NotNull(vm.GenerateCommand);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_成功_显示成功消息()
    {
        var generator = new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 5, TimeSpan.FromSeconds(1)));
        var vm = CreateViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.False(vm.IsGenerating);
        Assert.Contains("成功", vm.ResultMessage);
        Assert.Contains("5", vm.ResultMessage);
    }

    [Fact]
    public async Task ExecuteGenerateAsync_失败_显示错误消息()
    {
        var generator = new FakeProjectGenerator(GenerationResult.Fail("测试错误"));
        var vm = CreateViewModel(generator);

        await vm.ExecuteGenerateAsync();

        Assert.False(vm.IsGenerating);
        Assert.Contains("失败", vm.ResultMessage);
        Assert.Contains("测试错误", vm.ResultMessage);
    }

    [Fact]
    public void BuildConfigRegistry_返回正确配置()
    {
        var vm = CreateViewModel();
        var registry = vm.BuildConfigRegistry();

        Assert.NotNull(registry.GetSection("Scaffold"));
        Assert.NotNull(registry.GetSection("Scaffold.Collection"));
        Assert.NotNull(registry.GetSection("Scaffold.Vision"));
        Assert.NotNull(registry.GetSection("Scaffold.System"));
        Assert.NotNull(registry.GetSection("Scaffold.UI"));
    }
}
