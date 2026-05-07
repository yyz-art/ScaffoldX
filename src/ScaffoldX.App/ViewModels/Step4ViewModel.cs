using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using ScaffoldX.Core.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 文件树节点，用于步骤四的 TreeView 展示。
/// </summary>
public class FileTreeNode : BindableBase
{
    /// <summary>节点显示名称（文件名或目录名）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>是否为目录节点。</summary>
    public bool IsDirectory { get; init; }

    /// <summary>子节点集合。</summary>
    public ObservableCollection<FileTreeNode> Children { get; } = new();
}

/// <summary>
/// 步骤四 ViewModel：确认与生成，负责展示文件树预览、执行生成并报告进度。
/// </summary>
public class Step4ViewModel : BindableBase
{
    private readonly IProjectGenerator _generator;

    private ProjectConfig? _config;
    private int _progress;
    private string _statusMessage = "准备就绪，点击「开始生成」按钮开始生成项目。";
    private bool _isGenerating;
    private bool _isCompleted;
    private bool _isError;
    private string _elapsedText = string.Empty;
    private GenerationResult? _lastResult;

    /// <summary>
    /// 初始化步骤四 ViewModel，注入项目生成器服务。
    /// </summary>
    /// <param name="generator">项目生成器服务。</param>
    public Step4ViewModel(IProjectGenerator generator)
    {
        _generator = generator;
        FileTreeRoot = new ObservableCollection<FileTreeNode>();

        GenerateCommand = new DelegateCommand(async () => await ExecuteGenerateAsync(), CanExecuteGenerate)
            .ObservesProperty(() => IsGenerating)
            .ObservesProperty(() => IsCompleted);

        OpenFolderCommand = new DelegateCommand(ExecuteOpenFolder, () => IsCompleted && _lastResult?.Success == true)
            .ObservesProperty(() => IsCompleted);

        OpenInVsCommand = new DelegateCommand(ExecuteOpenInVs, () => IsCompleted && _lastResult?.Success == true)
            .ObservesProperty(() => IsCompleted);
    }

    /// <summary>文件树根节点集合，绑定到 TreeView。</summary>
    public ObservableCollection<FileTreeNode> FileTreeRoot { get; }

