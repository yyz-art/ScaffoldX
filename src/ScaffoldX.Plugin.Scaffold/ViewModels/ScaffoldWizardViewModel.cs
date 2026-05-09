using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScaffoldX.Plugin.Scaffold.Services;
using ScaffoldX.Plugin.Scaffold.Views;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public sealed class ScaffoldWizardViewModel : INotifyPropertyChanged
{
    private readonly IValidationService _validationService;
    private readonly IProjectGenerator _projectGenerator;
    private int _currentStepIndex;
    private UserControl? _currentView;

    public Step1ViewModel Step1 { get; }
    public Step2DeviceConfigViewModel Step2 { get; }
    public Step3FunctionConfigViewModel Step3 { get; }
    public Step4SystemConfigViewModel Step4 { get; }
    public Step5ConfirmViewModel Step5 { get; }

    public ScaffoldWizardViewModel() : this(
        new ValidationService(),
        new EnhancedProjectGenerator(
            new ScribanTemplateEngine(),
            new ValidationService(),
            EmbeddedTemplateSource.ForTemplatesAssembly()))
    {
    }

    public ScaffoldWizardViewModel(IValidationService validationService, IProjectGenerator projectGenerator)
    {
        _validationService = validationService;
        _projectGenerator = projectGenerator;

        Step1 = new Step1ViewModel(_validationService);
        Step2 = new Step2DeviceConfigViewModel(_validationService);
        Step3 = new Step3FunctionConfigViewModel();
        Step4 = new Step4SystemConfigViewModel();
        Step5 = new Step5ConfirmViewModel(_projectGenerator, Step1, Step2, Step3, Step4);

        PreviousCommand = new RelayCommand(GoToPrevious, () => CanGoPrevious);
        NextCommand = new RelayCommand(GoToNext, () => CanGoNext);

        // Listen for property changes
        Step1.PropertyChanged += (_, _) => RefreshCanExecute();
        Step2.PropertyChanged += (_, _) => RefreshCanExecute();
        Step3.PropertyChanged += (_, _) => RefreshCanExecute();
        Step4.PropertyChanged += (_, _) => RefreshCanExecute();
        Step5.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Step5ConfirmViewModel.IsGenerating))
            {
                RefreshCanExecute();
            }
        };

        UpdateCurrentView();
    }

    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        private set
        {
            if (_currentStepIndex != value)
            {
                _currentStepIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(NextButtonText));
                UpdateStepBrushes();
                UpdateCurrentView();
            }
        }
    }

    public UserControl? CurrentView
    {
        get => _currentView;
        private set
        {
            if (_currentView != value)
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanGoPrevious => CurrentStepIndex > 0 && !Step5.IsGenerating;

    public bool CanGoNext
    {
        get
        {
            if (Step5.IsGenerating) return false;

            return CurrentStepIndex switch
            {
                0 => Step1.IsValid,
                1 => Step2.IsValid,
                2 => Step3.IsValid,
                3 => Step4.IsValid,
                4 => Step5.CanGenerate,
                _ => false
            };
        }
    }

    public string NextButtonText => CurrentStepIndex == 4 ? "生成" : "下一步";

    // Step indicator brushes
    public Brush Step1Brush => GetStepBrush(0);
    public Brush Step2Brush => GetStepBrush(1);
    public Brush Step3Brush => GetStepBrush(2);
    public Brush Step4Brush => GetStepBrush(3);
    public Brush Step5Brush => GetStepBrush(4);

    public FontWeight Step1FontWeight => GetStepFontWeight(0);
    public FontWeight Step2FontWeight => GetStepFontWeight(1);
    public FontWeight Step3FontWeight => GetStepFontWeight(2);
    public FontWeight Step4FontWeight => GetStepFontWeight(3);
    public FontWeight Step5FontWeight => GetStepFontWeight(4);

    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }

    private Brush GetStepBrush(int stepIndex)
    {
        var isActive = stepIndex == CurrentStepIndex;
        var isCompleted = stepIndex < CurrentStepIndex;

        if (isActive)
            return new SolidColorBrush(Colors.DodgerBlue);
        if (isCompleted)
            return new SolidColorBrush(Colors.Green);
        return new SolidColorBrush(Colors.Gray);
    }

    private FontWeight GetStepFontWeight(int stepIndex)
    {
        return stepIndex == CurrentStepIndex ? FontWeights.Bold : FontWeights.Normal;
    }

    private void UpdateStepBrushes()
    {
        OnPropertyChanged(nameof(Step1Brush));
        OnPropertyChanged(nameof(Step1FontWeight));
        OnPropertyChanged(nameof(Step2Brush));
        OnPropertyChanged(nameof(Step2FontWeight));
        OnPropertyChanged(nameof(Step3Brush));
        OnPropertyChanged(nameof(Step3FontWeight));
        OnPropertyChanged(nameof(Step4Brush));
        OnPropertyChanged(nameof(Step4FontWeight));
        OnPropertyChanged(nameof(Step5Brush));
        OnPropertyChanged(nameof(Step5FontWeight));
    }

    private void UpdateCurrentView()
    {
        // Update Step2 project type when navigating to it
        if (CurrentStepIndex == 1)
        {
            Step2.SetProjectType(Step1.ProjectTypeEnum);
        }

        // Update Step3 project type when navigating to it
        if (CurrentStepIndex == 2)
        {
            Step3.SetProjectType(Step1.ProjectTypeEnum);
        }

        // Update Step4 visibility when navigating to it
        if (CurrentStepIndex == 3)
        {
            Step4.SetProjectType(Step1.ProjectTypeEnum);
        }

        CurrentView = CurrentStepIndex switch
        {
            0 => new Step1View { DataContext = Step1 },
            1 => new Step2DeviceConfigView { DataContext = Step2 },
            2 => new Step3FunctionConfigView { DataContext = Step3 },
            3 => new Step4SystemConfigView { DataContext = Step4 },
            4 => new Step5ConfirmView { DataContext = Step5 },
            _ => null
        };
    }

    private void GoToPrevious()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
        }
    }

    private void GoToNext()
    {
        if (CurrentStepIndex < 4)
        {
            CurrentStepIndex++;
        }
        else if (CurrentStepIndex == 4)
        {
            _ = Step5.ExecuteGenerateAsync();
        }
    }

    public void RefreshCanExecute()
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        if (PreviousCommand is RelayCommand prevCmd)
            prevCmd.RaiseCanExecuteChanged();
        if (NextCommand is RelayCommand nextCmd)
            nextCmd.RaiseCanExecuteChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
