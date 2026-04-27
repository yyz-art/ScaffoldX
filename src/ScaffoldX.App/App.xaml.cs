using System.Windows;
using Prism.Ioc;
using ScaffoldX.App.Services;
using ScaffoldX.App.ViewModels;
using ScaffoldX.App.Views;

namespace ScaffoldX.App;

/// <summary>
/// 应用程序入口，负责 Prism 容器注册和主窗口创建。
/// </summary>
public partial class App : Prism.Unity.PrismApplication
{
    /// <summary>
    /// 创建应用程序主窗口。
    /// </summary>
    /// <returns>主窗口实例。</returns>
    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    /// <summary>
    /// 注册所有服务、ViewModel 和视图到 DI 容器。
    /// </summary>
    /// <param name="containerRegistry">容器注册接口。</param>
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 服务注册（单例）
        containerRegistry.RegisterSingleton<IValidationService, ValidationService>();
        containerRegistry.RegisterSingleton<IHistoryService, HistoryService>();
        containerRegistry.RegisterSingleton<ITemplateEngine, ScribanTemplateEngine>();
        containerRegistry.RegisterSingleton<IProjectGenerator, ProjectGenerator>();

        // ViewModel 注册（单例，在步骤间共享状态）
        containerRegistry.RegisterSingleton<ProjectHistoryViewModel>();
        containerRegistry.RegisterSingleton<Step1ViewModel>();
        containerRegistry.RegisterSingleton<Step2ViewModel>();
        containerRegistry.RegisterSingleton<Step3ViewModel>();
        containerRegistry.RegisterSingleton<Step4ViewModel>();
        containerRegistry.RegisterSingleton<MainWindowViewModel>();

        // 视图导航注册
        containerRegistry.RegisterForNavigation<ProjectHistoryView>();
        containerRegistry.RegisterForNavigation<Step1ProjectTypeView>();
        containerRegistry.RegisterForNavigation<Step2BasicInfoView>();
        containerRegistry.RegisterForNavigation<Step3SpecificConfigView>();
        containerRegistry.RegisterForNavigation<Step4ConfirmGenerateView>();
    }
}
