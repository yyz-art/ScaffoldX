using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests;

public class ScaffoldPluginTests
{
    private static Scaffold.ScaffoldPlugin CreatePlugin()
    {
        return new Scaffold.ScaffoldPlugin();
    }

    [Fact]
    public void Metadata_Id_IsScaffold()
    {
        var plugin = CreatePlugin();
        Assert.Equal("Scaffold", plugin.Metadata.Id);
    }

    [Fact]
    public void Metadata_DisplayName_IsNotEmpty()
    {
        var plugin = CreatePlugin();
        Assert.False(string.IsNullOrEmpty(plugin.Metadata.DisplayName));
    }

    [Fact]
    public void Metadata_Order_Is1()
    {
        var plugin = CreatePlugin();
        Assert.Equal(1, plugin.Metadata.Order);
    }

    [Fact]
    public void Metadata_Dependencies_IsEmpty()
    {
        var plugin = CreatePlugin();
        Assert.Empty(plugin.Metadata.Dependencies);
    }

    [Fact]
    public async Task OnLoadedAsync_RegistersConfigSections()
    {
        var plugin = CreatePlugin();
        var host = new TestPluginHost();

        await plugin.OnLoadedAsync(host);

        Assert.NotNull(host.GetConfigSection("Scaffold"));
        Assert.NotNull(host.GetConfigSection("Scaffold.Collection"));
        Assert.NotNull(host.GetConfigSection("Scaffold.Vision"));
        Assert.NotNull(host.GetConfigSection("Scaffold.System"));
        Assert.NotNull(host.GetConfigSection("Scaffold.UI"));
    }

    [Fact]
    public async Task OnLoadedAsync_RegistersViews()
    {
        var plugin = CreatePlugin();
        var host = new TestPluginHost();

        await plugin.OnLoadedAsync(host);

        Assert.True(host.HasView("MainRegion", "Scaffold"));
    }

    [Fact]
    public async Task OnLoadedAsync_RegistersServices()
    {
        var plugin = CreatePlugin();
        var host = new TestPluginHost();

        await plugin.OnLoadedAsync(host);

        Assert.NotNull(host.GetService<Scaffold.Services.ITemplateEngine>());
        Assert.NotNull(host.GetService<Scaffold.Services.IValidationService>());
    }

    [Fact]
    public async Task OnLoadedAsync_ChangesStateToLoaded()
    {
        var plugin = CreatePlugin();
        Assert.Equal(PluginState.NotLoaded, plugin.State);

        var host = new TestPluginHost();
        await plugin.OnLoadedAsync(host);

        Assert.Equal(PluginState.Loaded, plugin.State);
    }

    [Fact]
    public async Task OnUnloadingAsync_ChangesState()
    {
        var plugin = CreatePlugin();
        var host = new TestPluginHost();
        await plugin.OnLoadedAsync(host);

        await plugin.OnUnloadingAsync();

        Assert.Equal(PluginState.Unloading, plugin.State);
    }

    private sealed class TestPluginHost : IPluginHost
    {
        private readonly Dictionary<string, IConfigSection> _configSections = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<(string, string), (Func<object>, Func<object>)> _views = new();

        public void RegisterView(string regionName, string navigationKey, Func<object> viewFactory, Func<object> viewModelFactory)
        {
            _views[(regionName, navigationKey)] = (viewFactory, viewModelFactory);
        }

        public void NavigateTo(string regionName, string navigationKey) { }

        public T? GetService<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var svc) ? (T?)svc : null;
        }

        public void RegisterConfigSection(IConfigSection section)
        {
            _configSections[section.SectionId] = section;
        }

        public IConfigSection? GetConfigSection(string sectionId)
        {
            return _configSections.TryGetValue(sectionId, out var s) ? s : null;
        }

        public bool HasView(string regionName, string navigationKey)
        {
            return _views.ContainsKey((regionName, navigationKey));
        }

        public void RegisterService<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
        }
    }
}
