using System.Windows;
using Prism.Ioc;
using Prism.Regions;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Shell.Plugins;
using ScaffoldX.Shell.ViewModels;
using ScaffoldX.Shell.Views;

namespace ScaffoldX.Shell;

public partial class App : Prism.Unity.PrismApplication
{
    private PluginBootstrapper? _bootstrapper;

    protected override Window CreateShell()
    {
        return Container.Resolve<ShellView>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<PluginHost>();
        containerRegistry.RegisterSingleton<IPluginHost>(sp => sp.Resolve<PluginHost>());
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();

        var host = Container.Resolve<IPluginHost>();
        var regionManager = Container.Resolve<IRegionManager>();

        // 手动创建所有插件实例
        var plugins = new List<IPlugin>
        {
            new global::ScaffoldX.Plugin.Scaffold.ScaffoldPlugin(),
            new global::ScaffoldX.Plugin.Annotation.AnnotationPlugin(),
            new global::ScaffoldX.Plugin.Annotation.Vision.VisionPlugin(),
            new global::ScaffoldX.Plugin.Training.TrainingPlugin(),
            new global::ScaffoldX.Plugin.Management.ManagementPlugin()
        };

        _bootstrapper = new PluginBootstrapper(plugins);
        await _bootstrapper.LoadAllAsync((PluginHost)host);

        // 使用 MainWindow 属性获取 CreateShell 创建的实例
        var loadedVm = new ShellViewModel(_bootstrapper.LoadedPlugins, regionManager, host);
        MainWindow.DataContext = loadedVm;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_bootstrapper is not null)
        {
            await _bootstrapper.UnloadAllAsync();
        }
        base.OnExit(e);
    }
}
