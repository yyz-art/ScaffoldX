using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using ScaffoldX.Core.Vision;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// SAM 3 分割标注命令处理器，管理文本/点/参考图三种提示模式的交互。
/// </summary>
public class Sam3LabelingCommandHandler : BindableBase
{
    private readonly IAutoLabelingService _autoLabelingService;
    private readonly AnnotationContext _ctx;
    private readonly ILogger _logger = Log.ForContext<Sam3LabelingCommandHandler>();

    private Sam3PromptMode _currentPromptMode = Sam3PromptMode.Point;
    private string _textPromptInput = string.Empty;
    private bool _isProcessing;
    private float _confidenceThreshold = 0.5f;
    private string? _referenceImagePath;
    private byte[,]? _currentMaskPreview;
    private CancellationTokenSource? _maskPreviewCts;

    public Sam3LabelingCommandHandler(IAutoLabelingService autoLabelingService, AnnotationContext ctx)
    {
        _autoLabelingService = autoLabelingService;
        _ctx = ctx;

        SegmentByTextCommand = new DelegateCommand(ExecuteSegmentByText, CanSegmentByText);
        SegmentAllByTextCommand = new DelegateCommand(ExecuteSegmentAllByText, CanSegmentAllByText);
        EnterPointModeCommand = new DelegateCommand(ExecuteEnterPointMode);
        AcceptMaskCommand = new DelegateCommand(ExecuteAcceptMask, () => CurrentMaskPreview != null);
        ClearPointsCommand = new DelegateCommand(ExecuteClearPoints, () => PromptPoints.Count > 0);
        SelectReferenceCommand = new DelegateCommand(ExecuteSelectReference);
        SegmentByReferenceCommand = new DelegateCommand(ExecuteSegmentByReference, CanSegmentByReference);
    }

    public Sam3PromptMode CurrentPromptMode
    {
        get => _currentPromptMode;
        set => SetProperty(ref _currentPromptMode, value);
    }

    public ObservableCollection<Sam3Point> PromptPoints { get; } = new();

