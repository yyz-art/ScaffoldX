using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ScaffoldX.Plugin.Management.Commands;
using ScaffoldX.Plugin.Management.Models;
using ScaffoldX.Plugin.Management.Services;

namespace ScaffoldX.Plugin.Management.ViewModels;

public sealed class ManagementViewModel : INotifyPropertyChanged
{
    private readonly IProjectHistoryService? _historyService;
    private string _statusMessage = "就绪";
    private ProjectHistoryRecord? _selectedProject;
    private string _searchText = string.Empty;

    public ManagementViewModel()
    {
        RefreshCommand = new RelayCommand(() => { });
        DeleteProjectCommand = new RelayCommand(() => { }, () => false);
        OpenProjectCommand = new RelayCommand(() => { }, () => false);
        SearchCommand = new RelayCommand(() => { });
    }

    public ManagementViewModel(IProjectHistoryService historyService)
    {
        _historyService = historyService;
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        DeleteProjectCommand = new RelayCommand(async () => await DeleteProjectAsync(), () => SelectedProject is not null);
        OpenProjectCommand = new RelayCommand(OpenProject, () => SelectedProject is not null);
        SearchCommand = new RelayCommand(async () => await SearchAsync());
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ObservableCollection<ProjectHistoryRecord> Projects { get; } = [];

    public ProjectHistoryRecord? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetField(ref _selectedProject, value))
            {
                (DeleteProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (OpenProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public ICommand RefreshCommand { get; private set; }
    public ICommand DeleteProjectCommand { get; private set; }
    public ICommand OpenProjectCommand { get; private set; }
    public ICommand SearchCommand { get; private set; }

    public async Task RefreshAsync()
    {
        if (_historyService is null) return;
        StatusMessage = "正在刷新...";
        var records = await _historyService.GetAllRecordsAsync();
        Projects.Clear();
        foreach (var r in records) Projects.Add(r);
        StatusMessage = $"已加载 {records.Count} 个项目";
    }

    public async Task DeleteProjectAsync()
    {
        if (SelectedProject is null || _historyService is null) return;
        await _historyService.DeleteRecordAsync(SelectedProject.OutputPath);
        Projects.Remove(SelectedProject);
        SelectedProject = null;
        StatusMessage = "项目已删除";
    }

    public void OpenProject()
    {
        if (SelectedProject is null) return;
        try
        {
            Process.Start(new ProcessStartInfo(SelectedProject.OutputPath) { UseShellExecute = true });
        }
        catch
        {
            StatusMessage = "无法打开项目路径";
        }
    }

    public async Task SearchAsync()
    {
        if (_historyService is null) return;
        var records = await _historyService.GetAllRecordsAsync();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? records
            : records.Where(r =>
                r.ProjectName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.ProjectType.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Projects.Clear();
        foreach (var r in filtered) Projects.Add(r);
        StatusMessage = $"找到 {filtered.Count} 个项目";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class ProjectRecord
{
    public string ProjectName { get; init; } = string.Empty;
    public string ProjectType { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
