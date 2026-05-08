using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScaffoldX.App.ViewModels;

namespace ScaffoldX.App.Views;

/// <summary>
/// YOLO 标注工具视图的交互逻辑，支持键盘快捷键和画布缩放/平移。
/// </summary>
public partial class AnnotationView : UserControl
{
    private TranslateTransform _translateTransform = new();
    private ScaleTransform _scaleTransform = new();
    private bool _isPanning;
    private Point _panStartPoint;

    public AnnotationView()
    {
        InitializeComponent();

        // 初始化缩放/平移变换组
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_scaleTransform);
        transformGroup.Children.Add(_translateTransform);
        AnnotationCanvas.RenderTransform = transformGroup;

        // 注册键盘和鼠标事件
        KeyDown += OnKeyDown;
        AnnotationCanvas.MouseWheel += OnMouseWheel;
        AnnotationCanvas.MouseDown += OnPanMouseDown;
        AnnotationCanvas.MouseMove += OnPanMouseMove;
        AnnotationCanvas.MouseUp += OnPanMouseUp;

        DataContextChanged += (_, _) =>
        {
            if (DataContext is AnnotationViewModel vm)
            {
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(AnnotationViewModel.CurrentAnnotation)
                        or nameof(AnnotationViewModel.CurrentImage))
                    {
                        DrawingHelper.RefreshBoxesDisplay(BoxesCanvas, AnnotationCanvas, vm);
                    }
                    else if (e.PropertyName is nameof(AnnotationViewModel.CurrentPolygonPoints))
                    {
                        DrawingHelper.UpdateDrawingPolygon(DrawingPolygon, vm);
                    }
                    else if (e.PropertyName is nameof(AnnotationViewModel.ZoomLevel))
                    {
                        UpdateZoom(vm.ZoomLevel);
                    }
                    else if (e.PropertyName is nameof(AnnotationViewModel.ObbCenter)
                        or nameof(AnnotationViewModel.ObbSize)
                        or nameof(AnnotationViewModel.ObbAngle)
                        or nameof(AnnotationViewModel.IsDrawingObb)
                        or nameof(AnnotationViewModel.IsRotatingObb))
                    {
                        DrawingHelper.UpdateDrawingObb(DrawingObbRect, vm);
                    }
                };
            }
        };
    }

    // ── 键盘快捷键 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 处理键盘快捷键：Delete/Backspace 删除、Ctrl+Z/Y 撤销重做、
    /// 方向键导航图像、1-9 选类别、Space 切换多边形、Escape 取消、B/P 切模式。
    /// </summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not AnnotationViewModel vm) return;

        // Ctrl 组合键
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.Z:
                    if (vm.UndoCommand.CanExecute())
                    {
                        vm.UndoCommand.Execute();
                        e.Handled = true;
                    }
                    return;
                case Key.Y:
                    if (vm.RedoCommand.CanExecute())
                    {
                        vm.RedoCommand.Execute();
                        e.Handled = true;
                    }
                    return;
            }
        }

        // 普通键
        switch (e.Key)
        {
            case Key.Delete:
            case Key.Back:
                if (vm.DeleteSelectedBoxCommand.CanExecute())
                {
                    vm.DeleteSelectedBoxCommand.Execute();
                    e.Handled = true;
                }
                break;

            case Key.Left:
                if (vm.PreviousImageCommand.CanExecute())
                {
                    vm.PreviousImageCommand.Execute();
                    e.Handled = true;
                }
                break;

            case Key.Right:
                if (vm.NextImageCommand.CanExecute())
                {
                    vm.NextImageCommand.Execute();
                    e.Handled = true;
                }
                break;

            case Key.Space:
                vm.TogglePolygonModeCommand.Execute();
                e.Handled = true;
                break;

            case Key.Escape:
                vm.CancelDrawingCommand.Execute();
                e.Handled = true;
                break;

            case Key.B:
                vm.SwitchToBboxModeCommand.Execute();
                e.Handled = true;
                break;

            case Key.P:
                vm.SwitchToPolygonModeCommand.Execute();
                e.Handled = true;
                break;

            case Key.O:
                vm.SwitchToObbModeCommand.Execute();
                e.Handled = true;
                break;

            case Key.D0:
            case Key.NumPad0:
                vm.SelectClassCommand.Execute(9);
                e.Handled = true;
                break;

            default:
                // 数字键 1-9 选择类别
                var digit = e.Key switch
                {
                    Key.D1 or Key.NumPad1 => 0,
                    Key.D2 or Key.NumPad2 => 1,
                    Key.D3 or Key.NumPad3 => 2,
                    Key.D4 or Key.NumPad4 => 3,
                    Key.D5 or Key.NumPad5 => 4,
                    Key.D6 or Key.NumPad6 => 5,
                    Key.D7 or Key.NumPad7 => 6,
                    Key.D8 or Key.NumPad8 => 7,
                    Key.D9 or Key.NumPad9 => 8,
                    _ => -1
                };
                if (digit >= 0)
                {
                    vm.SelectClassCommand.Execute(digit);
                    e.Handled = true;
                }
                break;
        }
    }

    // ── 缩放/平移 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 鼠标滚轮缩放：以光标位置为中心进行缩放，步长 10%。
    /// </summary>
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not AnnotationViewModel vm) return;

        var zoomFactor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
        var newZoom = vm.ZoomLevel * zoomFactor;

        newZoom = Math.Clamp(newZoom, 0.1, 10.0);

        var mousePos = e.GetPosition(AnnotationCanvas);
        var scaleChange = newZoom / vm.ZoomLevel;

        _translateTransform.X = mousePos.X - scaleChange * (mousePos.X - _translateTransform.X);
        _translateTransform.Y = mousePos.Y - scaleChange * (mousePos.Y - _translateTransform.Y);

        _scaleTransform.ScaleX = newZoom;
        _scaleTransform.ScaleY = newZoom;

        vm.ZoomLevel = newZoom;
        e.Handled = true;
    }

    /// <summary>
    /// 中键（或 Ctrl+左键）按下开始平移。
    /// </summary>
    private void OnPanMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed ||
            (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control))
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            AnnotationCanvas.CaptureMouse();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 平移拖拽中：更新 TranslateTransform。
    /// </summary>
    private void OnPanMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;

        var currentPoint = e.GetPosition(this);
        var delta = currentPoint - _panStartPoint;

        _translateTransform.X += delta.X;
        _translateTransform.Y += delta.Y;

        _panStartPoint = currentPoint;
        e.Handled = true;
    }

    /// <summary>
    /// 中键释放结束平移。
    /// </summary>
    private void OnPanMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning) return;

        _isPanning = false;
        AnnotationCanvas.ReleaseMouseCapture();
        e.Handled = true;
    }

    /// <summary>
    /// 更新缩放变换（由 ViewModel 的 ZoomLevel 属性变更触发）。
    /// </summary>
    private void UpdateZoom(double zoomLevel)
    {
        _scaleTransform.ScaleX = zoomLevel;
        _scaleTransform.ScaleY = zoomLevel;
    }

    /// <summary>
    /// 将屏幕坐标转换为画布坐标（去除缩放和平移变换）。
    /// </summary>
    public Point ScreenToCanvas(Point screenPoint)
    {
        var transform = AnnotationCanvas.RenderTransform;
        if (transform == null) return screenPoint;

        var inverse = transform.Inverse;
        return inverse != null ? inverse.Transform(screenPoint) : screenPoint;
    }

    /// <summary>
    /// 将画布坐标转换为屏幕坐标（应用缩放和平移变换）。
    /// </summary>
    public Point CanvasToScreen(Point canvasPoint)
    {
        var transform = AnnotationCanvas.RenderTransform;
        return transform?.Transform(canvasPoint) ?? canvasPoint;
    }

    // ── 鼠标事件（绘制） ────────────────────────────────────────────────────

    /// <summary>
    /// 鼠标按下事件：开始绘制边界框、多边形顶点、OBB 或 SAM3 点提示。
    /// </summary>
    private void AnnotationCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;

        if (e.MiddleButton == MouseButtonState.Pressed ||
            (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control))
            return;

        Point position = e.GetPosition(AnnotationCanvas);

        // SAM3 点提示模式
        if (viewModel.Sam3PromptMode == ScaffoldX.App.Models.Sam3PromptMode.Point)
        {
            bool isPositive = e.LeftButton == MouseButtonState.Pressed;
            var normalizedPoint = DrawingHelper.ScreenToNormalized(position, AnnotationCanvas, viewModel);
            viewModel.Sam3Handler.AddPromptPoint(
                (float)normalizedPoint.X, (float)normalizedPoint.Y, isPositive);
            e.Handled = true;
            return;
        }

        if (viewModel.IsPolygonMode)
        {
            viewModel.PolygonMouseDownCommand.Execute((Point?)position);
            DrawingHelper.UpdateDrawingPolygon(DrawingPolygon, viewModel);
        }
        else if (viewModel.IsObbMode)
        {
            viewModel.ObbMouseDownCommand.Execute((Point?)position);
            AnnotationCanvas.CaptureMouse();
        }
        else
        {
            viewModel.ImageMouseDownCommand.Execute((Point?)position);

            DrawingRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(DrawingRect, position.X);
            Canvas.SetTop(DrawingRect, position.Y);
            DrawingRect.Width = 0;
            DrawingRect.Height = 0;

            AnnotationCanvas.CaptureMouse();
        }
    }

    /// <summary>
    /// 鼠标移动事件：更新绘制中的边界框、多边形预览或 OBB 预览。
    /// </summary>
    private void AnnotationCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;

        if (viewModel.IsPolygonMode) return;

        if (viewModel.IsObbMode)
        {
            Point position = e.GetPosition(AnnotationCanvas);
            viewModel.ObbMouseMoveCommand.Execute((Point?)position);
            return;
        }

        if (!viewModel.IsDrawing) return;

        Point pos = e.GetPosition(AnnotationCanvas);
        viewModel.ImageMouseMoveCommand.Execute((Point?)pos);

        var startX = Canvas.GetLeft(DrawingRect);
        var startY = Canvas.GetTop(DrawingRect);

        var x = Math.Min(startX, pos.X);
        var y = Math.Min(startY, pos.Y);
        var width = Math.Abs(pos.X - startX);
        var height = Math.Abs(pos.Y - startY);

        Canvas.SetLeft(DrawingRect, x);
        Canvas.SetTop(DrawingRect, y);
        DrawingRect.Width = width;
        DrawingRect.Height = height;
    }

    /// <summary>
    /// 鼠标释放事件：完成边界框绘制、多边形顶点添加或 OBB 尺寸定义。
    /// </summary>
    private void AnnotationCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;

        if (viewModel.IsPolygonMode) return;

        if (viewModel.IsObbMode)
        {
            Point position = e.GetPosition(AnnotationCanvas);
            viewModel.ObbMouseUpCommand.Execute((Point?)position);
            AnnotationCanvas.ReleaseMouseCapture();
            return;
        }

        Point pos = e.GetPosition(AnnotationCanvas);
        viewModel.ImageMouseUpCommand.Execute((Point?)pos);

        DrawingRect.Visibility = Visibility.Collapsed;
        AnnotationCanvas.ReleaseMouseCapture();

        DrawingHelper.RefreshBoxesDisplay(BoxesCanvas, AnnotationCanvas, viewModel);
    }

    /// <summary>
    /// 鼠标双击事件：在多边形模式下完成多边形绘制。
    /// </summary>
    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        if (DataContext is AnnotationViewModel viewModel && viewModel.IsPolygonMode)
        {
            Point position = e.GetPosition(AnnotationCanvas);
            viewModel.PolygonDoubleClickCommand.Execute((Point?)position);

            DrawingHelper.RefreshBoxesDisplay(BoxesCanvas, AnnotationCanvas, viewModel);
            DrawingHelper.UpdateDrawingPolygon(DrawingPolygon, viewModel);
            e.Handled = true;
            return;
        }

        base.OnMouseDoubleClick(e);
    }
}
