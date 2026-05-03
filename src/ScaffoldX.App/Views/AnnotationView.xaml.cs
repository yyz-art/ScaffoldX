using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScaffoldX.App.ViewModels;

namespace ScaffoldX.App.Views;

/// <summary>
/// YOLO 标注工具视图的交互逻辑。
/// </summary>
public partial class AnnotationView : UserControl
{
    public AnnotationView()
    {
        InitializeComponent();
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
                    }
                };
            }
        };
    }

    /// <summary>
    /// 鼠标按下事件：开始绘制边界框。
    /// </summary>
    private void AnnotationCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;

        var position = e.GetPosition(AnnotationCanvas);
        viewModel.ImageMouseDownCommand.Execute(position);

        // 显示绘制矩形
        DrawingRect.Visibility = Visibility.Visible;
        Canvas.SetLeft(DrawingRect, position.X);
        Canvas.SetTop(DrawingRect, position.Y);
        DrawingRect.Width = 0;
        DrawingRect.Height = 0;

        // 捕获鼠标
        AnnotationCanvas.CaptureMouse();
    }

    /// <summary>
    /// 鼠标移动事件：更新绘制中的边界框。
    /// </summary>
    private void AnnotationCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;
        if (!viewModel.IsDrawing) return;

        var position = e.GetPosition(AnnotationCanvas);
        viewModel.ImageMouseMoveCommand.Execute(position);

        // 更新绘制矩形
        var startX = Canvas.GetLeft(DrawingRect);
        var startY = Canvas.GetTop(DrawingRect);

        var x = Math.Min(startX, position.X);
        var y = Math.Min(startY, position.Y);
        var width = Math.Abs(position.X - startX);
        var height = Math.Abs(position.Y - startY);

        Canvas.SetLeft(DrawingRect, x);
        Canvas.SetTop(DrawingRect, y);
        DrawingRect.Width = width;
        DrawingRect.Height = height;
    }

    /// <summary>
    /// 鼠标释放事件：完成边界框绘制。
    /// </summary>
    private void AnnotationCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not AnnotationViewModel viewModel) return;

        var position = e.GetPosition(AnnotationCanvas);
        viewModel.ImageMouseUpCommand.Execute(position);

        // 隐藏绘制矩形
        DrawingRect.Visibility = Visibility.Collapsed;

        // 释放鼠标捕获
        AnnotationCanvas.ReleaseMouseCapture();

        // 刷新边界框显示
        RefreshBoxesDisplay(viewModel);
    }

    /// <summary>
    /// 刷新边界框显示，使用每个类别对应的颜色。
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
