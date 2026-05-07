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
    private readonly AnnotationContext _ctx;
    private readonly ILogger _logger = Log.ForContext<AutoLabelingCommandHandler>();

    private bool _isAutoDetecting;
    private float _confidenceThreshold = 0.5f;
    private int _autoDetectProgress;
    private int _autoDetectTotal;

    /// <summary>
    /// 初始化自动标注命令处理器。
    /// </summary>
    public AutoLabelingCommandHandler(IAutoLabelingService autoLabelingService, AnnotationContext ctx)
    {
        _autoLabelingService = autoLabelingService;
        _ctx = ctx;

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
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "选择模型目录（包含 encoder.pt、text_encoder.pt、decoder.pt）"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            _ctx.SetStatusMessage("正在加载模型...");
            await _autoLabelingService.LoadModelAsync(dialog.SelectedPath);
            IsModelLoaded = true;
            _ctx.SetStatusMessage($"模型已加载: {Path.GetFileName(dialog.SelectedPath)}");
            _logger.Information("自动标注模型已加载: {Path}", dialog.SelectedPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载模型失败");
            _ctx.SetStatusMessage($"加载模型失败: {ex.Message}");
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
            _ctx.SetStatusMessage("模型已卸载");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "卸载模型失败");
            _ctx.SetStatusMessage($"卸载失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 判断是否可以对当前图像执行自动检测。
    /// </summary>
    private bool CanAutoDetectCurrent()
        => IsModelLoaded && _ctx.GetCurrentAnnotation() != null && _ctx.GetCurrentImage() != null && !IsAutoDetecting;

    /// <summary>
    /// 对当前图像执行自动检测，将检测到的边界框添加到标注数据。
    /// </summary>
    private async void ExecuteAutoDetectCurrent()
    {
        var currentAnnotation = _ctx.GetCurrentAnnotation();
        if (currentAnnotation == null) return;

        try
        {
            IsAutoDetecting = true;
            _ctx.SetStatusMessage("正在自动标注当前图像...");

            _ctx.PushUndoSnapshot();

            var detections = await _autoLabelingService.DetectAsync(
                currentAnnotation.ImagePath, ConfidenceThreshold);

            foreach (var box in detections)
            {
                currentAnnotation.Boxes.Add(box);
            }

            _ctx.UpdateBoxesList();
            _ctx.UpdateClassDistribution();
            _ctx.SetStatusMessage($"自动标注完成: 检测到 {detections.Count} 个目标");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "自动标注失败");
            _ctx.SetStatusMessage($"自动标注失败: {ex.Message}");
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
        => IsModelLoaded && _ctx.GetProject() is { Annotations.Count: > 0 } && !IsAutoDetecting;

    /// <summary>
    /// 对所有未标注图像执行批量自动检测。
    /// </summary>
    private async void ExecuteAutoDetectAll()
    {
        var project = _ctx.GetProject();
        if (project == null) return;

        var unannotated = project.Annotations
            .Where(a => a.Boxes.Count == 0)
            .Select(a => a.ImagePath)
            .ToList();

        if (unannotated.Count == 0)
        {
            _ctx.SetStatusMessage("所有图像均已标注，无需自动标注");
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

            _ctx.SetStatusMessage($"正在批量自动标注 {unannotated.Count} 张图像...");

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

            _ctx.UpdateBoxesList();
            _ctx.UpdateStatistics();
            _ctx.SetStatusMessage($"批量自动标注完成: {unannotated.Count} 张图像, 共 {totalDetections} 个目标");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "批量自动标注失败");
            _ctx.SetStatusMessage($"批量自动标注失败: {ex.Message}");
        }
        finally
        {
            IsAutoDetecting = false;
        }
    }
}
