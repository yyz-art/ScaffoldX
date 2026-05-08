using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Services;
using ScaffoldX.Core.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 步骤二 ViewModel：基础信息表单，包含实时验证和字段联动逻辑。
/// 所有配置字段直接读写 ProjectConfig，无需手动字段拷贝。
/// </summary>
public class Step2ViewModel : BindableBase
{
    private readonly IValidationService _validationService;

    private string _projectNameError = string.Empty;
    private string _outputPathError = string.Empty;

    /// <summary>
    /// 初始化步骤二 ViewModel，注入验证服务。
    /// </summary>
    /// <param name="validationService">字段验证服务。</param>
    public Step2ViewModel(IValidationService validationService)
        : this(validationService, new ProjectConfig()) { }

    /// <summary>
    /// 初始化步骤二 ViewModel，注入验证服务并绑定到指定 ProjectConfig。
    /// </summary>
    /// <param name="validationService">字段验证服务。</param>
    /// <param name="config">项目配置对象。</param>
    public Step2ViewModel(IValidationService validationService, ProjectConfig config)
    {
        _validationService = validationService;
        Config = config;
        BrowseOutputPathCommand = new DelegateCommand(ExecuteBrowseOutputPath);
    }

    /// <summary>关联的项目配置对象，所有配置字段直接读写此对象。</summary>
    public ProjectConfig Config { get; set; }

    /// <summary>项目名称，变更时触发实时验证并联动命名空间前缀。</summary>
    public string ProjectName
    {
        get => Config.ProjectName;
        set
        {
            Config.ProjectName = value;
            RaisePropertyChanged();
            ValidateProjectName();
            // 自动生成命名空间前缀
            if (!string.IsNullOrWhiteSpace(value))
                NamespacePrefix = _validationService.ToPascalCase(value);
            RaisePropertyChanged(nameof(PreviewText));
            RaisePropertyChanged(nameof(HasErrors));
        }
    }

    /// <summary>输出路径，变更时触发实时验证。</summary>
    public string OutputPath
    {
        get => Config.OutputDirectory;
        set
        {
            Config.OutputDirectory = value;
            RaisePropertyChanged();
            ValidateOutputPath();
            RaisePropertyChanged(nameof(HasErrors));
        }
    }

    /// <summary>命名空间前缀，默认由项目名称自动生成。</summary>
    public string NamespacePrefix
    {
        get => Config.NamespacePrefix;
        set
        {
            Config.NamespacePrefix = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(PreviewText));
        }
    }

    /// <summary>目标 UI 框架："WPF" 或 "Avalonia"。</summary>
    public string UIFramework
    {
        get => Config.UIFramework;
        set
        {
            Config.UIFramework = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(PreviewText));
        }
    }

    /// <summary>.NET 目标版本：".NET 8" 或 ".NET 10"。同时更新 TargetFramework。</summary>
    public string DotNetVersion
    {
        get
        {
            if (Config.TargetFramework.Contains("net10")) return ".NET 10";
            return ".NET 8";
        }
        set
        {
            Config.TargetFramework = value switch
            {
                ".NET 10" => UIFramework == "WPF" ? "net10.0-windows" : "net10.0",
                _ => UIFramework == "WPF" ? "net8.0-windows" : "net8.0"
            };
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(PreviewText));
        }
    }

    /// <summary>项目描述文字。</summary>
    public string ProjectDescription
    {
        get => Config.Description;
        set
        {
            Config.Description = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>项目名称验证错误信息，为空表示验证通过。</summary>
    public string ProjectNameError
    {
        get => _projectNameError;
        private set => SetProperty(ref _projectNameError, value);
    }

    /// <summary>输出路径验证错误信息，为空表示验证通过。</summary>
    public string OutputPathError
    {
        get => _outputPathError;
        private set => SetProperty(ref _outputPathError, value);
    }

    /// <summary>是否存在验证错误，控制下一步按钮可用性。</summary>
    public bool HasErrors =>
        !string.IsNullOrEmpty(ProjectNameError) ||
        !string.IsNullOrEmpty(OutputPathError) ||
        string.IsNullOrWhiteSpace(ProjectName) ||
        string.IsNullOrWhiteSpace(OutputPath);

    /// <summary>底部预览文字，显示将生成的解决方案文件名和命名空间。</summary>
    public string PreviewText
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(ProjectName) ? "{项目名称}" : ProjectName;
            var ns = string.IsNullOrWhiteSpace(NamespacePrefix) ? "{命名空间}" : NamespacePrefix;
            var fw = UIFramework;
            var ver = DotNetVersion;
            return $"将生成 {name}.sln，命名空间 {ns}.*，框架 {fw} / {ver}";
        }
    }

    /// <summary>浏览输出路径命令（调用 Ookii 文件夹对话框）。</summary>
    public DelegateCommand BrowseOutputPathCommand { get; }

    /// <summary>重置所有字段（新建项目时调用）。</summary>
    public void Reset()
    {
        ProjectName = string.Empty;
        OutputPath = string.Empty;
        NamespacePrefix = string.Empty;
        UIFramework = "WPF";
        DotNetVersion = ".NET 8";
        ProjectDescription = string.Empty;
        ProjectNameError = string.Empty;
        OutputPathError = string.Empty;
    }

    private void ValidateProjectName()
    {
        var result = _validationService.ValidateProjectName(ProjectName);
        ProjectNameError = result.IsValid ? string.Empty : result.ErrorMessage;
    }

    private void ValidateOutputPath()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            OutputPathError = "输出路径不能为空";
            return;
        }
        var result = _validationService.ValidateOutputPath(OutputPath, ProjectName);
        OutputPathError = result.IsValid ? string.Empty : result.ErrorMessage;
    }

    private void ExecuteBrowseOutputPath()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "选择项目输出目录",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog() == true)
            OutputPath = dialog.SelectedPath;
    }
}
