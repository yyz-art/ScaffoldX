using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Plugin.Scaffold.Services;

namespace ScaffoldX.Plugin.Scaffold;

public sealed class ScaffoldPlugin : IPlugin
{
    private PluginState _state = PluginState.NotLoaded;

    public PluginMetadata Metadata { get; } = new()
    {
        Id = "Scaffold",
        DisplayName = "项目脚手架",
        Description = "向导式生成 .NET 10.0 WPF 工业项目骨架",
        Version = "1.0.0",
        IconKey = "Scaffold",
        Order = 1,
        Dependencies = [],
        FeatureToggles = []
    };

    public PluginState State => _state;

    public Task OnLoadedAsync(IPluginHost host)
    {
        _state = PluginState.Loading;

        RegisterConfigSections(host);
        RegisterServices(host);
        RegisterViews(host);

        _state = PluginState.Loaded;
        return Task.CompletedTask;
    }

    public Task OnUnloadingAsync()
    {
        _state = PluginState.Unloading;
        return Task.CompletedTask;
    }

    private void RegisterConfigSections(IPluginHost host)
    {
        host.RegisterConfigSection(new ScaffoldConfigSection());
        host.RegisterConfigSection(new CollectionConfigSection());
        host.RegisterConfigSection(new VisionConfigSection());
        host.RegisterConfigSection(new SystemConfigSection());
        host.RegisterConfigSection(new UIConfigSection());
    }

    private void RegisterServices(IPluginHost host)
    {
        host.RegisterService<ITemplateEngine>(new ScribanTemplateEngine());
        host.RegisterService<IValidationService>(new ValidationService());
        host.RegisterService<ITemplateSource>(EmbeddedTemplateSource.ForTemplatesAssembly());
        host.RegisterService<IProjectGenerator>(new EnhancedProjectGenerator(
            host.GetService<ITemplateEngine>()!,
            host.GetService<IValidationService>()!,
            host.GetService<ITemplateSource>()!));
    }

    private void RegisterViews(IPluginHost host)
    {
        host.RegisterView("MainRegion", "Scaffold",
            () => new Views.ScaffoldWizardView(),
            () => new ViewModels.ScaffoldWizardViewModel());
    }
}
