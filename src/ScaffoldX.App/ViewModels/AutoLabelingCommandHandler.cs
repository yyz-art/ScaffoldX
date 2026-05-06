using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 自动标注命令处理器，管理 ONNX 模型加载/卸载和自动检测功能。
/// </summary>
public class AutoLabelingCommandHandler : BindableBase
{
    private readonly IAutoLabelingService _autoLabelingService;
    private readonly Func<AnnotationData?> _getCurrentAnnotation;
    private readonly Func<AnnotationProject?> _getProject;
    private readonly Func<BitmapImage?> _getCurrentImage;
    private readonly Action<string> _setStatusMessage;
    private readonly Action _pushUndoSnapshot;
    private readonly Action _updateBoxesList;
    private readonly Action _updateClassDistribution;
    private readonly Action _updateStatistics;
    private readonly ILogger _logger = Log.ForContext<AutoLabelingCommandHandler>();

    private bool _isAutoDetecting;
    private float _confidenceThreshold = 0.5f;
    private int _autoDetectProgress;
    private int _autoDetectTotal;

    /// <summary>
    /// 初始化自动标注命令处理器。
    /// </summary>
    /// <param name="autoLabelingService">自动标注服务。</param>
    /// <param name="getCurrentAnnotation">获取当前标注数据的回调。</param>
    /// <param name="getProject">获取当前项目的回调。</param>
    /// <param name="getCurrentImage">获取当前图像的回调。</param>
    /// <param name="setStatusMessage">设置状态消息的回调。</param>
    /// <param name="pushUndoSnapshot">推送撤销快照的回调。</param>
    /// <param name="updateBoxesList">更新边界框列表的回调。</param>
    /// <param name="updateClassDistribution">更新类别分布的回调。</param>
    /// <param name="updateStatistics">更新统计信息的回调。</param>
    public AutoLabelingCommandHandler(
        IAutoLabelingService autoLabelingService,
        Func<AnnotationData?> getCurrentAnnotation,
        Func<AnnotationProject?> getProject,
        Func<BitmapImage?> getCurrentImage,
        Action<string> setStatusMessage,
        Action pushUndoSnapshot,
        Action updateBoxesList,
        Action updateClassDistribution,
        Action updateStatistics)
    {
        _autoLabelingService = autoLabelingService;
        _getCurrentAnnotation = getCurrentAnnotation;
        _getProject = getProject;
        _getCurrentImage = getCurrentImage;
        _setStatusMessage = setStatusMessage;
        _pushUndoSnapshot = pushUndoSnapshot;
        _updateBoxesList = updateBoxesList;
        _updateClassDistribution = updateClassDistribution;
        _updateStatistics = updateStatistics;

        LoadModelCommand = new DelegateCommand(ExecuteLoadModel);
        UnloadModelCommand = new DelegateCommand(ExecuteUnloadModel, () => IsModelLoaded);
        AutoDetectCurrentCommand = new DelegateCommand(ExecuteAutoDetectCurrent, CanAutoDetectCurrent);
        AutoDetectAllCommand = new DelegateCommand(ExecuteAutoDetectAll, CanAutoDetectAll);
    }

    /// <summary>模型是否已加载。</summary>
    public bool IsModelLoaded
    {
        get => _autoLabelingService.IsModelLoaded;
        private set
        {
            RaisePropertyChanged(nameof(IsModelLoaded));
            RaisePropertyChanged(nameof(LoadedModelName));
        }
    }

    /// <summary>已加载模型名称。</summary>
    public string LoadedModelName
    {
        get
        {
            if (!_autoLabelingService.IsModelLoaded) return "未加载模型";
            return Path.GetFileName(_autoLabelingService.LoadedModelPath ?? "未知模型");
        }
    }

    /// <summary>是否正在自动检测。</summary>
    public bool IsAutoDetecting
    {
        get => _isAutoDetecting;
        private set => SetProperty(ref _isAutoDetecting, value);
    }

    /// <summary>置信度阈值。</summary>
    public float ConfidenceThreshold
    {
        get => _confidenceThreshold;
        set => SetProperty(ref _confidenceThreshold, Math.Clamp(value, 0.1f, 0.95f));
    }

    /// <summary>自动检测进度当前值。</summary>
    public int AutoDetectProgress
    {
        get => _autoDetectProgress;
        private set => SetProperty(ref _autoDetectProgress, value);
    }

    /// <summary>自动检测进度总数。</summary>
    public int AutoDetectTotal
    {
        get => _autoDetectTotal;
        private set => SetProperty(ref _autoDetectTotal, value);
    }

    /// <summary>自动检测进度文字。</summary>
    public string AutoDetectProgressText => IsAutoDetecting
        ? $"自动标注中: {AutoDetectProgress} / {AutoDetectTotal}"
        : string.Empty;

