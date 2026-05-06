using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注服务实现，提供标注项目的完整生命周期管理。
/// </summary>
public class AnnotationService : IAnnotationService
{
    private readonly ILogger _logger = Log.ForContext<AnnotationService>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public async Task<AnnotationProject> CreateProjectAsync(
        string projectName,
        string projectDirectory,
        List<AnnotationClass> classes)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new ArgumentException("项目名称不能为空", nameof(projectName));

        if (string.IsNullOrWhiteSpace(projectDirectory))
            throw new ArgumentException("项目目录不能为空", nameof(projectDirectory));

        if (classes == null || classes.Count == 0)
            throw new ArgumentException("至少需要一个类别定义", nameof(classes));

        // 创建项目目录结构
        var imagesDir = Path.Combine(projectDirectory, "images");
        var labelsDir = Path.Combine(projectDirectory, "labels");

        Directory.CreateDirectory(imagesDir);
        Directory.CreateDirectory(labelsDir);

        var project = new AnnotationProject
        {
            ProjectName = projectName,
            ProjectDirectory = projectDirectory,
            Classes = classes,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        // 创建 classes.txt 文件
        var classesFilePath = Path.Combine(projectDirectory, "classes.txt");
        await File.WriteAllLinesAsync(classesFilePath, classes.Select(c => c.Name));

        _logger.Information("创建标注项目: {ProjectName}, 目录: {Directory}, 类别数: {ClassCount}",
            projectName, projectDirectory, classes.Count);

        return project;
    }

    /// <inheritdoc/>
    public async Task<AnnotationProject> LoadProjectAsync(string projectFilePath)
    {
        if (!File.Exists(projectFilePath))
            throw new FileNotFoundException("项目文件不存在", projectFilePath);

        var json = await File.ReadAllTextAsync(projectFilePath);
        var project = JsonSerializer.Deserialize<AnnotationProject>(json, JsonOptions);

        if (project == null)
            throw new InvalidOperationException("无法解析项目文件");

        _logger.Information("加载标注项目: {ProjectName}, 标注数: {AnnotationCount}",
            project.ProjectName, project.Annotations.Count);

        return project;
    }

    /// <inheritdoc/>
    public async Task SaveProjectAsync(AnnotationProject project)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        project.ModifiedAt = DateTime.Now;

        var projectFilePath = Path.Combine(project.ProjectDirectory, $"{project.ProjectName}.scaffoldx-annotation.json");
        var json = JsonSerializer.Serialize(project, JsonOptions);

        await File.WriteAllTextAsync(projectFilePath, json);

