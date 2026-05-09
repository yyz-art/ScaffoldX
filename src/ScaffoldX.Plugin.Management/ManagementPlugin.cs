using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Plugin.Management.Services;

namespace ScaffoldX.Plugin.Management;

public sealed class ManagementPlugin : IPlugin
{
    private PluginState _state = PluginState.NotLoaded;

    public PluginMetadata Metadata { get; } = new()
    {
        Id = "Management",
        DisplayName = "项目管理",
        Description = "已生成项目的管理、历史记录、一键打开",
        Version = "1.0.0",
        IconKey = "Management",
        Order = 5,
        Dependencies = ["Scaffold"],
        FeatureToggles = []
    };

    public PluginState State => _state;

    public Task OnLoadedAsync(IPluginHost host)
    {
        _state = PluginState.Loading;

        var historyService = new JsonProjectHistoryService();
        host.RegisterService<IProjectHistoryService>(historyService);

        host.RegisterView("MainRegion", "Management",
            () => new Views.ManagementView(),
            () => new ViewModels.ManagementViewModel(historyService));

        _state = PluginState.Loaded;
        return Task.CompletedTask;
    }

    public Task OnUnloadingAsync()
    {
        _state = PluginState.Unloading;
        return Task.CompletedTask;
    }
}
