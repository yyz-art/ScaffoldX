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
            Boxes = new List<BoundingBoxAnnotation>()
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
        return annotation.Boxes.Select(box =>
            $"{box.ClassIndex} {box.CenterX:F6} {box.CenterY:F6} {box.Width:F6} {box.Height:F6}"
        ).ToList();
    }

    /// <inheritdoc/>
    public List<BoundingBoxAnnotation> FromYoloFormat(
        IEnumerable<string> lines,
        int imageWidth,
        int imageHeight,
        List<string> classNames)
    {
        var boxes = new List<BoundingBoxAnnotation>();

        foreach (var line in lines)
        {
            var parts = line.Trim().Split(' ');
            if (parts.Length != 5)
                continue;

            if (!int.TryParse(parts[0], out var classIndex))
                continue;

            if (!double.TryParse(parts[1], out var centerX))
                continue;

            if (!double.TryParse(parts[2], out var centerY))
                continue;

            if (!double.TryParse(parts[3], out var width))
                continue;

            if (!double.TryParse(parts[4], out var height))
                continue;

            var className = classIndex < classNames.Count ? classNames[classIndex] : $"class_{classIndex}";

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

        return boxes;
    }

    /// <inheritdoc/>
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
            var xml = new XDocument(
                new XElement("annotation",
                    new XElement("folder", Path.GetFileName(outputPath)),
                    new XElement("filename", $"{imageName}{imageExt}"),
                    new XElement("size",
                        new XElement("width", annotation.ImageWidth),
                        new XElement("height", annotation.ImageHeight),
                        new XElement("depth", 3)),
                    new XElement("segmented", 0),
                    annotation.Boxes.Select(box =>
                    {
                        var absXmin = (int)(box.CenterX * annotation.ImageWidth - box.Width * annotation.ImageWidth / 2);
                        var absYmin = (int)(box.CenterY * annotation.ImageHeight - box.Height * annotation.ImageHeight / 2);
                        var absXmax = (int)(box.CenterX * annotation.ImageWidth + box.Width * annotation.ImageWidth / 2);
                        var absYmax = (int)(box.CenterY * annotation.ImageHeight + box.Height * annotation.ImageHeight / 2);

                        absXmin = Math.Max(0, absXmin);
                        absYmin = Math.Max(0, absYmin);
                        absXmax = Math.Min(annotation.ImageWidth, absXmax);
                        absYmax = Math.Min(annotation.ImageHeight, absYmax);

                        return new XElement("object",
                            new XElement("name", box.ClassName),
                            new XElement("pose", "Unspecified"),
                            new XElement("truncated", 0),
                            new XElement("difficult", 0),
                            new XElement("bndbox",
                                new XElement("xmin", absXmin),
                                new XElement("ymin", absYmin),
                                new XElement("xmax", absXmax),
                                new XElement("ymax", absYmax)));
                    })));

            await File.WriteAllTextAsync(
                Path.Combine(annotationsDir, $"{imageName}.xml"),
                xml.ToString());
        }

        // 生成 ImageSets/Main/train.txt
        var trainTxt = string.Join("\n", imageNames);
        await File.WriteAllTextAsync(Path.Combine(imageSetsDir, "train.txt"), trainTxt);

        _logger.Information("导出 Pascal VOC 数据集完成: {Count} 张图像", imageNames.Count);
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
