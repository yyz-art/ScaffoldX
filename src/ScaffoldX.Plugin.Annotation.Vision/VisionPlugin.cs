using ScaffoldX.Abstractions.Plugins;

namespace ScaffoldX.Plugin.Annotation.Vision;

public sealed class VisionPlugin : IPlugin
{
    private PluginState _state = PluginState.NotLoaded;

    public PluginMetadata Metadata { get; } = new()
    {
        Id = "Annotation.Vision",
        DisplayName = "SAM3 视觉推理",
        Description = "SAM3 交互式分割标注引擎（TorchSharp 隔离）",
        Version = "1.0.0",
        IconKey = "Vision",
        Order = 3,
        Dependencies = ["Annotation"],
        FeatureToggles = []
    };

    public PluginState State => _state;

    public Task OnLoadedAsync(IPluginHost host)
    {
        _state = PluginState.Loading;

        RegisterServices(host);

        _state = PluginState.Loaded;
        return Task.CompletedTask;
    }

    public Task OnUnloadingAsync()
    {
        _state = PluginState.Unloading;
        return Task.CompletedTask;
    }

    private void RegisterServices(IPluginHost host)
    {
        host.RegisterService<Services.ISam3SegmentationEngine>(new Services.Sam3SegmentorStub());
    }
}
