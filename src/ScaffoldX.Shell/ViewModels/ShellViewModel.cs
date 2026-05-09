using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Prism.Regions;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Shell.Plugins;

namespace ScaffoldX.Shell.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly IRegionManager _regionManager;
    private readonly IPluginHost _pluginHost;
    private IPlugin? _selectedPlugin;
    private string _currentNavigationKey = string.Empty;

    public ObservableCollection<IPlugin> Plugins { get; }
    public ICommand NavigateCommand { get; }

    public IPlugin? SelectedPlugin
    {
        get => _selectedPlugin;
        set
        {
            if (_selectedPlugin != value)
            {
                _selectedPlugin = value;
                OnPropertyChanged();
            }
        }
    }

    public string CurrentNavigationKey
    {
        get => _currentNavigationKey;
        private set
        {
            if (_currentNavigationKey != value)
            {
                _currentNavigationKey = value;
                OnPropertyChanged();
            }
        }
    }

    public ShellViewModel(IReadOnlyList<IPlugin> plugins, IRegionManager regionManager, IPluginHost pluginHost)
    {
        _regionManager = regionManager;
        _pluginHost = pluginHost;
        var sorted = plugins.OrderBy(p => p.Metadata.Order).ToList();
        Plugins = new ObservableCollection<IPlugin>(sorted);
        NavigateCommand = new RelayCommand<IPlugin>(Navigate);

        if (sorted.Count > 0)
        {
            var firstPlugin = sorted[0];
            SelectedPlugin = firstPlugin;
            CurrentNavigationKey = firstPlugin.Metadata.Id;
            NavigateToPlugin(firstPlugin);
        }
    }

    private void Navigate(IPlugin? plugin)
    {
        if (plugin is not null)
        {
            SelectedPlugin = plugin;
            CurrentNavigationKey = plugin.Metadata.Id;
            NavigateToPlugin(plugin);
        }
    }

    private void NavigateToPlugin(IPlugin plugin)
    {
        var region = _regionManager.Regions["MainRegion"];
        region.RemoveAll();
        
        // 从 PluginHost 获取视图
        if (_pluginHost is PluginHost host)
        {
            var view = host.GetView("MainRegion", plugin.Metadata.Id);
            if (view != null)
            {
                region.Add(view, plugin.Metadata.Id);
                region.Activate(view);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;

        public RelayCommand(Action<T?> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter is T t ? t : default);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
