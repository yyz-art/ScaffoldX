using Xunit;
using ScaffoldX.Abstractions.Config;

namespace ScaffoldX.Abstractions.Tests.Config;

public class ScaffoldConfigSectionTests
{
    private readonly ScaffoldConfigSection _section = new();

    [Fact]
    public void SectionId_IsScaffold()
    {
        Assert.Equal("Scaffold", _section.SectionId);
    }

    [Fact]
    public void DisplayName_IsNotEmpty()
    {
        Assert.NotEmpty(_section.DisplayName);
    }

    [Fact]
    public void GetDefaults_ContainsProjectName()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("ProjectName"));
    }

    [Fact]
    public void GetDefaults_ContainsTargetFramework()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal("net10.0-windows", defaults["TargetFramework"]);
    }

    [Fact]
    public void GetDefaults_ContainsDerivedVariables()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("TargetFrameworkShort"));
        Assert.True(defaults.ContainsKey("IsWPF"));
        Assert.True(defaults.ContainsKey("IsAvalonia"));
        Assert.True(defaults.ContainsKey("XamlExt"));
        Assert.True(defaults.ContainsKey("SolutionName"));
        Assert.True(defaults.ContainsKey("RootNamespace"));
        Assert.True(defaults.ContainsKey("AssemblyName"));
    }

    [Fact]
    public void GetDefaults_DefaultUIFramework_IsWPF()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal("WPF", defaults["UIFramework"]);
        Assert.Equal(true, defaults["IsWPF"]);
        Assert.Equal(false, defaults["IsAvalonia"]);
    }

    [Fact]
    public void GetDefaults_WpfXamlVariables()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal("xaml", defaults["XamlExt"]);
        Assert.Equal(".xaml.cs", defaults["XamlCodeBehindExt"]);
        Assert.Equal("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", defaults["XamlNs"]);
    }

    [Fact]
    public void Validate_EmptyProjectName_HasError()
    {
        _section.ProjectName = "";
        var errors = _section.Validate();
        Assert.Contains(errors, e => e.PropertyName == "ProjectName");
    }

    [Fact]
    public void Validate_ValidProjectName_NoError()
    {
        _section.ProjectName = "MyApp";
        var errors = _section.Validate();
        Assert.DoesNotContain(errors, e => e.PropertyName == "ProjectName");
    }

    [Fact]
    public void Validate_EmptyOutputDirectory_HasError()
    {
        _section.OutputDirectory = "";
        var errors = _section.Validate();
        Assert.Contains(errors, e => e.PropertyName == "OutputDirectory");
    }
}
