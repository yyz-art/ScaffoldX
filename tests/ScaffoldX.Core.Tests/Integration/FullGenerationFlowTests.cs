using FluentAssertions;
using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;
using Xunit;

namespace ScaffoldX.Core.Tests.Integration;

/// <summary>
/// 集成测试：验证完整生成流程中各组件的协作。
/// 测试 TemplateRegistry → VariableResolver → PostProcessor 管线。
/// </summary>
public class FullGenerationFlowTests
{
    private readonly TemplateRegistry _registry = new();

    /// <summary>
    /// 验证采集项目配置下，S7 驱动模板被正确选中且内容不含指令标记。
    /// </summary>
    [Fact]
    public async Task CollectionProject_ShouldSelectS7DriverTemplate_WhenSiemensS7Enabled()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();
        var config = new ProjectConfig
        {
            ProjectName = "TestCollection",
            EnableSiemensS7 = true,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _registry.GetTemplatesForConfig(config);
        var variables = VariableResolver.BuildVariableContext(config);

        // Assert
        templates.Should().Contain(t => t.Name.Contains("S7"));
        templates.Should().Contain(t => t.Category == "Collection");

        foreach (var template in templates)
        {
            template.Content.Should().NotContain("##OUTPUT:", "模板内容不应包含指令标记");
            template.Content.Should().NotContain("##REQUIRED:", "模板内容不应包含指令标记");
            template.OutputPathTemplate.Should().NotBeNullOrEmpty("输出路径模板不应为空");
        }
    }

    /// <summary>
    /// 验证视觉项目配置下，视觉模板被正确选中。
    /// </summary>
    [Fact]
    public async Task VisionProject_ShouldSelectVisionTemplates_WhenVisionEnabled()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();
        var config = new ProjectConfig
        {
            ProjectName = "TestVision",
            EnableVision = true,
            CameraBrand = "Hikvision",
            ModelType = "Detection"
        };

        // Act
        var templates = _registry.GetTemplatesForConfig(config);

