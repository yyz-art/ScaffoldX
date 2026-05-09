using Xunit;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Templates;

namespace ScaffoldX.Abstractions.Tests.Templates;

public class TemplateFilterRegressionTests
{
    private static IReadOnlyDictionary<string, object> BuildContext(
        bool enableS7 = false, bool enableModbus = false, bool enableOpcUa = false,
        bool enableMitsubishi = false, bool enableOmron = false, bool enableSimulation = true,
        bool enableVision = false, bool enableUserMgmt = false, bool enableRolePerm = false,
        bool enableSystemLog = false, bool enableThemeSwitcher = false,
        bool enableLoginWindow = false, bool enableLocalization = false,
        string navigationStyle = "LeftSidebar")
    {
        var registry = new ConfigRegistry();
        registry.Register(new ScaffoldConfigSection
        {
            ProjectName = "TestApp",
            TargetFramework = "net10.0-windows",
            UIFramework = "WPF",
            OutputDirectory = @"C:\Test",
        });
        registry.Register(new CollectionConfigSection
        {
            EnableSiemensS7 = enableS7,
            EnableModbusTcp = enableModbus,
            EnableOpcUa = enableOpcUa,
            EnableMitsubishiMc = enableMitsubishi,
            EnableOmronFins = enableOmron,
            EnableSimulationDriver = enableSimulation,
        });
        registry.Register(new VisionConfigSection
        {
            EnableVision = enableVision,
        });
        registry.Register(new SystemConfigSection
        {
            EnableUserManagement = enableUserMgmt,
            EnableRolePermission = enableRolePerm,
            EnableSystemLog = enableSystemLog,
            EnableThemeSwitcher = enableThemeSwitcher,
            EnableLoginWindow = enableLoginWindow,
        });
        registry.Register(new UIConfigSection
        {
            NavigationStyle = navigationStyle,
            EnableLocalization = enableLocalization,
        });
        return new AggregatedConfigResolver(registry).BuildVariableContext();
    }

    [Fact]
    public async Task Filter_S7Enabled_IncludesS7Templates()
    {
        var ctx = BuildContext(enableS7: true);
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "S7Driver.cs");
        Assert.Contains(result, d => d.Name == "S7DriverCsproj");
        Assert.DoesNotContain(result, d => d.Name == "ModbusTcpDriver.cs");
    }

    [Fact]
    public async Task Filter_S7Disabled_ExcludesS7Templates()
    {
        var ctx = BuildContext(enableModbus: true);
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.DoesNotContain(result, d => d.Name == "S7Driver.cs");
        Assert.Contains(result, d => d.Name == "ModbusTcpDriver.cs");
    }

    [Fact]
    public async Task Filter_LeftSidebar_IncludesSidebarExcludesTopNav()
    {
        var ctx = BuildContext(navigationStyle: "LeftSidebar");
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "SidebarView.xaml");
        Assert.Contains(result, d => d.Name == "SidebarViewModel.cs");
        Assert.DoesNotContain(result, d => d.Name == "TopNavView.xaml");
        Assert.DoesNotContain(result, d => d.Name == "TopNavViewModel.cs");
    }

    [Fact]
    public async Task Filter_TopNav_IncludesTopNavExcludesSidebar()
    {
        var ctx = BuildContext(navigationStyle: "TopNav");
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "TopNavView.xaml");
        Assert.Contains(result, d => d.Name == "TopNavViewModel.cs");
        Assert.DoesNotContain(result, d => d.Name == "SidebarView.xaml");
        Assert.DoesNotContain(result, d => d.Name == "SidebarViewModel.cs");
    }

    [Fact]
    public async Task Filter_VisionEnabled_IncludesVisionTemplates()
    {
        var ctx = BuildContext(enableVision: true);
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Category == "Vision");
    }

    [Fact]
    public async Task Filter_VisionDisabled_ExcludesVisionTemplates()
    {
        var ctx = BuildContext();
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.DoesNotContain(result, d => d.Category == "Vision");
    }

    [Fact]
    public async Task Filter_LocalizationEnabled_IncludesResxTemplates()
    {
        var ctx = BuildContext(enableLocalization: true);
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "StringsEnUsResx");
        Assert.Contains(result, d => d.Name == "StringsZhCnResx");
    }

    [Fact]
    public async Task Filter_LocalizationDisabled_ExcludesResxTemplates()
    {
        var ctx = BuildContext();
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.DoesNotContain(result, d => d.Name == "StringsEnUsResx");
        Assert.DoesNotContain(result, d => d.Name == "StringsZhCnResx");
    }

    [Fact]
    public async Task Filter_UserMgmtEnabled_IncludesUserMgmtTemplates()
    {
        var ctx = BuildContext(enableUserMgmt: true);
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "IUserService.cs");
        Assert.Contains(result, d => d.Name == "UserService.cs");
    }

    [Fact]
    public async Task Filter_SystemCore_IncludedWhenAnyModuleEnabled()
    {
        var ctx = BuildContext(enableSystemLog: true);
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "UserRole.cs");
        Assert.Contains(result, d => d.Name == "IMenuModule.cs");
    }

    [Fact]
    public async Task Filter_AlwaysIncludesRequiredTemplates()
    {
        var ctx = BuildContext();
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "Solution.sln");
        Assert.Contains(result, d => d.Name == "AppCsproj");
        Assert.Contains(result, d => d.Name == "CoreCsproj");
    }

    [Fact]
    public async Task Filter_LoginWindow_IncludedWhenEnabled()
    {
        var ctx = BuildContext(enableLoginWindow: true);
        var metas = await LoadAllMetas();
        var filter = new DeclarativeTemplateFilter();
        var result = filter.Apply(metas, ctx);

        Assert.Contains(result, d => d.Name == "LoginWindowXaml");
        Assert.Contains(result, d => d.Name == "LoginViewModel.cs");
    }

    private static async Task<List<TemplateDescriptor>> LoadAllMetas()
    {
        var templatesDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "ScaffoldX.Templates");
        templatesDir = Path.GetFullPath(templatesDir);

        if (!Directory.Exists(templatesDir))
        {
            var altDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..",
                "src", "ScaffoldX.Templates");
            templatesDir = Path.GetFullPath(altDir);
        }

        var metas = await TemplateMetadataLoader.LoadFromDirectoryAsync(templatesDir);
        return metas.Select(m => m.ToDescriptor()).ToList();
    }
}
