using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ScaffoldX.Plugin.Scaffold.Services;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public enum ProjectTypeCategory
{
    None,
    Collection,
    Vision,
    System
}

public sealed class Step1ViewModel : INotifyPropertyChanged
{
    private readonly IValidationService _validationService;
    private ProjectTypeCategory _projectType = ProjectTypeCategory.None;
    private string _projectName = string.Empty;
    private string _outputDirectory = string.Empty;
    private string _projectNameError = string.Empty;
    private string _outputDirectoryError = string.Empty;
    private string _projectTypeError = string.Empty;

    public Step1ViewModel(IValidationService validationService)
    {
        _validationService = validationService;
        BrowseCommand = new RelayCommand(OnBrowse);
    }

    public string ProjectName
    {
        get => _projectName;
        set
        {
            if (_projectName != value)
            {
                _projectName = value;
                OnPropertyChanged();
                ValidateProjectName();
            }
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (_outputDirectory != value)
            {
                _outputDirectory = value;
                OnPropertyChanged();
                ValidateOutputDirectory();
            }
        }
    }

    public ProjectTypeCategory ProjectTypeEnum => _projectType;

    public string SelectedProjectType
    {
        get => _projectType.ToString();
        set
        {
            if (Enum.TryParse<ProjectTypeCategory>(value, out var type) && _projectType != type)
            {
                _projectType = type;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProjectTypeEnum));
                OnPropertyChanged(nameof(CollectionCardBrush));
                OnPropertyChanged(nameof(VisionCardBrush));
                OnPropertyChanged(nameof(SystemCardBrush));
                ValidateProjectType();
            }
        }
    }

    public string ProjectNameError
    {
        get => _projectNameError;
        private set
        {
            if (_projectNameError != value)
            {
                _projectNameError = value;
                OnPropertyChanged();
            }
        }
    }

    public string OutputDirectoryError
    {
        get => _outputDirectoryError;
        private set
        {
            if (_outputDirectoryError != value)
            {
                _outputDirectoryError = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProjectTypeError
    {
        get => _projectTypeError;
        private set
        {
            if (_projectTypeError != value)
            {
                _projectTypeError = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsValid =>
        string.IsNullOrEmpty(ProjectNameError) &&
        string.IsNullOrEmpty(OutputDirectoryError) &&
        string.IsNullOrEmpty(ProjectTypeError) &&
        !string.IsNullOrWhiteSpace(ProjectName) &&
        !string.IsNullOrWhiteSpace(OutputDirectory) &&
        _projectType != ProjectTypeCategory.None;

    // Card background brushes
    public Brush CollectionCardBrush => GetCardBrush(ProjectTypeCategory.Collection);
    public Brush VisionCardBrush => GetCardBrush(ProjectTypeCategory.Vision);
    public Brush SystemCardBrush => GetCardBrush(ProjectTypeCategory.System);

    public ICommand BrowseCommand { get; }

    private Brush GetCardBrush(ProjectTypeCategory type)
    {
        var isSelected = _projectType == type;
        return isSelected 
            ? new SolidColorBrush(Colors.DodgerBlue) 
            : new SolidColorBrush(Colors.LightGray);
    }

    private void OnBrowse()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择输出目录"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputDirectory = dialog.FolderName;
        }
    }

    private void ValidateProjectName()
    {
        var result = _validationService.ValidateProjectName(ProjectName);
        ProjectNameError = result.IsValid ? string.Empty : result.ErrorMessage;
    }

    private void ValidateOutputDirectory()
    {
        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            OutputDirectoryError = "输出目录不能为空";
        }
        else if (!Directory.Exists(OutputDirectory))
        {
            OutputDirectoryError = "目录不存在";
        }
        else
        {
            OutputDirectoryError = string.Empty;
        }
    }

    private void ValidateProjectType()
    {
        ProjectTypeError = _projectType == ProjectTypeCategory.None ? "请选择项目类型" : string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
