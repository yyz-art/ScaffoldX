using FluentAssertions;
using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;
using Xunit;

namespace ScaffoldX.Core.Tests.TemplateProcessing;

/// <summary>
/// PostProcessor 单元测试，验证后处理逻辑。
/// </summary>
public class PostProcessorTests
{
    private readonly PostProcessor _sut = new();
    private readonly ProjectConfig _config = new() { ProjectName = "TestProject" };

    [Fact]
    public void Process_ShouldNormalizeLineEndings()
    {
        // Arrange
        var content = "line1\r\nline2\rline3\n";

        // Act
        var result = _sut.Process(content, "test.cs", _config);

        // Assert
        result.Should().NotContain("\r");
        result.Should().Contain("line1\nline2\nline3");
    }

    [Fact]
    public void Process_ShouldRestoreXmlEntities_ForXamlFiles()
    {
        // Arrange
        var content = "&lt;Window&gt; &amp; &quot;test&quot;";

        // Act
        var result = _sut.Process(content, "test.xaml", _config);

        // Assert
        result.Should().Contain("<Window>");
        result.Should().Contain("&");
        result.Should().Contain("\"test\"");
    }

    [Fact]
    public void Process_ShouldNotRestoreXmlEntities_ForCsFiles()
    {
        // Arrange
        var content = "&lt;Window&gt; &amp; &quot;test&quot;";

        // Act
        var result = _sut.Process(content, "test.cs", _config);

        // Assert
        result.Should().Contain("&lt;Window&gt;");
    }

    [Fact]
    public void Process_ShouldTrimTrailingWhitespace()
    {
        // Arrange
        var content = "line1   \nline2\t\n";

        // Act
        var result = _sut.Process(content, "test.cs", _config);

        // Assert
        result.Should().Be("line1\nline2\n");
    }

    [Fact]
    public void Process_ShouldEnsureTrailingNewline()
    {
        // Arrange
        var content = "some content";

        // Act
        var result = _sut.Process(content, "test.cs", _config);

        // Assert
        result.Should().EndWith("\n");
    }

    [Fact]
    public void Process_ShouldHandleEmptyContent()
    {
        // Arrange
        var content = string.Empty;

        // Act
        var result = _sut.Process(content, "test.cs", _config);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("test.xaml")]
    [InlineData("test.axaml")]
    [InlineData("test.xml")]
    [InlineData("test.csproj")]
    [InlineData("test.props")]
    [InlineData("test.cs")]
    [InlineData("test.json")]
    public void Process_ShouldNotThrow_ForAnyFileType(string path)
    {
        // Act
        var result = _sut.Process("test", path, _config);

        // Assert - just verify it doesn't throw
        result.Should().NotBeNull();
    }
}
