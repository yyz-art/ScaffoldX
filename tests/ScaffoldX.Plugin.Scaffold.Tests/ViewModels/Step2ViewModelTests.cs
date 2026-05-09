using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step2ViewModelTests
{
    private Step2ViewModel CreateViewModel()
    {
        return new Step2ViewModel();
    }

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = CreateViewModel();

        Assert.Equal(string.Empty, vm.Description);
        Assert.Equal(string.Empty, vm.Author);
        Assert.Equal(string.Empty, vm.Company);
        Assert.Equal(string.Empty, vm.Copyright);
        Assert.Equal("net10.0-windows", vm.TargetFramework);
    }

    [Fact]
    public void Description_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Description = "Test Description";

        Assert.Equal("Test Description", vm.Description);
    }

    [Fact]
    public void Author_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Author = "Test Author";

        Assert.Equal("Test Author", vm.Author);
    }

    [Fact]
    public void Company_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Company = "Test Company";

        Assert.Equal("Test Company", vm.Company);
    }

    [Fact]
    public void Copyright_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.Copyright = "Test Copyright";

        Assert.Equal("Test Copyright", vm.Copyright);
    }

    [Fact]
    public void TargetFramework_固定值()
    {
        var vm = CreateViewModel();

        Assert.Equal("net10.0-windows", vm.TargetFramework);
    }
}
