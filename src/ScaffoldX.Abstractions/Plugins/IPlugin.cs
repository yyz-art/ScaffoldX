namespace ScaffoldX.Abstractions.Plugins;

public interface IPlugin
{
    PluginMetadata Metadata { get; }
    PluginState State { get; }
    Task OnLoadedAsync(IPluginHost host);
    Task OnUnloadingAsync();
}

public interface IPluginHost
{
    void RegisterView(string regionName, string navigationKey, Func<object> viewFactory, Func<object> viewModelFactory);
    void NavigateTo(string regionName, string navigationKey);
    T? GetService<T>() where T : class;
    void RegisterService<T>(T instance) where T : class;
    void RegisterConfigSection(Config.IConfigSection section);
    Config.IConfigSection? GetConfigSection(string sectionId);
}

public sealed class PluginMetadata
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string IconKey { get; init; } = string.Empty;
    public int Order { get; init; }
    public IReadOnlyList<string> Dependencies { get; init; } = [];
    public IReadOnlyList<FeatureToggle> FeatureToggles { get; init; } = [];
}

public enum PluginState
{
    NotLoaded,
    Loading,
    Loaded,
    Unloading,
    Error,
}

public sealed class FeatureToggle
{
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool DefaultValue { get; init; }
    public string Group { get; init; } = string.Empty;
}
