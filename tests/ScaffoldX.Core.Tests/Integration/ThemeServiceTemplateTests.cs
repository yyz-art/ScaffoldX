using FluentAssertions;
using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;
using Scriban;
using Scriban.Runtime;
using Xunit;

namespace ScaffoldX.Core.Tests.Integration;

/// <summary>
/// 集成测试：验证 ThemeService.cs.stpl 模板渲染后生成正确的六套主题定义。
/// </summary>
public class ThemeServiceTemplateTests
{
    private readonly TemplateRegistry _registry = new(new AssemblyTemplateSource());

    /// <summary>
    /// 六套主题的 ID 列表，用于验证渲染后的 ThemeService 包含所有主题。
    /// </summary>
    private static readonly string[] ExpectedThemeIds =
    {
        "DeepSeaBlue", "MossGreen", "CharcoalBlack",
        "BlueSteel", "AmberIndustrial", "NeutralSteel"
    };

    /// <summary>
    /// 验证 ThemeService 模板渲染后包含六套主题定义。
    /// </summary>
    [Fact]
    public async Task ThemeService_ShouldContainSixThemes_WhenRendered()
    {
        // Arrange
        await _registry.LoadTemplatesAsync();
        var themeServiceTemplate = _registry.GetAllTemplates()
            .FirstOrDefault(t => t.Name.Contains("ThemeService") && !t.Name.Contains("ITheme"));
        themeServiceTemplate.Should().NotBeNull("ThemeService 模板应存在于模板注册表中");

        var config = new ProjectConfig { ProjectName = "ThemeProject" };
        var variables = BuildVariableContext(config);

        // Act
        var rendered = RenderTemplate(themeServiceTemplate!.Content, variables);

        // Assert
        foreach (var themeId in ExpectedThemeIds)
        {
            rendered.Should().Contain($"\"{themeId}\"",
                $"ThemeService 应包含主题 '{themeId}' 的定义");
        }
    }

    /// <summary>
    /// 验证 ThemeService 模板渲染后包含 GetAvailableThemes 方法。
    /// </summary>
    [Fact]
    public async Task ThemeService_ShouldContainGetAvailableThemes_WhenRendered()
    {
        // Arrange
        await _registry.LoadTemplatesAsync();
        var themeServiceTemplate = _registry.GetAllTemplates()
            .FirstOrDefault(t => t.Name.Contains("ThemeService") && !t.Name.Contains("ITheme"));

        var config = new ProjectConfig { ProjectName = "ThemeProject" };
        var variables = BuildVariableContext(config);

        // Act
        var rendered = RenderTemplate(themeServiceTemplate!.Content, variables);

        // Assert
        rendered.Should().Contain("GetAvailableThemes",
            "ThemeService 应包含 GetAvailableThemes 方法");
    }

    /// <summary>
    /// 验证 ThemeService 模板渲染后包含 SetThemeAsync 方法和 GetCurrentTheme 方法。
    /// </summary>
    [Fact]
    public async Task ThemeService_ShouldContainSetThemeAndGetCurrent_WhenRendered()
    {
        // Arrange
        await _registry.LoadTemplatesAsync();
        var themeServiceTemplate = _registry.GetAllTemplates()
            .FirstOrDefault(t => t.Name.Contains("ThemeService") && !t.Name.Contains("ITheme"));

        var config = new ProjectConfig { ProjectName = "ThemeProject" };
        var variables = BuildVariableContext(config);

        // Act
        var rendered = RenderTemplate(themeServiceTemplate!.Content, variables);

        // Assert
        rendered.Should().Contain("SetThemeAsync",
            "ThemeService 应包含 SetThemeAsync 方法");
        rendered.Should().Contain("GetCurrentTheme",
            "ThemeService 应包含 GetCurrentTheme 方法");
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

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
