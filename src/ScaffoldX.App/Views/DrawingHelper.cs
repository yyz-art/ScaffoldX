using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ScaffoldX.App.Constants;
using ScaffoldX.App.ViewModels;

namespace ScaffoldX.App.Views;

/// <summary>
/// 绘图辅助静态类，从 AnnotationView 中提取的边界框、多边形、OBB 显示刷新和绘制预览逻辑。
/// </summary>
internal static class DrawingHelper
{
    /// <summary>
    /// 计算图像在画布中的显示区域（适配居中）。
    /// </summary>
    internal static (double displayWidth, double displayHeight, double offsetX, double offsetY)
        CalculateImageLayout(double canvasWidth, double canvasHeight, int imageWidth, int imageHeight)
    {
        var imageAspect = (double)imageWidth / imageHeight;
        var canvasAspect = canvasWidth / canvasHeight;

        if (imageAspect > canvasAspect)
        {
            var displayWidth = canvasWidth;
            var displayHeight = canvasWidth / imageAspect;
            return (displayWidth, displayHeight, 0, (canvasHeight - displayHeight) / 2);
        }
        else
        {
            var displayHeight = canvasHeight;
            var displayWidth = canvasHeight * imageAspect;
            return (displayWidth, displayHeight, (canvasWidth - displayWidth) / 2, 0);
        }
    }

    /// <summary>
    /// 刷新边界框、多边形和 OBB 显示，使用每个类别对应的颜色。
    /// </summary>
    internal static void RefreshBoxesDisplay(Canvas boxesCanvas, Canvas annotationCanvas, AnnotationViewModel viewModel)
    {
        boxesCanvas.Children.Clear();

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

        var canvasWidth = annotationCanvas.ActualWidth;
        var canvasHeight = annotationCanvas.ActualHeight;
        var layout = CalculateImageLayout(canvasWidth, canvasHeight, imageWidth, imageHeight);

        // 绘制边界框
        foreach (var box in viewModel.CurrentAnnotation.Boxes)
        {
            var boxX = layout.offsetX + (box.CenterX - box.Width / 2) * layout.displayWidth;
            var boxY = layout.offsetY + (box.CenterY - box.Height / 2) * layout.displayHeight;
            var boxWidth = box.Width * layout.displayWidth;
            var boxHeight = box.Height * layout.displayHeight;

            var boxColor = classColorMap.TryGetValue(box.ClassName, out var c) ? c : Color.FromRgb(255, 0, 0);

            var rect = new Rectangle
            {
                Width = boxWidth,
                Height = boxHeight,
                Stroke = new SolidColorBrush(boxColor),
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(rect, boxX);
            Canvas.SetTop(rect, boxY);
            boxesCanvas.Children.Add(rect);

            AddLabel(boxesCanvas, box.ClassName, boxColor, boxX, boxY + AnnotationConstants.LabelOffset);
        }

        // 绘制多边形标注
        foreach (var polygon in viewModel.CurrentAnnotation.Polygons)
        {
            var polyColor = classColorMap.TryGetValue(polygon.ClassName, out var pc)
                ? pc
                : Color.FromRgb(0, 255, 0);

            var points = new PointCollection(
                polygon.Points.Select(p => new Point(
                    layout.offsetX + p.X * layout.displayWidth,
                    layout.offsetY + p.Y * layout.displayHeight)));

            var polyline = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(polyColor),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, polyColor.R, polyColor.G, polyColor.B))
            };

            boxesCanvas.Children.Add(polyline);

