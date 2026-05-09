using ScaffoldX.Abstractions.Plugins;

namespace ScaffoldX.Shell.Plugins;

public static class PluginLoader
{
    public static List<IPlugin> SortByDependencies(IReadOnlyList<IPlugin> plugins)
    {
        var idToPlugin = plugins.ToDictionary(p => p.Metadata.Id, p => p);
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();
        var result = new List<IPlugin>();

        foreach (var plugin in plugins)
        {
            Visit(plugin.Metadata.Id, idToPlugin, visited, visiting, result);
        }

        return result;
    }

    private static void Visit(
        string id,
        Dictionary<string, IPlugin> idToPlugin,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<IPlugin> result)
    {
        if (visited.Contains(id)) return;

        if (visiting.Contains(id))
            throw new InvalidOperationException($"Circular dependency detected involving plugin '{id}'.");

        if (!idToPlugin.TryGetValue(id, out var plugin))
            throw new InvalidOperationException($"Plugin '{id}' has a dependency on '{id}' which is not available.");

        visiting.Add(id);

        foreach (var depId in plugin.Metadata.Dependencies)
        {
            if (!idToPlugin.ContainsKey(depId))
                throw new InvalidOperationException($"Plugin '{id}' depends on '{depId}' which is not loaded.");
            Visit(depId, idToPlugin, visited, visiting, result);
        }

        visiting.Remove(id);
        visited.Add(id);
        result.Add(plugin);
    }
}