    /// <summary>生成进度百分比（0-100）。</summary>
    public int Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    /// <summary>当前状态描述文字。</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>是否正在生成中。</summary>
    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            if (SetProperty(ref _isGenerating, value))
                RaisePropertyChanged(nameof(GenerateButtonText));
        }
    }

    /// <summary>是否已完成生成（成功或失败）。</summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        private set => SetProperty(ref _isCompleted, value);
    }

    /// <summary>是否生成出错。</summary>
    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    /// <summary>生成耗时文字，如 "耗时 3.2 秒"。</summary>
    public string ElapsedText
    {
        get => _elapsedText;
        private set => SetProperty(ref _elapsedText, value);
    }

    /// <summary>生成按钮文字（生成中时显示"生成中..."）。</summary>
    public string GenerateButtonText => IsGenerating ? "生成中..." : "开始生成";

    /// <summary>项目名称（用于统计面板显示）。</summary>
    public string ProjectName => _config?.ProjectName ?? string.Empty;

    /// <summary>项目类型显示名称。</summary>
    public string ProjectTypeDisplay => _config?.ProjectType switch
    {
        "Collection" => "工业采集",
        "Vision"     => "视觉检测",
        "System"     => "系统定制",
        _            => string.Empty
    };

    /// <summary>目标框架显示文字。</summary>
    public string TargetFrameworkDisplay => _config is null
        ? string.Empty
        : $"{_config.UIFramework} / {_config.TargetFramework}";

    /// <summary>输出路径。</summary>
    public string OutputPath => _config?.OutputDirectory ?? string.Empty;

    /// <summary>预估生成文件数量。</summary>
    public int EstimatedFileCount => _lastResult?.FileCount > 0 ? _lastResult.FileCount : EstimateFileCount();

    /// <summary>开始生成命令。</summary>
    public DelegateCommand GenerateCommand { get; }

    /// <summary>打开输出文件夹命令。</summary>
    public DelegateCommand OpenFolderCommand { get; }

    /// <summary>用 Visual Studio 打开解决方案命令。</summary>
    public DelegateCommand OpenInVsCommand { get; }

    /// <summary>
    /// 由主 ViewModel 在进入步骤四前调用，传入最终配置并构建文件树预览。
    /// </summary>
    /// <param name="config">完整的项目配置。</param>
    public void Initialize(ProjectConfig config)
    {
        _config = config;
        IsGenerating = false;
        IsCompleted = false;
        IsError = false;
        Progress = 0;
        StatusMessage = "准备就绪，点击「开始生成」按钮开始生成项目。";
        ElapsedText = string.Empty;
        _lastResult = null;

        BuildFileTree(config);

        RaisePropertyChanged(nameof(ProjectName));
        RaisePropertyChanged(nameof(ProjectTypeDisplay));
        RaisePropertyChanged(nameof(TargetFrameworkDisplay));
        RaisePropertyChanged(nameof(OutputPath));
        RaisePropertyChanged(nameof(EstimatedFileCount));
    }

    private bool CanExecuteGenerate() => !IsGenerating && !IsCompleted;

    private async Task ExecuteGenerateAsync()
    {
        if (_config is null) return;

        IsGenerating = true;
        IsCompleted = false;
        IsError = false;
        Progress = 0;

        var progressReporter = new Progress<GenerationProgress>(p =>
        {
            Progress = p.Percent;
            StatusMessage = p.Message;
        });

        try
        {
            _lastResult = await _generator.GenerateAsync(_config, progressReporter);

            if (_lastResult.Success)
            {
                StatusMessage = $"生成成功！共生成 {_lastResult.FileCount} 个文件。";
                ElapsedText = $"耗时 {_lastResult.Elapsed.TotalSeconds:F1} 秒";
                Progress = 100;
            }
            else
            {
                StatusMessage = $"生成失败：{_lastResult.ErrorMessage}";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成异常：{ex.Message}";
            IsError = true;
        }
        finally
        {
            IsGenerating = false;
            IsCompleted = true;
            RaisePropertyChanged(nameof(EstimatedFileCount));
        }
    }

    private void ExecuteOpenFolder()
    {
        var path = _lastResult?.OutputPath ?? _config?.OutputDirectory;
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        });
    }

    private void ExecuteOpenInVs()
    {
        var path = _lastResult?.OutputPath ?? _config?.OutputDirectory;
        if (string.IsNullOrEmpty(path)) return;

        var slnFile = Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (slnFile is null) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = slnFile,
            UseShellExecute = true
        });
    }

    private void BuildFileTree(ProjectConfig config)
    {
        FileTreeRoot.Clear();

        var root = new FileTreeNode { Name = $"{config.ProjectName}/", IsDirectory = true };

        // 解决方案文件
        root.Children.Add(new FileTreeNode { Name = $"{config.ProjectName}.sln" });

        // 主项目
        var mainProject = new FileTreeNode { Name = $"{config.ProjectName}.{config.UIFramework}/", IsDirectory = true };
        mainProject.Children.Add(new FileTreeNode { Name = "App.xaml" });
        mainProject.Children.Add(new FileTreeNode { Name = "App.xaml.cs" });
        mainProject.Children.Add(new FileTreeNode { Name = "MainWindow.xaml" });

        var viewsNode = new FileTreeNode { Name = "Views/", IsDirectory = true };
        var vmNode    = new FileTreeNode { Name = "ViewModels/", IsDirectory = true };
        var svcNode   = new FileTreeNode { Name = "Services/", IsDirectory = true };

        switch (config.ProjectType)
        {
            case "Collection":
                viewsNode.Children.Add(new FileTreeNode { Name = "MainView.xaml" });
                viewsNode.Children.Add(new FileTreeNode { Name = "DataMonitorView.xaml" });
                vmNode.Children.Add(new FileTreeNode { Name = "MainViewModel.cs" });
                vmNode.Children.Add(new FileTreeNode { Name = "DataMonitorViewModel.cs" });
                svcNode.Children.Add(new FileTreeNode { Name = "IDriverService.cs" });
                svcNode.Children.Add(new FileTreeNode { Name = "PlcDriverService.cs" });
                break;
            case "Vision":
                viewsNode.Children.Add(new FileTreeNode { Name = "MainView.xaml" });
                viewsNode.Children.Add(new FileTreeNode { Name = "InspectionView.xaml" });
                vmNode.Children.Add(new FileTreeNode { Name = "MainViewModel.cs" });
                vmNode.Children.Add(new FileTreeNode { Name = "InspectionViewModel.cs" });
                svcNode.Children.Add(new FileTreeNode { Name = "ICameraService.cs" });
                svcNode.Children.Add(new FileTreeNode { Name = "InferenceService.cs" });
                break;
            case "System":
                viewsNode.Children.Add(new FileTreeNode { Name = "LoginWindow.xaml" });
                viewsNode.Children.Add(new FileTreeNode { Name = "MainView.xaml" });
                vmNode.Children.Add(new FileTreeNode { Name = "LoginViewModel.cs" });
                vmNode.Children.Add(new FileTreeNode { Name = "MainViewModel.cs" });
                svcNode.Children.Add(new FileTreeNode { Name = "IUserService.cs" });
                svcNode.Children.Add(new FileTreeNode { Name = "UserService.cs" });
                break;
        }

        mainProject.Children.Add(viewsNode);
        mainProject.Children.Add(vmNode);
        mainProject.Children.Add(svcNode);
        mainProject.Children.Add(new FileTreeNode { Name = $"{config.ProjectName}.{config.UIFramework}.csproj" });

        root.Children.Add(mainProject);

        // Core 项目
        var coreProject = new FileTreeNode { Name = $"{config.ProjectName}.Core/", IsDirectory = true };
        coreProject.Children.Add(new FileTreeNode { Name = "Models/" , IsDirectory = true });
        coreProject.Children.Add(new FileTreeNode { Name = "Interfaces/", IsDirectory = true });
        coreProject.Children.Add(new FileTreeNode { Name = $"{config.ProjectName}.Core.csproj" });
        root.Children.Add(coreProject);

        // README
        root.Children.Add(new FileTreeNode { Name = "README.md" });

        FileTreeRoot.Add(root);
    }

    private int EstimateFileCount()
    {
        return _config?.ProjectType switch
        {
            "Collection" => 28,
            "Vision"     => 32,
            "System"     => 35,
            _            => 20
        };
    }
}
