using ScaffoldX.App.Models;
using ScaffoldX.App.Services.FormatExporters;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注数据导出门面实现，委托到各格式专用导出器。
/// 遵循 ISP 原则，仅暴露导出操作。使用 <see cref="CoordinateMapper"/> 进行坐标变换。
/// </summary>
public class AnnotationExporter : IAnnotationExporter
{
    /// <inheritdoc/>
    public List<string> ToYoloFormat(AnnotationData annotation)
        => YoloFormatExporter.ToYoloFormat(annotation);

    /// <inheritdoc/>
    public (List<BoundingBoxAnnotation> Boxes, List<PolygonAnnotation> Polygons, List<PolylineAnnotation> Polylines, List<CircleAnnotation> Circles, List<OrientedBoundingBoxAnnotation> OrientedBoxes) FromYoloFormat(
        IEnumerable<string> lines, int imageWidth, int imageHeight, List<string> classNames)
        => YoloFormatExporter.FromYoloFormat(lines, imageWidth, imageHeight, classNames);

    /// <inheritdoc/>
    public Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8)
        => YoloFormatExporter.ExportYoloDatasetAsync(project, outputPath, trainValSplit);

    /// <inheritdoc/>
    public Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath)
        => CocoFormatExporter.ExportCocoDatasetAsync(project, outputPath);

    /// <inheritdoc/>
    public Task ExportVocDatasetAsync(AnnotationProject project, string outputPath)
        => VocFormatExporter.ExportVocDatasetAsync(project, outputPath);

    /// <inheritdoc/>
    public Task ExportDotDatasetAsync(AnnotationProject project, string outputPath)
        => DotFormatExporter.ExportDotDatasetAsync(project, outputPath);

    /// <inheritdoc/>
    public Task ExportMotDatasetAsync(AnnotationProject project, string outputPath)
        => MotFormatExporter.ExportMotDatasetAsync(project, outputPath);
}
