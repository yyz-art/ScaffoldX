using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Plugin.Scaffold.Services;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public sealed class Step4ViewModel : INotifyPropertyChanged
{
    private readonly IProjectGenerator _projectGenerator;
    private readonly Step1ViewModel _step1;
    private readonly Step2ViewModel _step2;
    private readonly Step3ViewModel _step3;
    private bool _isGenerating;
    private GenerationResult? _result;
    private int _progressPercent;
    private string _progressMessage = string.Empty;
    private string _resultMessage = string.Empty;

    public Step4ViewModel(
        IProjectGenerator projectGenerator,
        Step1ViewModel step1,
        Step2ViewModel step2,
        Step3ViewModel step3)
    {
        _projectGenerator = projectGenerator;
        _step1 = step1;
        _step2 = step2;
        _step3 = step3;
        GenerateCommand = new RelayCommand(() => _ = ExecuteGenerateAsync(), () => CanGenerate);
    }

    public string SummaryProjectName => _step1.ProjectName;
    public string SummaryProjectType => _step1.SelectedProjectType;
    public string SummaryOutputDirectory => _step1.OutputDirectory;
    public string SummaryTargetFramework => "net10.0-windows";
    public string SummaryAuthor => _step2.Author;

    public string SummaryDriversText
    {
        get
        {
            var drivers = new List<string>();
            if (_step3.HasSiemensS7) drivers.Add("西门子 S7");
            if (_step3.HasMitsubishiQ) drivers.Add("三菱 MC");
            if (_step3.HasModbusTcp) drivers.Add("Modbus TCP");
            if (_step3.HasOpcUa) drivers.Add("OPC UA");
            return drivers.Count > 0 ? string.Join(", ", drivers) : "无";
        }
    }

    public string SummaryVisionText
    {
        get
        {
            var vision = new List<string>();
            if (_step3.HasHikVision) vision.Add("海康相机");
            if (_step3.HasDaHua) vision.Add("大华相机");
            if (_step3.HasYoloDetection) vision.Add("YOLO 检测");
            if (_step3.HasSam3Segmentation) vision.Add("SAM3 分割");
            return vision.Count > 0 ? string.Join(", ", vision) : "无";
        }
    }

    public string SummarySystemText
    {
        get
        {
            var system = new List<string>();
            if (_step3.HasUserManagement) system.Add("用户管理");
            if (_step3.HasThemeSwitcher) system.Add("主题切换");
            return system.Count > 0 ? string.Join(", ", system) : "无";
        }
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            if (_isGenerating != value)
            {
                _isGenerating = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGenerate));
                ((RelayCommand)GenerateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        private set
        {
            if (_progressPercent != value)
            {
                _progressPercent = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        private set
        {
            if (_progressMessage != value)
            {
                _progressMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string ResultMessage
    {
        get => _resultMessage;
        private set
        {
            if (_resultMessage != value)
            {
                _resultMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanGenerate => !IsGenerating && _step1.IsValid;

    public ICommand GenerateCommand { get; }

    public async Task ExecuteGenerateAsync()
    {
        IsGenerating = true;
        ResultMessage = string.Empty;
        ProgressPercent = 0;
        ProgressMessage = "准备生成...";

        try
        {
            var registry = BuildConfigRegistry();
            var progress = new Progress<GenerationProgress>(p =>
            {
                ProgressPercent = p.Percent;
                ProgressMessage = p.Message;
            });

            _result = await _projectGenerator.GenerateAsync(registry, progress);

            if (_result.Success)
            {
                ResultMessage = $"生成成功！共生成 {_result.FileCount} 个文件，耗时 {_result.Elapsed.TotalSeconds:F1} 秒";
            }
            else
            {
                ResultMessage = $"生成失败：{_result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            ResultMessage = $"发生异常：{ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    public ConfigRegistry BuildConfigRegistry()
    {
        var registry = new ConfigRegistry();

        registry.Register(new ScaffoldConfigSection
        {
            ProjectName = _step1.ProjectName,
            OutputDirectory = _step1.OutputDirectory,
            ProjectType = _step1.SelectedProjectType,
            TargetFramework = "net10.0-windows",
            Author = _step2.Author,
            Company = _step2.Company,
            Description = _step2.Description,
        });

        registry.Register(new CollectionConfigSection
        {
            EnableSiemensS7 = _step3.HasSiemensS7,
            EnableMitsubishiMc = _step3.HasMitsubishiQ,
            EnableModbusTcp = _step3.HasModbusTcp,
            EnableOpcUa = _step3.HasOpcUa,
        });

        registry.Register(new VisionConfigSection
        {
            EnableVision = _step3.HasAnyVision,
            CameraBrand = _step3.HasHikVision ? "HikVision" : _step3.HasDaHua ? "DaHua" : "",
            ModelType = _step3.HasYoloDetection ? "YoloDetection" : _step3.HasSam3Segmentation ? "Sam3Segmentation" : "",
        });

        registry.Register(new SystemConfigSection
        {
            EnableUserManagement = _step3.HasUserManagement,
            EnableThemeSwitcher = _step3.HasThemeSwitcher,
        });

        registry.Register(new UIConfigSection());

        return registry;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
