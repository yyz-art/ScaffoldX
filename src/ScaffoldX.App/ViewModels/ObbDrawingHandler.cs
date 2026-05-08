using System.Windows;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// OBB（旋转边界框）绘制处理器，管理 OBB 模式切换、拖拽绘制和角度设置。
/// </summary>
public class ObbDrawingHandler : BindableBase
{
    private readonly AnnotationContext _ctx;

    private bool _isObbMode;

    /// <summary>
    /// 初始化 OBB 绘制处理器。
    /// </summary>
    public ObbDrawingHandler(AnnotationContext ctx)
    {
        _ctx = ctx;

        ToggleObbModeCommand = new DelegateCommand(ExecuteToggleObbMode);
        FinishObbCommand = new DelegateCommand(ExecuteFinishObb, CanFinishObb);
        CancelObbCommand = new DelegateCommand(ExecuteCancelObb, () => _ctx.DrawingState.IsDrawingObb || _ctx.DrawingState.IsRotatingObb);

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
        get => _ctx.DrawingState.IsDrawingObb;
        private set
        {
            _ctx.DrawingState.IsDrawingObb = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>是否正在旋转 OBB（设置角度阶段）。</summary>
    public bool IsRotatingObb
    {
        get => _ctx.DrawingState.IsRotatingObb;
        private set
        {
            _ctx.DrawingState.IsRotatingObb = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>OBB 中心点（屏幕坐标）。</summary>
    public Point ObbCenter
    {
        get => _ctx.DrawingState.ObbCenter;
        private set
        {
            _ctx.DrawingState.ObbCenter = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>OBB 尺寸（屏幕坐标）。</summary>
    public Size ObbSize
    {
        get => _ctx.DrawingState.ObbSize;
        private set
        {
            _ctx.DrawingState.ObbSize = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>OBB 旋转角度（弧度）。</summary>
    public double ObbAngle
    {
        get => _ctx.DrawingState.ObbAngle;
        private set
        {
            _ctx.DrawingState.ObbAngle = value;
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
        if (IsObbMode && _ctx.GetIsPolygonMode())
            _ctx.DisablePolygonMode();
        _ctx.SetStatusMessage(IsObbMode
            ? "OBB 模式：拖拽定义中心和大小，松开后移动鼠标设置角度，单击确认"
            : "边界框模式");
    }

    /// <summary>
    /// 在 OBB 模式下处理鼠标按下事件，开始绘制或确认角度。
    /// </summary>
    /// <param name="point">鼠标点击的屏幕坐标。</param>
    public void ExecuteObbMouseDown(Point point)
    {
        if (!IsObbMode || _ctx.GetProject() == null || _ctx.GetCurrentAnnotation() == null) return;

        if (_ctx.DrawingState.IsRotatingObb)
        {
            ExecuteFinishObb();
            return;
        }

        _ctx.DrawingState.IsDrawingObb = true;
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

        if (_ctx.DrawingState.IsDrawingObb)
        {
            var dx = Math.Abs(point.X - ObbCenter.X) * 2;
            var dy = Math.Abs(point.Y - ObbCenter.Y) * 2;
            ObbSize = new Size(Math.Max(dx, 1), Math.Max(dy, 1));
            RaisePropertyChanged(nameof(ObbSize));
        }
        else if (_ctx.DrawingState.IsRotatingObb)
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
        if (!IsObbMode || !_ctx.DrawingState.IsDrawingObb) return;

        if (ObbSize.Width < CoordinateMapper.MinBoxSize || ObbSize.Height < CoordinateMapper.MinBoxSize)
        {
            ExecuteCancelObb();
            return;
        }

        _ctx.DrawingState.IsDrawingObb = false;
        _ctx.DrawingState.IsRotatingObb = true;
        RaisePropertyChanged(nameof(IsDrawingObb));
        RaisePropertyChanged(nameof(IsRotatingObb));
        CancelObbCommand.RaiseCanExecuteChanged();
        FinishObbCommand.RaiseCanExecuteChanged();
        _ctx.SetStatusMessage("移动鼠标设置旋转角度，单击确认");
    }

    /// <summary>
    /// 判断是否可以完成 OBB 绘制（处于旋转阶段且尺寸有效）。
    /// </summary>
    private bool CanFinishObb() => _ctx.DrawingState.IsRotatingObb && ObbSize.Width >= CoordinateMapper.MinBoxSize && ObbSize.Height >= CoordinateMapper.MinBoxSize;

    /// <summary>
    /// 完成 OBB 绘制，创建旋转边界框标注并添加到当前标注数据。
    /// </summary>
    public void ExecuteFinishObb()
    {
        var currentAnnotation = _ctx.GetCurrentAnnotation();
        var currentImage = _ctx.GetCurrentImage();
        if (currentAnnotation == null || currentImage == null) return;
        if (!_ctx.DrawingState.IsRotatingObb || ObbSize.Width < CoordinateMapper.MinBoxSize || ObbSize.Height < CoordinateMapper.MinBoxSize) return;

        _ctx.PushUndoSnapshot();

        var project = _ctx.GetProject();
        var selectedIndex = _ctx.GetSelectedClassIndex();
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

        _ctx.DrawingState.IsDrawingObb = false;
        _ctx.DrawingState.IsRotatingObb = false;
        ObbSize = new Size(0, 0);
        ObbAngle = 0;
        RaisePropertyChanged(nameof(IsDrawingObb));
        RaisePropertyChanged(nameof(IsRotatingObb));
        RaisePropertyChanged(nameof(ObbSize));
        RaisePropertyChanged(nameof(ObbAngle));
        CancelObbCommand.RaiseCanExecuteChanged();
        FinishObbCommand.RaiseCanExecuteChanged();
        _ctx.UpdateBoxesList();
        _ctx.UpdateClassDistribution();

        _ctx.SetStatusMessage($"已添加 OBB 标注: {selectedClass.Name} (角度: {angle * 180 / Math.PI:F1}°)");
    }

    /// <summary>
    /// 取消当前 OBB 绘制，重置绘制状态。
    /// </summary>
    public void ExecuteCancelObb()
    {
        _ctx.DrawingState.IsDrawingObb = false;
        _ctx.DrawingState.IsRotatingObb = false;
        ObbSize = new Size(0, 0);
        ObbAngle = 0;
        RaisePropertyChanged(nameof(IsDrawingObb));
        RaisePropertyChanged(nameof(IsRotatingObb));
        RaisePropertyChanged(nameof(ObbSize));
        RaisePropertyChanged(nameof(ObbAngle));
        CancelObbCommand.RaiseCanExecuteChanged();
        FinishObbCommand.RaiseCanExecuteChanged();
        _ctx.SetStatusMessage("已取消 OBB 绘制");
    }
}
