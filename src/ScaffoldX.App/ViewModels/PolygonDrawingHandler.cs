using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 多边形绘制处理器，管理多边形模式切换、顶点添加和多边形标注完成。
/// </summary>
public class PolygonDrawingHandler : BindableBase
{
    private readonly DrawingStateManager _drawingState;
    private readonly Func<AnnotationProject?> _getProject;
    private readonly Func<AnnotationData?> _getCurrentAnnotation;
    private readonly Func<BitmapImage?> _getCurrentImage;
    private readonly Func<int> _getSelectedClassIndex;
    private readonly Func<bool> _getIsObbMode;
    private readonly Action _disableObbMode;
    private readonly Action<string> _setStatusMessage;
    private readonly Action _pushUndoSnapshot;
    private readonly Action _updateBoxesList;
    private readonly Action _updateClassDistribution;

    private bool _isPolygonMode;

    /// <summary>
    /// 初始化多边形绘制处理器。
    /// </summary>
    /// <param name="drawingState">绘图状态管理器。</param>
    /// <param name="getProject">获取当前项目的回调。</param>
    /// <param name="getCurrentAnnotation">获取当前标注数据的回调。</param>
    /// <param name="getCurrentImage">获取当前图像的回调。</param>
    /// <param name="getSelectedClassIndex">获取当前选中类别索引的回调。</param>
    /// <param name="getIsObbMode">获取是否处于 OBB 模式的回调。</param>
    /// <param name="disableObbMode">禁用 OBB 模式的回调。</param>
    /// <param name="setStatusMessage">设置状态消息的回调。</param>
    /// <param name="pushUndoSnapshot">推送撤销快照的回调。</param>
    /// <param name="updateBoxesList">更新边界框列表的回调。</param>
    /// <param name="updateClassDistribution">更新类别分布的回调。</param>
    public PolygonDrawingHandler(
        DrawingStateManager drawingState,
        Func<AnnotationProject?> getProject,
        Func<AnnotationData?> getCurrentAnnotation,
        Func<BitmapImage?> getCurrentImage,
        Func<int> getSelectedClassIndex,
        Func<bool> getIsObbMode,
        Action disableObbMode,
        Action<string> setStatusMessage,
        Action pushUndoSnapshot,
        Action updateBoxesList,
        Action updateClassDistribution)
    {
        _drawingState = drawingState;
        _getProject = getProject;
        _getCurrentAnnotation = getCurrentAnnotation;
        _getCurrentImage = getCurrentImage;
        _getSelectedClassIndex = getSelectedClassIndex;
        _getIsObbMode = getIsObbMode;
        _disableObbMode = disableObbMode;
        _setStatusMessage = setStatusMessage;
        _pushUndoSnapshot = pushUndoSnapshot;
        _updateBoxesList = updateBoxesList;
        _updateClassDistribution = updateClassDistribution;

        TogglePolygonModeCommand = new DelegateCommand(ExecuteTogglePolygonMode);
        FinishPolygonCommand = new DelegateCommand(ExecuteFinishPolygon, CanFinishPolygon);
        CancelPolygonCommand = new DelegateCommand(ExecuteCancelPolygon, () => _drawingState.IsDrawingPolygon);
        PolygonMouseDownCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecutePolygonMouseDown(p.Value); });
        PolygonDoubleClickCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecutePolygonDoubleClick(p.Value); });
    }

    /// <summary>是否处于多边形绘制模式。</summary>
    public bool IsPolygonMode
    {
        get => _isPolygonMode;
        set
        {
            if (SetProperty(ref _isPolygonMode, value))
            {
                if (!value) ExecuteCancelPolygon();
                RaisePropertyChanged(nameof(PolygonModeButtonText));
            }
        }
    }

    /// <summary>多边形模式切换按钮文字。</summary>
    public string PolygonModeButtonText => IsPolygonMode ? "退出多边形" : "多边形";

    /// <summary>当前正在绘制的多边形顶点集合（屏幕坐标）。</summary>
    public ObservableCollection<Point> CurrentPolygonPoints { get; } = new();

    /// <summary>切换多边形模式命令。</summary>
    public DelegateCommand TogglePolygonModeCommand { get; }

    /// <summary>完成多边形绘制命令。</summary>
    public DelegateCommand FinishPolygonCommand { get; }

    /// <summary>取消多边形绘制命令。</summary>
    public DelegateCommand CancelPolygonCommand { get; }

    /// <summary>多边形模式鼠标按下命令。</summary>
    public DelegateCommand<Point?> PolygonMouseDownCommand { get; }

    /// <summary>多边形模式双击命令。</summary>
    public DelegateCommand<Point?> PolygonDoubleClickCommand { get; }

    /// <summary>
    /// 切换多边形绘制模式。
    /// </summary>
    public void ExecuteTogglePolygonMode()
    {
        IsPolygonMode = !IsPolygonMode;
        if (IsPolygonMode && _getIsObbMode())
            _disableObbMode();
        _setStatusMessage(IsPolygonMode ? "多边形模式：单击添加顶点，双击完成" : "边界框模式");
    }

    /// <summary>
    /// 在多边形模式下处理鼠标按下事件，添加顶点。
    /// </summary>
    /// <param name="point">鼠标点击的屏幕坐标。</param>
    public void ExecutePolygonMouseDown(Point point)
    {
        if (!IsPolygonMode || _getProject() == null || _getCurrentAnnotation() == null) return;

        _drawingState.IsDrawingPolygon = true;
        CurrentPolygonPoints.Add(point);
        RaisePropertyChanged(nameof(CurrentPolygonPoints));
        CancelPolygonCommand.RaiseCanExecuteChanged();
        FinishPolygonCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 在多边形模式下处理双击事件，完成多边形绘制。
    /// </summary>
    /// <param name="point">双击的屏幕坐标。</param>
    public void ExecutePolygonDoubleClick(Point point)
    {
        if (!IsPolygonMode || !_drawingState.IsDrawingPolygon) return;

        if (CurrentPolygonPoints.Count > 0 && CurrentPolygonPoints.Last() != point)
            CurrentPolygonPoints.Add(point);

        ExecuteFinishPolygon();
    }

    /// <summary>
    /// 判断是否可以完成多边形绘制（至少 3 个顶点）。
    /// </summary>
    private bool CanFinishPolygon() => _drawingState.IsDrawingPolygon && CurrentPolygonPoints.Count >= 3;

    /// <summary>
    /// 完成多边形绘制，创建多边形标注并添加到当前标注数据。
    /// </summary>
    public void ExecuteFinishPolygon()
    {
        var currentAnnotation = _getCurrentAnnotation();
        var currentImage = _getCurrentImage();
        if (currentAnnotation == null || currentImage == null || CurrentPolygonPoints.Count < 3)
            return;

        _pushUndoSnapshot();

        var project = _getProject();
        var selectedIndex = _getSelectedClassIndex();
        var selectedClass = selectedIndex < project!.Classes.Count
            ? project.Classes[selectedIndex]
            : project.Classes.FirstOrDefault();

        if (selectedClass == null) return;

        var normalizedPoints = CurrentPolygonPoints
            .Select(p => new System.Drawing.PointF(
                (float)(p.X / currentImage.PixelWidth),
                (float)(p.Y / currentImage.PixelHeight)))
            .ToList();

        var polygon = new PolygonAnnotation
        {
            ClassIndex = selectedClass.Index,
            ClassName = selectedClass.Name,
            Points = normalizedPoints
        };

        currentAnnotation.Polygons.Add(polygon);
        _drawingState.IsDrawingPolygon = false;
        CurrentPolygonPoints.Clear();

        RaisePropertyChanged(nameof(CurrentPolygonPoints));
        _updateBoxesList();
        _updateClassDistribution();
        _setStatusMessage($"已添加多边形标注: {selectedClass.Name} ({normalizedPoints.Count} 个顶点)");
    }

    /// <summary>
    /// 取消当前多边形绘制，清空顶点。
    /// </summary>
    public void ExecuteCancelPolygon()
    {
        _drawingState.IsDrawingPolygon = false;
        CurrentPolygonPoints.Clear();
        RaisePropertyChanged(nameof(CurrentPolygonPoints));
        CancelPolygonCommand.RaiseCanExecuteChanged();
        FinishPolygonCommand.RaiseCanExecuteChanged();
        _setStatusMessage("已取消多边形绘制");
    }
}
