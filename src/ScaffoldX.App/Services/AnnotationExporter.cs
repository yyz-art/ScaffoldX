using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注数据导出实现，负责将标注项目导出为各种训练框架格式。
/// 遵循 ISP 原则，仅暴露导出操作。使用 <see cref="CoordinateMapper"/> 进行坐标变换。
/// </summary>
public class AnnotationExporter : IAnnotationExporter
{
    private readonly ILogger _logger = Log.ForContext<AnnotationExporter>();

    /// <inheritdoc/>
    public List<string> ToYoloFormat(AnnotationData annotation)
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

    /// <inheritdoc/>
    public (List<BoundingBoxAnnotation> Boxes, List<PolygonAnnotation> Polygons, List<PolylineAnnotation> Polylines, List<CircleAnnotation> Circles, List<OrientedBoundingBoxAnnotation> OrientedBoxes) FromYoloFormat(
        IEnumerable<string> lines,
        int imageWidth,
        int imageHeight,
        List<string> classNames)
    {
        var boxes = new List<BoundingBoxAnnotation>();
        var polygons = new List<PolygonAnnotation>();
        var polylines = new List<PolylineAnnotation>();
        var circles = new List<CircleAnnotation>();
        var orientedBoxes = new List<OrientedBoundingBoxAnnotation>();

        foreach (var line in lines)
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
                continue;

            if (!int.TryParse(parts[0], out var classIndex))
                continue;

            var className = classIndex < classNames.Count ? classNames[classIndex] : $"class_{classIndex}";

            if (parts.Length == 4)
            {
                if (!float.TryParse(parts[1], out var cx)) continue;
                if (!float.TryParse(parts[2], out var cy)) continue;
                if (!float.TryParse(parts[3], out var radius)) continue;

                circles.Add(new CircleAnnotation
                {
                    ClassIndex = classIndex,
                    ClassName = className,
                    CenterX = cx,
                    CenterY = cy,
                    Radius = radius
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
                    ClassIndex = classIndex,
                    ClassName = className,
                    CenterX = centerX,
                    CenterY = centerY,
                    Width = width,
                    Height = height
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
                    ClassIndex = classIndex,
                    ClassName = className,
                    CenterX = obbCx,
                    CenterY = obbCy,
                    Width = obbW,
                    Height = obbH,
                    Angle = obbAngle
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
                        ClassIndex = classIndex,
                        ClassName = className,
                        Points = points
                    });
                }
            }
        }

        return (boxes, polygons, polylines, circles, orientedBoxes);
    }

    /// <inheritdoc/>
    public async Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

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

        _logger.Information("导出 YOLO 数据集完成: 训练集 {TrainCount} 张, 验证集 {ValCount} 张",
            trainAnnotations.Count, valAnnotations.Count);
    }

    /// <inheritdoc/>
    public async Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        Directory.CreateDirectory(outputPath);

        var imagesDir = Path.Combine(outputPath, "images");
        Directory.CreateDirectory(imagesDir);

        var images = new List<object>();
        var annotations = new List<object>();
        var categories = project.Classes.Select((c, i) => new { id = i + 1, name = c.Name, supercategory = "none" }).ToList();
        int annotationId = 1;

        for (int imageId = 0; imageId < project.Annotations.Count; imageId++)
        {
            var annotation = project.Annotations[imageId];
            var imageName = Path.GetFileName(annotation.ImagePath);

            if (File.Exists(annotation.ImagePath))
            {
                File.Copy(annotation.ImagePath, Path.Combine(imagesDir, imageName), true);
            }

            images.Add(new
            {
                id = imageId + 1,
                file_name = imageName,
                width = annotation.ImageWidth,
                height = annotation.ImageHeight
            });

            foreach (var box in annotation.Boxes)
            {
                var (absX, absY, absW, absH) = CoordinateMapper.ToAbsoluteBbox(
                    box.CenterX, box.CenterY, box.Width, box.Height,
                    annotation.ImageWidth, annotation.ImageHeight);

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = box.ClassIndex + 1,
                    bbox = new[] { Math.Round(absX, 2), Math.Round(absY, 2), Math.Round(absW, 2), Math.Round(absH, 2) },
                    area = Math.Round(absW * absH, 2),
                    segmentation = Array.Empty<double[]>(),
                    iscrowd = 0
                });
            }

            foreach (var polygon in annotation.Polygons)
            {
                var segCoords = new List<double>();
                foreach (var pt in polygon.Points)
                {
                    var (ptAbsX, ptAbsY) = CoordinateMapper.ToAbsolute(pt.X, pt.Y, annotation.ImageWidth, annotation.ImageHeight);
                    segCoords.Add(Math.Round(ptAbsX, 2));
                    segCoords.Add(Math.Round(ptAbsY, 2));
                }

                var xs = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var polyAbsX = xs.Min();
                var polyAbsY = ys.Min();
                var polyAbsW = xs.Max() - xs.Min();
                var polyAbsH = ys.Max() - ys.Min();

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = polygon.ClassIndex + 1,
                    bbox = new[] { Math.Round(polyAbsX, 2), Math.Round(polyAbsY, 2), Math.Round(polyAbsW, 2), Math.Round(polyAbsH, 2) },
                    area = Math.Round(polyAbsW * polyAbsH, 2),
                    segmentation = new[] { segCoords.ToArray() },
                    iscrowd = 0
                });
            }

            foreach (var polyline in annotation.Polylines)
            {
                var segCoords = new List<double>();
                foreach (var pt in polyline.Points)
                {
                    var (ptAbsX, ptAbsY) = CoordinateMapper.ToAbsolute(pt.X, pt.Y, annotation.ImageWidth, annotation.ImageHeight);
                    segCoords.Add(Math.Round(ptAbsX, 2));
                    segCoords.Add(Math.Round(ptAbsY, 2));
                }

                var xs = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var plAbsX = xs.Min();
                var plAbsY = ys.Min();
                var plAbsW = xs.Max() - xs.Min();
                var plAbsH = ys.Max() - ys.Min();

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = polyline.ClassIndex + 1,
                    bbox = new[] { Math.Round(plAbsX, 2), Math.Round(plAbsY, 2), Math.Round(plAbsW, 2), Math.Round(plAbsH, 2) },
                    area = Math.Round(plAbsW * plAbsH, 2),
                    segmentation = new[] { segCoords.ToArray() },
                    iscrowd = 0
                });
            }

            foreach (var circle in annotation.Circles)
            {
                var absCx = CoordinateMapper.ToAbsolute(circle.CenterX, annotation.ImageWidth);
                var absCy = CoordinateMapper.ToAbsolute(circle.CenterY, annotation.ImageHeight);
                var absR = CoordinateMapper.ToAbsolute(circle.Radius, annotation.ImageWidth);

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = circle.ClassIndex + 1,
                    bbox = new[] { Math.Round(absCx - absR, 2), Math.Round(absCy - absR, 2), Math.Round(absR * 2, 2), Math.Round(absR * 2, 2) },
                    area = Math.Round(Math.PI * absR * absR, 2),
                    segmentation = Array.Empty<double[]>(),
                    iscrowd = 0
                });
            }
        }

        var cocoJson = new
        {
            images,
            annotations,
            categories
        };

        var json = JsonSerializer.Serialize(cocoJson, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputPath, "annotations.json"), json);

        _logger.Information("导出 COCO 数据集完成: {ImageCount} 张图像, {AnnotationCount} 个标注",
            images.Count, annotations.Count);
    }

    /// <inheritdoc/>
    public async Task ExportVocDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        var imagesDir = Path.Combine(outputPath, "JPEGImages");
        var annotationsDir = Path.Combine(outputPath, "Annotations");
        var imageSetsDir = Path.Combine(outputPath, "ImageSets", "Main");
        Directory.CreateDirectory(imagesDir);
        Directory.CreateDirectory(annotationsDir);
        Directory.CreateDirectory(imageSetsDir);

        var imageNames = new List<string>();

        foreach (var annotation in project.Annotations)
        {
            var imageName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
            var imageExt = Path.GetExtension(annotation.ImagePath);
            imageNames.Add(imageName);

            if (File.Exists(annotation.ImagePath))
            {
                File.Copy(annotation.ImagePath, Path.Combine(imagesDir, $"{imageName}{imageExt}"), true);
            }

            var objectElements = new List<XElement>();

            foreach (var box in annotation.Boxes)
            {
                var (vAbsX, vAbsY, vAbsW, vAbsH) = CoordinateMapper.ToAbsoluteBbox(
                    box.CenterX, box.CenterY, box.Width, box.Height,
                    annotation.ImageWidth, annotation.ImageHeight);
                var absXmin = CoordinateMapper.Clamp((int)vAbsX, 0, annotation.ImageWidth);
                var absYmin = CoordinateMapper.Clamp((int)vAbsY, 0, annotation.ImageHeight);
                var absXmax = CoordinateMapper.Clamp((int)(vAbsX + vAbsW), 0, annotation.ImageWidth);
                var absYmax = CoordinateMapper.Clamp((int)(vAbsY + vAbsH), 0, annotation.ImageHeight);

                objectElements.Add(new XElement("object",
                    new XElement("name", box.ClassName),
                    new XElement("pose", "Unspecified"),
                    new XElement("truncated", 0),
                    new XElement("difficult", 0),
                    new XElement("bndbox",
                        new XElement("xmin", absXmin),
                        new XElement("ymin", absYmin),
                        new XElement("xmax", absXmax),
                        new XElement("ymax", absYmax))));
            }

            foreach (var polygon in annotation.Polygons)
            {
                var pointElements = new List<XElement>();
                foreach (var p in polygon.Points)
                {
                    var px = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth), 0, annotation.ImageWidth);
                    var py = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight), 0, annotation.ImageHeight);
                    pointElements.Add(new XElement("x", px));
                    pointElements.Add(new XElement("y", py));
                }

                objectElements.Add(new XElement("object",
                    new XElement("name", polygon.ClassName),
                    new XElement("pose", "Unspecified"),
                    new XElement("truncated", 0),
                    new XElement("difficult", 0),
                    new XElement("polygon", pointElements)));
            }

            foreach (var obb in annotation.OrientedBoxes)
            {
                var absCx = Math.Round(CoordinateMapper.ToAbsolute(obb.CenterX, annotation.ImageWidth), 2);
                var absCy = Math.Round(CoordinateMapper.ToAbsolute(obb.CenterY, annotation.ImageHeight), 2);
                var absW = Math.Round(CoordinateMapper.ToAbsolute(obb.Width, annotation.ImageWidth), 2);
                var absH = Math.Round(CoordinateMapper.ToAbsolute(obb.Height, annotation.ImageHeight), 2);
                var angleDeg = Math.Round(obb.Angle * 180.0 / Math.PI, 2);

                objectElements.Add(new XElement("object",
                    new XElement("name", obb.ClassName),
                    new XElement("pose", "Unspecified"),
                    new XElement("truncated", 0),
                    new XElement("difficult", 0),
                    new XElement("robndbox",
                        new XElement("cx", absCx),
                        new XElement("cy", absCy),
                        new XElement("w", absW),
                        new XElement("h", absH),
                        new XElement("angle", angleDeg))));
            }

            foreach (var polyline in annotation.Polylines)
            {
                var pointElements = new List<XElement>();
                foreach (var p in polyline.Points)
                {
                    var px = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth), 0, annotation.ImageWidth);
                    var py = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight), 0, annotation.ImageHeight);
                    pointElements.Add(new XElement("x", px));
                    pointElements.Add(new XElement("y", py));
                }

                objectElements.Add(new XElement("object",
                    new XElement("name", polyline.ClassName),
                    new XElement("pose", "Unspecified"),
                    new XElement("truncated", 0),
                    new XElement("difficult", 0),
                    new XElement("polyline", pointElements)));
            }

            foreach (var circle in annotation.Circles)
            {
                var absCx = Math.Round(CoordinateMapper.ToAbsolute(circle.CenterX, annotation.ImageWidth), 2);
                var absCy = Math.Round(CoordinateMapper.ToAbsolute(circle.CenterY, annotation.ImageHeight), 2);
                var absR = Math.Round(CoordinateMapper.ToAbsolute(circle.Radius, annotation.ImageWidth), 2);

                var xmin = CoordinateMapper.Clamp((int)(absCx - absR), 0, annotation.ImageWidth);
                var ymin = CoordinateMapper.Clamp((int)(absCy - absR), 0, annotation.ImageHeight);
                var xmax = CoordinateMapper.Clamp((int)(absCx + absR), 0, annotation.ImageWidth);
                var ymax = CoordinateMapper.Clamp((int)(absCy + absR), 0, annotation.ImageHeight);

                objectElements.Add(new XElement("object",
                    new XElement("name", circle.ClassName),
                    new XElement("pose", "Unspecified"),
                    new XElement("truncated", 0),
                    new XElement("difficult", 0),
                    new XElement("circle",
                        new XElement("cx", absCx),
                        new XElement("cy", absCy),
                        new XElement("r", absR))));
            }

            var xml = new XDocument(
                new XElement("annotation",
                    new XElement("folder", Path.GetFileName(outputPath)),
                    new XElement("filename", $"{imageName}{imageExt}"),
                    new XElement("size",
                        new XElement("width", annotation.ImageWidth),
                        new XElement("height", annotation.ImageHeight),
                        new XElement("depth", 3)),
                    new XElement("segmented", 0),
                    objectElements));

            await File.WriteAllTextAsync(
                Path.Combine(annotationsDir, $"{imageName}.xml"),
                xml.ToString());
        }

        var trainTxt = string.Join("\n", imageNames);
        await File.WriteAllTextAsync(Path.Combine(imageSetsDir, "train.txt"), trainTxt);

        _logger.Information("导出 Pascal VOC 数据集完成: {Count} 张图像", imageNames.Count);
    }

    /// <inheritdoc/>
    public async Task ExportDotDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        Directory.CreateDirectory(outputPath);

        foreach (var annotation in project.Annotations)
        {
            var imageName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
            var lines = new List<string>();

            foreach (var obb in annotation.OrientedBoxes)
            {
                var corners = ComputeObbCorners(
                    obb.CenterX, obb.CenterY, obb.Width, obb.Height, obb.Angle,
                    annotation.ImageWidth, annotation.ImageHeight);

                var className = obb.ClassName;
                lines.Add($"{corners.x1:F1} {corners.y1:F1} {corners.x2:F1} {corners.y2:F1} " +
                          $"{corners.x3:F1} {corners.y3:F1} {corners.x4:F1} {corners.y4:F1} " +
                          $"{className} 1.0");
            }

            foreach (var box in annotation.Boxes)
            {
                var absCx = CoordinateMapper.ToAbsolute(box.CenterX, annotation.ImageWidth);
                var absCy = CoordinateMapper.ToAbsolute(box.CenterY, annotation.ImageHeight);
                var absW = CoordinateMapper.ToAbsolute(box.Width, annotation.ImageWidth);
                var absH = CoordinateMapper.ToAbsolute(box.Height, annotation.ImageHeight);

                var x1 = absCx - absW / 2;
                var y1 = absCy - absH / 2;
                var x2 = absCx + absW / 2;
                var y2 = absCy - absH / 2;
                var x3 = absCx + absW / 2;
                var y3 = absCy + absH / 2;
                var x4 = absCx - absW / 2;
                var y4 = absCy + absH / 2;

                lines.Add($"{x1:F1} {y1:F1} {x2:F1} {y2:F1} {x3:F1} {y3:F1} {x4:F1} {y4:F1} " +
                          $"{box.ClassName} 1.0");
            }

            foreach (var polygon in annotation.Polygons)
            {
                if (polygon.Points.Count < 3) continue;

                var corners = new List<(float x, float y)>();
                for (int i = 0; i < 4; i++)
                {
                    var pt = polygon.Points[Math.Min(i, polygon.Points.Count - 1)];
                    corners.Add((CoordinateMapper.ToAbsolute(pt.X, annotation.ImageWidth),
                                 CoordinateMapper.ToAbsolute(pt.Y, annotation.ImageHeight)));
                }

                lines.Add($"{corners[0].x:F1} {corners[0].y:F1} {corners[1].x:F1} {corners[1].y:F1} " +
                          $"{corners[2].x:F1} {corners[2].y:F1} {corners[3].x:F1} {corners[3].y:F1} " +
                          $"{polygon.ClassName} 1.0");
            }

            if (lines.Count > 0)
            {
                await File.WriteAllLinesAsync(Path.Combine(outputPath, $"{imageName}.txt"), lines);
            }
        }

        await File.WriteAllLinesAsync(
            Path.Combine(outputPath, "classes.txt"),
            project.Classes.Select(c => c.Name));

        _logger.Information("导出 DOTA 数据集完成: {Count} 张图像", project.Annotations.Count);
    }

    /// <inheritdoc/>
    public async Task ExportMotDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        Directory.CreateDirectory(outputPath);

        var gtLines = new List<string>();

        var classNameToId = project.Classes
            .Select((c, i) => (c.Name, Id: i))
            .ToDictionary(x => x.Name, x => x.Id);

        for (int frameId = 1; frameId <= project.Annotations.Count; frameId++)
        {
            var annotation = project.Annotations[frameId - 1];
            int trackId = 1;

            foreach (var box in annotation.Boxes)
            {
                var (mAbsX, mAbsY, mAbsW, mAbsH) = CoordinateMapper.ToAbsoluteBbox(
                    box.CenterX, box.CenterY, box.Width, box.Height,
                    annotation.ImageWidth, annotation.ImageHeight);
                var classId = classNameToId.GetValueOrDefault(box.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{mAbsX:F1},{mAbsY:F1},{mAbsW:F1},{mAbsH:F1},1,{classId},1");
                trackId++;
            }

            foreach (var polygon in annotation.Polygons)
            {
                if (polygon.Points.Count == 0) continue;

                var xs = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var minX = xs.Min();
                var minY = ys.Min();
                var w = xs.Max() - minX;
                var h = ys.Max() - minY;
                var classId = classNameToId.GetValueOrDefault(polygon.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{minX:F1},{minY:F1},{w:F1},{h:F1},1,{classId},1");
                trackId++;
            }

            foreach (var obb in annotation.OrientedBoxes)
            {
                var corners = ComputeObbCorners(
                    obb.CenterX, obb.CenterY, obb.Width, obb.Height, obb.Angle,
                    annotation.ImageWidth, annotation.ImageHeight);

                var allX = new[] { corners.x1, corners.x2, corners.x3, corners.x4 };
                var allY = new[] { corners.y1, corners.y2, corners.y3, corners.y4 };
                var minX = allX.Min();
                var minY = allY.Min();
                var w = allX.Max() - minX;
                var h = allY.Max() - minY;
                var classId = classNameToId.GetValueOrDefault(obb.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{minX:F1},{minY:F1},{w:F1},{h:F1},1,{classId},1");
                trackId++;
            }

            foreach (var polyline in annotation.Polylines)
            {
                if (polyline.Points.Count == 0) continue;

                var xs = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var minX = xs.Min();
                var minY = ys.Min();
                var w = xs.Max() - minX;
                var h = ys.Max() - minY;
                var classId = classNameToId.GetValueOrDefault(polyline.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{minX:F1},{minY:F1},{w:F1},{h:F1},1,{classId},1");
                trackId++;
            }

            foreach (var circle in annotation.Circles)
            {
                var absCx = CoordinateMapper.ToAbsolute(circle.CenterX, annotation.ImageWidth);
                var absCy = CoordinateMapper.ToAbsolute(circle.CenterY, annotation.ImageHeight);
                var absR = CoordinateMapper.ToAbsolute(circle.Radius, annotation.ImageWidth);
                var classId = classNameToId.GetValueOrDefault(circle.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{absCx - absR:F1},{absCy - absR:F1},{absR * 2:F1},{absR * 2:F1},1,{classId},1");
                trackId++;
            }
        }

        await File.WriteAllLinesAsync(Path.Combine(outputPath, "gt.txt"), gtLines);

        _logger.Information("导出 MOT 数据集完成: {FrameCount} 帧, {LineCount} 条记录",
            project.Annotations.Count, gtLines.Count);
    }

    /// <summary>
    /// 根据旋转边界框的中心坐标、宽高和旋转角度，计算 4 个角点的绝对像素坐标。
    /// </summary>
    private static (float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) ComputeObbCorners(
        float centerX, float centerY, float width, float height, float angle,
        int imageWidth, int imageHeight)
    {
        var absCx = CoordinateMapper.ToAbsolute(centerX, imageWidth);
        var absCy = CoordinateMapper.ToAbsolute(centerY, imageHeight);
        var absW = CoordinateMapper.ToAbsolute(width, imageWidth);
        var absH = CoordinateMapper.ToAbsolute(height, imageHeight);

        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);
        var hw = absW / 2;
        var hh = absH / 2;

        return (
            absCx + (-hw * cos - -hh * sin), absCy + (-hw * sin + -hh * cos),
            absCx + (hw * cos - -hh * sin),  absCy + (hw * sin + -hh * cos),
            absCx + (hw * cos - hh * sin),   absCy + (hw * sin + hh * cos),
            absCx + (-hw * cos - hh * sin),  absCy + (-hw * sin + hh * cos)
        );
    }

    /// <summary>
    /// 复制图像和对应的标注文件到目标目录。
    /// </summary>
    private async Task CopyImageAndLabelAsync(
        AnnotationData annotation,
        AnnotationProject project,
        string imagesDir,
        string labelsDir)
    {
        var imageName = Path.GetFileName(annotation.ImagePath);
        var labelName = Path.GetFileNameWithoutExtension(annotation.ImagePath) + ".txt";

        var destImagePath = Path.Combine(imagesDir, imageName);
        if (File.Exists(annotation.ImagePath))
        {
            File.Copy(annotation.ImagePath, destImagePath, true);
        }

        var destLabelPath = Path.Combine(labelsDir, labelName);
        var lines = ToYoloFormat(annotation);
        await File.WriteAllLinesAsync(destLabelPath, lines);
    }
}
