using System.IO;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services.FormatExporters;

/// <summary>
/// YOLO 格式导出器，负责 YOLO txt 格式转换和 YOLO 数据集导出。
/// </summary>
internal static class YoloFormatExporter
{
    private static readonly ILogger Logger = Log.ForContext("Class", nameof(YoloFormatExporter));

    /// <summary>
    /// 将标注数据导出为 YOLO 格式的文本行。
    /// </summary>
    internal static List<string> ToYoloFormat(AnnotationData annotation)
    {
        var lines = new List<string>();

        foreach (var box in annotation.Boxes)
        {
            lines.Add($"{box.ClassIndex} {box.CenterX:F6} {box.CenterY:F6} {box.Width:F6} {box.Height:F6}");
        }

        foreach (var polygon in annotation.Polygons)
        {
            var coords = string.Join(" ", polygon.Points.Select(p => $"{p.X:F6} {p.Y:F6}"));
            lines.Add($"{polygon.ClassIndex} {coords}");
        }

        foreach (var obb in annotation.OrientedBoxes)
        {
            lines.Add($"{obb.ClassIndex} {obb.CenterX:F6} {obb.CenterY:F6} {obb.Width:F6} {obb.Height:F6} {obb.Angle:F6}");
        }

        foreach (var polyline in annotation.Polylines)
        {
            var coords = string.Join(" ", polyline.Points.Select(p => $"{p.X:F6} {p.Y:F6}"));
            lines.Add($"{polyline.ClassIndex} {coords}");
        }

        foreach (var circle in annotation.Circles)
        {
            lines.Add($"{circle.ClassIndex} {circle.CenterX:F6} {circle.CenterY:F6} {circle.Radius:F6}");
        }

        return lines;
    }

    /// <summary>
    /// 从 YOLO 格式文本行解析标注数据。
    /// </summary>
    internal static (List<BoundingBoxAnnotation> Boxes, List<PolygonAnnotation> Polygons, List<PolylineAnnotation> Polylines, List<CircleAnnotation> Circles, List<OrientedBoundingBoxAnnotation> OrientedBoxes) FromYoloFormat(
        IEnumerable<string> lines, int imageWidth, int imageHeight, List<string> classNames)
    {
        var boxes = new List<BoundingBoxAnnotation>();
        var polygons = new List<PolygonAnnotation>();
        var polylines = new List<PolylineAnnotation>();
        var circles = new List<CircleAnnotation>();
        var orientedBoxes = new List<OrientedBoundingBoxAnnotation>();

        foreach (var line in lines)
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;
            if (!int.TryParse(parts[0], out var classIndex)) continue;

            var className = classIndex < classNames.Count ? classNames[classIndex] : $"class_{classIndex}";

            if (parts.Length == 4)
            {
                if (!float.TryParse(parts[1], out var cx)) continue;
                if (!float.TryParse(parts[2], out var cy)) continue;
                if (!float.TryParse(parts[3], out var radius)) continue;

                circles.Add(new CircleAnnotation
                {
                    ClassIndex = classIndex, ClassName = className,
                    CenterX = cx, CenterY = cy, Radius = radius
                });
            }
            else if (parts.Length == 5)
            {
                if (!double.TryParse(parts[1], out var centerX)) continue;
                if (!double.TryParse(parts[2], out var centerY)) continue;
                if (!double.TryParse(parts[3], out var width)) continue;
                if (!double.TryParse(parts[4], out var height)) continue;

                boxes.Add(new BoundingBoxAnnotation
                {
                    ClassIndex = classIndex, ClassName = className,
                    CenterX = centerX, CenterY = centerY, Width = width, Height = height
                });
            }
            else if (parts.Length == 6)
            {
                if (!float.TryParse(parts[1], out var obbCx)) continue;
                if (!float.TryParse(parts[2], out var obbCy)) continue;
                if (!float.TryParse(parts[3], out var obbW)) continue;
                if (!float.TryParse(parts[4], out var obbH)) continue;
                if (!float.TryParse(parts[5], out var obbAngle)) continue;

                orientedBoxes.Add(new OrientedBoundingBoxAnnotation
                {
                    ClassIndex = classIndex, ClassName = className,
                    CenterX = obbCx, CenterY = obbCy, Width = obbW, Height = obbH, Angle = obbAngle
                });
            }
            else if (parts.Length > 6 && (parts.Length - 1) % 2 == 0)
            {
                var points = new List<System.Drawing.PointF>();
                bool valid = true;

                for (int i = 1; i < parts.Length; i += 2)
                {
                    if (!float.TryParse(parts[i], out var x) || !float.TryParse(parts[i + 1], out var y))
                    {
                        valid = false;
                        break;
                    }
                    points.Add(new System.Drawing.PointF(x, y));
                }

                if (valid && points.Count >= 3)
                {
                    polygons.Add(new PolygonAnnotation
                    {
                        ClassIndex = classIndex, ClassName = className, Points = points
                    });
                }
            }
        }

