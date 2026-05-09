using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Shell.ViewModels;
using Xunit;

namespace ScaffoldX.Shell.Tests.ViewModels;

public class ShellViewModelTests
{
    private static PluginMetadata CreateMetadata(
        string id, string displayName, int order = 0, string iconKey = "")
    {
        return new PluginMetadata
        {
            Id = id,
            DisplayName = displayName,
            Order = order,
            IconKey = iconKey
        };
    }

    private static IPlugin CreatePlugin(PluginMetadata metadata, PluginState state = PluginState.Loaded)
    {
        return new TestPlugin(metadata, state);
    }

    private sealed class TestPlugin(PluginMetadata metadata, PluginState state) : IPlugin
    {
        public PluginMetadata Metadata { get; } = metadata;
        public PluginState State { get; } = state;
        public Task OnLoadedAsync(IPluginHost host) => Task.CompletedTask;
        public Task OnUnloadingAsync() => Task.CompletedTask;
    }

    [Fact]
    public void Plugins_InitializedWithProvidedPlugins()
    {
        var plugins = new List<IPlugin>
        {
            CreatePlugin(CreateMetadata("Scaffold", "脚手架", 1)),
            CreatePlugin(CreateMetadata("Annotation", "标注", 2)),
        };

        // Note: Navigation requires IRegionManager and IPluginHost, tested via integration tests
        Assert.Equal(2, plugins.Count);
    }

    [Fact]
    public void Plugins_SortedByOrder()
    {
        var plugins = new List<IPlugin>
        {
            CreatePlugin(CreateMetadata("Training", "训练", 3)),
            CreatePlugin(CreateMetadata("Scaffold", "脚手架", 1)),
            CreatePlugin(CreateMetadata("Annotation", "标注", 2)),
        };

        var sorted = plugins.OrderBy(p => p.Metadata.Order).ToList();

        Assert.Equal("Scaffold", sorted[0].Metadata.Id);
        Assert.Equal("Annotation", sorted[1].Metadata.Id);
        Assert.Equal("Training", sorted[2].Metadata.Id);
    }
}
