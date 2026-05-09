using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Shell.Plugins;
using Xunit;

namespace ScaffoldX.Shell.Tests.Plugins;

public class PluginBootstrapperTests
{
    private static IPlugin CreatePlugin(
        string id,
        string[]? dependencies = null,
        Action<IPluginHost>? onLoaded = null)
    {
        dependencies ??= [];
        var metadata = new PluginMetadata
        {
            Id = id,
            DisplayName = id,
            Dependencies = dependencies
        };

        var state = PluginState.NotLoaded;

        return new TestPlugin(metadata, state, onLoaded);
    }

    private sealed class TestPlugin(
        PluginMetadata metadata,
        PluginState initialState,
        Action<IPluginHost>? onLoaded) : IPlugin
    {
        public PluginMetadata Metadata { get; } = metadata;
        public PluginState State { get; private set; } = initialState;

        public Task OnLoadedAsync(IPluginHost host)
        {
            onLoaded?.Invoke(host);
            State = PluginState.Loaded;
            return Task.CompletedTask;
        }

        public Task OnUnloadingAsync()
        {
            State = PluginState.Unloading;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task LoadAllAsync_SortsByDependencies()
    {
        var loadOrder = new List<string>();
        var a = CreatePlugin("A", ["B"], _ => loadOrder.Add("A"));
        var b = CreatePlugin("B", ["C"], _ => loadOrder.Add("B"));
        var c = CreatePlugin("C", onLoaded: _ => loadOrder.Add("C"));

        var bootstrapper = new PluginBootstrapper([a, b, c]);
        var host = new PluginHost();
        await bootstrapper.LoadAllAsync(host);

        Assert.Equal(["C", "B", "A"], loadOrder);
    }

    [Fact]
    public async Task LoadAllAsync_RegistersConfigSectionsFromPlugins()
    {
        var section = new TestConfigSection("TestSection");
        var plugin = CreatePlugin("P", onLoaded: h => h.RegisterConfigSection(section));

        var bootstrapper = new PluginBootstrapper([plugin]);
        var host = new PluginHost();
        await bootstrapper.LoadAllAsync(host);

        Assert.Same(section, host.GetConfigSection("TestSection"));
    }

    [Fact]
    public async Task LoadAllAsync_RegistersViewsFromPlugins()
    {
        var plugin = CreatePlugin("P", onLoaded: h =>
            h.RegisterView("MainRegion", "TestView", () => "view", () => "vm"));

        var bootstrapper = new PluginBootstrapper([plugin]);
        var host = new PluginHost();
        await bootstrapper.LoadAllAsync(host);

        Assert.True(host.HasView("MainRegion", "TestView"));
    }

    [Fact]
    public async Task LoadAllAsync_PluginError_DoesNotStopOtherPlugins()
    {
        var loadOrder = new List<string>();
        var bad = CreatePlugin("Bad", onLoaded: _ => throw new InvalidOperationException("boom"));
        var good = CreatePlugin("Good", onLoaded: _ => loadOrder.Add("Good"));

        var bootstrapper = new PluginBootstrapper([bad, good]);
        var host = new PluginHost();
        await bootstrapper.LoadAllAsync(host);

        Assert.Contains("Good", loadOrder);
        Assert.Contains(bad, bootstrapper.FailedPlugins);
    }

    [Fact]
    public async Task LoadAllAsync_AllPluginsLoaded_NoFailedPlugins()
    {
        var a = CreatePlugin("A");
        var b = CreatePlugin("B");

        var bootstrapper = new PluginBootstrapper([a, b]);
        var host = new PluginHost();
        await bootstrapper.LoadAllAsync(host);

        Assert.Empty(bootstrapper.FailedPlugins);
        Assert.Equal(2, bootstrapper.LoadedPlugins.Count);
    }

    [Fact]
    public async Task UnloadAllAsync_CallsOnUnloadingInReverseOrder()
    {
        var unloadOrder = new List<string>();
        var a = CreatePlugin("A", onLoaded: _ => { });
        var b = CreatePlugin("B", onLoaded: _ => { });

        var bootstrapper = new PluginBootstrapper([a, b]);
        var host = new PluginHost();
        await bootstrapper.LoadAllAsync(host);
        await bootstrapper.UnloadAllAsync();

        Assert.Equal(2, bootstrapper.LoadedPlugins.Count);
    }

    private sealed class TestConfigSection(string sectionId) : IConfigSection
    {
        public string SectionId { get; } = sectionId;
        public string DisplayName { get; } = sectionId;
        public Dictionary<string, object> GetDefaults() => [];
        public IReadOnlyList<ValidationError> Validate() => [];
    }
}
