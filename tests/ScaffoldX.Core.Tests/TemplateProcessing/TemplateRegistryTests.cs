using FluentAssertions;
using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;
using Xunit;

namespace ScaffoldX.Core.Tests.TemplateProcessing;

/// <summary>
/// TemplateRegistry 单元测试，验证模板加载和过滤逻辑。
/// </summary>
public class TemplateRegistryTests
{
    private readonly TemplateRegistry _sut;

    public TemplateRegistryTests()
    {
        _sut = new TemplateRegistry(new AssemblyTemplateSource());
    }

    [Fact]
    public async Task LoadTemplatesAsync_ShouldLoadTemplates_WhenAssemblyExists()
    {
        // Arrange & Act
        var count = await _sut.LoadTemplatesAsync();

        // Assert
        count.Should().BeGreaterThan(0);
        _sut.GetAllTemplates().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTemplatesForConfig_ShouldReturnVisionTemplates_WhenVisionEnabled()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            EnableVision = true,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().NotBeEmpty();
        templates.Should().Contain(t => t.Category == "Vision");
    }

    [Fact]
    public async Task GetTemplatesForConfig_ShouldNotReturnVisionTemplates_WhenVisionDisabled()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            EnableVision = false,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().NotContain(t => t.Category == "Vision");
    }

    [Fact]
    public async Task GetTemplatesForConfig_ShouldIncludeCommonTemplates_Always()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            EnableVision = false,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().Contain(t => t.Category == "Common");
    }

    [Fact]
    public async Task GetTemplatesForConfig_ShouldReturnSystemTemplates_WhenSystemModulesEnabled()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            EnableUserManagement = true,
            EnableRolePermission = false,
            EnableSystemLog = false,
            EnableThemeSwitcher = false
        };

        // Act
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().Contain(t => t.Category == "System");
    }

    [Fact]
    public async Task GetTemplatesForConfig_ShouldIncludeLocalizationTemplates_WhenLocalizationEnabled()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            EnableLocalization = true,
            EnableVision = false,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().Contain(t => t.Name.Contains("Localization"));
        templates.Should().Contain(t => t.Name.Contains("Resx") || t.Name.Contains("Strings"));
    }

    [Fact]
    public async Task GetTemplatesForConfig_ShouldNotIncludeLocalizationTemplates_WhenLocalizationDisabled()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            EnableLocalization = false,
            EnableVision = false,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().NotContain(t => t.Name.Contains("Localization"));
        templates.Should().NotContain(t => t.Name.Contains("Resx") || t.Name.Contains("Strings"));
    }
}
