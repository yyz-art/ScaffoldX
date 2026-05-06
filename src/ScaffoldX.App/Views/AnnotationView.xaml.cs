using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
                        RefreshBoxesDisplay(vm);
                        RefreshPolygonDisplay(vm);
                    }
                    else if (e.PropertyName is nameof(AnnotationViewModel.CurrentPolygonPoints))
                    {
                        UpdateDrawingPolygon(vm);
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
                        UpdateDrawingObb(vm);
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

        // 限制缩放范围
        newZoom = Math.Clamp(newZoom, 0.1, 10.0);

        // 以鼠标位置为中心缩放
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
        // 中键平移或 Ctrl+左键平移
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
    /// 用于在缩放/平移状态下正确计算标注坐标。
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
    /// 鼠标按下事件：开始绘制边界框、多边形顶点或 OBB。
    /// 中键/Ctrl+左键平移时跳过绘制逻辑。
    /// </summary>
    private void AnnotationCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;

        // 中键平移或 Ctrl+左键平移时不处理绘制
        if (e.MiddleButton == MouseButtonState.Pressed ||
            (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control))
            return;

        Point position = e.GetPosition(AnnotationCanvas);

        if (viewModel.IsPolygonMode)
        {
            viewModel.PolygonMouseDownCommand.Execute((Point?)position);
            UpdateDrawingPolygon(viewModel);
        }
        else if (viewModel.IsObbMode)
        {
            viewModel.ObbMouseDownCommand.Execute((Point?)position);
            AnnotationCanvas.CaptureMouse();
        }
        else
        {
            viewModel.ImageMouseDownCommand.Execute((Point?)position);

            // 显示绘制矩形
            DrawingRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(DrawingRect, position.X);
            Canvas.SetTop(DrawingRect, position.Y);
            DrawingRect.Width = 0;
            DrawingRect.Height = 0;

            // 捕获鼠标
            AnnotationCanvas.CaptureMouse();
        }
    }

    /// <summary>
    /// 鼠标移动事件：更新绘制中的边界框、多边形预览或 OBB 预览。
    /// </summary>
    private void AnnotationCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;

        // 多边形模式下不需要处理鼠标移动
        if (viewModel.IsPolygonMode) return;

        // OBB 模式下更新 OBB 预览
        if (viewModel.IsObbMode)
        {
            Point position = e.GetPosition(AnnotationCanvas);
            viewModel.ObbMouseMoveCommand.Execute((Point?)position);
            return;
        }

        if (!viewModel.IsDrawing) return;

        Point pos = e.GetPosition(AnnotationCanvas);
        viewModel.ImageMouseMoveCommand.Execute((Point?)pos);

        // 更新绘制矩形
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

        if (viewModel.IsPolygonMode)
        {
            // 多边形模式下，鼠标释放不做额外处理（MouseDown 已处理添加顶点）
            return;
        }

        if (viewModel.IsObbMode)
        {
            Point position = e.GetPosition(AnnotationCanvas);
            viewModel.ObbMouseUpCommand.Execute((Point?)position);
            AnnotationCanvas.ReleaseMouseCapture();
            return;
        }

        Point pos = e.GetPosition(AnnotationCanvas);
        viewModel.ImageMouseUpCommand.Execute((Point?)pos);

        // 隐藏绘制矩形
        DrawingRect.Visibility = Visibility.Collapsed;

        // 释放鼠标捕获
        AnnotationCanvas.ReleaseMouseCapture();

        // 刷新边界框显示
        RefreshBoxesDisplay(viewModel);
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

            // 刷新显示
            RefreshBoxesDisplay(viewModel);
            UpdateDrawingPolygon(viewModel);
            e.Handled = true;
            return;
        }

        base.OnMouseDoubleClick(e);
    }

    // ── 显示刷新 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 刷新边界框和多边形显示，使用每个类别对应的颜色。
    /// </summary>
    private void RefreshBoxesDisplay(AnnotationViewModel viewModel)
    {
        BoxesCanvas.Children.Clear();

        if (viewModel.CurrentAnnotation == null || viewModel.CurrentImage == null)
            return;

        var imageWidth = viewModel.CurrentImage.PixelWidth;
        var imageHeight = viewModel.CurrentImage.PixelHeight;

        // 构建类别名称到颜色的映射
        var classColorMap = new Dictionary<string, Color>();
        foreach (var cls in viewModel.Classes)
        {
            if (TryParseColor(cls.Color, out var color))
            {
                classColorMap[cls.Name] = color;
            }
        }

        foreach (var box in viewModel.CurrentAnnotation.Boxes)
        {
            var canvasWidth = AnnotationCanvas.ActualWidth;
            var canvasHeight = AnnotationCanvas.ActualHeight;

            var imageAspect = (double)imageWidth / imageHeight;
            var canvasAspect = canvasWidth / canvasHeight;

            double displayWidth, displayHeight, offsetX, offsetY;

            if (imageAspect > canvasAspect)
            {
                displayWidth = canvasWidth;
                displayHeight = canvasWidth / imageAspect;
                offsetX = 0;
                offsetY = (canvasHeight - displayHeight) / 2;
            }
            else
            {
                displayHeight = canvasHeight;
                displayWidth = canvasHeight * imageAspect;
                offsetX = (canvasWidth - displayWidth) / 2;
                offsetY = 0;
            }

            var boxX = offsetX + (box.CenterX - box.Width / 2) * displayWidth;
            var boxY = offsetY + (box.CenterY - box.Height / 2) * displayHeight;
            var boxWidth = box.Width * displayWidth;
            var boxHeight = box.Height * displayHeight;

            // 获取该类别对应的颜色，没有则用红色
            var boxColor = classColorMap.TryGetValue(box.ClassName, out var c) ? c : Color.FromRgb(255, 0, 0);

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = boxWidth,
                Height = boxHeight,
                Stroke = new SolidColorBrush(boxColor),
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(rect, boxX);
            Canvas.SetTop(rect, boxY);
            BoxesCanvas.Children.Add(rect);

            // 类别标签
            var labelBg = Color.FromArgb(180, boxColor.R, boxColor.G, boxColor.B);
            var label = new TextBlock
            {
                Text = box.ClassName,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(labelBg),
                Padding = new Thickness(4, 2, 4, 2),
                FontSize = 12
            };

            Canvas.SetLeft(label, boxX);
            Canvas.SetTop(label, boxY - 20);
            BoxesCanvas.Children.Add(label);
        }

        // 绘制多边形标注
        foreach (var polygon in viewModel.CurrentAnnotation.Polygons)
        {
            var canvasWidth = AnnotationCanvas.ActualWidth;
            var canvasHeight = AnnotationCanvas.ActualHeight;

            var imageAspect = (double)imageWidth / imageHeight;
            var canvasAspect = canvasWidth / canvasHeight;

            double displayWidth, displayHeight, offsetX, offsetY;

            if (imageAspect > canvasAspect)
            {
                displayWidth = canvasWidth;
                displayHeight = canvasWidth / imageAspect;
                offsetX = 0;
                offsetY = (canvasHeight - displayHeight) / 2;
            }
            else
            {
                displayHeight = canvasHeight;
                displayWidth = canvasHeight * imageAspect;
                offsetX = (canvasWidth - displayWidth) / 2;
                offsetY = 0;
            }

            // 获取该类别对应的颜色，没有则用绿色
            var polyColor = classColorMap.TryGetValue(polygon.ClassName, out var pc)
                ? pc
                : Color.FromRgb(0, 255, 0);

            var points = new PointCollection(
                polygon.Points.Select(p => new Point(
                    offsetX + p.X * displayWidth,
                    offsetY + p.Y * displayHeight)));

            var polyline = new System.Windows.Shapes.Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(polyColor),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, polyColor.R, polyColor.G, polyColor.B))
            };

            BoxesCanvas.Children.Add(polyline);

            // 类别标签
            var labelBg = Color.FromArgb(180, polyColor.R, polyColor.G, polyColor.B);
            var labelPoint = points.FirstOrDefault();
            var label = new TextBlock
            {
                Text = polygon.ClassName,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(labelBg),
                Padding = new Thickness(4, 2, 4, 2),
                FontSize = 12
            };

            Canvas.SetLeft(label, labelPoint.X);
            Canvas.SetTop(label, labelPoint.Y - 20);
            BoxesCanvas.Children.Add(label);
        }

        // 绘制旋转边界框（OBB）标注
        foreach (var obb in viewModel.CurrentAnnotation.OrientedBoxes)
        {
            var canvasWidth = AnnotationCanvas.ActualWidth;
            var canvasHeight = AnnotationCanvas.ActualHeight;

            var imageAspect = (double)imageWidth / imageHeight;
            var canvasAspect = canvasWidth / canvasHeight;

            double displayWidth, displayHeight, offsetX, offsetY;

            if (imageAspect > canvasAspect)
            {
                displayWidth = canvasWidth;
                displayHeight = canvasWidth / imageAspect;
                offsetX = 0;
                offsetY = (canvasHeight - displayHeight) / 2;
            }
            else
            {
                displayHeight = canvasHeight;
                displayWidth = canvasHeight * imageAspect;
                offsetX = (canvasWidth - displayWidth) / 2;
                offsetY = 0;
            }

            // 获取该类别对应的颜色，没有则用橙色
            var obbColor = classColorMap.TryGetValue(obb.ClassName, out var oc)
                ? oc
                : Color.FromRgb(255, 152, 0);

            // 计算 OBB 在画布上的位置和尺寸
            var obbCenterX = offsetX + obb.CenterX * displayWidth;
            var obbCenterY = offsetY + obb.CenterY * displayHeight;
            var obbWidth = obb.Width * displayWidth;
            var obbHeight = obb.Height * displayHeight;

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = obbWidth,
                Height = obbHeight,
                Stroke = new SolidColorBrush(obbColor),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, obbColor.R, obbColor.G, obbColor.B))
            };

            // 设置旋转（以矩形中心为旋转中心）
            var rotateTransform = new RotateTransform(obb.Angle * 180 / Math.PI);
            rotateTransform.CenterX = obbWidth / 2;
            rotateTransform.CenterY = obbHeight / 2;
            rect.RenderTransform = rotateTransform;

            Canvas.SetLeft(rect, obbCenterX - obbWidth / 2);
            Canvas.SetTop(rect, obbCenterY - obbHeight / 2);
            BoxesCanvas.Children.Add(rect);

            // 类别标签
            var labelBgO = Color.FromArgb(180, obbColor.R, obbColor.G, obbColor.B);
            var labelO = new TextBlock
            {
                Text = $"{obb.ClassName} ({obb.Angle * 180 / Math.PI:F1}°)",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(labelBgO),
                Padding = new Thickness(4, 2, 4, 2),
                FontSize = 12
            };

            Canvas.SetLeft(labelO, obbCenterX - obbWidth / 2);
            Canvas.SetTop(labelO, obbCenterY - obbHeight / 2 - 20);
            BoxesCanvas.Children.Add(labelO);
        }
    }

    /// <summary>
    /// 刷新已保存的多边形显示（委托给 RefreshBoxesDisplay 统一处理）。
    /// </summary>
    private void RefreshPolygonDisplay(AnnotationViewModel viewModel)
    {
        // 多边形已在 RefreshBoxesDisplay 中统一绘制
    }

    /// <summary>
    /// 更新正在绘制的多边形预览线。
    /// </summary>
    private void UpdateDrawingPolygon(AnnotationViewModel viewModel)
    {
        if (!viewModel.IsPolygonMode || viewModel.CurrentPolygonPoints.Count == 0)
        {
            DrawingPolygon.Visibility = Visibility.Collapsed;
            return;
        }

        DrawingPolygon.Visibility = Visibility.Visible;
        DrawingPolygon.Points = new PointCollection(viewModel.CurrentPolygonPoints);
    }

    /// <summary>
    /// 更新正在绘制的 OBB 预览矩形（带旋转）。
    /// </summary>
    private void UpdateDrawingObb(AnnotationViewModel viewModel)
    {
        if (!viewModel.IsObbMode || (!viewModel.IsDrawingObb && !viewModel.IsRotatingObb))
        {
            DrawingObbRect.Visibility = Visibility.Collapsed;
            return;
        }

        DrawingObbRect.Visibility = Visibility.Visible;
        DrawingObbRect.Width = viewModel.ObbSize.Width;
        DrawingObbRect.Height = viewModel.ObbSize.Height;

        // 设置旋转中心为矩形中心
        var rotateTransform = new RotateTransform(viewModel.ObbAngle * 180 / Math.PI);
        rotateTransform.CenterX = viewModel.ObbSize.Width / 2;
        rotateTransform.CenterY = viewModel.ObbSize.Height / 2;
        DrawingObbRect.RenderTransform = rotateTransform;

        // 定位矩形（左上角 = 中心 - 半宽/半高）
        Canvas.SetLeft(DrawingObbRect, viewModel.ObbCenter.X - viewModel.ObbSize.Width / 2);
        Canvas.SetTop(DrawingObbRect, viewModel.ObbCenter.Y - viewModel.ObbSize.Height / 2);
    }

    /// <summary>
    /// 尝试解析十六进制颜色字符串（如 "#FF0000"）。
    /// </summary>
    private static bool TryParseColor(string hex, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        try
        {
            color = (Color)ColorConverter.ConvertFromString(hex);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
