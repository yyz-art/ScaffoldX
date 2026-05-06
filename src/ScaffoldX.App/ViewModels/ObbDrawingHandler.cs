using System.Windows;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// OBB（旋转边界框）绘制处理器，管理 OBB 模式切换、拖拽绘制和角度设置。
/// </summary>
public class ObbDrawingHandler : BindableBase
{
    private readonly DrawingStateManager _drawingState;
    private readonly Func<AnnotationProject?> _getProject;
    private readonly Func<AnnotationData?> _getCurrentAnnotation;
    private readonly Func<BitmapImage?> _getCurrentImage;
    private readonly Func<int> _getSelectedClassIndex;
    private readonly Func<bool> _getIsPolygonMode;
    private readonly Action _disablePolygonMode;
    private readonly Action<string> _setStatusMessage;
    private readonly Action _pushUndoSnapshot;
    private readonly Action _updateBoxesList;
    private readonly Action _updateClassDistribution;

    private bool _isObbMode;

    /// <summary>
    /// 初始化 OBB 绘制处理器。
    /// </summary>
    /// <param name="drawingState">绘图状态管理器。</param>
    /// <param name="getProject">获取当前项目的回调。</param>
    /// <param name="getCurrentAnnotation">获取当前标注数据的回调。</param>
    /// <param name="getCurrentImage">获取当前图像的回调。</param>
    /// <param name="getSelectedClassIndex">获取当前选中类别索引的回调。</param>
    /// <param name="getIsPolygonMode">获取是否处于多边形模式的回调。</param>
    /// <param name="disablePolygonMode">禁用多边形模式的回调。</param>
    /// <param name="setStatusMessage">设置状态消息的回调。</param>
    /// <param name="pushUndoSnapshot">推送撤销快照的回调。</param>
    /// <param name="updateBoxesList">更新边界框列表的回调。</param>
    /// <param name="updateClassDistribution">更新类别分布的回调。</param>
    public ObbDrawingHandler(
        DrawingStateManager drawingState,
        Func<AnnotationProject?> getProject,
        Func<AnnotationData?> getCurrentAnnotation,
        Func<BitmapImage?> getCurrentImage,
        Func<int> getSelectedClassIndex,
        Func<bool> getIsPolygonMode,
        Action disablePolygonMode,
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
        _getIsPolygonMode = getIsPolygonMode;
        _disablePolygonMode = disablePolygonMode;
        _setStatusMessage = setStatusMessage;
        _pushUndoSnapshot = pushUndoSnapshot;
        _updateBoxesList = updateBoxesList;
        _updateClassDistribution = updateClassDistribution;

        ToggleObbModeCommand = new DelegateCommand(ExecuteToggleObbMode);
        FinishObbCommand = new DelegateCommand(ExecuteFinishObb, CanFinishObb);
        CancelObbCommand = new DelegateCommand(ExecuteCancelObb, () => _drawingState.IsDrawingObb || _drawingState.IsRotatingObb);

        ObbMouseDownCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteObbMouseDown(p.Value); });
        ObbMouseMoveCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteObbMouseMove(p.Value); });
        ObbMouseUpCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteObbMouseUp(p.Value); });
    }

    /// <summary>是否处于 OBB（旋转边界框）绘制模式。</summary>
    public bool IsObbMode
    {
        get => _isObbMode;
        set
        {
            if (SetProperty(ref _isObbMode, value))
            {
                if (!value) ExecuteCancelObb();
                RaisePropertyChanged(nameof(ObbModeButtonText));
            }
        }
    }

    /// <summary>OBB 模式切换按钮文字。</summary>
    public string ObbModeButtonText => IsObbMode ? "退出 OBB" : "OBB";

    /// <summary>是否正在绘制 OBB（定义尺寸阶段）。</summary>
    public bool IsDrawingObb
    {
        get => _drawingState.IsDrawingObb;
        private set
        {
            _drawingState.IsDrawingObb = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>是否正在旋转 OBB（设置角度阶段）。</summary>
    public bool IsRotatingObb
    {
        get => _drawingState.IsRotatingObb;
        private set
        {
            _drawingState.IsRotatingObb = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>OBB 中心点（屏幕坐标）。</summary>
    public Point ObbCenter
    {
        get => _drawingState.ObbCenter;
        private set
        {
            _drawingState.ObbCenter = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>OBB 尺寸（屏幕坐标）。</summary>
    public Size ObbSize
    {
        get => _drawingState.ObbSize;
        private set
        {
            _drawingState.ObbSize = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>OBB 旋转角度（弧度）。</summary>
    public double ObbAngle
    {
        get => _drawingState.ObbAngle;
        private set
        {
            _drawingState.ObbAngle = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>切换 OBB 模式命令。</summary>
    public DelegateCommand ToggleObbModeCommand { get; }

    /// <summary>完成 OBB 绘制命令。</summary>
    public DelegateCommand FinishObbCommand { get; }

    /// <summary>取消 OBB 绘制命令。</summary>
    public DelegateCommand CancelObbCommand { get; }

    /// <summary>OBB 模式鼠标按下命令。</summary>
    public DelegateCommand<Point?> ObbMouseDownCommand { get; }

    /// <summary>OBB 模式鼠标移动命令。</summary>
    public DelegateCommand<Point?> ObbMouseMoveCommand { get; }

    /// <summary>OBB 模式鼠标抬起命令。</summary>
    public DelegateCommand<Point?> ObbMouseUpCommand { get; }

    /// <summary>
    /// 切换 OBB 绘制模式。
    /// </summary>
    public void ExecuteToggleObbMode()
    {
        IsObbMode = !IsObbMode;
        if (IsObbMode && _getIsPolygonMode())
            _disablePolygonMode();
        _setStatusMessage(IsObbMode
            ? "OBB 模式：拖拽定义中心和大小，松开后移动鼠标设置角度，单击确认"
            : "边界框模式");
    }

    /// <summary>
    /// 在 OBB 模式下处理鼠标按下事件，开始绘制或确认角度。
    /// </summary>
    /// <param name="point">鼠标点击的屏幕坐标。</param>
    public void ExecuteObbMouseDown(Point point)
    {
        if (!IsObbMode || _getProject() == null || _getCurrentAnnotation() == null) return;

        if (_drawingState.IsRotatingObb)
        {
            ExecuteFinishObb();
            return;
        }

        _drawingState.IsDrawingObb = true;
        ObbCenter = point;
        ObbSize = new Size(0, 0);
        ObbAngle = 0;
        RaisePropertyChanged(nameof(IsDrawingObb));
        RaisePropertyChanged(nameof(ObbCenter));
        RaisePropertyChanged(nameof(ObbSize));
        RaisePropertyChanged(nameof(ObbAngle));
        CancelObbCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 在 OBB 模式下处理鼠标移动事件，更新尺寸或角度。
    /// </summary>
    /// <param name="point">鼠标当前位置的屏幕坐标。</param>
    public void ExecuteObbMouseMove(Point point)
    {
        if (!IsObbMode) return;

        if (_drawingState.IsDrawingObb)
        {
            var dx = Math.Abs(point.X - ObbCenter.X) * 2;
            var dy = Math.Abs(point.Y - ObbCenter.Y) * 2;
            ObbSize = new Size(Math.Max(dx, 1), Math.Max(dy, 1));
            RaisePropertyChanged(nameof(ObbSize));
        }
        else if (_drawingState.IsRotatingObb)
        {
            var dx = point.X - ObbCenter.X;
            var dy = point.Y - ObbCenter.Y;
            ObbAngle = Math.Atan2(dy, dx);
            RaisePropertyChanged(nameof(ObbAngle));
        }
    }

    /// <summary>
    /// 在 OBB 模式下处理鼠标抬起事件，完成尺寸定义并进入角度设置阶段。
    /// </summary>
    /// <param name="point">鼠标抬起的屏幕坐标。</param>
    public void ExecuteObbMouseUp(Point point)
    {
        if (!IsObbMode || !_drawingState.IsDrawingObb) return;

        if (ObbSize.Width < 5 || ObbSize.Height < 5)
        {
            ExecuteCancelObb();
            return;
        }

        _drawingState.IsDrawingObb = false;
        _drawingState.IsRotatingObb = true;
        RaisePropertyChanged(nameof(IsDrawingObb));
        RaisePropertyChanged(nameof(IsRotatingObb));
        CancelObbCommand.RaiseCanExecuteChanged();
        FinishObbCommand.RaiseCanExecuteChanged();
        _setStatusMessage("移动鼠标设置旋转角度，单击确认");
    }

    /// <summary>
    /// 判断是否可以完成 OBB 绘制（处于旋转阶段且尺寸有效）。
    /// </summary>
    private bool CanFinishObb() => _drawingState.IsRotatingObb && ObbSize.Width >= 5 && ObbSize.Height >= 5;

    /// <summary>
    /// 完成 OBB 绘制，创建旋转边界框标注并添加到当前标注数据。
    /// </summary>
    public void ExecuteFinishObb()
    {
        var currentAnnotation = _getCurrentAnnotation();
        var currentImage = _getCurrentImage();
        if (currentAnnotation == null || currentImage == null) return;
        if (!_drawingState.IsRotatingObb || ObbSize.Width < 5 || ObbSize.Height < 5) return;

        _pushUndoSnapshot();

        var project = _getProject();
        var selectedIndex = _getSelectedClassIndex();
        var selectedClass = selectedIndex < project!.Classes.Count
            ? project.Classes[selectedIndex]
            : project.Classes.FirstOrDefault();

        if (selectedClass == null) return;

        var centerX = (float)(ObbCenter.X / currentImage.PixelWidth);
        var centerY = (float)(ObbCenter.Y / currentImage.PixelHeight);
        var width = (float)(ObbSize.Width / currentImage.PixelWidth);
        var height = (float)(ObbSize.Height / currentImage.PixelHeight);
        var angle = (float)ObbAngle;

        var obb = new OrientedBoundingBoxAnnotation
        {
            ClassIndex = selectedClass.Index,
            ClassName = selectedClass.Name,
            CenterX = centerX,
            CenterY = centerY,
            Width = width,
            Height = height,
            Angle = angle
        };

        currentAnnotation.OrientedBoxes.Add(obb);

        _drawingState.IsDrawingObb = false;
        _drawingState.IsRotatingObb = false;
        ObbSize = new Size(0, 0);
        ObbAngle = 0;
        RaisePropertyChanged(nameof(IsDrawingObb));
        RaisePropertyChanged(nameof(IsRotatingObb));
        RaisePropertyChanged(nameof(ObbSize));
        RaisePropertyChanged(nameof(ObbAngle));
        CancelObbCommand.RaiseCanExecuteChanged();
        FinishObbCommand.RaiseCanExecuteChanged();
        _updateBoxesList();
        _updateClassDistribution();

        _setStatusMessage($"已添加 OBB 标注: {selectedClass.Name} (角度: {angle * 180 / Math.PI:F1}°)");
    }

    /// <summary>
    /// 取消当前 OBB 绘制，重置绘制状态。
    /// </summary>
    public void ExecuteCancelObb()
    {
        _drawingState.IsDrawingObb = false;
        _drawingState.IsRotatingObb = false;
        ObbSize = new Size(0, 0);
        ObbAngle = 0;
        RaisePropertyChanged(nameof(IsDrawingObb));
        RaisePropertyChanged(nameof(IsRotatingObb));
        RaisePropertyChanged(nameof(ObbSize));
        RaisePropertyChanged(nameof(ObbAngle));
        CancelObbCommand.RaiseCanExecuteChanged();
        FinishObbCommand.RaiseCanExecuteChanged();
        _setStatusMessage("已取消 OBB 绘制");
    }
}
