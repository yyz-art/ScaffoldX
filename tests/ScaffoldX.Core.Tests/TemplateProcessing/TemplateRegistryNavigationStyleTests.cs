using FluentAssertions;
using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;
using Xunit;

namespace ScaffoldX.Core.Tests.TemplateProcessing;

/// <summary>
/// TemplateRegistry 单元测试：验证 NavigationStyle 对 Shell 导航模板的筛选逻辑。
/// </summary>
public class TemplateRegistryNavigationStyleTests
{
    private readonly TemplateRegistry _sut;

    public TemplateRegistryNavigationStyleTests()
    {
        _sut = new TemplateRegistry(new AssemblyTemplateSource());
    }

    /// <summary>
    /// 验证 NavigationStyle=LeftSidebar 时，模板集合排除 TopNavView 和 TopNavViewModel。
    /// </summary>
    [Fact]
    public async Task GetTemplatesForConfig_ShouldExcludeTopNavTemplates_WhenNavigationStyleIsLeftSidebar()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            ProjectName = "LeftSidebarTest",
            NavigationStyle = "LeftSidebar",
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
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().NotContain(t => t.Name.Contains("TopNavView"),
            "LeftSidebar 导航样式不应包含 TopNavView 模板");
        templates.Should().NotContain(t => t.Name.Contains("TopNavViewModel"),
            "LeftSidebar 导航样式不应包含 TopNavViewModel 模板");
    }

    /// <summary>
    /// 验证 NavigationStyle=TopNav 时，模板集合排除 SidebarView 和 SidebarViewModel。
    /// </summary>
    [Fact]
    public async Task GetTemplatesForConfig_ShouldExcludeSidebarTemplates_WhenNavigationStyleIsTopNav()
    {
        // Arrange
        await _sut.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            ProjectName = "TopNavTest",
            NavigationStyle = "TopNav",
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
        var templates = _sut.GetTemplatesForConfig(config);

        // Assert
        templates.Should().NotContain(t => t.Name.Contains("SidebarView"),
            "TopNav 导航样式不应包含 SidebarView 模板");
        templates.Should().NotContain(t => t.Name.Contains("SidebarViewModel"),
            "TopNav 导航样式不应包含 SidebarViewModel 模板");
    }
}