    /// <summary>加载模型命令。</summary>
    public DelegateCommand LoadModelCommand { get; }

    /// <summary>卸载模型命令。</summary>
    public DelegateCommand UnloadModelCommand { get; }

    /// <summary>自动检测当前图像命令。</summary>
    public DelegateCommand AutoDetectCurrentCommand { get; }

    /// <summary>自动检测所有图像命令。</summary>
    public DelegateCommand AutoDetectAllCommand { get; }

    /// <summary>
    /// 打开文件对话框加载 ONNX 模型，并尝试查找同目录下的 classes.txt。
    /// </summary>
    private async void ExecuteLoadModel()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ONNX 模型|*.onnx|所有文件|*.*",
            Title = "选择 ONNX 模型文件"
        };

        if (dialog.ShowDialog() != true) return;

        var modelDir = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        var classesFile = Path.Combine(modelDir, "classes.txt");
        var classesPath = File.Exists(classesFile) ? classesFile : null;

        try
        {
            _setStatusMessage("正在加载模型...");
            await _autoLabelingService.LoadModelAsync(dialog.FileName, classesPath);
            IsModelLoaded = true;
            _setStatusMessage($"模型已加载: {Path.GetFileName(dialog.FileName)}");
            _logger.Information("自动标注模型已加载: {Path}", dialog.FileName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载模型失败");
            _setStatusMessage($"加载模型失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 卸载当前模型并释放资源。
    /// </summary>
    private void ExecuteUnloadModel()
    {
        try
        {
            _autoLabelingService.UnloadModel();
            IsModelLoaded = false;
            _setStatusMessage("模型已卸载");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "卸载模型失败");
            _setStatusMessage($"卸载失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 判断是否可以对当前图像执行自动检测。
    /// </summary>
    private bool CanAutoDetectCurrent()
        => IsModelLoaded && _getCurrentAnnotation() != null && _getCurrentImage() != null && !IsAutoDetecting;

    /// <summary>
    /// 对当前图像执行自动检测，将检测到的边界框添加到标注数据。
    /// </summary>
    private async void ExecuteAutoDetectCurrent()
    {
        var currentAnnotation = _getCurrentAnnotation();
        if (currentAnnotation == null) return;

        try
        {
            IsAutoDetecting = true;
            _setStatusMessage("正在自动标注当前图像...");

            _pushUndoSnapshot();

            var detections = await _autoLabelingService.DetectAsync(
                currentAnnotation.ImagePath, ConfidenceThreshold);

            foreach (var box in detections)
            {
                currentAnnotation.Boxes.Add(box);
            }

            _updateBoxesList();
            _updateClassDistribution();
            _setStatusMessage($"自动标注完成: 检测到 {detections.Count} 个目标");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "自动标注失败");
            _setStatusMessage($"自动标注失败: {ex.Message}");
        }
        finally
        {
            IsAutoDetecting = false;
        }
    }

    /// <summary>
    /// 判断是否可以执行批量自动检测。
    /// </summary>
    private bool CanAutoDetectAll()
        => IsModelLoaded && _getProject() is { Annotations.Count: > 0 } && !IsAutoDetecting;

    /// <summary>
    /// 对所有未标注图像执行批量自动检测。
    /// </summary>
    private async void ExecuteAutoDetectAll()
    {
        var project = _getProject();
        if (project == null) return;

        var unannotated = project.Annotations
            .Where(a => a.Boxes.Count == 0)
            .Select(a => a.ImagePath)
            .ToList();

        if (unannotated.Count == 0)
        {
            _setStatusMessage("所有图像均已标注，无需自动标注");
            return;
        }

        try
        {
            IsAutoDetecting = true;
            AutoDetectTotal = unannotated.Count;
            AutoDetectProgress = 0;

            var progress = new Progress<(int current, int total)>(p =>
            {
                AutoDetectProgress = p.current;
                RaisePropertyChanged(nameof(AutoDetectProgressText));
            });

            _setStatusMessage($"正在批量自动标注 {unannotated.Count} 张图像...");

            var results = await _autoLabelingService.DetectBatchAsync(
                unannotated, ConfidenceThreshold, progress);

            int totalDetections = 0;
            foreach (var annotation in project.Annotations)
            {
                if (results.TryGetValue(annotation.ImagePath, out var detections))
                {
                    foreach (var box in detections)
                    {
                        annotation.Boxes.Add(box);
                    }
                    totalDetections += detections.Count;
                }
            }

            _updateBoxesList();
            _updateStatistics();
            _setStatusMessage($"批量自动标注完成: {unannotated.Count} 张图像, 共 {totalDetections} 个目标");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "批量自动标注失败");
            _setStatusMessage($"批量自动标注失败: {ex.Message}");
        }
        finally
        {
            IsAutoDetecting = false;
        }
    }
}
