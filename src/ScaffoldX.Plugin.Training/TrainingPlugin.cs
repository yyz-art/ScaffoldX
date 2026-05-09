using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Plugin.Training.Services;

namespace ScaffoldX.Plugin.Training;

public sealed class TrainingPlugin : IPlugin
{
    private PluginState _state = PluginState.NotLoaded;

    public PluginMetadata Metadata { get; } = new()
    {
        Id = "Training",
        DisplayName = "模型训练",
        Description = "YOLO 模型训练配置与脚本生成、模型导出",
        Version = "1.0.0",
        IconKey = "Training",
        Order = 4,
        Dependencies = [],
        FeatureToggles = []
    };

    public PluginState State => _state;

    public Task OnLoadedAsync(IPluginHost host)
    {
        _state = PluginState.Loading;

        host.RegisterView("MainRegion", "Training",
            () => new Views.TrainingView(),
            () => new ViewModels.TrainingViewModel());

        host.RegisterService<ITrainingScriptGenerator>(new TrainingScriptGenerator());
        host.RegisterService<IModelExportService>(new ModelExportService());

        _state = PluginState.Loaded;
        return Task.CompletedTask;
    }

    public Task OnUnloadingAsync()
    {
        _state = PluginState.Unloading;
        return Task.CompletedTask;
    }
}
