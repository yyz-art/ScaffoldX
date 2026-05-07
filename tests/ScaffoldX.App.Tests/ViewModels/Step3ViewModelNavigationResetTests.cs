using FluentAssertions;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Step3ViewModel 测试：验证 NavigationStyle 选项和 Reset 行为。
/// </summary>
public class Step3ViewModelNavigationResetTests
{
    /// <summary>
    /// 验证 NavigationStyleOptions 包含 LeftSidebar 和 TopNav 两个选项。
    /// </summary>
    [Fact]
    public void NavigationStyleOptions_ShouldContainLeftSidebarAndTopNav()
    {
        // Arrange & Act
        var vm = new Step3ViewModel();

        // Assert
        vm.NavigationStyleOptions.Should().Contain("LeftSidebar");
        vm.NavigationStyleOptions.Should().Contain("TopNav");
    }

    /// <summary>
    /// 验证 Reset 方法将 NavigationStyle 恢复为默认值 "LeftSidebar"。
    /// </summary>
    [Fact]
    public void Reset_ShouldRestoreNavigationStyleToLeftSidebar()
    {
        // Arrange
        var vm = new Step3ViewModel();
        vm.NavigationStyle = "TopNav";

        // Act
        vm.Reset();

        // Assert
        vm.NavigationStyle.Should().Be("LeftSidebar");
    }
}
