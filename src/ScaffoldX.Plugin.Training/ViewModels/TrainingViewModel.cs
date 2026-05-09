using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ScaffoldX.Plugin.Training.Models;

namespace ScaffoldX.Plugin.Training.ViewModels;

public sealed class TrainingViewModel : INotifyPropertyChanged
{
    private string _statusMessage = "就绪";
    private bool _isTraining;
    private int _trainingProgress;
    private string _trainedModelPath = string.Empty;

    public TrainingConfig Config { get; } = new();

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public bool IsTraining
    {
        get => _isTraining;
        set => SetField(ref _isTraining, value);
    }

    public int TrainingProgress
    {
        get => _trainingProgress;
        set => SetField(ref _trainingProgress, value);
    }

    public string TrainedModelPath
    {
        get => _trainedModelPath;
        set
        {
            if (SetField(ref _trainedModelPath, value))
                ExportModelCommand.RaiseCanExecuteChanged();
        }
    }

    public DelegateCommand StartTrainingCommand { get; }
    public DelegateCommand ExportModelCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TrainingViewModel()
    {
        StartTrainingCommand = new DelegateCommand(
            _ => { IsTraining = true; StatusMessage = "训练已启动"; },
            _ => Config.Validate().IsValid
        );
        ExportModelCommand = new DelegateCommand(
            _ => { StatusMessage = "模型导出中..."; },
            _ => !string.IsNullOrWhiteSpace(TrainedModelPath)
        );
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class DelegateCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool> _canExecute;

    public DelegateCommand(Action<object?> execute, Func<object?, bool> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute(parameter);
    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CanExecuteChanged;
}
