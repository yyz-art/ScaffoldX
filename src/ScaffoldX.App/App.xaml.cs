using System.Windows;
using Prism.Ioc;
using ScaffoldX.App.Services;
using ScaffoldX.App.ViewModels;
using ScaffoldX.App.Views;
using ScaffoldX.Core.FileGeneration;
using ScaffoldX.Core.TemplateProcessing;
using ScaffoldX.Core.Vision;

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
        // Core 层服务注册
        containerRegistry.RegisterSingleton<IVariableResolver, VariableResolver>();
        containerRegistry.RegisterSingleton<IPostProcessor, PostProcessor>();
        containerRegistry.RegisterSingleton<ITemplateSource, AssemblyTemplateSource>();
        containerRegistry.RegisterSingleton<ITemplateRegistry, TemplateRegistry>();
        containerRegistry.RegisterSingleton<IFileTreeBuilder, FileTreeBuilder>();

        // Vision 引擎工厂（AutoLabelingService 通过工厂创建 Sam3Segmentor）
        containerRegistry.RegisterSingleton<Func<ISam3SegmentationEngine>>(sp => () => new Sam3Segmentor());

        // App 层服务注册
        containerRegistry.RegisterSingleton<IDialogService, WpfDialogService>();
        containerRegistry.RegisterSingleton<IValidationService, ValidationService>();
        containerRegistry.RegisterSingleton<IHistoryService, HistoryService>();
        containerRegistry.RegisterSingleton<ITemplateEngine, ScribanTemplateEngine>();
        containerRegistry.RegisterSingleton<IProjectGenerator, ProjectGenerator>();
        containerRegistry.RegisterSingleton<IAnnotationRepository, AnnotationRepository>();
        containerRegistry.RegisterSingleton<IAnnotationExporter, AnnotationExporter>();
        containerRegistry.RegisterSingleton<IAnnotationService, AnnotationService>();
        containerRegistry.RegisterSingleton<IAutoLabelingService, AutoLabelingService>();
        containerRegistry.RegisterSingleton<IYoloTrainingService, YoloTrainingService>();
        containerRegistry.RegisterSingleton<IVideoFrameService, VideoFrameService>();
        // ViewModel 注册（单例，在步骤间共享状态）
        containerRegistry.RegisterSingleton<ProjectHistoryViewModel>();
        containerRegistry.RegisterSingleton<Step1ViewModel>();
        containerRegistry.RegisterSingleton<Step2ViewModel>();
        containerRegistry.RegisterSingleton<Step3ViewModel>();
        containerRegistry.RegisterSingleton<Step4ViewModel>();
        containerRegistry.RegisterSingleton<MainWindowViewModel>();
        containerRegistry.RegisterSingleton<AnnotationViewModel>();
        containerRegistry.RegisterSingleton<YoloTrainingViewModel>();

        // 视图导航注册
        containerRegistry.RegisterForNavigation<ProjectHistoryView>();
        containerRegistry.RegisterForNavigation<Step1ProjectTypeView>();
        containerRegistry.RegisterForNavigation<Step2BasicInfoView>();
        containerRegistry.RegisterForNavigation<Step3SpecificConfigView>();
        containerRegistry.RegisterForNavigation<Step4ConfirmGenerateView>();
        containerRegistry.RegisterForNavigation<AnnotationView>();
        containerRegistry.RegisterForNavigation<YoloTrainingView>();
    }
}
