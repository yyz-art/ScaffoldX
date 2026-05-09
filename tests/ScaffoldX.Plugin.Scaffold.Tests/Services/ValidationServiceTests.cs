using ScaffoldX.Plugin.Scaffold.Services;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _svc = new();

    [Theory]
    [InlineData("MyProject", true)]
    [InlineData("A", true)]
    [InlineData("Test_123", true)]
    [InlineData("1Project", false)]
    [InlineData("", false)]
    [InlineData("ab!", false)]
    public void ValidateProjectName(string name, bool expected)
    {
        var result = _svc.ValidateProjectName(name);
        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void ToPascalCase_UnderscoreSeparated()
    {
        Assert.Equal("MyProject", _svc.ToPascalCase("my_project"));
    }

    [Fact]
    public void ToPascalCase_HyphenSeparated()
    {
        Assert.Equal("TestProject", _svc.ToPascalCase("test-project"));
    }

    [Fact]
    public void ToPascalCase_EmptyInput()
    {
        Assert.Equal(string.Empty, _svc.ToPascalCase(""));
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("256.0.0.1", false)]
    [InlineData("", false)]
    public void ValidateIpAddress(string ip, bool expected)
    {
        var result = _svc.ValidateIpAddress(ip);
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData(102, true)]
    [InlineData(0, false)]
    [InlineData(65536, false)]
    public void ValidatePort(int port, bool expected)
    {
        var result = _svc.ValidatePort(port);
        Assert.Equal(expected, result.IsValid);
    }
}
