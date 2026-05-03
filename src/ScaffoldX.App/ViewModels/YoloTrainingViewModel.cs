using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// YOLO 训练平台 ViewModel，提供模型训练配置、启动和监控功能。
/// </summary>
public class YoloTrainingViewModel : BindableBase
{
    private readonly IYoloTrainingService _trainingService;
    private readonly ILogger _logger = Log.ForContext<YoloTrainingViewModel>();
    private CancellationTokenSource? _cancellationTokenSource;

    private string _datasetPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _pretrainedModel = "yolov8n.pt";
    private int _epochs = 100;
    private int _batchSize = 16;
    private int _imageSize = 640;
    private double _learningRate = 0.01;
    private int _numClasses = 1;
    private string _classNamesText = "object";
    private bool _useGpu = true;
    private int _workers = 8;

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

    /// <summary>
    /// 初始化训练 ViewModel，注册命令。
    /// </summary>
    /// <param name="trainingService">YOLO 训练服务。</param>
    public YoloTrainingViewModel(IYoloTrainingService trainingService)
    {
        _trainingService = trainingService;

        // 命令注册
        CheckEnvironmentCommand = new DelegateCommand(async () => await ExecuteCheckEnvironmentAsync());
        InstallDependenciesCommand = new DelegateCommand(async () => await ExecuteInstallDependenciesAsync());
        BrowseDatasetCommand = new DelegateCommand(ExecuteBrowseDataset);
        BrowseOutputCommand = new DelegateCommand(ExecuteBrowseOutput);
        StartTrainingCommand = new DelegateCommand(async () => await ExecuteStartTrainingAsync(), () => CanStartTraining)
            .ObservesProperty(() => IsTraining)
            .ObservesProperty(() => DatasetPath);
        CancelTrainingCommand = new DelegateCommand(ExecuteCancelTraining, () => IsTraining)
            .ObservesProperty(() => IsTraining);
        ExportOnnxCommand = new DelegateCommand(async () => await ExecuteExportOnnxAsync(), () => CanExportOnnx)
            .ObservesProperty(() => IsCompleted);
        ResumeTrainingCommand = new DelegateCommand(async () => await ExecuteResumeTrainingAsync(), () => CanResumeTraining)
            .ObservesProperty(() => IsTraining)
            .ObservesProperty(() => ResumeModelPath);
        ValidateModelCommand = new DelegateCommand(async () => await ExecuteValidateModelAsync(), () => CanValidateModel)
            .ObservesProperty(() => IsTraining)
            .ObservesProperty(() => IsCompleted);
        BrowseResumeModelCommand = new DelegateCommand(ExecuteBrowseResumeModel);

        // 预训练模型列表
        AvailableModels = new ObservableCollection<PretrainedModel>(_trainingService.GetAvailableModels());
    }

    // ── 属性 ──────────────────────────────────────────────────────────────────

    /// <summary>数据集路径。</summary>
    public string DatasetPath
    {
        get => _datasetPath;
        set
        {
            if (SetProperty(ref _datasetPath, value))
            {
                RaisePropertyChanged(nameof(CanStartTraining));
            }
        }
    }

    /// <summary>输出路径。</summary>
    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    /// <summary>预训练模型。</summary>
    public string PretrainedModel
    {
        get => _pretrainedModel;
        set => SetProperty(ref _pretrainedModel, value);
    }

    /// <summary>训练轮数。</summary>
    public int Epochs
    {
        get => _epochs;
        set => SetProperty(ref _epochs, value);
    }

    /// <summary>批次大小。</summary>
    public int BatchSize
    {
        get => _batchSize;
        set => SetProperty(ref _batchSize, value);
    }

    /// <summary>图像尺寸。</summary>
    public int ImageSize
    {
        get => _imageSize;
        set => SetProperty(ref _imageSize, value);
    }

    /// <summary>学习率。</summary>
    public double LearningRate
    {
        get => _learningRate;
        set => SetProperty(ref _learningRate, value);
    }

