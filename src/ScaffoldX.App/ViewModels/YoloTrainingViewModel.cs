using System.Collections.ObjectModel;
using System.IO;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// YOLO 训练平台 ViewModel，提供模型训练配置、启动和监控功能。
/// </summary>
public partial class YoloTrainingViewModel : BindableBase
{
    private readonly IYoloTrainingService _trainingService;
    private readonly ILogger _logger = Log.ForContext<YoloTrainingViewModel>();
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 初始化训练 ViewModel，注册命令。
    /// </summary>
    /// <param name="trainingService">YOLO 训练服务。</param>
    public YoloTrainingViewModel(IYoloTrainingService trainingService)
    {
        _trainingService = trainingService;

        CheckEnvironmentCommand = new DelegateCommand(async () => await ExecuteCheckEnvironmentAsync());
        InstallDependenciesCommand = new DelegateCommand(async () => await ExecuteInstallDependenciesAsync());
        BrowseDatasetCommand = new DelegateCommand(ExecuteBrowseDataset);
        BrowseOutputCommand = new DelegateCommand(ExecuteBrowseOutput);
        StartTrainingCommand = new DelegateCommand(async () => await ExecuteStartTrainingAsync(), () => CanStartTraining)
            .ObservesProperty(() => IsTraining)
            .ObservesProperty(() => Config.DatasetPath);
        CancelTrainingCommand = new DelegateCommand(ExecuteCancelTraining, () => IsTraining)
            .ObservesProperty(() => IsTraining);
        ExportOnnxCommand = new DelegateCommand(async () => await ExecuteExportOnnxAsync(), () => CanExportOnnx)
            .ObservesProperty(() => IsCompleted);
        ResumeTrainingCommand = new DelegateCommand(async () => await ExecuteResumeTrainingAsync(), () => CanResumeTraining)
            .ObservesProperty(() => IsTraining)
            .ObservesProperty(() => Config.ResumeModelPath);
        ValidateModelCommand = new DelegateCommand(async () => await ExecuteValidateModelAsync(), () => CanValidateModel)
            .ObservesProperty(() => IsTraining)
            .ObservesProperty(() => IsCompleted);
        BrowseResumeModelCommand = new DelegateCommand(ExecuteBrowseResumeModel);

        AvailableModels = new ObservableCollection<PretrainedModel>(_trainingService.GetAvailableModels());
    }

    // ── 子 ViewModel ────────────────────────────────────────────────────────

    /// <summary>训练配置子 ViewModel。</summary>
    public TrainingConfigViewModel Config { get; } = new();

    // ── 训练状态属性 ────────────────────────────────────────────────────────

    private bool _isTraining;
    private bool _isCompleted;
    private bool _isError;
    private int _currentEpoch;
    private double _loss;
    private double _map50;
    private double _map50_95;
    private string _statusMessage = "就绪";
    private string _elapsedText = string.Empty;
    private string _environmentStatus = "未检查";
    private EnvironmentCheckResult? _environmentCheck;

    /// <summary>是否正在训练。</summary>
    public bool IsTraining
    {
        get => _isTraining;
        private set
        {
            if (SetProperty(ref _isTraining, value))
            {
                RaisePropertyChanged(nameof(TrainingButtonText));
                RaisePropertyChanged(nameof(CanStartTraining));
            }
        }
    }

    /// <summary>是否训练完成。</summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        private set => SetProperty(ref _isCompleted, value);
    }

    /// <summary>是否出错。</summary>
    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    /// <summary>当前轮次。</summary>
    public int CurrentEpoch
    {
        get => _currentEpoch;
        private set
        {
            if (SetProperty(ref _currentEpoch, value))
            {
                RaisePropertyChanged(nameof(ProgressPercent));
                RaisePropertyChanged(nameof(ProgressText));
            }
        }
    }

    /// <summary>损失值。</summary>
    public double Loss
    {
        get => _loss;
        private set => SetProperty(ref _loss, value);
    }

    /// <summary>mAP@0.5。</summary>
    public double Map50
    {
        get => _map50;
        private set => SetProperty(ref _map50, value);
    }

    /// <summary>mAP@0.5:0.95。</summary>
    public double Map50_95
    {
        get => _map50_95;
        private set => SetProperty(ref _map50_95, value);
    }

    /// <summary>状态消息。</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>耗时文字。</summary>
    public string ElapsedText
    {
        get => _elapsedText;
        private set => SetProperty(ref _elapsedText, value);
    }

    /// <summary>环境检查状态。</summary>
    public string EnvironmentStatus
    {
        get => _environmentStatus;
        private set => SetProperty(ref _environmentStatus, value);
    }

    /// <summary>训练按钮文字。</summary>
    public string TrainingButtonText => IsTraining ? "训练中..." : "开始训练";

    /// <summary>进度百分比。</summary>
    public double ProgressPercent => Config.Epochs > 0 ? (double)CurrentEpoch / Config.Epochs * 100 : 0;

    /// <summary>进度文字。</summary>
    public string ProgressText => $"{CurrentEpoch} / {Config.Epochs}";

    /// <summary>是否可以开始训练。</summary>
    public bool CanStartTraining => !IsTraining && !string.IsNullOrWhiteSpace(Config.DatasetPath);

    /// <summary>是否可以导出 ONNX。</summary>
    public bool CanExportOnnx => IsCompleted && !IsError;

    /// <summary>可用的预训练模型列表。</summary>
    public ObservableCollection<PretrainedModel> AvailableModels { get; }

    // ── 模型验证 ──────────────────────────────────────────────────────────

    private string _validationStatus = string.Empty;
    private ModelValidationResult? _validationResult;

    /// <summary>验证状态文字。</summary>
    public string ValidationStatus
    {
        get => _validationStatus;
        private set => SetProperty(ref _validationStatus, value);
    }

    /// <summary>验证结果。</summary>
    public ModelValidationResult? ValidationResult
    {
        get => _validationResult;
        private set => SetProperty(ref _validationResult, value);
    }

    /// <summary>是否可以验证模型。</summary>
    public bool CanValidateModel => !IsTraining && IsCompleted && File.Exists(Config.OutputPath);

    /// <summary>是否可以恢复训练。</summary>
    public bool CanResumeTraining => !IsTraining && !string.IsNullOrWhiteSpace(Config.ResumeModelPath) && File.Exists(Config.ResumeModelPath);

    // ── 命令 ──────────────────────────────────────────────────────────────

    public DelegateCommand CheckEnvironmentCommand { get; }
    public DelegateCommand InstallDependenciesCommand { get; }
    public DelegateCommand BrowseDatasetCommand { get; }
    public DelegateCommand BrowseOutputCommand { get; }
    public DelegateCommand StartTrainingCommand { get; }
    public DelegateCommand CancelTrainingCommand { get; }
    public DelegateCommand ExportOnnxCommand { get; }
    public DelegateCommand ResumeTrainingCommand { get; }
    public DelegateCommand ValidateModelCommand { get; }
    public DelegateCommand BrowseResumeModelCommand { get; }
}
