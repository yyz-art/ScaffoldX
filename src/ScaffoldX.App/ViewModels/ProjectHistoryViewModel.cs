using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 历史项目列表 ViewModel，负责加载、展示和管理历史记录。
/// </summary>
public class ProjectHistoryViewModel : BindableBase
{
    private readonly IHistoryService _historyService;
    private ProjectHistory? _selectedItem;

    /// <summary>当外部请求新建项目时触发（由主窗口 ViewModel 订阅）。</summary>
    public event EventHandler? NewProjectRequested;

    /// <summary>
    /// 初始化历史列表 ViewModel，注入历史服务并加载数据。
    /// </summary>
    /// <param name="historyService">历史记录持久化服务。</param>
    public ProjectHistoryViewModel(IHistoryService historyService)
    {
        _historyService = historyService;
        HistoryItems = new ObservableCollection<ProjectHistory>();

        NewProjectCommand = new DelegateCommand(ExecuteNewProject);
        OpenFolderCommand = new DelegateCommand(ExecuteOpenFolder, CanExecuteItemCommand)
            .ObservesProperty(() => SelectedItem);
        DeleteHistoryCommand = new DelegateCommand(ExecuteDeleteHistory, CanExecuteItemCommand)
            .ObservesProperty(() => SelectedItem);

        _ = LoadHistoryAsync();
    }

    /// <summary>历史记录集合，绑定到 DataGrid。</summary>
    public ObservableCollection<ProjectHistory> HistoryItems { get; }

    /// <summary>当前选中的历史记录条目。</summary>
    public ProjectHistory? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    /// <summary>新建项目命令。</summary>
    public DelegateCommand NewProjectCommand { get; }

    /// <summary>打开所在文件夹命令。</summary>
    public DelegateCommand OpenFolderCommand { get; }

    /// <summary>从历史中删除命令。</summary>
    public DelegateCommand DeleteHistoryCommand { get; }

    private void ExecuteNewProject()
    {
        NewProjectRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteOpenFolder()
    {
        if (SelectedItem is null) return;
        var path = SelectedItem.OutputPath;
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
        }
    }

    private async void ExecuteDeleteHistory()
    {
        if (SelectedItem is null) return;
        var name = SelectedItem.ProjectName;
        await _historyService.DeleteAsync(name);
        var item = HistoryItems.FirstOrDefault(h => h.ProjectName == name);
        if (item is not null)
            HistoryItems.Remove(item);
    }

    private bool CanExecuteItemCommand() => SelectedItem is not null;

    private async Task LoadHistoryAsync()
    {
        var items = await _historyService.LoadAsync();
        HistoryItems.Clear();
        foreach (var item in items)
            HistoryItems.Add(item);
    }
}