    /// <summary>类别数量。</summary>
    public int NumClasses
    {
        get => _numClasses;
        set => SetProperty(ref _numClasses, value);
    }

    /// <summary>类别名称（逗号分隔）。</summary>
    public string ClassNamesText
    {
        get => _classNamesText;
        set => SetProperty(ref _classNamesText, value);
    }

    /// <summary>是否使用 GPU。</summary>
    public bool UseGpu
    {
        get => _useGpu;
        set => SetProperty(ref _useGpu, value);
    }

    /// <summary>工作线程数。</summary>
    public int Workers
    {
        get => _workers;
        set => SetProperty(ref _workers, value);
    }

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
    public double ProgressPercent => Epochs > 0 ? (double)CurrentEpoch / Epochs * 100 : 0;

    /// <summary>进度文字。</summary>
    public string ProgressText => $"{CurrentEpoch} / {Epochs}";

    /// <summary>是否可以开始训练。</summary>
    public bool CanStartTraining => !IsTraining && !string.IsNullOrWhiteSpace(DatasetPath);

    /// <summary>是否可以导出 ONNX。</summary>
    public bool CanExportOnnx => IsCompleted && !IsError;

    /// <summary>可用的预训练模型列表。</summary>
    public ObservableCollection<PretrainedModel> AvailableModels { get; }

    // ── 恢复训练 ──────────────────────────────────────────────────────────

    private string _resumeModelPath = string.Empty;

    /// <summary>恢复训练的模型路径。</summary>
    public string ResumeModelPath
    {
        get => _resumeModelPath;
        set
        {
            if (SetProperty(ref _resumeModelPath, value))
            {
                RaisePropertyChanged(nameof(CanResumeTraining));
            }
        }
    }

    /// <summary>是否可以恢复训练。</summary>
    public bool CanResumeTraining => !IsTraining && !string.IsNullOrWhiteSpace(ResumeModelPath) && File.Exists(ResumeModelPath);

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
    public bool CanValidateModel => !IsTraining && IsCompleted && File.Exists(OutputPath);

    // ── 命令 ──────────────────────────────────────────────────────────────────

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

    // ── 命令实现 ──────────────────────────────────────────────────────────────

    private async Task ExecuteCheckEnvironmentAsync()
    {
        EnvironmentStatus = "检查中...";
        StatusMessage = "正在检查训练环境...";

        try
        {
            _environmentCheck = await _trainingService.CheckEnvironmentAsync();

            if (_environmentCheck.IsReady)
            {
                EnvironmentStatus = $"就绪 - Python {_environmentCheck.PythonVersion}, " +
                                   $"Ultralytics {_environmentCheck.UltralyticsVersion}";
                if (_environmentCheck.CudaAvailable)
                {
                    EnvironmentStatus += $", CUDA {_environmentCheck.CudaVersion}";
                }
                StatusMessage = "训练环境就绪";
            }
            else
            {
                EnvironmentStatus = $"未就绪 - {_environmentCheck.ErrorMessage}";
                StatusMessage = "训练环境未就绪，请安装依赖";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "检查环境失败");
            EnvironmentStatus = "检查失败";
            StatusMessage = $"检查环境失败: {ex.Message}";
        }
    }

    private async Task ExecuteInstallDependenciesAsync()
    {
        StatusMessage = "正在安装依赖...";
        IsTraining = true;

        try
        {
            var progress = new Progress<string>(message =>
            {
                StatusMessage = message;
            });

            var success = await _trainingService.InstallDependenciesAsync(progress);

            if (success)
            {
                StatusMessage = "依赖安装完成";
                await ExecuteCheckEnvironmentAsync();
            }
            else
            {
                StatusMessage = "依赖安装失败";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "安装依赖失败");
            StatusMessage = $"安装依赖失败: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsTraining = false;
        }
    }

