using MaterialDesignThemes.Wpf;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class PackIconValidationTests
{
    [Fact]
    public void SummaryItem_AllIconValues_AreValidPackIconKinds()
    {
        var allIconStrings = GetAllSummaryItemIcons();

        foreach (var iconString in allIconStrings)
        {
            var canParse = Enum.TryParse<PackIconKind>(iconString, out var kind);
            Assert.True(canParse, $"SummaryItem.Icon '{iconString}' 不是有效的 PackIconKind 枚举值");
        }
    }

    [Fact]
    public void SummaryItem_AllIconValues_NonDefaultKind()
    {
        var allIconStrings = GetAllSummaryItemIcons();

        foreach (var iconString in allIconStrings)
        {
            Enum.TryParse<PackIconKind>(iconString, out var kind);
            Assert.NotEqual(PackIconKind.None, kind);
        }
    }

    [Fact]
    public void Step5Confirm_ResultIcons_AreValidPackIconKinds()
    {
        var resultIcons = new[] { "CheckCircle", "CloseCircle", "AlertCircle" };

        foreach (var icon in resultIcons)
        {
            var canParse = Enum.TryParse<PackIconKind>(icon, out _);
            Assert.True(canParse, $"ResultIcon '{icon}' 不是有效的 PackIconKind 枚举值");
        }
    }

    private static List<string> GetAllSummaryItemIcons()
    {
        var icons = new List<string>();
        var validationService = new ValidationService();

        var step1 = new Step1ViewModel(validationService)
        {
            ProjectName = "TestProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.Collection.ToString()
        };
        var step2 = new Step2DeviceConfigViewModel(validationService);
        step2.SetProjectType(ProjectTypeCategory.Collection);
        step2.HasSiemensS7 = true;
        step2.HasMitsubishiMc = true;
        step2.HasModbusTcp = true;
        step2.HasOpcUa = true;

        var step3 = new Step3FunctionConfigViewModel();
        step3.SetProjectType(ProjectTypeCategory.Collection);

        var step4 = new Step4SystemConfigViewModel();
        step4.SetProjectType(ProjectTypeCategory.Collection);

        var vm = new Step5ConfirmViewModel(
            new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 5, TimeSpan.FromSeconds(1))),
            step1, step2, step3, step4);

        icons.AddRange(vm.DeviceSummaryItems.Select(i => i.Icon));
        icons.AddRange(vm.FunctionSummaryItems.Select(i => i.Icon));
        icons.AddRange(vm.SystemSummaryItems.Select(i => i.Icon));

        var step1v = new Step1ViewModel(validationService)
        {
            ProjectName = "VisionProject",
            OutputDirectory = Path.GetTempPath(),
            SelectedProjectType = ProjectTypeCategory.Vision.ToString()
        };
        var step2v = new Step2DeviceConfigViewModel(validationService);
        step2v.SetProjectType(ProjectTypeCategory.Vision);
        step2v.HasHikVision = true;
        step2v.HasDaHua = true;

        var step3v = new Step3FunctionConfigViewModel();
        step3v.SetProjectType(ProjectTypeCategory.Vision);
        step3v.SelectedAlgorithm = VisionAlgorithm.Yolo;

        var step4v = new Step4SystemConfigViewModel();
        step4v.SetProjectType(ProjectTypeCategory.Vision);

        var vmv = new Step5ConfirmViewModel(
            new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 5, TimeSpan.FromSeconds(1))),
            step1v, step2v, step3v, step4v);

        icons.AddRange(vmv.DeviceSummaryItems.Select(i => i.Icon));
        icons.AddRange(vmv.FunctionSummaryItems.Select(i => i.Icon));
        icons.AddRange(vmv.SystemSummaryItems.Select(i => i.Icon));

        step3v.SelectedAlgorithm = VisionAlgorithm.Sam3;
        var vmv2 = new Step5ConfirmViewModel(
            new FakeProjectGenerator(GenerationResult.Ok(Path.GetTempPath(), 5, TimeSpan.FromSeconds(1))),
            step1v, step2v, step3v, step4v);

        icons.AddRange(vmv2.FunctionSummaryItems.Select(i => i.Icon));

        return icons.Distinct().ToList();
    }

    private class FakeProjectGenerator : IProjectGenerator
    {
        private readonly GenerationResult _result;

        public FakeProjectGenerator(GenerationResult result) => _result = result;

        public Task<GenerationResult> GenerateAsync(ConfigRegistry configRegistry, IProgress<GenerationProgress>? progress = null)
        {
            progress?.Report(new GenerationProgress("测试进度", 50));
            return Task.FromResult(_result);
        }
    }
}
