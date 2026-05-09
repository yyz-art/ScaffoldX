using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public sealed class Step2ViewModel : INotifyPropertyChanged
{
    private string _description = string.Empty;
    private string _author = string.Empty;
    private string _company = string.Empty;
    private string _copyright = string.Empty;
    private string _targetFramework = "net10.0-windows";

    public Step2ViewModel()
    {
        BackCommand = new RelayCommand(OnBack);
        NextCommand = new RelayCommand(OnNext);
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public string Author
    {
        get => _author;
        set { _author = value; OnPropertyChanged(); }
    }

    public string Company
    {
        get => _company;
        set { _company = value; OnPropertyChanged(); }
    }

    public string Copyright
    {
        get => _copyright;
        set { _copyright = value; OnPropertyChanged(); }
    }

    public string TargetFramework
    {
        get => _targetFramework;
        set { _targetFramework = value; OnPropertyChanged(); }
    }

    public ICommand BackCommand { get; }
    public ICommand NextCommand { get; }

    public event Action? BackRequested;
    public event Action? NextRequested;

    private void OnBack() => BackRequested?.Invoke();
    private void OnNext() => NextRequested?.Invoke();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
