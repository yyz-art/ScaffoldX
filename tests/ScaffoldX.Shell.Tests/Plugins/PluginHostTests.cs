using Xunit;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Shell.Plugins;

namespace ScaffoldX.Shell.Tests.Plugins;

public class PluginHostTests
{
    [Fact]
    public void RegisterConfigSection_And_GetConfigSection()
    {
        var host = new PluginHost();
        var section = new TestConfigSection("test", "Test");

        host.RegisterConfigSection(section);
        var result = host.GetConfigSection("test");

        Assert.NotNull(result);
        Assert.Same(section, result);
    }

    [Fact]
    public void GetConfigSection_NotRegistered_ReturnsNull()
    {
        var host = new PluginHost();

        var result = host.GetConfigSection("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void RegisterView_AddsToRegistry()
    {
        var host = new PluginHost();
        host.RegisterView("MainRegion", "ScaffoldView",
            () => new object(),
            () => new object());

        Assert.True(host.HasView("MainRegion", "ScaffoldView"));
    }

    [Fact]
    public void RegisterService_And_GetService()
    {
        var host = new PluginHost();
        var service = new TestService();
        host.RegisterService<ITestService>(service);

        var result = host.GetService<ITestService>();
        Assert.Same(service, result);
    }

    [Fact]
    public void GetService_NotRegistered_ReturnsNull()
    {
        var host = new PluginHost();
        var result = host.GetService<ITestService>();
        Assert.Null(result);
    }

    private sealed class TestConfigSection(string sectionId, string displayName) : IConfigSection
    {
        public string SectionId => sectionId;
        public string DisplayName => displayName;
        public Dictionary<string, object> GetDefaults() => new();
        public IReadOnlyList<ValidationError> Validate() => [];
    }

    private interface ITestService { }
    private sealed class TestService : ITestService { }
}
