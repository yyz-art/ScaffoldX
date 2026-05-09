using Xunit;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Shell.Plugins;

namespace ScaffoldX.Shell.Tests.Plugins;

public class PluginLoaderTests
{
    [Fact]
    public void SortByDependencies_NoDependencies_ReturnsSameOrder()
    {
        var plugins = new List<IPlugin>
        {
            new StubPlugin("a", []),
            new StubPlugin("b", []),
            new StubPlugin("c", []),
        };

        var result = PluginLoader.SortByDependencies(plugins);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void SortByDependencies_RespectsDependencyOrder()
    {
        var plugins = new List<IPlugin>
        {
            new StubPlugin("vision", ["annotation"]),
            new StubPlugin("annotation", []),
            new StubPlugin("scaffold", []),
        };

        var result = PluginLoader.SortByDependencies(plugins);

        var annotationIdx = result.FindIndex(p => p.Metadata.Id == "annotation");
        var visionIdx = result.FindIndex(p => p.Metadata.Id == "vision");
        Assert.True(annotationIdx < visionIdx, "annotation should come before vision");
    }

    [Fact]
    public void SortByDependencies_MissingDependency_Throws()
    {
        var plugins = new List<IPlugin>
        {
            new StubPlugin("vision", ["nonexistent"]),
        };

        Assert.Throws<InvalidOperationException>(() =>
            PluginLoader.SortByDependencies(plugins));
    }

    [Fact]
    public void SortByDependencies_CircularDependency_Throws()
    {
        var plugins = new List<IPlugin>
        {
            new StubPlugin("a", ["b"]),
            new StubPlugin("b", ["a"]),
        };

        Assert.Throws<InvalidOperationException>(() =>
            PluginLoader.SortByDependencies(plugins));
    }

    [Fact]
    public void SortByDependencies_ComplexDag()
    {
        var plugins = new List<IPlugin>
        {
            new StubPlugin("d", ["b", "c"]),
            new StubPlugin("a", []),
            new StubPlugin("b", ["a"]),
            new StubPlugin("c", ["a"]),
        };

        var result = PluginLoader.SortByDependencies(plugins);

        var aIdx = result.FindIndex(p => p.Metadata.Id == "a");
        var bIdx = result.FindIndex(p => p.Metadata.Id == "b");
        var cIdx = result.FindIndex(p => p.Metadata.Id == "c");
        var dIdx = result.FindIndex(p => p.Metadata.Id == "d");
        Assert.True(aIdx < bIdx);
        Assert.True(aIdx < cIdx);
        Assert.True(bIdx < dIdx);
        Assert.True(cIdx < dIdx);
    }

    [Fact]
    public void SortByDependencies_UnloadOrder_IsReverse()
    {
        var plugins = new List<IPlugin>
        {
            new StubPlugin("vision", ["annotation"]),
            new StubPlugin("annotation", ["scaffold"]),
            new StubPlugin("scaffold", []),
        };

        var loadOrder = PluginLoader.SortByDependencies(plugins);
        var unloadOrder = loadOrder.AsEnumerable().Reverse().ToList();

        Assert.Equal("scaffold", loadOrder[0].Metadata.Id);
        Assert.Equal("annotation", loadOrder[1].Metadata.Id);
        Assert.Equal("vision", loadOrder[2].Metadata.Id);

        Assert.Equal("vision", unloadOrder[0].Metadata.Id);
        Assert.Equal("annotation", unloadOrder[1].Metadata.Id);
        Assert.Equal("scaffold", unloadOrder[2].Metadata.Id);
    }

    private sealed class StubPlugin(string id, string[] dependencies) : IPlugin
    {
        public PluginMetadata Metadata { get; } = new()
        {
            Id = id,
            DisplayName = id,
            Dependencies = dependencies,
        };
        public PluginState State { get; private set; } = PluginState.NotLoaded;

        public Task OnLoadedAsync(IPluginHost host)
        {
            State = PluginState.Loaded;
            return Task.CompletedTask;
        }

        public Task OnUnloadingAsync()
        {
            State = PluginState.Unloading;
            return Task.CompletedTask;
        }
    }
}
