using FluentAssertions;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// ScribanTemplateEngine 单元测试，验证模板渲染和错误处理。
/// </summary>
public class ScribanTemplateEngineTests
{
    private readonly ScribanTemplateEngine _sut = new();

    [Fact]
    public void Render_ShouldReplaceVariables_WhenValidTemplate()
    {
        // Arrange
        var template = "Hello {{Name}}, welcome to {{Project}}!";
        var variables = new Dictionary<string, object>
        {
            ["Name"] = "World",
            ["Project"] = "ScaffoldX"
        };

        // Act
        var result = _sut.Render(template, variables);

        // Assert
        result.Should().Be("Hello World, welcome to ScaffoldX!");
    }

    [Fact]
    public void Render_ShouldHandleEmptyTemplate()
    {
        // Arrange
        var variables = new Dictionary<string, object>();

        // Act
        var result = _sut.Render("", variables);

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void Render_ShouldHandleTemplateWithNoVariables()
    {
        // Arrange
        var template = "No variables here.";
        var variables = new Dictionary<string, object>();

        // Act
        var result = _sut.Render(template, variables);

        // Assert
        result.Should().Be("No variables here.");
    }

    [Fact]
    public void Render_ShouldHandleBooleanVariables()
    {
        // Arrange
        var template = "{{if EnableFeature}}enabled{{else}}disabled{{end}}";
        var variables = new Dictionary<string, object>
        {
            ["EnableFeature"] = true
        };

        // Act
        var result = _sut.Render(template, variables);

        // Assert
        result.Should().Contain("enabled");
    }

    [Fact]
    public void Render_ShouldThrow_WhenTemplateHasInvalidSyntax()
    {
        // Arrange
        var template = "{{invalid syntax {{{{";
        var variables = new Dictionary<string, object>();

        // Act & Assert
        var act = () => _sut.Render(template, variables);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*模板解析失败*");
    }

    [Fact]
    public void Render_ShouldHandleNestedObjectProperties()
    {
        // Arrange
        var template = "{{Config.Name}} v{{Config.Version}}";
        var configObj = new Dictionary<string, object>
        {
            ["Name"] = "MyApp",
            ["Version"] = "1.0.0"
        };
        var variables = new Dictionary<string, object>
        {
            ["Config"] = configObj
        };

        // Act
        var result = _sut.Render(template, variables);

        // Assert
        result.Should().Be("MyApp v1.0.0");
    }

    [Fact]
    public void Render_ShouldHandleLoopSyntax()
    {
        // Arrange
        var template = "{{for item in Items}}{{item}} {{end}}";
        var variables = new Dictionary<string, object>
        {
            ["Items"] = new List<string> { "A", "B", "C" }
        };

        // Act
        var result = _sut.Render(template, variables);

        // Assert
        result.Should().Contain("A");
        result.Should().Contain("B");
        result.Should().Contain("C");
    }
}