    public string TextPromptInput
    {
        get => _textPromptInput;
        set
        {
            if (SetProperty(ref _textPromptInput, value))
            {
                SegmentByTextCommand.RaiseCanExecuteChanged();
                SegmentAllByTextCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                SegmentByTextCommand.RaiseCanExecuteChanged();
                SegmentAllByTextCommand.RaiseCanExecuteChanged();
                SegmentByReferenceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public float ConfidenceThreshold
    {
        get => _confidenceThreshold;
        set => SetProperty(ref _confidenceThreshold, Math.Clamp(value, 0.1f, 0.95f));
    }

    public string? ReferenceImagePath
    {
        get => _referenceImagePath;
        set => SetProperty(ref _referenceImagePath, value);
    }

    public byte[,]? CurrentMaskPreview
    {
        get => _currentMaskPreview;
        set
        {
            SetProperty(ref _currentMaskPreview, value);
            RaisePropertyChanged(nameof(HasMaskPreview));
        }
    }

    public bool HasMaskPreview => CurrentMaskPreview != null;
    public bool IsSam3ModelLoaded => _autoLabelingService.IsModelLoaded && _autoLabelingService.CurrentMode == AutoLabelingMode.Segmentation;

    public DelegateCommand SegmentByTextCommand { get; }
    public DelegateCommand SegmentAllByTextCommand { get; }
    public DelegateCommand EnterPointModeCommand { get; }
    public DelegateCommand AcceptMaskCommand { get; }
    public DelegateCommand ClearPointsCommand { get; }
    public DelegateCommand SelectReferenceCommand { get; }
    public DelegateCommand SegmentByReferenceCommand { get; }

    private bool CanSegmentByText()
        => IsSam3ModelLoaded && !string.IsNullOrWhiteSpace(TextPromptInput) && !IsProcessing;

    private bool CanSegmentAllByText()
        => IsSam3ModelLoaded && !string.IsNullOrWhiteSpace(TextPromptInput) && !IsProcessing
           && _ctx.GetProject() is { Annotations.Count: > 0 };

    private bool CanSegmentByReference()
        => IsSam3ModelLoaded && !string.IsNullOrEmpty(ReferenceImagePath) && !IsProcessing;

    private async void ExecuteSegmentByText()
    {
        var annotation = _ctx.GetCurrentAnnotation();
        if (annotation == null) return;

        try
        {
            IsProcessing = true;
            _ctx.SetStatusMessage("正在使用 SAM 3 文本分割...");

            var prompts = TextPromptInput.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var results = await _autoLabelingService.SegmentByTextAsync(annotation.ImagePath, prompts, ConfidenceThreshold);

            _ctx.PushUndoSnapshot();
            foreach (var seg in results)
            {
                annotation.Segmentations.Add(seg);
            }

            _ctx.UpdateBoxesList();
            _ctx.UpdateClassDistribution();
            _ctx.SetStatusMessage($"SAM 3 文本分割完成: {results.Count} 个对象");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SAM 3 文本分割失败");
            _ctx.SetStatusMessage($"分割失败: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async void ExecuteSegmentAllByText()
    {
        var project = _ctx.GetProject();
        if (project == null) return;

        var unannotated = project.Annotations
            .Where(a => a.Segmentations.Count == 0 && a.Boxes.Count == 0)
            .Select(a => a.ImagePath)
            .ToList();

        if (unannotated.Count == 0)
        {
            _ctx.SetStatusMessage("所有图像均已标注");
            return;
        }

        try
        {
            IsProcessing = true;
            var prompts = TextPromptInput.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int totalSegs = 0;

            foreach (var annotation in project.Annotations.Where(a => unannotated.Contains(a.ImagePath)))
            {
                _ctx.SetStatusMessage($"正在分割 {Path.GetFileName(annotation.ImagePath)}...");
                var results = await _autoLabelingService.SegmentByTextAsync(annotation.ImagePath, prompts, ConfidenceThreshold);
                foreach (var seg in results)
                    annotation.Segmentations.Add(seg);
                totalSegs += results.Count;
            }

            _ctx.UpdateBoxesList();
            _ctx.UpdateStatistics();
            _ctx.SetStatusMessage($"批量分割完成: {unannotated.Count} 张图像, 共 {totalSegs} 个对象");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "批量 SAM 3 分割失败");
            _ctx.SetStatusMessage($"批量分割失败: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void ExecuteEnterPointMode()
    {
        CurrentPromptMode = Sam3PromptMode.Point;
        PromptPoints.Clear();
        CurrentMaskPreview = null;
        _ctx.SetStatusMessage("已进入点提示模式：左键=正点，右键=负点");
    }

    public void AddPromptPoint(float normalizedX, float normalizedY, bool isPositive)
    {
        PromptPoints.Add(new Sam3Point
        {
            X = normalizedX,
            Y = normalizedY,
            Label = isPositive ? 1 : 0
        });

        RaisePropertyChanged(nameof(PromptPoints));

        // Cancel previous preview update to avoid stale results
        _maskPreviewCts?.Cancel();
        _maskPreviewCts = new CancellationTokenSource();
        var cts = _maskPreviewCts;
        _ = UpdateMaskPreviewAsync(cts.Token);
    }

    private async Task UpdateMaskPreviewAsync(CancellationToken ct)
    {
        var annotation = _ctx.GetCurrentAnnotation();
        if (annotation == null || PromptPoints.Count == 0) return;

        try
        {
            var positives = PromptPoints.Where(p => p.Label == 1).Select(p => new PointF(p.X, p.Y));
            var negatives = PromptPoints.Where(p => p.Label == 0).Select(p => new PointF(p.X, p.Y));

            var result = await _autoLabelingService.SegmentByPointsAsync(annotation.ImagePath, positives, negatives, ct);
            if (!ct.IsCancellationRequested)
                CurrentMaskPreview = result.Mask;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Warning(ex, "更新掩码预览失败");
        }
    }

    private void ExecuteAcceptMask()
    {
        var annotation = _ctx.GetCurrentAnnotation();
        if (annotation == null || CurrentMaskPreview == null) return;

        _ctx.PushUndoSnapshot();

        var contour = MaskToPolygonConverter.Convert(CurrentMaskPreview, 2.0f);
        var seg = new SegmentationAnnotation
        {
            ClassName = "segment",
            Confidence = 1.0f,
            Polygon = contour,
            Mask = CurrentMaskPreview
        };

        annotation.Segmentations.Add(seg);
        CurrentMaskPreview = null;
        PromptPoints.Clear();

        _ctx.UpdateBoxesList();
        _ctx.UpdateClassDistribution();
        _ctx.SetStatusMessage("已接受分割掩码");
    }

    private void ExecuteClearPoints()
    {
        _maskPreviewCts?.Cancel();
        PromptPoints.Clear();
        CurrentMaskPreview = null;
        _ctx.SetStatusMessage("已清除提示点");
    }

    private void ExecuteSelectReference()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
            Title = "选择参考图像"
        };

        if (dialog.ShowDialog() == true)
        {
            ReferenceImagePath = dialog.FileName;
            _ctx.SetStatusMessage($"已选择参考图: {Path.GetFileName(dialog.FileName)}");
        }
    }

    private async void ExecuteSegmentByReference()
    {
        var annotation = _ctx.GetCurrentAnnotation();
        if (annotation == null || string.IsNullOrEmpty(ReferenceImagePath)) return;

        try
        {
            IsProcessing = true;
            _ctx.SetStatusMessage("正在使用参考图分割...");

            var results = await _autoLabelingService.SegmentByReferenceAsync(
                annotation.ImagePath, ReferenceImagePath, ConfidenceThreshold);

            _ctx.PushUndoSnapshot();
            foreach (var seg in results)
                annotation.Segmentations.Add(seg);

            _ctx.UpdateBoxesList();
            _ctx.UpdateClassDistribution();
            _ctx.SetStatusMessage($"参考图分割完成: {results.Count} 个对象");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "参考图分割失败");
            _ctx.SetStatusMessage($"参考图分割失败: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
