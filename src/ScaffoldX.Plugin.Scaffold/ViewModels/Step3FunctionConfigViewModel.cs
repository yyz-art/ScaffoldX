using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public enum VisionAlgorithm
{
    None,
    Yolo,
    Sam3
}

public enum StorageType
{
    Sqlite,
    InfluxDb,
    SqlServer
}

public sealed class Step3FunctionConfigViewModel : INotifyPropertyChanged
{
    private ProjectTypeCategory _projectType;

    // Collection 配置
    private int _scanCycleMs = 1000;
    private bool _enableDataLogging = true;
    private StorageType _storageType = StorageType.Sqlite;
    private string _databaseConnectionString = string.Empty;
    private string _dataRetentionPolicy = "30 天";

    // Vision 配置
    private VisionAlgorithm _selectedAlgorithm = VisionAlgorithm.Yolo;

    // YOLO 参数
    private string _yoloModelVersion = "YOLOv8n";
    private double _yoloConfidenceThreshold = 0.5;
    private double _yoloIouThreshold = 0.45;
    private string _yoloInputSize = "640";

    // SAM3 参数
    private string _sam3ModelType = "SAM3-H";
    private string _sam3SegmentMode = "自动分割";
    private double _sam3StabilityScoreThresh = 0.95;
    private double _sam3CropNmsThresh = 0.7;

    public Step3FunctionConfigViewModel()
    {
        SelectYoloCommand = new RelayCommand(() => SelectedAlgorithm = VisionAlgorithm.Yolo);
        SelectSam3Command = new RelayCommand(() => SelectedAlgorithm = VisionAlgorithm.Sam3);
    }

    public void SetProjectType(ProjectTypeCategory type)
    {
        _projectType = type;
        OnPropertyChanged(nameof(IsCollectionVisible));
        OnPropertyChanged(nameof(IsVisionVisible));
        OnPropertyChanged(nameof(IsSystemVisible));
    }

    // Visibility
    public bool IsCollectionVisible => _projectType == ProjectTypeCategory.Collection;
    public bool IsVisionVisible => _projectType == ProjectTypeCategory.Vision;
    public bool IsSystemVisible => _projectType == ProjectTypeCategory.System;

    #region Collection Properties

    public int ScanCycleMs
    {
        get => _scanCycleMs;
        set { _scanCycleMs = value; OnPropertyChanged(); }
    }

    public bool EnableDataLogging
    {
        get => _enableDataLogging;
        set { _enableDataLogging = value; OnPropertyChanged(); }
    }

    public bool StorageTypeSqlite
    {
        get => _storageType == StorageType.Sqlite;
        set { if (value) StorageType = StorageType.Sqlite; }
    }

    public bool StorageTypeInfluxDb
    {
        get => _storageType == StorageType.InfluxDb;
        set { if (value) StorageType = StorageType.InfluxDb; }
    }

    public bool StorageTypeSqlServer
    {
        get => _storageType == StorageType.SqlServer;
        set { if (value) StorageType = StorageType.SqlServer; }
    }

    public StorageType StorageType
    {
        get => _storageType;
        set
        {
            _storageType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StorageTypeSqlite));
            OnPropertyChanged(nameof(StorageTypeInfluxDb));
            OnPropertyChanged(nameof(StorageTypeSqlServer));
            OnPropertyChanged(nameof(StorageTypeRequiresConnectionString));
        }
    }

    public bool StorageTypeRequiresConnectionString => _storageType is StorageType.InfluxDb or StorageType.SqlServer;

    public string DatabaseConnectionString
    {
        get => _databaseConnectionString;
        set { _databaseConnectionString = value; OnPropertyChanged(); }
    }

    public string DataRetentionPolicy
    {
        get => _dataRetentionPolicy;
        set { _dataRetentionPolicy = value; OnPropertyChanged(); }
    }

    #endregion

    #region Vision Properties

    public VisionAlgorithm SelectedAlgorithm
    {
        get => _selectedAlgorithm;
        set
        {
            _selectedAlgorithm = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsYoloSelected));
            OnPropertyChanged(nameof(IsSam3Selected));
            OnPropertyChanged(nameof(YoloCardBrush));
            OnPropertyChanged(nameof(Sam3CardBrush));
        }
    }

    public bool IsYoloSelected => _selectedAlgorithm == VisionAlgorithm.Yolo;
    public bool IsSam3Selected => _selectedAlgorithm == VisionAlgorithm.Sam3;

    public Brush YoloCardBrush => GetAlgorithmCardBrush(VisionAlgorithm.Yolo);
    public Brush Sam3CardBrush => GetAlgorithmCardBrush(VisionAlgorithm.Sam3);

    private Brush GetAlgorithmCardBrush(VisionAlgorithm algorithm)
    {
        var isSelected = _selectedAlgorithm == algorithm;
        return isSelected
            ? new SolidColorBrush(Colors.DodgerBlue)
            : new SolidColorBrush(Colors.LightGray);
    }

    public ICommand SelectYoloCommand { get; }
    public ICommand SelectSam3Command { get; }

    #endregion

    #region YOLO Parameters

    public string YoloModelVersion
    {
        get => _yoloModelVersion;
        set { _yoloModelVersion = value; OnPropertyChanged(); }
    }

    public double YoloConfidenceThreshold
    {
        get => _yoloConfidenceThreshold;
        set { _yoloConfidenceThreshold = value; OnPropertyChanged(); }
    }

    public double YoloIouThreshold
    {
        get => _yoloIouThreshold;
        set { _yoloIouThreshold = value; OnPropertyChanged(); }
    }

    public string YoloInputSize
    {
        get => _yoloInputSize;
        set { _yoloInputSize = value; OnPropertyChanged(); }
    }

    #endregion

    #region SAM3 Parameters

    public string Sam3ModelType
    {
        get => _sam3ModelType;
        set { _sam3ModelType = value; OnPropertyChanged(); }
    }

    public string Sam3SegmentMode
    {
        get => _sam3SegmentMode;
        set { _sam3SegmentMode = value; OnPropertyChanged(); }
    }

    public double Sam3StabilityScoreThresh
    {
        get => _sam3StabilityScoreThresh;
        set { _sam3StabilityScoreThresh = value; OnPropertyChanged(); }
    }

    public double Sam3CropNmsThresh
    {
        get => _sam3CropNmsThresh;
        set { _sam3CropNmsThresh = value; OnPropertyChanged(); }
    }

    #endregion

    public bool IsValid => _projectType switch
    {
        ProjectTypeCategory.Collection => true,
        ProjectTypeCategory.Vision => _selectedAlgorithm != VisionAlgorithm.None,
        ProjectTypeCategory.System => true,
        _ => false
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
