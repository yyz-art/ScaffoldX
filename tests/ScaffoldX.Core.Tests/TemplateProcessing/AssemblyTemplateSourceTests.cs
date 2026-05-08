using FluentAssertions;
using ScaffoldX.Core.TemplateProcessing;
using Xunit;

namespace ScaffoldX.Core.Tests.TemplateProcessing;

/// <summary>
/// Unit tests for <see cref="AssemblyTemplateSource"/> covering template loading
/// and directive extraction logic.
/// </summary>
public class AssemblyTemplateSourceTests
{
    // ── ExtractOutputPathTemplate (tested via reflection on private static) ─

    [Fact]
    public void ExtractOutputPathTemplate_WithDirective_ExtractsAndRemovesDirective()
    {
        var content = "##OUTPUT: src/MyProject/Foo.cs\nnamespace MyProject;";

        var result = InvokeExtractOutputPathTemplate(ref content, "Foo");

        result.Should().Be("src/MyProject/Foo.cs");
        content.Should().NotContain("##OUTPUT:");
        content.Should().Contain("namespace MyProject;");
    }

    [Fact]
    public void ExtractOutputPathTemplate_NoDirective_ReturnsDefaultPath()
    {
        var content = "namespace MyProject;";

        var result = InvokeExtractOutputPathTemplate(ref content, "My.Foo");

        // Default: templateName.Replace('.', '/') + ".cs"
        result.Should().Be("My/Foo.cs");
    }

    [Fact]
    public void ExtractOutputPathTemplate_DirectiveWithLeadingWhitespace_Extracts()
    {
        var content = "  ##OUTPUT: src/Foo.cs\nbody";

        var result = InvokeExtractOutputPathTemplate(ref content, "Foo");

        result.Should().Be("src/Foo.cs");
    }

    [Fact]
    public void ExtractOutputPathTemplate_MultipleDirectives_ExtractsFirstOnly()
    {
        var content = "##OUTPUT: first.cs\n##OUTPUT: second.cs\nbody";

        var result = InvokeExtractOutputPathTemplate(ref content, "Foo");

        result.Should().Be("first.cs");
    }

    // ── ExtractIsRequired (tested via reflection on private static) ─────────

    [Fact]
    public void ExtractIsRequired_False_ReturnsFalse()
    {
        var content = "##REQUIRED: false\nbody";

        var result = InvokeExtractIsRequired(ref content);

        result.Should().BeFalse();
        content.Should().NotContain("##REQUIRED:");
    }

    [Fact]
    public void ExtractIsRequired_True_ReturnsTrue()
    {
        var content = "##REQUIRED: true\nbody";

        var result = InvokeExtractIsRequired(ref content);

        result.Should().BeTrue();
    }

    [Fact]
    public void ExtractIsRequired_NoDirective_ReturnsTrue()
    {
        var content = "body content";

        var result = InvokeExtractIsRequired(ref content);

        result.Should().BeTrue(); // default is true
    }

    [Fact]
    public void ExtractIsRequired_FalseUpperCase_ReturnsFalse()
    {
        var content = "##REQUIRED: FALSE\nbody";

        var result = InvokeExtractIsRequired(ref content);

        result.Should().BeFalse();
    }

    // ── LoadTemplatesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task LoadTemplatesAsync_LoadsTemplatesFromAssembly()
    {
        var source = new AssemblyTemplateSource();

        var templates = await source.LoadTemplatesAsync();

        templates.Should().NotBeEmpty("because ScaffoldX.Templates assembly contains embedded .stpl resources");
    }

    [Fact]
    public async Task LoadTemplatesAsync_AllTemplatesHaveName()
    {
        var source = new AssemblyTemplateSource();

        var templates = await source.LoadTemplatesAsync();

        templates.Should().AllSatisfy(t => t.Name.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public async Task LoadTemplatesAsync_AllTemplatesHaveCategory()
    {
        var source = new AssemblyTemplateSource();

        var templates = await source.LoadTemplatesAsync();

        templates.Should().AllSatisfy(t => t.Category.Should().NotBeNullOrWhiteSpace());
    }

    // ── Reflection helpers ──────────────────────────────────────────────────

    private static string InvokeExtractOutputPathTemplate(ref string content, string templateName)
    {
        var method = typeof(AssemblyTemplateSource).GetMethod(
            "ExtractOutputPathTemplate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        // ref parameter: pass as object array
        var args = new object[] { content, templateName };
        var result = (string)method.Invoke(null, args)!;
        content = (string)args[0]; // read back the modified ref
        return result;
    }

    private static bool InvokeExtractIsRequired(ref string content)
    {
        var method = typeof(AssemblyTemplateSource).GetMethod(
            "ExtractIsRequired",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var args = new object[] { content };
        var result = (bool)method.Invoke(null, args)!;
        content = (string)args[0]; // read back the modified ref
        return result;
    }
}
