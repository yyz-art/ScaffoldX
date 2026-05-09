using ScaffoldX.Plugin.Scaffold.Services;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.Services;

public class ScribanTemplateEngineTests
{
    private readonly ScribanTemplateEngine _engine = new();

    [Fact]
    public void Render_SimpleVariable()
    {
        var result = _engine.Render("Hello {{ name }}!", new Dictionary<string, object> { ["name"] = "World" });
        Assert.Equal("Hello World!", result);
    }

    [Fact]
    public void Render_MultipleVariables()
    {
        var result = _engine.Render("{{ a }} + {{ b }} = {{ c }}", new Dictionary<string, object>
        {
            ["a"] = "1", ["b"] = "2", ["c"] = "3"
        });
        Assert.Equal("1 + 2 = 3", result);
    }

    [Fact]
    public void Render_BooleanCondition()
    {
        var result = _engine.Render("{{ if enabled }}YES{{ end }}", new Dictionary<string, object>
        {
            ["enabled"] = true
        });
        Assert.Equal("YES", result);
    }

    [Fact]
    public void Render_InvalidTemplate_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.Render("{{ invalid.syntax.here!! }}", new Dictionary<string, object>()));
    }

    [Fact]
    public void Render_ForLoop()
    {
        var result = _engine.Render("{{ for item in items }}{{ item }}{{ end }}", new Dictionary<string, object>
        {
            ["items"] = new[] { "A", "B", "C" }
        });
        Assert.Equal("ABC", result);
    }
}
