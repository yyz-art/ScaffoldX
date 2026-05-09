using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Plugin.Annotation.Services;

namespace ScaffoldX.Plugin.Annotation;

public sealed class AnnotationPlugin : IPlugin
{
    private PluginState _state = PluginState.NotLoaded;

    public PluginMetadata Metadata { get; } = new()
    {
        Id = "Annotation",
        DisplayName = "数据标注",
        Description = "YOLO 格式标注编辑器，支持矩形框、多边形、旋转框、折线、圆形标注",
        Version = "1.0.0",
        IconKey = "Annotation",
        Order = 2,
        Dependencies = [],
        FeatureToggles =
        [
            new FeatureToggle
            {
                Key = "AutoLabeling",
                DisplayName = "自动标注",
                Description = "启用 YOLO 检测模型自动标注功能",
                DefaultValue = false,
                Group = "标注"
            },
            new FeatureToggle
            {
                Key = "Sam3Segmentation",
                DisplayName = "SAM3 分割",
                Description = "启用 SAM3 交互式分割标注（需要 Vision 子插件）",
                DefaultValue = false,
                Group = "标注"
            }
        ]
    };

    public PluginState State => _state;

    public Task OnLoadedAsync(IPluginHost host)
    {
        _state = PluginState.Loading;

        RegisterViews(host);

        _state = PluginState.Loaded;
        return Task.CompletedTask;
    }

    public Task OnUnloadingAsync()
    {
        _state = PluginState.Unloading;
        return Task.CompletedTask;
    }

    private void RegisterViews(IPluginHost host)
    {
        host.RegisterView("MainRegion", "Annotation",
            () => new Views.AnnotationView(),
            () => new ViewModels.AnnotationViewModel());
    }
}