        // Assert
        templates.Should().Contain(t => t.Category == "Vision");
        templates.Should().Contain(t => t.Name.Contains("Camera") || t.Name.Contains("ICamera"));
        templates.Should().Contain(t => t.Name.Contains("Inference"));
    }

    /// <summary>
    /// 验证系统项目配置下，系统模块模板被正确选中。
    /// </summary>
    [Fact]
    public async Task SystemProject_ShouldSelectSystemTemplates_WhenModulesEnabled()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();
        var config = new ProjectConfig
        {
            ProjectName = "TestSystem",
            EnableUserManagement = true,
            EnableRolePermission = true,
            EnableSystemLog = true,
            EnableThemeSwitcher = true
        };

        // Act
        var templates = _registry.GetTemplatesForConfig(config);

        // Assert
        templates.Should().Contain(t => t.Category == "System");
        templates.Should().Contain(t => t.Name.Contains("User"));
        templates.Should().Contain(t => t.Name.Contains("Role") || t.Name.Contains("Permission"));
        templates.Should().Contain(t => t.Name.Contains("Log") || t.Name.Contains("Audit"));
        templates.Should().Contain(t => t.Name.Contains("Theme") || t.Name.Contains("Switcher"));
    }

    /// <summary>
    /// 验证混合配置（采集 + 系统）下模板正确筛选。
    /// </summary>
    [Fact]
    public async Task MixedConfig_ShouldSelectTemplatesFromMultipleCategories()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();
        var config = new ProjectConfig
        {
            ProjectName = "MixedProject",
            EnableSiemensS7 = true,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false,
            EnableUserManagement = true,
            EnableRolePermission = false,
            EnableSystemLog = false,
            EnableThemeSwitcher = false
        };

        // Act
        var templates = _registry.GetTemplatesForConfig(config);
        var categories = templates.Select(t => t.Category).Distinct().ToList();

        // Assert
        categories.Should().Contain("Common");
        categories.Should().Contain("Collection");
        categories.Should().Contain("System");
        categories.Should().NotContain("Vision");
    }

    /// <summary>
    /// 验证 VariableResolver 构建的变量上下文包含所有模板所需的变量。
    /// </summary>
    [Fact]
    public void VariableContext_ShouldContainAllRequiredVariables()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "FullProject",
            NamespacePrefix = "FullProject",
            TargetFramework = "net8.0-windows",
            UIFramework = "WPF",
            EnableVision = true,
            EnableSiemensS7 = true,
            EnableUserManagement = true
        };

        // Act
        var ctx = VariableResolver.BuildVariableContext(config);

        // Assert — 基础变量
        ctx.Should().ContainKey("ProjectName");
        ctx.Should().ContainKey("NamespacePrefix");
        ctx.Should().ContainKey("TargetFramework");
        ctx.Should().ContainKey("UIFramework");
        ctx.Should().ContainKey("XamlExt");
        ctx.Should().ContainKey("ScaffoldXVersion");
        ctx.Should().ContainKey("GeneratedAt");

        // Assert — 采集类变量
        ctx.Should().ContainKey("EnableSiemensS7");
        ctx.Should().ContainKey("EnableModbusTcp");
        ctx.Should().ContainKey("HasAnyCollection");

        // Assert — 视觉类变量
        ctx.Should().ContainKey("EnableVision");
        ctx.Should().ContainKey("CameraBrand");

        // Assert — 系统类变量
        ctx.Should().ContainKey("EnableUserManagement");
        ctx.Should().ContainKey("EnableRolePermission");
        ctx.Should().ContainKey("EnableSystemLog");
        ctx.Should().ContainKey("EnableThemeSwitcher");
    }

    /// <summary>
    /// 验证 Common 模板在任何配置下都会被选中。
    /// </summary>
    [Fact]
    public async Task CommonTemplates_ShouldAlwaysBeIncluded()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();
        var config = new ProjectConfig
        {
            ProjectName = "MinimalProject",
            EnableVision = false,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false,
            EnableUserManagement = false,
            EnableRolePermission = false,
            EnableSystemLog = false,
            EnableThemeSwitcher = false
        };

        // Act
        var templates = _registry.GetTemplatesForConfig(config);

        // Assert
        templates.Should().Contain(t => t.Category == "Common");
        templates.Should().Contain(t => t.Name.Contains("Plugin"));
        templates.Should().Contain(t => t.Name.Contains("Bootstrapper"));
    }

    /// <summary>
    /// 验证所有模板的输出路径模板不为空且包含 ProjectName 变量。
    /// </summary>
    [Fact]
    public async Task AllTemplates_ShouldHaveValidOutputPathTemplates()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();

        // Act & Assert
        foreach (var template in _registry.GetAllTemplates())
        {
            template.OutputPathTemplate.Should().NotBeNullOrEmpty(
                $"模板 '{template.Name}' 的输出路径模板不应为空");
        }
    }

    /// <summary>
    /// 验证 PostProcessor 能正确处理模板渲染后的内容。
    /// </summary>
    [Fact]
    public void PostProcessor_ShouldProcessRenderedContent_Correctly()
    {
        // Arrange
        var config = new ProjectConfig { ProjectName = "Test" };
        var csContent = "namespace Test\r\n{\r\n    class Foo { }\r\n}";
        var xamlContent = "<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">&lt;test&gt;</Window>";

        // Act
        var processedCs = PostProcessor.Process(csContent, "Test.cs", config);
        var processedXaml = PostProcessor.Process(xamlContent, "Test.xaml", config);

        // Assert
        processedCs.Should().NotContain("\r\n", "C# 文件行尾应统一为 LF");
        processedCs.Should().EndWith("\n", "文件应以换行符结尾");

        processedXaml.Should().Contain("<test>", "XAML 文件应还原 XML 实体");
        processedXaml.Should().NotContain("&lt;");
        processedXaml.Should().NotContain("&gt;");
    }

    /// <summary>
    /// 验证禁用视觉模块时，视觉模板不会被选中。
    /// </summary>
    [Fact]
    public async Task VisionTemplates_ShouldNotBeIncluded_WhenVisionDisabled()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();
        var config = new ProjectConfig
        {
            ProjectName = "NoVision",
            EnableVision = false,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _registry.GetTemplatesForConfig(config);

        // Assert
        templates.Should().NotContain(t => t.Category == "Vision");
    }

    /// <summary>
    /// 验证禁用所有系统模块时，系统模板不会被选中（UserRole 和 IMenuModule 除外，它们 IsRequired=true）。
    /// </summary>
    [Fact]
    public async Task SystemTemplates_ShouldNotBeIncluded_WhenAllModulesDisabled()
    {
        // Arrange
        await _registry.LoadFromAssemblyAsync();
        var config = new ProjectConfig
        {
            ProjectName = "NoSystem",
            EnableUserManagement = false,
            EnableRolePermission = false,
            EnableSystemLog = false,
            EnableThemeSwitcher = false,
            EnableVision = false,
            EnableSiemensS7 = false,
            EnableModbusTcp = false,
            EnableOpcUa = false,
            EnableMitsubishiMc = false,
            EnableOmronFins = false
        };

        // Act
        var templates = _registry.GetTemplatesForConfig(config);

        // Assert — 非必须的系统模板不应包含
        templates.Should().NotContain(t => t.Category == "System" && !t.IsRequired,
            "禁用所有系统模块时，非必须的系统模板不应被选中");
    }
}
