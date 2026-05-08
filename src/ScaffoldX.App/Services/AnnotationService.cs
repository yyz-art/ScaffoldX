using System.IO;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注服务门面实现，组合 <see cref="IAnnotationRepository"/> 和 <see cref="IAnnotationExporter"/>。
/// <para>
/// 向后兼容——现有代码可继续注入 <see cref="IAnnotationService"/>。
/// 新代码应优先注入更细粒度的 <see cref="IAnnotationRepository"/> 或 <see cref="IAnnotationExporter"/>。
/// </para>
/// </summary>
public class AnnotationService : IAnnotationService
{
    private readonly IAnnotationRepository _repository;
    private readonly IAnnotationExporter _exporter;
    private readonly ILogger _logger = Log.ForContext<AnnotationService>();

    /// <summary>
    /// 使用默认的 <see cref="AnnotationRepository"/> 和 <see cref="AnnotationExporter"/> 初始化门面。
    /// 便于测试和简单场景。
    /// </summary>
    public AnnotationService() : this(new AnnotationRepository(), new AnnotationExporter())
    {
    }

    /// <summary>
    /// 初始化标注服务门面。
    /// </summary>
    /// <param name="repository">仓储实现。</param>
    /// <param name="exporter">导出实现。</param>
    public AnnotationService(IAnnotationRepository repository, IAnnotationExporter exporter)
    {
        _repository = repository;
        _exporter = exporter;
    }

    // ── IAnnotationRepository 委托 ──────────────────────────────────────────

    /// <inheritdoc/>
    public Task<AnnotationProject> CreateProjectAsync(string projectName, string projectDirectory, List<AnnotationClass> classes)
        => _repository.CreateProjectAsync(projectName, projectDirectory, classes);

    /// <inheritdoc/>
    public Task<AnnotationProject> LoadProjectAsync(string projectFilePath)
        => _repository.LoadProjectAsync(projectFilePath);

    /// <inheritdoc/>
    public Task SaveProjectAsync(AnnotationProject project)
        => _repository.SaveProjectAsync(project);

    /// <inheritdoc/>
    public Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths)
        => _repository.AddImagesAsync(project, imagePaths);

    /// <inheritdoc/>
    /// <remarks>
    /// 更新标注数据后自动保存对应的 YOLO 格式标注文件（跨切面关注点）。
    /// </remarks>
    public async Task UpdateAnnotationAsync(AnnotationProject project, AnnotationData annotation)
    {
        await _repository.UpdateAnnotationAsync(project, annotation);
        await SaveYoloLabelAsync(project, annotation);
    }

    // ── IAnnotationExporter 委托 ────────────────────────────────────────────

    /// <inheritdoc/>
    public List<string> ToYoloFormat(AnnotationData annotation)
        => _exporter.ToYoloFormat(annotation);

    /// <inheritdoc/>
    public (List<BoundingBoxAnnotation> Boxes, List<PolygonAnnotation> Polygons, List<PolylineAnnotation> Polylines, List<CircleAnnotation> Circles, List<OrientedBoundingBoxAnnotation> OrientedBoxes) FromYoloFormat(
        IEnumerable<string> lines, int imageWidth, int imageHeight, List<string> classNames)
        => _exporter.FromYoloFormat(lines, imageWidth, imageHeight, classNames);

    /// <inheritdoc/>
    public Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8)
        => _exporter.ExportYoloDatasetAsync(project, outputPath, trainValSplit);

    /// <inheritdoc/>
    public Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath)
        => _exporter.ExportCocoDatasetAsync(project, outputPath);

    /// <inheritdoc/>
    public Task ExportVocDatasetAsync(AnnotationProject project, string outputPath)
        => _exporter.ExportVocDatasetAsync(project, outputPath);

    /// <inheritdoc/>
    public Task ExportDotDatasetAsync(AnnotationProject project, string outputPath)
        => _exporter.ExportDotDatasetAsync(project, outputPath);

    /// <inheritdoc/>
    public Task ExportMotDatasetAsync(AnnotationProject project, string outputPath)
        => _exporter.ExportMotDatasetAsync(project, outputPath);

    // ── 跨切面：标注更新时自动保存 YOLO 标签 ───────────────────────────────

    /// <summary>
    /// 保存单个图像的 YOLO 格式标注文件。
    /// </summary>
    private async Task SaveYoloLabelAsync(AnnotationProject project, AnnotationData annotation)
    {
        var labelsDir = Path.Combine(project.ProjectDirectory, "labels");
        Directory.CreateDirectory(labelsDir);

        var imageName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
        var labelPath = Path.Combine(labelsDir, $"{imageName}.txt");

        var lines = _exporter.ToYoloFormat(annotation);
        await File.WriteAllLinesAsync(labelPath, lines);
    }
}
