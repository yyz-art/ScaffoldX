using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;
using Xunit;

namespace ScaffoldX.Plugin.Management.Tests;

public class ManagementPluginTests
{
    private static Management.ManagementPlugin CreatePlugin() => new();

    [Fact]
    public void Metadata_Id_IsManagement()
    {
        Assert.Equal("Management", CreatePlugin().Metadata.Id);
    }

    [Fact]
    public void Metadata_Order_Is5()
    {
        Assert.Equal(5, CreatePlugin().Metadata.Order);
    }

    [Fact]
    public void Metadata_Dependencies_ContainsScaffold()
    {
        Assert.Contains("Scaffold", CreatePlugin().Metadata.Dependencies);
    }

    [Fact]
    public async Task OnLoadedAsync_RegistersView()
    {
        var plugin = CreatePlugin();
        var host = new TestPluginHost();
        await plugin.OnLoadedAsync(host);
        Assert.True(host.HasView("MainRegion", "Management"));
    }

    [Fact]
    public async Task OnLoadedAsync_ChangesStateToLoaded()
    {
        var plugin = CreatePlugin();
        var host = new TestPluginHost();
        await plugin.OnLoadedAsync(host);
        Assert.Equal(PluginState.Loaded, plugin.State);
    }

    private sealed class TestPluginHost : IPluginHost
    {
        private readonly Dictionary<string, IConfigSection> _configSections = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<(string, string), (Func<object>, Func<object>)> _views = new();

        public void RegisterView(string regionName, string navigationKey, Func<object> viewFactory, Func<object> viewModelFactory)
            => _views[(regionName, navigationKey)] = (viewFactory, viewModelFactory);
        public void NavigateTo(string regionName, string navigationKey) { }
        public T? GetService<T>() where T : class => _services.TryGetValue(typeof(T), out var svc) ? (T?)svc : null;
        public void RegisterService<T>(T instance) where T : class => _services[typeof(T)] = instance;
        public void RegisterConfigSection(IConfigSection section) => _configSections[section.SectionId] = section;
        public IConfigSection? GetConfigSection(string sectionId) => _configSections.TryGetValue(sectionId, out var s) ? s : null;
        public bool HasView(string regionName, string navigationKey) => _views.ContainsKey((regionName, navigationKey));
    }
}
