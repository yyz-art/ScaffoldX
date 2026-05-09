using ScaffoldX.Abstractions.Plugins;

namespace ScaffoldX.Shell.Plugins;

public sealed class PluginBootstrapper
{
    private readonly IReadOnlyList<IPlugin> _plugins;
    private readonly List<IPlugin> _loadedPlugins = [];
    private readonly List<IPlugin> _failedPlugins = [];

    public IReadOnlyList<IPlugin> LoadedPlugins => _loadedPlugins;
    public IReadOnlyList<IPlugin> FailedPlugins => _failedPlugins;

    public PluginBootstrapper(IReadOnlyList<IPlugin> plugins)
    {
        _plugins = plugins;
    }

    public async Task LoadAllAsync(IPluginHost host)
    {
        var sorted = PluginLoader.SortByDependencies(_plugins);

        foreach (var plugin in sorted)
        {
            try
            {
                await plugin.OnLoadedAsync(host);
                _loadedPlugins.Add(plugin);
            }
            catch
            {
                _failedPlugins.Add(plugin);
            }
        }
    }

    public async Task UnloadAllAsync()
    {
        for (var i = _loadedPlugins.Count - 1; i >= 0; i--)
        {
            await _loadedPlugins[i].OnUnloadingAsync();
        }
    }
}
