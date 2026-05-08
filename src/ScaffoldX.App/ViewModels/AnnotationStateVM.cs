using System.Collections.ObjectModel;
using System.Windows;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 标注状态子 ViewModel，管理当前项目、标注数据、统计信息和辅助方法。
/// 从 AnnotationViewModel 提取，降低 God object 复杂度。
/// </summary>
public class AnnotationStateVM : BindableBase
{
    private readonly DrawingStateManager _drawingState;

    private AnnotationProject? _project;
    private AnnotationData? _currentAnnotation;
    private bool _isDrawing;
    private BoundingBoxAnnotation? _selectedBox;
    private string _statusMessage = "就绪";
    private string _projectName = string.Empty;
    private int _totalImages;
    private int _annotatedImages;
    private int _currentBoxCount;

    private int _totalBoxCount;
    private int _totalPolygonCount;
    private int _totalObbCount;
    private int _totalPolylineCount;
    private int _totalCircleCount;
    private int _totalAnnotationCount;
    private string _classDistributionText = string.Empty;
    private int _annotatedImageCount;

    /// <summary>
    /// 初始化标注状态子 ViewModel。
    /// </summary>
    /// <param name="drawingState">共享绘图状态管理器。</param>
    public AnnotationStateVM(DrawingStateManager drawingState)
    {
        _drawingState = drawingState;
    }

    // ── 项目与标注数据 ──────────────────────────────────────────────────────

    /// <summary>当前标注项目。</summary>
    public AnnotationProject? Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    /// <summary>当前图像的标注数据。</summary>
    public AnnotationData? CurrentAnnotation
    {
        get => _currentAnnotation;
        set
        {
            if (SetProperty(ref _currentAnnotation, value))
            {
                RaisePropertyChanged(nameof(CurrentBoxes));
                RaisePropertyChanged(nameof(HasBoxes));
            }
        }
    }

    // ── 绘图状态 ──────────────────────────────────────────────────────────────

    /// <summary>是否正在绘制边界框。</summary>
    public bool IsDrawing
    {
        get => _isDrawing;
        set => SetProperty(ref _isDrawing, value);
    }