        _logger.Debug("保存标注项目: {ProjectName}", project.ProjectName);
    }

    /// <inheritdoc/>
    public async Task AddImageAsync(AnnotationProject project, string imagePath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("图像文件不存在", imagePath);

        var annotation = new AnnotationData
        {
            ImagePath = imagePath,
            ImageWidth = 0,
            ImageHeight = 0,
            Boxes = new List<BoundingBoxAnnotation>(),
            Polygons = new List<PolygonAnnotation>(),
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>(),
            Polylines = new List<PolylineAnnotation>(),
            Circles = new List<CircleAnnotation>()
        };

        // 获取图像尺寸
        try
        {
            using var image = Image.FromFile(imagePath);
            annotation.ImageWidth = image.Width;
            annotation.ImageHeight = image.Height;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "无法读取图像尺寸: {ImagePath}", imagePath);
        }

        // 检查是否已存在
        var existing = project.Annotations.FirstOrDefault(a => a.ImagePath == imagePath);
        if (existing != null)
        {
            _logger.Debug("图像已存在于项目中: {ImagePath}", imagePath);
            return;
        }

        project.Annotations.Add(annotation);
        _logger.Debug("添加图像到项目: {ImagePath}, 尺寸: {Width}x{Height}",
            imagePath, annotation.ImageWidth, annotation.ImageHeight);

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            await AddImageAsync(project, imagePath);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAnnotationAsync(AnnotationProject project, AnnotationData annotation)
    {
        if (project == null || annotation == null)
            throw new ArgumentNullException(nameof(project));

        var existing = project.Annotations.FirstOrDefault(a => a.ImagePath == annotation.ImagePath);
        if (existing != null)
        {
            existing.Boxes = annotation.Boxes;
            existing.Polygons = annotation.Polygons;
            existing.OrientedBoxes = annotation.OrientedBoxes;
            existing.Polylines = annotation.Polylines;
            existing.Circles = annotation.Circles;
            existing.ImageWidth = annotation.ImageWidth;
            existing.ImageHeight = annotation.ImageHeight;
        }
        else
        {
            project.Annotations.Add(annotation);
        }

        project.ModifiedAt = DateTime.Now;

        // 同时保存 YOLO 格式的标注文件
        await SaveYoloLabelAsync(project, annotation);

        _logger.Debug("更新标注: {ImagePath}, 边界框数: {BoxCount}",
            annotation.ImagePath, annotation.Boxes.Count);
    }

    /// <inheritdoc/>
    public async Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        if (trainValSplit <= 0 || trainValSplit >= 1)
            throw new ArgumentException("训练集比例必须在 0-1 之间", nameof(trainValSplit));

        // 创建 YOLO 数据集目录结构
        var trainImagesDir = Path.Combine(outputPath, "train", "images");
        var trainLabelsDir = Path.Combine(outputPath, "train", "labels");
        var valImagesDir = Path.Combine(outputPath, "val", "images");
        var valLabelsDir = Path.Combine(outputPath, "val", "labels");

        Directory.CreateDirectory(trainImagesDir);
        Directory.CreateDirectory(trainLabelsDir);
        Directory.CreateDirectory(valImagesDir);
        Directory.CreateDirectory(valLabelsDir);

        // 随机打乱并分割数据集
        var random = new Random(42);
        var shuffled = project.Annotations.OrderBy(_ => random.Next()).ToList();

        var trainCount = (int)(shuffled.Count * trainValSplit);
        var trainAnnotations = shuffled.Take(trainCount).ToList();
        var valAnnotations = shuffled.Skip(trainCount).ToList();

        // 复制训练集
        foreach (var annotation in trainAnnotations)
        {
            await CopyImageAndLabelAsync(annotation, project, trainImagesDir, trainLabelsDir);
        }

        // 复制验证集
        foreach (var annotation in valAnnotations)
        {
            await CopyImageAndLabelAsync(annotation, project, valImagesDir, valLabelsDir);
        }

        // 创建 data.yaml 配置文件
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

        // 创建 classes.txt
        await File.WriteAllLinesAsync(
            Path.Combine(outputPath, "classes.txt"),
            project.Classes.Select(c => c.Name));

        _logger.Information("导出 YOLO 数据集完成: 训练集 {TrainCount} 张, 验证集 {ValCount} 张",
            trainAnnotations.Count, valAnnotations.Count);
    }

    /// <inheritdoc/>
    public List<string> ToYoloFormat(AnnotationData annotation)
    {
        var lines = new List<string>();

        // 边界框格式: class_index center_x center_y width height
        foreach (var box in annotation.Boxes)
        {
            lines.Add($"{box.ClassIndex} {box.CenterX:F6} {box.CenterY:F6} {box.Width:F6} {box.Height:F6}");
        }

        // 多边形格式: class_index x1 y1 x2 y2 ... xn yn
        foreach (var polygon in annotation.Polygons)
        {
            var coords = string.Join(" ", polygon.Points.Select(p => $"{p.X:F6} {p.Y:F6}"));
            lines.Add($"{polygon.ClassIndex} {coords}");
        }

        // 旋转边界框（OBB）格式: class_index center_x center_y width height angle
        foreach (var obb in annotation.OrientedBoxes)
        {
            lines.Add($"{obb.ClassIndex} {obb.CenterX:F6} {obb.CenterY:F6} {obb.Width:F6} {obb.Height:F6} {obb.Angle:F6}");
        }

        // 折线格式: class_index x1 y1 x2 y2 ... xn yn（is_closed 区分折线与多边形）
        foreach (var polyline in annotation.Polylines)
        {
            var coords = string.Join(" ", polyline.Points.Select(p => $"{p.X:F6} {p.Y:F6}"));
            lines.Add($"{polyline.ClassIndex} {coords}");
        }

        // 圆形格式: class_index center_x center_y radius
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
                // 圆形格式: class_index center_x center_y radius（4 个值）
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
                // 边界框格式: class_index center_x center_y width height
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
                // 旋转边界框（OBB）格式: class_index center_x center_y width height angle（6 个值）
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
                // 多边形格式: class_index x1 y1 x2 y2 ... xn yn（至少 4 个顶点 = 8 个坐标值 + 类别）
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
    /// <remarks>
    /// COCO 格式不原生支持旋转边界框（OBB）。OBB 标注将作为轴对齐的外接矩形导出到 bbox 字段，
    /// 但旋转信息会丢失。如需保留 OBB 信息，请使用 YOLO OBB 格式导出。
    /// </remarks>
    public async Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        Directory.CreateDirectory(outputPath);

        // 复制图像到 images/ 子目录
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

            // 复制图像
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

            // 转换边界框为 COCO 格式 [x, y, width, height]（绝对像素坐标）
            foreach (var box in annotation.Boxes)
            {
                var absX = box.CenterX * annotation.ImageWidth - box.Width * annotation.ImageWidth / 2;
                var absY = box.CenterY * annotation.ImageHeight - box.Height * annotation.ImageHeight / 2;
                var absW = box.Width * annotation.ImageWidth;
                var absH = box.Height * annotation.ImageHeight;

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

            // 转换多边形为 COCO 格式 segmentation（绝对像素坐标）
            foreach (var polygon in annotation.Polygons)
            {
                var segCoords = new List<double>();
                foreach (var pt in polygon.Points)
                {
                    segCoords.Add(Math.Round(pt.X * annotation.ImageWidth, 2));
                    segCoords.Add(Math.Round(pt.Y * annotation.ImageHeight, 2));
                }

                // 计算边界框（从多边形点推导）
                var xs = polygon.Points.Select(p => p.X * annotation.ImageWidth).ToList();
                var ys = polygon.Points.Select(p => p.Y * annotation.ImageHeight).ToList();
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

            // 转换折线为 COCO 格式 segmentation（绝对像素坐标）
            foreach (var polyline in annotation.Polylines)
            {
                var segCoords = new List<double>();
                foreach (var pt in polyline.Points)
                {
                    segCoords.Add(Math.Round(pt.X * annotation.ImageWidth, 2));
                    segCoords.Add(Math.Round(pt.Y * annotation.ImageHeight, 2));
                }

                var xs = polyline.Points.Select(p => p.X * annotation.ImageWidth).ToList();
                var ys = polyline.Points.Select(p => p.Y * annotation.ImageHeight).ToList();
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

            // 转换圆形为 COCO 格式（用外接正方形的 bbox 和圆弧 segmentation 近似）
            foreach (var circle in annotation.Circles)
            {
                var absCx = circle.CenterX * annotation.ImageWidth;
                var absCy = circle.CenterY * annotation.ImageHeight;
                var absR = circle.Radius * annotation.ImageWidth;

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

            // 复制图像
            if (File.Exists(annotation.ImagePath))
            {
                File.Copy(annotation.ImagePath, Path.Combine(imagesDir, $"{imageName}{imageExt}"), true);
            }

            // 生成 VOC XML
            var objectElements = new List<XElement>();

            // 边界框对象
            foreach (var box in annotation.Boxes)
            {
                var absXmin = (int)(box.CenterX * annotation.ImageWidth - box.Width * annotation.ImageWidth / 2);
                var absYmin = (int)(box.CenterY * annotation.ImageHeight - box.Height * annotation.ImageHeight / 2);
                var absXmax = (int)(box.CenterX * annotation.ImageWidth + box.Width * annotation.ImageWidth / 2);
                var absYmax = (int)(box.CenterY * annotation.ImageHeight + box.Height * annotation.ImageHeight / 2);

                absXmin = Math.Max(0, absXmin);
                absYmin = Math.Max(0, absYmin);
                absXmax = Math.Min(annotation.ImageWidth, absXmax);
                absYmax = Math.Min(annotation.ImageHeight, absYmax);

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

            // 多边形对象
            foreach (var polygon in annotation.Polygons)
            {
                var pointElements = new List<XElement>();
                foreach (var p in polygon.Points)
                {
                    var px = Math.Max(0, Math.Min(annotation.ImageWidth, (int)(p.X * annotation.ImageWidth)));
                    var py = Math.Max(0, Math.Min(annotation.ImageHeight, (int)(p.Y * annotation.ImageHeight)));
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

            // 旋转边界框（OBB）对象
            foreach (var obb in annotation.OrientedBoxes)
            {
                var absCx = Math.Round(obb.CenterX * annotation.ImageWidth, 2);
                var absCy = Math.Round(obb.CenterY * annotation.ImageHeight, 2);
                var absW = Math.Round(obb.Width * annotation.ImageWidth, 2);
                var absH = Math.Round(obb.Height * annotation.ImageHeight, 2);
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

            // 折线对象
            foreach (var polyline in annotation.Polylines)
            {
                var pointElements = new List<XElement>();
                foreach (var p in polyline.Points)
                {
                    var px = Math.Max(0, Math.Min(annotation.ImageWidth, (int)(p.X * annotation.ImageWidth)));
                    var py = Math.Max(0, Math.Min(annotation.ImageHeight, (int)(p.Y * annotation.ImageHeight)));
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

            // 圆形对象（用外接正方形 bndbox 近似）
            foreach (var circle in annotation.Circles)
            {
                var absCx = Math.Round(circle.CenterX * annotation.ImageWidth, 2);
                var absCy = Math.Round(circle.CenterY * annotation.ImageHeight, 2);
                var absR = Math.Round(circle.Radius * annotation.ImageWidth, 2);

                var xmin = Math.Max(0, (int)(absCx - absR));
                var ymin = Math.Max(0, (int)(absCy - absR));
                var xmax = Math.Min(annotation.ImageWidth, (int)(absCx + absR));
                var ymax = Math.Min(annotation.ImageHeight, (int)(absCy + absR));

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

        // 生成 ImageSets/Main/train.txt
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

            // 旋转边界框（OBB）: 将 center/size/angle 转换为 4 个角点
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

            // 普通边界框: 转换为轴对齐的 4 个角点
            foreach (var box in annotation.Boxes)
            {
                var absCx = box.CenterX * annotation.ImageWidth;
                var absCy = box.CenterY * annotation.ImageHeight;
                var absW = box.Width * annotation.ImageWidth;
                var absH = box.Height * annotation.ImageHeight;

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

            // 多边形: 取前 4 个点（若不足 4 点则用最后一个点填充）
            foreach (var polygon in annotation.Polygons)
            {
                if (polygon.Points.Count < 3) continue;

                var corners = new List<(float x, float y)>();
                for (int i = 0; i < 4; i++)
                {
                    var pt = polygon.Points[Math.Min(i, polygon.Points.Count - 1)];
                    corners.Add((pt.X * annotation.ImageWidth, pt.Y * annotation.ImageHeight));
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

        // 导出 classes.txt
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

        // 建立类别名称到 ID 的映射
        var classNameToId = project.Classes
            .Select((c, i) => (c.Name, Id: i))
            .ToDictionary(x => x.Name, x => x.Id);

        for (int frameId = 1; frameId <= project.Annotations.Count; frameId++)
        {
            var annotation = project.Annotations[frameId - 1];
            int trackId = 1;

            // 边界框
            foreach (var box in annotation.Boxes)
            {
                var absX = box.CenterX * annotation.ImageWidth - box.Width * annotation.ImageWidth / 2;
                var absY = box.CenterY * annotation.ImageHeight - box.Height * annotation.ImageHeight / 2;
                var absW = box.Width * annotation.ImageWidth;
                var absH = box.Height * annotation.ImageHeight;
                var classId = classNameToId.GetValueOrDefault(box.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{absX:F1},{absY:F1},{absW:F1},{absH:F1},1,{classId},1");
                trackId++;
            }

            // 多边形（用外接矩形表示）
            foreach (var polygon in annotation.Polygons)
            {
                if (polygon.Points.Count == 0) continue;

                var xs = polygon.Points.Select(p => p.X * annotation.ImageWidth).ToList();
                var ys = polygon.Points.Select(p => p.Y * annotation.ImageHeight).ToList();
                var minX = xs.Min();
                var minY = ys.Min();
                var w = xs.Max() - minX;
                var h = ys.Max() - minY;
                var classId = classNameToId.GetValueOrDefault(polygon.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{minX:F1},{minY:F1},{w:F1},{h:F1},1,{classId},1");
                trackId++;
            }

            // 旋转边界框（用外接矩形表示）
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

            // 折线（用外接矩形表示）
            foreach (var polyline in annotation.Polylines)
            {
                if (polyline.Points.Count == 0) continue;

                var xs = polyline.Points.Select(p => p.X * annotation.ImageWidth).ToList();
                var ys = polyline.Points.Select(p => p.Y * annotation.ImageHeight).ToList();
                var minX = xs.Min();
                var minY = ys.Min();
                var w = xs.Max() - minX;
                var h = ys.Max() - minY;
                var classId = classNameToId.GetValueOrDefault(polyline.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{minX:F1},{minY:F1},{w:F1},{h:F1},1,{classId},1");
                trackId++;
            }

            // 圆形（用外接正方形表示）
            foreach (var circle in annotation.Circles)
            {
                var absCx = circle.CenterX * annotation.ImageWidth;
                var absCy = circle.CenterY * annotation.ImageHeight;
                var absR = circle.Radius * annotation.ImageWidth;
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
        var absCx = centerX * imageWidth;
        var absCy = centerY * imageHeight;
        var absW = width * imageWidth;
        var absH = height * imageHeight;

        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);
        var hw = absW / 2;
        var hh = absH / 2;

        // 4 个角点（从左上角顺时针）: 先计算相对于中心的偏移，再旋转
        return (
            absCx + (-hw * cos - -hh * sin), absCy + (-hw * sin + -hh * cos),  // 左上
            absCx + (hw * cos - -hh * sin),  absCy + (hw * sin + -hh * cos),   // 右上
            absCx + (hw * cos - hh * sin),   absCy + (hw * sin + hh * cos),    // 右下
            absCx + (-hw * cos - hh * sin),  absCy + (-hw * sin + hh * cos)    // 左下
        );
    }

    /// <summary>
    /// 保存单个图像的 YOLO 格式标注文件。
    /// </summary>
    private async Task SaveYoloLabelAsync(AnnotationProject project, AnnotationData annotation)
    {
        var labelsDir = Path.Combine(project.ProjectDirectory, "labels");
        Directory.CreateDirectory(labelsDir);

        var imageName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
        var labelPath = Path.Combine(labelsDir, $"{imageName}.txt");

        var lines = ToYoloFormat(annotation);
        await File.WriteAllLinesAsync(labelPath, lines);
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

        // 复制图像
        var destImagePath = Path.Combine(imagesDir, imageName);
        if (File.Exists(annotation.ImagePath))
        {
            File.Copy(annotation.ImagePath, destImagePath, true);
        }

        // 保存标注文件
        var destLabelPath = Path.Combine(labelsDir, labelName);
        var lines = ToYoloFormat(annotation);
        await File.WriteAllLinesAsync(destLabelPath, lines);
    }
}
