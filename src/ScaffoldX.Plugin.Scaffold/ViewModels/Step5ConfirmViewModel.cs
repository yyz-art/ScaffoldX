using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Plugin.Scaffold.Services;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public class SummaryItem
{
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class Step5ConfirmViewModel : INotifyPropertyChanged
{
    private readonly IProjectGenerator? _projectGenerator;
    private Step1ViewModel? _step1;
    private Step2DeviceConfigViewModel? _step2;
    private Step3FunctionConfigViewModel? _step3;
    private Step4SystemConfigViewModel? _step4;

    private bool _isGenerating;
    private bool _isSuccess;
    private bool _showGenerationResult;
    private string _generationStatus = string.Empty;
    private string _currentOperation = string.Empty;
    private double _generationProgress;

    public Step5ConfirmViewModel()
    {
        GenerateCommand = new RelayCommand(async () => await ExecuteGenerateAsync(), () => CanGenerate);
        OpenProjectDirectoryCommand = new RelayCommand(OpenProjectDirectory, () => IsSuccess);
        ViewLogCommand = new RelayCommand(ViewLog);

        DeviceSummaryItems = new ObservableCollection<SummaryItem>();
        FunctionSummaryItems = new ObservableCollection<SummaryItem>();
        SystemSummaryItems = new ObservableCollection<SummaryItem>();
    }

    public Step5ConfirmViewModel(
        IProjectGenerator projectGenerator,
        Step1ViewModel step1,
        Step2DeviceConfigViewModel step2,
        Step3FunctionConfigViewModel step3,
        Step4SystemConfigViewModel step4) : this()
    {
        _projectGenerator = projectGenerator;
        _step1 = step1;
        _step2 = step2;
        _step3 = step3;
        _step4 = step4;

        UpdateSummary();
    }

    public void SetViewModels(
        Step1ViewModel step1,
        Step2DeviceConfigViewModel step2,
        Step3FunctionConfigViewModel step3,
        Step4SystemConfigViewModel step4)
    {
        _step1 = step1;
        _step2 = step2;
        _step3 = step3;
        _step4 = step4;

        UpdateSummary();
    }

    #region Properties

    public string ProjectName => _step1?.ProjectName ?? string.Empty;

    public string ProjectTypeDisplay => _step1?.ProjectTypeEnum switch
    {
        ProjectTypeCategory.Collection => "数据采集",
        ProjectTypeCategory.Vision => "视觉",
        ProjectTypeCategory.System => "系统",
        _ => "未知"
    };

    public string OutputDirectory => _step1?.OutputDirectory ?? string.Empty;

    public bool HasDeviceConfig => _step1?.ProjectTypeEnum is ProjectTypeCategory.Collection or ProjectTypeCategory.Vision;

    public bool HasFunctionConfig => _step1?.ProjectTypeEnum is ProjectTypeCategory.Collection or ProjectTypeCategory.Vision;

    public bool HasSystemConfig => true;

    public ObservableCollection<SummaryItem> DeviceSummaryItems { get; }

    public ObservableCollection<SummaryItem> FunctionSummaryItems { get; }

    public ObservableCollection<SummaryItem> SystemSummaryItems { get; }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            _isGenerating = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGenerate));
            (GenerateCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        private set
        {
            _isSuccess = value;
            OnPropertyChanged();
            (OpenProjectDirectoryCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool ShowGenerationResult
    {
        get => _showGenerationResult;
        private set { _showGenerationResult = value; OnPropertyChanged(); }
    }

    public bool CanGenerate => !IsGenerating && !ShowGenerationResult;

    public string GenerationStatus
    {
        get => _generationStatus;
        private set { _generationStatus = value; OnPropertyChanged(); }
    }

    public string CurrentOperation
    {
        get => _currentOperation;
        private set { _currentOperation = value; OnPropertyChanged(); }
    }

    public double GenerationProgress
    {
        get => _generationProgress;
        private set { _generationProgress = value; OnPropertyChanged(); }
    }

    public string ResultTitle { get; private set; } = string.Empty;
    public string ResultMessage { get; private set; } = string.Empty;
    public string ResultIcon { get; private set; } = "AlertCircle";
    public Brush ResultIconForeground { get; private set; } = Brushes.Gray;
    public Brush ResultCardBackground { get; private set; } = Brushes.White;

    public ICommand GenerateCommand { get; }
    public ICommand OpenProjectDirectoryCommand { get; }
    public ICommand ViewLogCommand { get; }

    #endregion

    private void UpdateSummary()
    {
        DeviceSummaryItems.Clear();
        FunctionSummaryItems.Clear();
        SystemSummaryItems.Clear();

        if (_step2 == null) return;

        // Device Summary
        if (_step2.HasSiemensS7)
            DeviceSummaryItems.Add(new SummaryItem { Icon = "Cpu", Description = $"Siemens S7 - {_step2.S7Ip}:{_step2.S7Port}" });
        if (_step2.HasMitsubishiMc)
            DeviceSummaryItems.Add(new SummaryItem { Icon = "Chip", Description = $"Mitsubishi MC - {_step2.McIp}:{_step2.McPort}" });
        if (_step2.HasModbusTcp)
            DeviceSummaryItems.Add(new SummaryItem { Icon = "LanConnect", Description = $"Modbus TCP - {_step2.ModbusIp}:{_step2.ModbusPort}" });
        if (_step2.HasOpcUa)
            DeviceSummaryItems.Add(new SummaryItem { Icon = "Cloud", Description = $"OPC UA - {_step2.OpcUaEndpoint}" });
        if (_step2.HasHikVision)
            DeviceSummaryItems.Add(new SummaryItem { Icon = "Camera", Description = $"海康威视 - {_step2.HikIp}:{_step2.HikPort}" });
        if (_step2.HasDaHua)
            DeviceSummaryItems.Add(new SummaryItem { Icon = "CameraOutline", Description = $"大华 - {_step2.DaHuaIp}:{_step2.DaHuaPort}" });

        // Function Summary
        if (_step3 != null)
        {
            if (_step1?.ProjectTypeEnum == ProjectTypeCategory.Collection)
            {
                FunctionSummaryItems.Add(new SummaryItem { Icon = "Timer", Description = $"扫描周期: {_step3.ScanCycleMs}ms" });
                FunctionSummaryItems.Add(new SummaryItem { Icon = "Database", Description = $"数据存储: {_step3.StorageType}" });
                FunctionSummaryItems.Add(new SummaryItem { Icon = "CalendarClock", Description = $"保留策略: {_step3.DataRetentionPolicy}" });
            }
            else if (_step1?.ProjectTypeEnum == ProjectTypeCategory.Vision)
            {
                var algorithm = _step3.SelectedAlgorithm.ToString();
                FunctionSummaryItems.Add(new SummaryItem { Icon = "Brain", Description = $"算法: {algorithm}" });

                if (_step3.IsYoloSelected)
                {
                    FunctionSummaryItems.Add(new SummaryItem { Icon = "Cog", Description = $"模型: {_step3.YoloModelVersion}" });
                    FunctionSummaryItems.Add(new SummaryItem { Icon = "Percent", Description = $"置信度: {_step3.YoloConfidenceThreshold:F2}" });
                }
                else if (_step3.IsSam3Selected)
                {
                    FunctionSummaryItems.Add(new SummaryItem { Icon = "Cog", Description = $"模型: {_step3.Sam3ModelType}" });
                    FunctionSummaryItems.Add(new SummaryItem { Icon = "Shape", Description = $"分割模式: {_step3.Sam3SegmentMode}" });
                }
            }
        }

        // System Summary
        if (_step4 != null)
        {
            if (_step4.EnableUserManagement)
                SystemSummaryItems.Add(new SummaryItem { Icon = "AccountGroup", Description = "用户管理: 已启用" });
            if (_step4.EnableRoleBasedAccess)
                SystemSummaryItems.Add(new SummaryItem { Icon = "Shield", Description = "RBAC: 已启用" });
            SystemSummaryItems.Add(new SummaryItem { Icon = "Palette", Description = $"主题: {_step4.SelectedTheme}" });
            SystemSummaryItems.Add(new SummaryItem { Icon = "FormatColorFill", Description = $"强调色: {_step4.AccentColor}" });
            if (_step4.EnableLogging)
                SystemSummaryItems.Add(new SummaryItem { Icon = "FileDocument", Description = $"日志: {_step4.LogLevel}" });
        }

        OnPropertyChanged(nameof(DeviceSummaryItems));
        OnPropertyChanged(nameof(FunctionSummaryItems));
        OnPropertyChanged(nameof(SystemSummaryItems));
    }

    public async Task ExecuteGenerateAsync()
    {
        if (_projectGenerator == null || _step1 == null)
        {
            ShowError("生成器未初始化");
            return;
        }

        IsGenerating = true;
        ShowGenerationResult = false;
        GenerationProgress = 0;

        try
        {
            var config = new ConfigRegistry();
            config.Register(new ScaffoldConfigSection
            {
                ProjectName = _step1.ProjectName,
                OutputDirectory = _step1.OutputDirectory,
                ProjectType = _step1.ProjectTypeEnum.ToString()
            });

            // Simulate generation steps
            GenerationStatus = "正在准备...";
            CurrentOperation = "初始化项目结构";
            GenerationProgress = 10;
            await Task.Delay(500);

            GenerationStatus = "正在生成文件...";
            CurrentOperation = "创建项目文件";
            GenerationProgress = 40;
            await Task.Delay(800);

            GenerationStatus = "正在配置依赖...";
            CurrentOperation = "添加 NuGet 包引用";
            GenerationProgress = 70;
            await Task.Delay(600);

            GenerationStatus = "正在完成...";
            CurrentOperation = "清理和验证";
            GenerationProgress = 90;
            await Task.Delay(400);

            // Actual generation
            var result = await _projectGenerator.GenerateAsync(config);

            GenerationProgress = 100;
            if (result.Success)
            {
                ShowSuccess();
            }
            else
            {
                ShowError($"生成失败: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"生成失败: {ex.Message}");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private void ShowSuccess()
    {
        IsSuccess = true;
        ShowGenerationResult = true;
        ResultTitle = "生成成功!";
        ResultMessage = $"项目 '{ProjectName}' 已成功创建到:\n{OutputDirectory}";
        ResultIcon = "CheckCircle";
        ResultIconForeground = new SolidColorBrush(Colors.Green);
        ResultCardBackground = new SolidColorBrush(Color.FromRgb(232, 245, 233));

        OnPropertyChanged(nameof(ResultTitle));
        OnPropertyChanged(nameof(ResultMessage));
        OnPropertyChanged(nameof(ResultIcon));
        OnPropertyChanged(nameof(ResultIconForeground));
        OnPropertyChanged(nameof(ResultCardBackground));
    }

    private void ShowError(string message)
    {
        IsSuccess = false;
        ShowGenerationResult = true;
        ResultTitle = "生成失败";
        ResultMessage = message;
        ResultIcon = "CloseCircle";
        ResultIconForeground = new SolidColorBrush(Colors.Red);
        ResultCardBackground = new SolidColorBrush(Color.FromRgb(255, 235, 238));

        OnPropertyChanged(nameof(ResultTitle));
        OnPropertyChanged(nameof(ResultMessage));
        OnPropertyChanged(nameof(ResultIcon));
        OnPropertyChanged(nameof(ResultIconForeground));
        OnPropertyChanged(nameof(ResultCardBackground));
    }

    private void OpenProjectDirectory()
    {
        if (Directory.Exists(OutputDirectory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = OutputDirectory,
                UseShellExecute = true
            });
        }
    }

    private void ViewLog()
    {
        // TODO: Implement log viewer
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
