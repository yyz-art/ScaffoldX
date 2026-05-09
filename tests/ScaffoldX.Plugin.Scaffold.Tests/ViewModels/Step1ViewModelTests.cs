using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step1ViewModelTests
{
    private readonly IValidationService _validationService = new ValidationService();

    private Step1ViewModel CreateViewModel()
    {
        return new Step1ViewModel(_validationService);
    }

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = CreateViewModel();

        Assert.Equal(ProjectTypeCategory.None, vm.ProjectTypeEnum);
        Assert.Equal(string.Empty, vm.ProjectName);
        Assert.Equal(string.Empty, vm.OutputDirectory);
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void ProjectName_设置有效值_通过验证()
    {
        var vm = CreateViewModel();
        vm.OutputDirectory = Path.GetTempPath();
        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();

        vm.ProjectName = "ValidProject";

        Assert.Equal("ValidProject", vm.ProjectName);
        Assert.True(string.IsNullOrEmpty(vm.ProjectNameError));
        Assert.True(vm.IsValid);
    }

    [Fact]
    public void ProjectName_设置无效值_显示错误()
    {
        var vm = CreateViewModel();

        vm.ProjectName = "1Invalid";

        Assert.False(string.IsNullOrEmpty(vm.ProjectNameError));
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void ProjectName_设置空值_显示错误()
    {
        var vm = CreateViewModel();
        vm.OutputDirectory = Path.GetTempPath();
        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();
        vm.ProjectName = "ValidName"; // 先设置有效值

        vm.ProjectName = ""; // 再设置为空

        Assert.False(string.IsNullOrEmpty(vm.ProjectNameError));
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void OutputDirectory_设置有效路径_通过验证()
    {
        var vm = CreateViewModel();
        vm.ProjectName = "TestProject";
        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();

        vm.OutputDirectory = Path.GetTempPath();

        Assert.Equal(Path.GetTempPath(), vm.OutputDirectory);
        Assert.True(string.IsNullOrEmpty(vm.OutputDirectoryError));
        Assert.True(vm.IsValid);
    }

    [Fact]
    public void OutputDirectory_设置无效路径_显示错误()
    {
        var vm = CreateViewModel();

        vm.OutputDirectory = "C:\\NonExistentPath\\12345";

        Assert.False(string.IsNullOrEmpty(vm.OutputDirectoryError));
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void OutputDirectory_设置空值_显示错误()
    {
        var vm = CreateViewModel();
        vm.ProjectName = "ValidName";
        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();
        vm.OutputDirectory = Path.GetTempPath(); // 先设置有效值

        vm.OutputDirectory = ""; // 再设置为空

        Assert.False(string.IsNullOrEmpty(vm.OutputDirectoryError));
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void SelectedProjectType_设置Collection_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();

        Assert.Equal(ProjectTypeCategory.Collection, vm.ProjectTypeEnum);
        Assert.Equal("Collection", vm.SelectedProjectType);
    }

    [Fact]
    public void SelectedProjectType_设置Vision_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedProjectType = ProjectTypeCategory.Vision.ToString();

        Assert.Equal(ProjectTypeCategory.Vision, vm.ProjectTypeEnum);
        Assert.Equal("Vision", vm.SelectedProjectType);
    }

    [Fact]
    public void SelectedProjectType_设置System_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedProjectType = ProjectTypeCategory.System.ToString();

        Assert.Equal(ProjectTypeCategory.System, vm.ProjectTypeEnum);
        Assert.Equal("System", vm.SelectedProjectType);
    }

    [Fact]
    public void SelectedProjectType_未选择时_IsValid返回False()
    {
        var vm = CreateViewModel();
        vm.ProjectName = "ValidProject";
        vm.OutputDirectory = Path.GetTempPath();

        Assert.Equal(ProjectTypeCategory.None, vm.ProjectTypeEnum);
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void IsValid_所有字段有效_返回True()
    {
        var vm = CreateViewModel();
        vm.ProjectName = "ValidProject";
        vm.OutputDirectory = Path.GetTempPath();
        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_任一字段无效_返回False()
    {
        var vm = CreateViewModel();
        vm.ProjectName = "ValidProject";
        vm.OutputDirectory = Path.GetTempPath();
        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();

        Assert.True(vm.IsValid);

        vm.ProjectName = "";
        Assert.False(vm.IsValid);

        vm.ProjectName = "ValidProject";
        vm.OutputDirectory = "";
        Assert.False(vm.IsValid);

        vm.OutputDirectory = Path.GetTempPath();
        vm.SelectedProjectType = ProjectTypeCategory.None.ToString();
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void ProjectTypeEnum_返回正确枚举值()
    {
        var vm = CreateViewModel();

        Assert.Equal(ProjectTypeCategory.None, vm.ProjectTypeEnum);

        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();
        Assert.Equal(ProjectTypeCategory.Collection, vm.ProjectTypeEnum);

        vm.SelectedProjectType = ProjectTypeCategory.Vision.ToString();
        Assert.Equal(ProjectTypeCategory.Vision, vm.ProjectTypeEnum);

        vm.SelectedProjectType = ProjectTypeCategory.System.ToString();
        Assert.Equal(ProjectTypeCategory.System, vm.ProjectTypeEnum);
    }

    [Fact]
    public void BrowseCommand_不为Null()
    {
        var vm = CreateViewModel();

        Assert.NotNull(vm.BrowseCommand);
    }

    [Fact]
    public void CardBrushes_选择Collection时_集合卡片为蓝色()
    {
        var vm = CreateViewModel();

        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();

        Assert.NotNull(vm.CollectionCardBrush);
        Assert.NotNull(vm.VisionCardBrush);
        Assert.NotNull(vm.SystemCardBrush);
    }

    [Fact]
    public void CardBrushes_选择Vision时_视觉卡片为蓝色()
    {
        var vm = CreateViewModel();

        vm.SelectedProjectType = ProjectTypeCategory.Vision.ToString();

        Assert.NotNull(vm.CollectionCardBrush);
        Assert.NotNull(vm.VisionCardBrush);
        Assert.NotNull(vm.SystemCardBrush);
    }

    [Fact]
    public void CardBrushes_选择System时_系统卡片为蓝色()
    {
        var vm = CreateViewModel();

        vm.SelectedProjectType = ProjectTypeCategory.System.ToString();

        Assert.NotNull(vm.CollectionCardBrush);
        Assert.NotNull(vm.VisionCardBrush);
        Assert.NotNull(vm.SystemCardBrush);
    }

    [Fact]
    public void PropertyChanged_项目类型变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.SelectedProjectType = ProjectTypeCategory.Collection.ToString();

        Assert.Contains(nameof(vm.ProjectTypeEnum), propertyChangedEvents);
        Assert.Contains(nameof(vm.CollectionCardBrush), propertyChangedEvents);
        Assert.Contains(nameof(vm.VisionCardBrush), propertyChangedEvents);
        Assert.Contains(nameof(vm.SystemCardBrush), propertyChangedEvents);
    }
}