    private void ExecuteBrowseDataset()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "选择 YOLO 数据集目录"
        };

        if (dialog.ShowDialog() == true)
        {
            DatasetPath = dialog.SelectedPath;

            // 尝试从 data.yaml 读取类别信息
            var dataYamlPath = Path.Combine(dialog.SelectedPath, "data.yaml");
            if (File.Exists(dataYamlPath))
            {
                try
                {
                    var content = File.ReadAllText(dataYamlPath);
                    var ncMatch = System.Text.RegularExpressions.Regex.Match(content, @"nc:\s*(\d+)");
                    if (ncMatch.Success)
                    {
                        NumClasses = int.Parse(ncMatch.Groups[1].Value);
                    }

                    var namesMatch = System.Text.RegularExpressions.Regex.Match(content, @"names:\s*\[(.+?)\]");
                    if (namesMatch.Success)
                    {
                        ClassNamesText = namesMatch.Groups[1].Value
                            .Replace("'", "")
                            .Replace("\"", "")
                            .Trim();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "解析 data.yaml 失败");
                }
            }
        }
    }

    private void ExecuteBrowseOutput()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "选择模型输出目录"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.SelectedPath;
        }
    }

    private async Task ExecuteStartTrainingAsync()
    {
        if (string.IsNullOrWhiteSpace(DatasetPath))
        {
            StatusMessage = "请选择数据集路径";
            return;
        }

        // 设置默认输出路径
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            OutputPath = Path.Combine(DatasetPath, "runs");
        }

        IsTraining = true;
        IsCompleted = false;
        IsError = false;
        CurrentEpoch = 0;
        Loss = 0;
        Map50 = 0;
        Map50_95 = 0;

        _cancellationTokenSource = new CancellationTokenSource();

        var config = new YoloTrainingConfig
        {
            DatasetPath = DatasetPath,
            OutputPath = OutputPath,
            PretrainedModel = PretrainedModel,
            Epochs = Epochs,
            BatchSize = BatchSize,
            ImageSize = ImageSize,
            LearningRate = LearningRate,
            NumClasses = NumClasses,
            ClassNames = ClassNamesText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList(),
            UseGpu = UseGpu,
            Workers = Workers
        };

        var progress = new Progress<TrainingProgress>(p =>
        {
            CurrentEpoch = p.CurrentEpoch;
            Loss = p.Loss;
            Map50 = p.Map50;
            Map50_95 = p.Map50_95;
            StatusMessage = p.StatusMessage;
        });

        try
        {
            StatusMessage = "正在启动训练...";

            var result = await _trainingService.TrainAsync(config, progress, _cancellationTokenSource.Token);

            if (result.Success)
            {
                IsCompleted = true;
                Map50 = result.FinalMap50;
                Map50_95 = result.FinalMap50_95;
                ElapsedText = $"耗时 {result.TotalTime.TotalMinutes:F1} 分钟";
                StatusMessage = $"训练完成！mAP@0.5: {Map50:F3}, mAP@0.5:0.95: {Map50_95:F3}";
            }
            else
            {
                IsError = true;
                StatusMessage = $"训练失败: {result.ErrorMessage}";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "训练已取消";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "训练过程中发生错误");
            IsError = true;
            StatusMessage = $"训练失败: {ex.Message}";
        }
        finally
        {
            IsTraining = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void ExecuteCancelTraining()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "正在取消训练...";
    }

    private async Task ExecuteExportOnnxAsync()
    {
        var modelPath = Path.Combine(OutputPath, "train", "weights", "best.pt");
        if (!File.Exists(modelPath))
        {
            StatusMessage = "找不到训练好的模型文件";
            return;
        }

        var outputPath = Path.Combine(OutputPath, "export");
        Directory.CreateDirectory(outputPath);

        StatusMessage = "正在导出 ONNX 模型...";

        try
        {
            var success = await _trainingService.ExportToOnnxAsync(modelPath, outputPath, ImageSize);

            if (success)
            {
                StatusMessage = $"ONNX 模型已导出到: {outputPath}";
            }
            else
            {
                StatusMessage = "ONNX 导出失败";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 ONNX 失败");
            StatusMessage = $"导出 ONNX 失败: {ex.Message}";
            IsError = true;
        }
    }

    // ── 恢复训练 ──────────────────────────────────────────────────────────

    private void ExecuteBrowseResumeModel()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PyTorch 模型|*.pt|所有文件|*.*",
            Title = "选择恢复训练的检查点文件"
        };

        if (dialog.ShowDialog() == true)
        {
            ResumeModelPath = dialog.FileName;
        }
    }

    private async Task ExecuteResumeTrainingAsync()
    {
        if (string.IsNullOrWhiteSpace(ResumeModelPath) || !File.Exists(ResumeModelPath))
        {
            StatusMessage = "请选择有效的检查点文件";
            return;
        }

        var config = BuildTrainingConfig();
        _cancellationTokenSource = new CancellationTokenSource();

        IsTraining = true;
        IsCompleted = false;
        IsError = false;
        StatusMessage = "正在从检查点恢复训练...";

        try
        {
            var progress = new Progress<TrainingProgress>(p =>
            {
                CurrentEpoch = p.CurrentEpoch;
                Loss = p.Loss;
                Map50 = p.Map50;
                Map50_95 = p.Map50_95;
                ElapsedText = p.Elapsed.ToString(@"hh\:mm\:ss");
                StatusMessage = p.StatusMessage;
            });

            var result = await _trainingService.ResumeTrainAsync(
                config, ResumeModelPath, progress, _cancellationTokenSource.Token);

            IsTraining = false;

            if (result.Success)
            {
                IsCompleted = true;
                StatusMessage = $"恢复训练完成！mAP@0.5: {result.FinalMap50:F3}, 耗时: {result.TotalTime:hh\\:mm\\:ss}";
                _logger.Information("恢复训练完成: {ModelPath}", result.ModelPath);
            }
            else
            {
                IsError = true;
                StatusMessage = $"恢复训练失败: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            IsTraining = false;
            IsError = true;
            _logger.Error(ex, "恢复训练异常");
            StatusMessage = $"恢复训练异常: {ex.Message}";
        }
    }

    // ── 模型验证 ──────────────────────────────────────────────────────────

    private async Task ExecuteValidateModelAsync()
    {
        var modelPath = Path.Combine(OutputPath, "train", "weights", "best.pt");
        if (!File.Exists(modelPath))
        {
            StatusMessage = "找不到训练好的模型文件";
            return;
        }

        ValidationStatus = "验证中...";
        StatusMessage = "正在验证模型性能...";

        try
        {
            var result = await _trainingService.ValidateAsync(modelPath, DatasetPath);
            ValidationResult = result;
            ValidationStatus = $"mAP@0.5: {result.Map50:F3}, mAP@0.5:0.95: {result.Map50_95:F3}, " +
                              $"Precision: {result.Precision:F3}, Recall: {result.Recall:F3}, " +
                              $"推理速度: {result.InferenceSpeed:F1}ms/张";
            StatusMessage = "模型验证完成";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "模型验证失败");
            ValidationStatus = $"验证失败: {ex.Message}";
            StatusMessage = $"模型验证失败: {ex.Message}";
        }
    }

    private YoloTrainingConfig BuildTrainingConfig()
    {
        return new YoloTrainingConfig
        {
            DatasetPath = DatasetPath,
            OutputPath = OutputPath,
            PretrainedModel = PretrainedModel,
            Epochs = Epochs,
            BatchSize = BatchSize,
            ImageSize = ImageSize,
            LearningRate = LearningRate,
            NumClasses = NumClasses,
            ClassNames = ClassNamesText.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList(),
            UseGpu = UseGpu,
            Workers = Workers
        };
    }
}
