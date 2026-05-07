using FluentAssertions;
using ScaffoldX.Core.Models;
using Xunit;

namespace ScaffoldX.Core.Tests.Models;

/// <summary>
/// Tests for ProjectConfig new fields: NavigationStyle, DefaultTheme, DefaultLanguage,
/// and the SetNavigationStyle convenience method.
/// </summary>
public class ProjectConfigNewFieldsTests
{
    [Fact]
    public void NavigationStyle_ShouldDefaultToLeftSidebar()
    {
        // Arrange & Act
        var config = new ProjectConfig();

        // Assert
        config.NavigationStyle.Should().Be("LeftSidebar");
    }

    [Fact]
    public void DefaultTheme_ShouldDefaultToIndustrialDark()
    {
        // Arrange & Act
        var config = new ProjectConfig();

        // Assert
        config.DefaultTheme.Should().Be("IndustrialDark");
    }

    [Fact]
    public void DefaultLanguage_ShouldDefaultToZhCN()
    {
        // Arrange & Act
        var config = new ProjectConfig();

        // Assert
        config.DefaultLanguage.Should().Be("zh-CN");
    }

    [Fact]
    public void SetNavigationStyle_ShouldUpdateNavigationStyle()
    {
        // Arrange
        var config = new ProjectConfig();

        // Act
        config.SetNavigationStyle("TopNav");

        // Assert
        config.NavigationStyle.Should().Be("TopNav");
    }

    [Fact]
    public void SetNavigationStyle_ShouldAcceptLeftSidebar()
    {
        // Arrange
        var config = new ProjectConfig { NavigationStyle = "TopNav" };

        // Act
        config.SetNavigationStyle("LeftSidebar");

        // Assert
        config.NavigationStyle.Should().Be("LeftSidebar");
    }
}