    /// <summary>绘制起点。</summary>
    public Point DrawStartPoint
    {
        get => _drawingState.DrawStartPoint;
        set
        {
            _drawingState.DrawStartPoint = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>绘制终点。</summary>
    public Point DrawEndPoint
    {
        get => _drawingState.DrawEndPoint;
        set
        {
            _drawingState.DrawEndPoint = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>当前选中的边界框。</summary>
    public BoundingBoxAnnotation? SelectedBox
    {
        get => _selectedBox;
        set => SetProperty(ref _selectedBox, value);
    }

    // ── 状态消息与项目信息 ────────────────────────────────────────────────────

    /// <summary>状态消息。</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>项目名称。</summary>
    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    /// <summary>总图像数。</summary>
    public int TotalImages
    {
        get => _totalImages;
        set => SetProperty(ref _totalImages, value);
    }

    /// <summary>已标注图像数。</summary>
    public int AnnotatedImages
    {
        get => _annotatedImages;
        set => SetProperty(ref _annotatedImages, value);
    }

    /// <summary>当前图像的边界框数量。</summary>
    public int CurrentBoxCount
    {
        get => _currentBoxCount;
        set => SetProperty(ref _currentBoxCount, value);
    }

    /// <summary>当前图像的边界框集合（用于绑定）。</summary>
    public ObservableCollection<BoundingBoxAnnotation> CurrentBoxes { get; } = new();

    /// <summary>当前图像的所有标注（边界框 + 多边形 + OBB）统一集合，用于 ListBox 绑定。</summary>
    public ObservableCollection<object> AllAnnotations { get; } = new();

    /// <summary>是否有边界框。</summary>
    public bool HasBoxes => CurrentBoxes.Count > 0;

    /// <summary>标注进度文字。</summary>
    public string AnnotationProgressText => Project == null
        ? string.Empty
        : $"已标注: {AnnotatedImages} / {TotalImages}";

    // ── 标注统计属性 ────────────────────────────────────────────────────────

    /// <summary>当前图像的边界框数量。</summary>
    public int TotalBoxCount
    {
        get => _totalBoxCount;
        set => SetProperty(ref _totalBoxCount, value);
    }

    /// <summary>当前图像的多边形数量。</summary>
    public int TotalPolygonCount
    {
        get => _totalPolygonCount;
        set => SetProperty(ref _totalPolygonCount, value);
    }

    /// <summary>当前图像的 OBB 数量。</summary>
    public int TotalObbCount
    {
        get => _totalObbCount;
        set => SetProperty(ref _totalObbCount, value);
    }

    /// <summary>当前图像的折线数量。</summary>
    public int TotalPolylineCount
    {
        get => _totalPolylineCount;
        set => SetProperty(ref _totalPolylineCount, value);
    }

    /// <summary>当前图像的圆形数量。</summary>
    public int TotalCircleCount
    {
        get => _totalCircleCount;
        set => SetProperty(ref _totalCircleCount, value);
    }

    /// <summary>当前图像的标注总数。</summary>
    public int TotalAnnotationCount
    {
        get => _totalAnnotationCount;
        set => SetProperty(ref _totalAnnotationCount, value);
    }

    /// <summary>项目中各类别的标注数量汇总文本。</summary>
    public string ClassDistributionText
    {
        get => _classDistributionText;
        set => SetProperty(ref _classDistributionText, value);
    }

    /// <summary>项目中已标注图像数量。</summary>
    public int AnnotatedImageCount
    {
        get => _annotatedImageCount;
        set => SetProperty(ref _annotatedImageCount, value);
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 刷新标注框列表和统一标注集合。
    /// </summary>
    public void UpdateBoxesList()
    {
        CurrentBoxes.Clear();
        AllAnnotations.Clear();

        if (CurrentAnnotation == null)
        {
            CurrentBoxCount = 0;
            UpdateAnnotationStatistics();
            RaisePropertyChanged(nameof(HasBoxes));
            return;
        }

        foreach (var box in CurrentAnnotation.Boxes)
        {
            CurrentBoxes.Add(box);
            AllAnnotations.Add(box);
        }

        foreach (var polygon in CurrentAnnotation.Polygons)
            AllAnnotations.Add(polygon);

        foreach (var obb in CurrentAnnotation.OrientedBoxes)
            AllAnnotations.Add(obb);

        foreach (var seg in CurrentAnnotation.Segmentations)
            AllAnnotations.Add(seg);

        CurrentBoxCount = CurrentBoxes.Count;
        RaisePropertyChanged(nameof(HasBoxes));
        UpdateAnnotationStatistics();
    }

    /// <summary>
    /// 刷新项目级统计信息。
    /// </summary>
    public void UpdateStatistics()
    {
        if (Project == null) return;

        TotalImages = Project.Annotations.Count;
        AnnotatedImages = Project.Annotations.Count(a =>
            a.Boxes.Count > 0 || a.Polygons.Count > 0 || a.OrientedBoxes.Count > 0 || a.Polylines.Count > 0 || a.Circles.Count > 0 || a.Segmentations.Count > 0);
        AnnotatedImageCount = AnnotatedImages;
        ProjectName = Project.ProjectName;

        RaisePropertyChanged(nameof(AnnotationProgressText));
        UpdateClassDistribution();
    }

    /// <summary>
    /// 刷新当前图像的标注类型统计。
    /// </summary>
    public void UpdateAnnotationStatistics()
    {
        if (CurrentAnnotation == null)
        {
            TotalBoxCount = 0;
            TotalPolygonCount = 0;
            TotalObbCount = 0;
            TotalPolylineCount = 0;
            TotalCircleCount = 0;
            TotalAnnotationCount = 0;
            return;
        }

        TotalBoxCount = CurrentAnnotation.Boxes.Count;
        TotalPolygonCount = CurrentAnnotation.Polygons.Count;
        TotalObbCount = CurrentAnnotation.OrientedBoxes.Count;
        TotalPolylineCount = CurrentAnnotation.Polylines.Count;
        TotalCircleCount = CurrentAnnotation.Circles.Count;
        TotalAnnotationCount = TotalBoxCount + TotalPolygonCount + TotalObbCount + TotalPolylineCount + TotalCircleCount + CurrentAnnotation.Segmentations.Count;
    }

    /// <summary>
    /// 刷新类别分布文本。
    /// </summary>
    public void UpdateClassDistribution()
    {
        if (Project == null)
        {
            ClassDistributionText = string.Empty;
            return;
        }

        var distribution = new Dictionary<string, int>();

        foreach (var annotation in Project.Annotations)
        {
            foreach (var box in annotation.Boxes)
                distribution[box.ClassName] = distribution.GetValueOrDefault(box.ClassName) + 1;

            foreach (var polygon in annotation.Polygons)
                distribution[polygon.ClassName] = distribution.GetValueOrDefault(polygon.ClassName) + 1;

            foreach (var obb in annotation.OrientedBoxes)
                distribution[obb.ClassName] = distribution.GetValueOrDefault(obb.ClassName) + 1;

            foreach (var seg in annotation.Segmentations)
                distribution[seg.ClassName] = distribution.GetValueOrDefault(seg.ClassName) + 1;
        }

        ClassDistributionText = distribution.Count == 0
            ? "暂无标注数据"
            : string.Join(", ", distribution.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}"));
    }
}
