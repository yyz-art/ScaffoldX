using FluentAssertions;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Tests for Step3ViewModel new fields: NavigationStyle, DefaultTheme, DefaultLanguage,
/// and their options lists.
/// </summary>
public class Step3ViewModelTests
{
    [Fact]
    public void NavigationStyle_ShouldDefaultToLeftSidebar()
    {
        // Arrange & Act
        var vm = new Step3ViewModel();

        // Assert
        vm.NavigationStyle.Should().Be("LeftSidebar");
    }

    [Fact]
    public void NavigationStyleOptions_ShouldContainLeftSidebarAndTopNav()
    {
        // Arrange & Act
        var vm = new Step3ViewModel();

        // Assert
        vm.NavigationStyleOptions.Should().Contain("LeftSidebar");
        vm.NavigationStyleOptions.Should().Contain("TopNav");
    }

    [Fact]
    public void DefaultTheme_ShouldDefaultToIndustrialDark()
    {
        // Arrange & Act
        var vm = new Step3ViewModel();

        // Assert
        vm.DefaultTheme.Should().Be("IndustrialDark");
    }

    [Fact]
    public void DefaultThemeOptions_ShouldContainIndustrialDark()
    {
        // Arrange & Act
        var vm = new Step3ViewModel();

        // Assert
        vm.DefaultThemeOptions.Should().Contain("IndustrialDark");
    }

    [Fact]
    public void DefaultLanguage_ShouldDefaultToZhCN()
    {
        // Arrange & Act
        var vm = new Step3ViewModel();

        // Assert
        vm.DefaultLanguage.Should().Be("zh-CN");
    }

    [Fact]
    public void DefaultLanguageOptions_ShouldContainZhCNAndEnUS()
    {
        // Arrange & Act
        var vm = new Step3ViewModel();

        // Assert
        vm.DefaultLanguageOptions.Should().Contain("zh-CN");
        vm.DefaultLanguageOptions.Should().Contain("en-US");
    }

    [Fact]
    public void Reset_ShouldRestoreNewFieldDefaults()
    {
        // Arrange
        var vm = new Step3ViewModel();
        vm.NavigationStyle = "TopNav";
        vm.DefaultTheme = "LightModern";
        vm.DefaultLanguage = "en-US";

        // Act
        vm.Reset();

        // Assert
        vm.NavigationStyle.Should().Be("LeftSidebar");
        vm.DefaultTheme.Should().Be("IndustrialDark");
        vm.DefaultLanguage.Should().Be("zh-CN");
    }
}
