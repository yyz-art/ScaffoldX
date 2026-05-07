using FluentAssertions;
using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;
using Scriban;
using Scriban.Runtime;
using Xunit;

namespace ScaffoldX.Core.Tests.Integration;

/// <summary>
/// 集成测试：验证 DefaultTheme 变量在 App.xaml 模板中的渲染结果。
/// 确保六套工业主题各自生成正确的 MaterialDesign 配色。
/// </summary>
public class ThemeRenderingTests
{
    private readonly TemplateRegistry _registry = new(new AssemblyTemplateSource());

    /// <summary>
    /// 六套主题对应的 PrimaryColor 和 SecondaryColor 验证数据。
    /// </summary>
    public static IEnumerable<object[]> ThemeColorData => new List<object[]>
    {
        new object[] { "DeepSeaBlue",     "BlueGrey",  "LightBlue" },
        new object[] { "MossGreen",       "Teal",      "Green"     },
        new object[] { "CharcoalBlack",   "Grey",      "BlueGrey"  },
        new object[] { "BlueSteel",       "Indigo",    "LightBlue" },
        new object[] { "AmberIndustrial", "Orange",    "Amber"     },
        new object[] { "NeutralSteel",    "Grey",      "BlueGrey"  },
    };

    /// <summary>
    /// 验证 App.xaml 模板渲染后包含对应主题的 PrimaryColor 和 SecondaryColor。
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeColorData))]
    public async Task AppXaml_ShouldContainThemeColors_WhenRendered(
        string defaultTheme, string expectedPrimary, string expectedSecondary)
    {
        // Arrange
        await _registry.LoadTemplatesAsync();
        var config = new ProjectConfig
        {
            ProjectName = "ThemeTest",
            DefaultTheme = defaultTheme,
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

        var appXamlTemplate = _registry.GetAllTemplates()
            .FirstOrDefault(t => t.Name.Contains("AppXaml") && !t.Name.Contains("Cs"));
        appXamlTemplate.Should().NotBeNull("App.xaml 模板应存在于模板注册表中");

        var variables = BuildVariableContext(config);

        // Act
        var rendered = RenderTemplate(appXamlTemplate!.Content, variables);

        // Assert
        rendered.Should().Contain(expectedPrimary,
            $"主题 '{defaultTheme}' 的 App.xaml 应包含 PrimaryColor '{expectedPrimary}'");
        rendered.Should().Contain(expectedSecondary,
            $"主题 '{defaultTheme}' 的 App.xaml 应包含 SecondaryColor '{expectedSecondary}'");
    }

    /// <summary>
    /// 验证 DeepSeaBlue 主题渲染后包含 "BlueGrey" 和 "LightBlue"。
    /// </summary>
    [Fact]
    public async Task AppXaml_DeepSeaBlue_ShouldContainBlueGreyAndLightBlue()
    {
        await _registry.LoadTemplatesAsync();
        var config = CreateConfig("DeepSeaBlue");
        var rendered = await RenderAppXaml(config);

        rendered.Should().Contain("BlueGrey");
        rendered.Should().Contain("LightBlue");
    }

    /// <summary>
    /// 验证 MossGreen 主题渲染后包含 "Teal" 和 "Green"。
    /// </summary>
    [Fact]
    public async Task AppXaml_MossGreen_ShouldContainTealAndGreen()
    {
        await _registry.LoadTemplatesAsync();
        var config = CreateConfig("MossGreen");
        var rendered = await RenderAppXaml(config);

        rendered.Should().Contain("Teal");
        rendered.Should().Contain("Green");
    }

    /// <summary>
    /// 验证 CharcoalBlack 主题渲染后包含 "Grey"。
    /// </summary>
    [Fact]
    public async Task AppXaml_CharcoalBlack_ShouldContainGrey()
    {
        await _registry.LoadTemplatesAsync();
        var config = CreateConfig("CharcoalBlack");
        var rendered = await RenderAppXaml(config);

        rendered.Should().Contain("Grey");
    }

    /// <summary>
    /// 验证 BlueSteel 主题渲染后包含 "Indigo"。
    /// </summary>
    [Fact]
    public async Task AppXaml_BlueSteel_ShouldContainIndigo()
    {
        await _registry.LoadTemplatesAsync();
        var config = CreateConfig("BlueSteel");
        var rendered = await RenderAppXaml(config);

        rendered.Should().Contain("Indigo");
    }

    /// <summary>
    /// 验证 AmberIndustrial 主题渲染后包含 "Orange" 和 "Amber"。
    /// </summary>
    [Fact]
    public async Task AppXaml_AmberIndustrial_ShouldContainOrangeAndAmber()
    {
        await _registry.LoadTemplatesAsync();
        var config = CreateConfig("AmberIndustrial");
        var rendered = await RenderAppXaml(config);

        rendered.Should().Contain("Orange");
        rendered.Should().Contain("Amber");
    }

    /// <summary>
    /// 验证 NeutralSteel 主题渲染后包含 "Grey"。
    /// </summary>
    [Fact]
    public async Task AppXaml_NeutralSteel_ShouldContainGrey()
    {
        await _registry.LoadTemplatesAsync();
        var config = CreateConfig("NeutralSteel");
        var rendered = await RenderAppXaml(config);

        rendered.Should().Contain("Grey");
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

    private static ProjectConfig CreateConfig(string defaultTheme) => new()
    {
        ProjectName = "ThemeTest",
        DefaultTheme = defaultTheme,
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

    private async Task<string> RenderAppXaml(ProjectConfig config)
    {
        var appXamlTemplate = _registry.GetAllTemplates()
            .FirstOrDefault(t => t.Name.Contains("AppXaml") && !t.Name.Contains("Cs"));
        appXamlTemplate.Should().NotBeNull("App.xaml 模板应存在于模板注册表中");

        var variables = BuildVariableContext(config);
        return RenderTemplate(appXamlTemplate!.Content, variables);
    }

    private static Dictionary<string, object> BuildVariableContext(ProjectConfig config)
    {
        var resolver = new VariableResolver();
        return resolver.BuildVariableContext(config);
    }

    private static string RenderTemplate(string templateContent, Dictionary<string, object> variables)
    {
        var parsed = Template.Parse(templateContent);
        if (parsed.HasErrors)
        {
            var errors = string.Join("; ", parsed.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"模板解析失败：{errors}");
        }

        var scriptObject = new ScriptObject();
        foreach (var kv in variables)
        {
            scriptObject.Add(kv.Key, kv.Value);
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        return parsed.Render(context);
    }
}