            var labelPoint = points.FirstOrDefault();
            AddLabel(boxesCanvas, polygon.ClassName, polyColor, labelPoint.X, labelPoint.Y + AnnotationConstants.LabelOffset);
        }

        // 绘制旋转边界框（OBB）标注
        foreach (var obb in viewModel.CurrentAnnotation.OrientedBoxes)
        {
            var obbColor = classColorMap.TryGetValue(obb.ClassName, out var oc)
                ? oc
                : Color.FromRgb(255, 152, 0);

            var obbCenterX = layout.offsetX + obb.CenterX * layout.displayWidth;
            var obbCenterY = layout.offsetY + obb.CenterY * layout.displayHeight;
            var obbWidth = obb.Width * layout.displayWidth;
            var obbHeight = obb.Height * layout.displayHeight;

            var rect = new Rectangle
            {
                Width = obbWidth,
                Height = obbHeight,
                Stroke = new SolidColorBrush(obbColor),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, obbColor.R, obbColor.G, obbColor.B))
            };

            var rotateTransform = new RotateTransform(obb.Angle * MathConstants.RadiansToDegrees);
            rotateTransform.CenterX = obbWidth / 2;
            rotateTransform.CenterY = obbHeight / 2;
            rect.RenderTransform = rotateTransform;

            Canvas.SetLeft(rect, obbCenterX - obbWidth / 2);
            Canvas.SetTop(rect, obbCenterY - obbHeight / 2);
            boxesCanvas.Children.Add(rect);

            AddLabel(boxesCanvas, $"{obb.ClassName} ({obb.Angle * MathConstants.RadiansToDegrees:F1}°)",
                obbColor, obbCenterX - obbWidth / 2, obbCenterY - obbHeight / 2 + AnnotationConstants.LabelOffset);
        }
    }

    /// <summary>
    /// 更新正在绘制的多边形预览线。
    /// </summary>
    internal static void UpdateDrawingPolygon(Polyline drawingPolygon, AnnotationViewModel viewModel)
    {
        if (!viewModel.IsPolygonMode || viewModel.CurrentPolygonPoints.Count == 0)
        {
            drawingPolygon.Visibility = Visibility.Collapsed;
            return;
        }

        drawingPolygon.Visibility = Visibility.Visible;
        drawingPolygon.Points = new PointCollection(viewModel.CurrentPolygonPoints);
    }

    /// <summary>
    /// 更新正在绘制的 OBB 预览矩形（带旋转）。
    /// </summary>
    internal static void UpdateDrawingObb(Rectangle drawingObbRect, AnnotationViewModel viewModel)
    {
        if (!viewModel.IsObbMode || (!viewModel.IsDrawingObb && !viewModel.IsRotatingObb))
        {
            drawingObbRect.Visibility = Visibility.Collapsed;
            return;
        }

        drawingObbRect.Visibility = Visibility.Visible;
        drawingObbRect.Width = viewModel.ObbSize.Width;
        drawingObbRect.Height = viewModel.ObbSize.Height;

        var rotateTransform = new RotateTransform(viewModel.ObbAngle * MathConstants.RadiansToDegrees);
        rotateTransform.CenterX = viewModel.ObbSize.Width / 2;
        rotateTransform.CenterY = viewModel.ObbSize.Height / 2;
        drawingObbRect.RenderTransform = rotateTransform;

        Canvas.SetLeft(drawingObbRect, viewModel.ObbCenter.X - viewModel.ObbSize.Width / 2);
        Canvas.SetTop(drawingObbRect, viewModel.ObbCenter.Y - viewModel.ObbSize.Height / 2);
    }

    /// <summary>
    /// 将屏幕坐标转换为归一化图像坐标（0-1）。用于 SAM3 点提示模式。
    /// </summary>
    internal static Point ScreenToNormalized(Point screenPoint, Canvas annotationCanvas, AnnotationViewModel viewModel)
    {
        if (viewModel.CurrentImage == null)
            return new Point(0, 0);

        var layout = CalculateImageLayout(
            annotationCanvas.ActualWidth, annotationCanvas.ActualHeight,
            viewModel.CurrentImage.PixelWidth, viewModel.CurrentImage.PixelHeight);

        var normalizedX = (screenPoint.X - layout.offsetX) / layout.displayWidth;
        var normalizedY = (screenPoint.Y - layout.offsetY) / layout.displayHeight;

        return new Point(Math.Clamp(normalizedX, 0, 1), Math.Clamp(normalizedY, 0, 1));
    }

    /// <summary>
    /// 尝试解析十六进制颜色字符串（如 "#FF0000"）。
    /// </summary>
    internal static bool TryParseColor(string hex, out Color color)
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

    /// <summary>
    /// 向画布添加类别标签。
    /// </summary>
    private static void AddLabel(Canvas canvas, string text, Color baseColor, double x, double y)
    {
        var labelBg = Color.FromArgb(180, baseColor.R, baseColor.G, baseColor.B);
        var label = new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(labelBg),
            Padding = new Thickness(4, 2, 4, 2),
            FontSize = 12
        };

        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        canvas.Children.Add(label);
    }
}
