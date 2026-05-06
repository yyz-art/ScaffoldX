using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 主窗口 ViewModel，管理向导步骤切换和共享的 ProjectConfig。
/// CurrentStep: 0=历史列表, 1=步骤一(类型), 2=步骤二(基础信息), 3=步骤三(专项配置), 4=步骤四(确认生成), 10=标注工具, 11=训练平台。
/// </summary>
public class MainWindowViewModel : BindableBase
{
    private readonly ProjectHistoryViewModel _historyVm;
    private readonly Step1ViewModel _step1Vm;
    private readonly Step2ViewModel _step2Vm;
    private readonly Step3ViewModel _step3Vm;
    private readonly Step4ViewModel _step4Vm;
    private readonly AnnotationViewModel _annotationVm;
    private readonly YoloTrainingViewModel _trainingVm;

    private BindableBase _currentView = null!;
    private int _currentStep;

    /// <summary>
    /// 初始化主窗口 ViewModel，注入各步骤子 ViewModel。
    /// </summary>
    public MainWindowViewModel(
        ProjectHistoryViewModel historyVm,
        Step1ViewModel step1Vm,
        Step2ViewModel step2Vm,
        Step3ViewModel step3Vm,
        Step4ViewModel step4Vm,
        AnnotationViewModel annotationVm,
        YoloTrainingViewModel trainingVm)
    {
        _historyVm = historyVm;
        _step1Vm = step1Vm;
        _step2Vm = step2Vm;
        _step3Vm = step3Vm;
        _step4Vm = step4Vm;
        _annotationVm = annotationVm;
        _trainingVm = trainingVm;

        SharedConfig = new ProjectConfig();

        GoBackCommand = new DelegateCommand(ExecuteGoBack, () => CanGoBack)
            .ObservesProperty(() => CanGoBack);
        GoNextCommand = new DelegateCommand(ExecuteGoNext, () => CanGoNext)
            .ObservesProperty(() => CanGoNext);
        NewProjectCommand = new DelegateCommand(ExecuteNewProject);
        ShowAnnotationCommand = new DelegateCommand(() => NavigateTo(10));
        ShowTrainingCommand = new DelegateCommand(() => NavigateTo(11));
        ShowHomeCommand = new DelegateCommand(() => NavigateTo(0));

        // 订阅历史列表的新建项目事件
        _historyVm.NewProjectRequested += OnNewProjectRequested;

        // 订阅各步骤的验证状态变化，以刷新 CanGoNext
        _step1Vm.PropertyChanged += (_, _) => RaisePropertyChanged(nameof(CanGoNext));
        _step2Vm.PropertyChanged += (_, _) => RaisePropertyChanged(nameof(CanGoNext));

        // 默认显示历史列表
        NavigateTo(0);
    }

    /// <summary>当前显示的子 ViewModel，绑定到主窗口 ContentControl。</summary>
    public BindableBase CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    /// <summary>当前步骤编号（0=历史列表, 1-4=向导步骤）。</summary>
    public int CurrentStep
    {
        get => _currentStep;
        private set
        {
            if (SetProperty(ref _currentStep, value))
            {
                RaisePropertyChanged(nameof(CanGoBack));
                RaisePropertyChanged(nameof(CanGoNext));
                RaisePropertyChanged(nameof(IsWizardVisible));
                RaisePropertyChanged(nameof(IsToolPage));
                RaisePropertyChanged(nameof(NextButtonText));
                RaisePropertyChanged(nameof(StepDescription));
                RaisePropertyChanged(nameof(StepProgress));
            }
        }
    }

    /// <summary>是否可以返回上一步。</summary>
    public bool CanGoBack => CurrentStep > 1 || IsToolPage;

    /// <summary>是否可以进入下一步或执行生成。</summary>
    public bool CanGoNext => CurrentStep switch
    {
        1 => _step1Vm.CanProceed,
        2 => !_step2Vm.HasErrors,
        3 => true,
        4 => false,
        _ => false
    };

    /// <summary>是否显示向导导航栏（步骤1-4时显示）。</summary>
    public bool IsWizardVisible => CurrentStep >= 1 && CurrentStep <= 4;

    /// <summary>下一步按钮文字。</summary>
    public string NextButtonText => CurrentStep == 3 ? "确认生成" : "下一步";

    /// <summary>当前步骤描述文字。</summary>
    public string StepDescription => CurrentStep switch
    {
        1 => "选择项目类型",
        2 => "填写基础信息",
        3 => "专项配置",
        4 => "确认并生成",
        10 => "YOLO 标注工具",
        11 => "YOLO 训练平台",
        _ => string.Empty
    };

