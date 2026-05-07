using FluentAssertions;
using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;
using Xunit;

namespace ScaffoldX.Core.Tests.TemplateProcessing;

/// <summary>
/// VariableResolver 单元测试，验证变量上下文构建和 PascalCase 转换。
/// </summary>
public class VariableResolverTests
{
    private readonly VariableResolver _sut = new();

    [Fact]
    public void BuildVariableContext_ShouldReturnBasicVariables_WhenConfigProvided()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "MyProject",
            NamespacePrefix = "MyProject",
            TargetFramework = "net8.0-windows",
            UIFramework = "WPF",
            Author = "TestUser",
            Company = "TestCompany",
            Description = "Test Description"
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("ProjectName");
        context["ProjectName"].Should().Be("MyProject");
        context["NamespacePrefix"].Should().Be("MyProject");
        context["TargetFramework"].Should().Be("net8.0-windows");
        context["UIFramework"].Should().Be("WPF");
    }

    [Fact]
    public void BuildVariableContext_ShouldSetVisionVariables_WhenVisionEnabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "VisionProject",
            EnableVision = true,
            CameraBrand = "Hikvision",
            ModelType = "Detection"
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("EnableVision");
        ((bool)context["EnableVision"]).Should().Be(true);
        ((string)context["CameraBrand"]).Should().Be("Hikvision");
        ((string)context["ModelType"]).Should().Be("Detection");
    }

    [Fact]
    public void BuildVariableContext_ShouldSetCollectionVariables_WhenDriversEnabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "CollectionProject",
            EnableSiemensS7 = true,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context["EnableSiemensS7"].Should().Be(true);
        context["EnableModbusTcp"].Should().Be(false);
        context["HasAnyCollection"].Should().Be(true);
    }

    [Fact]
    public void BuildVariableContext_ShouldSetXamlVariables_WhenWpfSelected()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "WpfProject",
            UIFramework = "WPF"
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context["XamlExt"].Should().Be("xaml");
        ((string)context["XamlNs"]).Should().Contain("schemas.microsoft.com");
    }

    [Fact]
    public void BuildVariableContext_ShouldSetXamlVariables_WhenAvaloniaSelected()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "AvaloniaProject",
            UIFramework = "Avalonia"
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context["XamlExt"].Should().Be("axaml");
        ((string)context["XamlNs"]).Should().Contain("avaloniaui");
    }

    [Fact]
    public void BuildVariableContext_ShouldIncludeScaffoldXMetadata()
    {
        // Arrange
        var config = new ProjectConfig { ProjectName = "TestProject" };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("ScaffoldXVersion");
        context.Should().ContainKey("GeneratedAt");
        context["ScaffoldXVersion"].Should().Be("1.0.0");
    }

    [Fact]
    public void BuildVariableContext_ShouldSetSystemVariables_WhenSystemModulesEnabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "SystemProject",
            EnableUserManagement = true,
            EnableRolePermission = true,
            EnableSystemLog = false,
            EnableThemeSwitcher = true
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context["EnableUserManagement"].Should().Be(true);
        context["EnableRolePermission"].Should().Be(true);
        context["EnableSystemLog"].Should().Be(false);
        context["EnableThemeSwitcher"].Should().Be(true);
    }

    [Fact]
    public void BuildVariableContext_ShouldIncludeNavigationStyle()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "NavProject",
            NavigationStyle = "TopNav"
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("NavigationStyle");
        ((string)context["NavigationStyle"]).Should().Be("TopNav");
    }

    [Fact]
    public void BuildVariableContext_ShouldIncludeDefaultTheme()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "ThemeProject",
            DefaultTheme = "LightModern"
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("DefaultTheme");
        ((string)context["DefaultTheme"]).Should().Be("LightModern");
    }

    [Fact]
    public void BuildVariableContext_ShouldIncludeDefaultLanguage()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "LangProject",
            DefaultLanguage = "en-US"
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("DefaultLanguage");
        ((string)context["DefaultLanguage"]).Should().Be("en-US");
    }

    [Fact]
    public void BuildVariableContext_ShouldUseDefaultValues_WhenNewFieldsNotSet()
    {
        // Arrange
        var config = new ProjectConfig { ProjectName = "DefaultProject" };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        ((string)context["NavigationStyle"]).Should().Be("LeftSidebar");
        ((string)context["DefaultTheme"]).Should().Be("IndustrialDark");
        ((string)context["DefaultLanguage"]).Should().Be("zh-CN");
    }

    [Fact]
    public void BuildVariableContext_ShouldIncludeEnableLocalization()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "I18nProject",
            EnableLocalization = true
        };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("EnableLocalization");
        ((bool)context["EnableLocalization"]).Should().Be(true);
    }

    [Fact]
    public void BuildVariableContext_ShouldDefaultEnableLocalizationToFalse()
    {
        // Arrange
        var config = new ProjectConfig { ProjectName = "NoI18nProject" };

        // Act
        var context = _sut.BuildVariableContext(config);

        // Assert
        context.Should().ContainKey("EnableLocalization");
        ((bool)context["EnableLocalization"]).Should().Be(false);
    }

    [Theory]
    [InlineData("my_project", "MyProject")]
    [InlineData("my-project", "MyProject")]
    [InlineData("myProject", "MyProject")]
    [InlineData("MY_PROJECT", "MyProject")]
    [InlineData("", "")]
    public void ToPascalCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Act
        var result = _sut.ToPascalCase(input);

        // Assert
        result.Should().Be(expected);
    }
}
