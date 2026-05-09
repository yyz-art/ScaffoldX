using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Plugin.Annotation.Models;
using Xunit;

namespace ScaffoldX.Plugin.Annotation.Tests;

public class AnnotationPluginTests
{
    private static Annotation.AnnotationPlugin CreatePlugin()
    {
        return new Annotation.AnnotationPlugin();
    }

    [Fact]
    public void Metadata_Id_IsAnnotation()
    {
        var plugin = CreatePlugin();
        Assert.Equal("Annotation", plugin.Metadata.Id);
    }

    [Fact]
    public void Metadata_DisplayName_IsNotEmpty()
    {
        var plugin = CreatePlugin();
        Assert.False(string.IsNullOrEmpty(plugin.Metadata.DisplayName));
    }

    [Fact]
    public void Metadata_Order_Is2()
    {
        var plugin = CreatePlugin();
        Assert.Equal(2, plugin.Metadata.Order);
    }

    [Fact]
    public void Metadata_Dependencies_IsEmpty()
    {
        var plugin = CreatePlugin();
        Assert.Empty(plugin.Metadata.Dependencies);
    }

    [Fact]
    public void Metadata_FeatureToggles_ContainsAutoLabeling()
    {
        var plugin = CreatePlugin();
        Assert.Contains(plugin.Metadata.FeatureToggles, t => t.Key == "AutoLabeling");
    }

    [Fact]
    public void Metadata_FeatureToggles_ContainsSam3Segmentation()
    {
        var plugin = CreatePlugin();
        Assert.Contains(plugin.Metadata.FeatureToggles, t => t.Key == "Sam3Segmentation");
    }

    [Fact]
    public async Task OnLoadedAsync_RegistersViews()
    {
        var plugin = CreatePlugin();
        var host = new TestPluginHost();

        await plugin.OnLoadedAsync(host);

        Assert.True(host.HasView("MainRegion", "Annotation"));
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

        public void RegisterService<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
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
    }
}
