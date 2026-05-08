using System.Drawing;
using System.IO;
using System.Text.Json;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注项目的数据访问实现，负责项目的 CRUD 和图像管理。
/// 遵循 ISP 原则，仅暴露仓储操作。
/// </summary>
public class AnnotationRepository : IAnnotationRepository
{
    private readonly ILogger _logger = Log.ForContext<AnnotationRepository>();

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
    public async Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths)
    {
        foreach (var imagePath in imagePaths)
        {
            await AddImageInternalAsync(project, imagePath);
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

        _logger.Debug("更新标注: {ImagePath}, 边界框数: {BoxCount}",
            annotation.ImagePath, annotation.Boxes.Count);
    }

    /// <summary>
    /// 添加单个图像到项目（内部方法）。
    /// </summary>
    private async Task AddImageInternalAsync(AnnotationProject project, string imagePath)
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
}
