using System.IO;
using FluentAssertions;
using Moq;
using ScaffoldX.App.Services;
using ScaffoldX.Core.TemplateProcessing;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// ValidationService 单元测试，覆盖所有 5 个公共方法的正常和边界场景。
/// </summary>
public class ValidationServiceTests
{
    private static IVariableResolver CreateResolver()
    {
        var real = new VariableResolver();
        var mock = new Mock<IVariableResolver>();
        mock.Setup(r => r.ToPascalCase(It.IsAny<string>()))
            .Returns((string input) => real.ToPascalCase(input));
        return mock.Object;
    }

    private readonly ValidationService _sut = new(CreateResolver());

    // ── ValidateProjectName ──────────────────────────────────────────────────

    [Theory]
    [InlineData("MyProject")]
    [InlineData("A")]
    [InlineData("Test_Project")]
    [InlineData("Project123")]
    [InlineData("a")]
    public void ValidateProjectName_ShouldReturnValid_WhenNameIsValid(string name)
    {
        var result = _sut.ValidateProjectName(name);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateProjectName_ShouldReturnInvalid_WhenNameIsNullOrEmpty(string? name)
    {
        var result = _sut.ValidateProjectName(name!);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("123Project")]   // 数字开头
    [InlineData("_Project")]     // 下划线开头
    [InlineData("My Project")]   // 含空格
    [InlineData("My-Project")]   // 含连字符
    public void ValidateProjectName_ShouldReturnInvalid_WhenNameHasInvalidFormat(string name)
    {
        var result = _sut.ValidateProjectName(name);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateProjectName_ShouldReturnInvalid_WhenNameExceeds50Chars()
    {
        var longName = new string('A', 51);
        var result = _sut.ValidateProjectName(longName);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateProjectName_ShouldReturnValid_WhenNameIsExactly50Chars()
    {
        var name = "A" + new string('b', 49); // 50 chars total
        var result = _sut.ValidateProjectName(name);
        result.IsValid.Should().BeTrue();
    }

    // ── ValidateIpAddress ────────────────────────────────────────────────────

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("255.255.255.255")]
    public void ValidateIpAddress_ShouldReturnValid_WhenIpIsValidIPv4(string ip)
    {
        var result = _sut.ValidateIpAddress(ip);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateIpAddress_ShouldReturnInvalid_WhenIpIsNullOrEmpty(string? ip)
    {
        var result = _sut.ValidateIpAddress(ip!);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("999.999.999.999")]
    [InlineData("abc.def.ghi.jkl")]
    [InlineData("not-an-ip")]
    public void ValidateIpAddress_ShouldReturnInvalid_WhenIpIsMalformed(string ip)
    {
        var result = _sut.ValidateIpAddress(ip);
        result.IsValid.Should().BeFalse();
    }

    // ── ValidatePort ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(80)]
    [InlineData(443)]
    [InlineData(8080)]
    [InlineData(65535)]
    public void ValidatePort_ShouldReturnValid_WhenPortIsInRange(int port)
    {
        var result = _sut.ValidatePort(port);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(99999)]
    public void ValidatePort_ShouldReturnInvalid_WhenPortIsOutOfRange(int port)
    {
        var result = _sut.ValidatePort(port);
        result.IsValid.Should().BeFalse();
    }

    // ── ToPascalCase ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("my_project", "MyProject")]
    [InlineData("my-project", "MyProject")]
    [InlineData("my project", "MyProject")]
    [InlineData("myProject", "MyProject")]
    [InlineData("MY_PROJECT", "MyProject")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    public void ToPascalCase_ShouldConvertCorrectly(string? input, string expected)
    {
        var result = _sut.ToPascalCase(input!);
        result.Should().Be(expected);
    }

    // ── ValidateOutputPath ───────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateOutputPath_ShouldReturnInvalid_WhenPathIsNullOrEmpty(string? path)
    {
        var result = _sut.ValidateOutputPath(path!, "TestProject");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateOutputPath_ShouldReturnInvalid_WhenPathDoesNotExist()
    {
        var result = _sut.ValidateOutputPath(@"Z:\nonexistent\path", "TestProject");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateOutputPath_ShouldReturnInvalid_WhenTargetDirAlreadyExists()
    {
        // Arrange — 创建临时目录和同名子目录
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ScaffoldX_Test_{Guid.NewGuid():N}");
        var existingSub = Path.Combine(tempRoot, "ExistingProject");
        Directory.CreateDirectory(existingSub);

        try
        {
            // Act
            var result = _sut.ValidateOutputPath(tempRoot, "ExistingProject");

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("已存在");
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ValidateOutputPath_ShouldReturnValid_WhenPathExistsAndNoConflict()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ScaffoldX_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            // Act
            var result = _sut.ValidateOutputPath(tempRoot, "NewProject");

            // Assert
            result.IsValid.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }
}