    /// <summary>步骤进度百分比（用于底部进度条）。</summary>
    public double StepProgress => CurrentStep switch
    {
        1 => 25,
        2 => 50,
        3 => 75,
        4 => 100,
        _ => 0
    };

    /// <summary>在步骤间共享的项目配置对象。</summary>
    public ProjectConfig SharedConfig { get; }

    /// <summary>返回上一步命令。</summary>
    public DelegateCommand GoBackCommand { get; }

    /// <summary>进入下一步或执行生成命令。</summary>
    public DelegateCommand GoNextCommand { get; }

    /// <summary>从历史列表进入新建向导命令。</summary>
    public DelegateCommand NewProjectCommand { get; }

    /// <summary>显示标注工具命令。</summary>
    public DelegateCommand ShowAnnotationCommand { get; }

    /// <summary>显示训练平台命令。</summary>
    public DelegateCommand ShowTrainingCommand { get; }

    /// <summary>返回首页命令。</summary>
    public DelegateCommand ShowHomeCommand { get; }

    /// <summary>当前是否在工具页面（标注/训练）。</summary>
    public bool IsToolPage => CurrentStep >= 10;

    private void ExecuteGoBack()
    {
        if (IsToolPage)
            NavigateTo(0);
        else if (CurrentStep > 1)
            NavigateTo(CurrentStep - 1);
    }

    private void ExecuteGoNext()
    {
        if (CurrentStep == 3)
        {
            // 将配置同步到 Step4，然后跳转
            SyncConfigToStep4();
            NavigateTo(4);
        }
        else if (CurrentStep >= 1 && CurrentStep < 4)
        {
            SyncConfigFromCurrentStep();
            NavigateTo(CurrentStep + 1);
        }
    }

    private void ExecuteNewProject()
    {
        // 重置配置
        SharedConfig.ProjectName = string.Empty;
        SharedConfig.ProjectType = string.Empty;
        _step1Vm.Reset();
        _step2Vm.Reset();
        _step3Vm.Reset();
        NavigateTo(1);
    }

    private void OnNewProjectRequested(object? sender, EventArgs e)
    {
        ExecuteNewProject();
    }

    private void NavigateTo(int step)
    {
        CurrentStep = step;
        CurrentView = step switch
        {
            0 => _historyVm,
            1 => _step1Vm,
            2 => _step2Vm,
            3 => _step3Vm,
            4 => _step4Vm,
            10 => _annotationVm,
            11 => _trainingVm,
            _ => _historyVm
        };
    }

    private void SyncConfigFromCurrentStep()
    {
        switch (CurrentStep)
        {
            case 1:
                SharedConfig.ProjectType = _step1Vm.SelectedProjectType;
                _step3Vm.ApplyProjectType(SharedConfig.ProjectType);
                break;
            case 2:
                SharedConfig.ProjectName = _step2Vm.ProjectName;
                SharedConfig.OutputPath = _step2Vm.OutputPath;
                SharedConfig.NamespacePrefix = _step2Vm.NamespacePrefix;
                SharedConfig.UIFramework = _step2Vm.UIFramework;
                SharedConfig.DotNetVersion = _step2Vm.DotNetVersion;
                SharedConfig.ProjectDescription = _step2Vm.ProjectDescription;
                break;
        }
    }

    private void SyncConfigToStep4()
    {
        // 同步步骤三的专项配置
        SharedConfig.SelectedDrivers = _step3Vm.GetSelectedDrivers();
        SharedConfig.EnableSimulationDriver = _step3Vm.EnableSimulationDriver;
        SharedConfig.DefaultPLCIp = _step3Vm.DefaultPLCIp;
        SharedConfig.DefaultPLCPort = _step3Vm.DefaultPLCPort;
        SharedConfig.S7Rack = _step3Vm.S7Rack;
        SharedConfig.S7Slot = _step3Vm.S7Slot;
        SharedConfig.OpcUaEndpoint = _step3Vm.OpcUaEndpoint;
        SharedConfig.CameraBrand = _step3Vm.CameraBrand;
        SharedConfig.ModelType = _step3Vm.ModelType;
        SharedConfig.ModelPath = _step3Vm.ModelPath;
        SharedConfig.EnablePipeline = _step3Vm.EnablePipeline;
        SharedConfig.SelectedModules = _step3Vm.GetSelectedModules();
        SharedConfig.EnableLoginWindow = _step3Vm.EnableLoginWindow;
        SharedConfig.ForcePasswordChange = _step3Vm.ForcePasswordChange;

        _step4Vm.Initialize(SharedConfig);
    }
}
