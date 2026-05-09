using Xunit;
using ScaffoldX.Abstractions.Config;

namespace ScaffoldX.Abstractions.Tests.Config;

public class CollectionConfigSectionTests
{
    private readonly CollectionConfigSection _section = new();

    [Fact]
    public void SectionId_IsScaffoldCollection()
    {
        Assert.Equal("Scaffold.Collection", _section.SectionId);
    }

    [Fact]
    public void GetDefaults_ContainsDriverFeatures()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("EnableSiemensS7"));
        Assert.True(defaults.ContainsKey("EnableModbusTcp"));
        Assert.True(defaults.ContainsKey("EnableOpcUa"));
        Assert.True(defaults.ContainsKey("EnableMitsubishiMc"));
        Assert.True(defaults.ContainsKey("EnableOmronFins"));
    }

    [Fact]
    public void GetDefaults_AllDriversDefaultFalse()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal(false, defaults["EnableSiemensS7"]);
        Assert.Equal(false, defaults["EnableModbusTcp"]);
        Assert.Equal(false, defaults["EnableOpcUa"]);
        Assert.Equal(false, defaults["EnableMitsubishiMc"]);
        Assert.Equal(false, defaults["EnableOmronFins"]);
    }

    [Fact]
    public void GetDefaults_ContainsHasAnyCollection()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("HasAnyCollection"));
        Assert.Equal(false, defaults["HasAnyCollection"]);
    }

    [Fact]
    public void GetDefaults_ContainsPlcDefaults()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal("192.168.1.1", defaults["DefaultPLCIp"]);
        Assert.Equal(102, defaults["DefaultPLCPort"]);
        Assert.Equal(0, defaults["S7Rack"]);
        Assert.Equal(1, defaults["S7Slot"]);
    }

    [Fact]
    public void GetDefaults_SimulationDriverDefaultTrue()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal(true, defaults["EnableSimulationDriver"]);
    }

    [Fact]
    public void GetDefaults_HasAnyCollection_TrueWhenAnyDriverEnabled()
    {
        _section.EnableSiemensS7 = true;
        var defaults = _section.GetDefaults();
        Assert.Equal(true, defaults["HasAnyCollection"]);
    }
}

public class VisionConfigSectionTests
{
    private readonly VisionConfigSection _section = new();

    [Fact]
    public void SectionId_IsScaffoldVision()
    {
        Assert.Equal("Scaffold.Vision", _section.SectionId);
    }

    [Fact]
    public void GetDefaults_ContainsVisionFeatures()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("EnableVision"));
        Assert.True(defaults.ContainsKey("CameraBrand"));
        Assert.True(defaults.ContainsKey("ModelType"));
    }

    [Fact]
    public void GetDefaults_EnableVisionDefaultFalse()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal(false, defaults["EnableVision"]);
    }

    [Fact]
    public void GetDefaults_ContainsPascalCaseDerived()
    {
        _section.CameraBrand = "HikVision";
        _section.ModelType = "Object-Detection";
        var defaults = _section.GetDefaults();
        Assert.Equal("HikVision", defaults["CameraBrandPascal"]);
        Assert.Equal("ObjectDetection", defaults["ModelTypePascal"]);
    }
}

public class SystemConfigSectionTests
{
    private readonly SystemConfigSection _section = new();

    [Fact]
    public void SectionId_IsScaffoldSystem()
    {
        Assert.Equal("Scaffold.System", _section.SectionId);
    }

    [Fact]
    public void GetDefaults_ContainsModuleFeatures()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("EnableUserManagement"));
        Assert.True(defaults.ContainsKey("EnableRolePermission"));
        Assert.True(defaults.ContainsKey("EnableSystemLog"));
        Assert.True(defaults.ContainsKey("EnableThemeSwitcher"));
    }

    [Fact]
    public void GetDefaults_AllModulesDefaultFalse()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal(false, defaults["EnableUserManagement"]);
        Assert.Equal(false, defaults["EnableRolePermission"]);
        Assert.Equal(false, defaults["EnableSystemLog"]);
        Assert.Equal(false, defaults["EnableThemeSwitcher"]);
    }

    [Fact]
    public void GetDefaults_ContainsSelectedModules()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("SelectedModules"));
    }

    [Fact]
    public void GetDefaults_SelectedModules_ContainsEnabledModules()
    {
        _section.EnableUserManagement = true;
        _section.EnableSystemLog = true;
        var defaults = _section.GetDefaults();
        var modules = Assert.IsType<List<string>>(defaults["SelectedModules"]);
        Assert.Equal(2, modules.Count);
        Assert.Contains("UserManagement", modules);
        Assert.Contains("SystemLog", modules);
    }

    [Fact]
    public void GetDefaults_ContainsExtendedOptions()
    {
        var defaults = _section.GetDefaults();
        Assert.True(defaults.ContainsKey("EnableLoginWindow"));
        Assert.True(defaults.ContainsKey("EnableCrossPlatform"));
        Assert.True(defaults.ContainsKey("ForcePasswordChange"));
        Assert.True(defaults.ContainsKey("DatabaseType"));
    }
}

public class UIConfigSectionTests
{
    private readonly UIConfigSection _section = new();

    [Fact]
    public void SectionId_IsScaffoldUI()
    {
        Assert.Equal("Scaffold.UI", _section.SectionId);
    }

    [Fact]
    public void GetDefaults_ContainsNavigationStyle()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal("LeftSidebar", defaults["NavigationStyle"]);
    }

    [Fact]
    public void GetDefaults_ContainsThemeAndLanguage()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal("IndustrialDark", defaults["DefaultTheme"]);
        Assert.Equal("zh-CN", defaults["DefaultLanguage"]);
    }

    [Fact]
    public void GetDefaults_EnableLocalizationDefaultFalse()
    {
        var defaults = _section.GetDefaults();
        Assert.Equal(false, defaults["EnableLocalization"]);
    }
}