        return (boxes, polygons, polylines, circles, orientedBoxes);
    }

    /// <summary>
    /// 导出 YOLO 格式数据集。
    /// </summary>
    internal static async Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (trainValSplit <= 0 || trainValSplit >= 1)
            throw new ArgumentException("训练集比例必须在 0-1 之间", nameof(trainValSplit));

        var trainImagesDir = Path.Combine(outputPath, "train", "images");
        var trainLabelsDir = Path.Combine(outputPath, "train", "labels");
        var valImagesDir = Path.Combine(outputPath, "val", "images");
        var valLabelsDir = Path.Combine(outputPath, "val", "labels");

        Directory.CreateDirectory(trainImagesDir);
        Directory.CreateDirectory(trainLabelsDir);
        Directory.CreateDirectory(valImagesDir);
        Directory.CreateDirectory(valLabelsDir);

        var random = new Random(42);
        var shuffled = project.Annotations.OrderBy(_ => random.Next()).ToList();

        var trainCount = (int)(shuffled.Count * trainValSplit);
        var trainAnnotations = shuffled.Take(trainCount).ToList();
        var valAnnotations = shuffled.Skip(trainCount).ToList();

        foreach (var annotation in trainAnnotations)
        {
            await CopyImageAndLabelAsync(annotation, project, trainImagesDir, trainLabelsDir);
        }

        foreach (var annotation in valAnnotations)
        {
            await CopyImageAndLabelAsync(annotation, project, valImagesDir, valLabelsDir);
        }

        var dataYaml = $"""
            # YOLO 数据集配置文件
            # 由 ScaffoldX 自动生成

            path: {outputPath}
            train: train/images
            val: val/images

            # 类别数量
            nc: {project.Classes.Count}

            # 类别名称
            names: [{string.Join(", ", project.Classes.Select(c => $"'{c.Name}'"))}]
            """;

        await File.WriteAllTextAsync(Path.Combine(outputPath, "data.yaml"), dataYaml);
        await File.WriteAllLinesAsync(
            Path.Combine(outputPath, "classes.txt"),
            project.Classes.Select(c => c.Name));

        Logger.Information("导出 YOLO 数据集完成: 训练集 {TrainCount} 张, 验证集 {ValCount} 张",
            trainAnnotations.Count, valAnnotations.Count);
    }

    private static async Task CopyImageAndLabelAsync(
        AnnotationData annotation, AnnotationProject project,
        string imagesDir, string labelsDir)
    {
        var imageName = Path.GetFileName(annotation.ImagePath);
        var labelName = Path.GetFileNameWithoutExtension(annotation.ImagePath) + ".txt";

        var destImagePath = Path.Combine(imagesDir, imageName);
        if (File.Exists(annotation.ImagePath))
        {
            File.Copy(annotation.ImagePath, destImagePath, true);
        }

        var destLabelPath = Path.Combine(labelsDir, labelName);
        var yoloLines = ToYoloFormat(annotation);
        await File.WriteAllLinesAsync(destLabelPath, yoloLines);
    }
}
