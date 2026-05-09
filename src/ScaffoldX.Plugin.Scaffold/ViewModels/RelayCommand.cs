using System.Windows.Input;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute() => _canExecute?.Invoke() ?? true;

    bool ICommand.CanExecute(object? parameter) => CanExecute();

    public void Execute() => _execute();

    void ICommand.Execute(object? parameter) => Execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
