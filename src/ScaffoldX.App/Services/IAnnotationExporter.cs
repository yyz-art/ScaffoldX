using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注数据导出接口，负责将标注项目导出为各种训练框架格式。
/// </summary>
public interface IAnnotationExporter
{
    /// <summary>导出为 YOLO 格式数据集。</summary>
    Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8);

    /// <summary>导出为 COCO JSON 格式数据集。</summary>
    Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>导出为 Pascal VOC XML 格式数据集。</summary>
    Task ExportVocDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>导出为 DOTA 格式数据集。</summary>
    Task ExportDotDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>导出为 MOT Challenge 格式数据集。</summary>
    Task ExportMotDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>将标注数据导出为 YOLO 格式的文本行。</summary>
    List<string> ToYoloFormat(AnnotationData annotation);

    /// <summary>从 YOLO 格式文本行解析标注数据。</summary>
    (List<BoundingBoxAnnotation> Boxes, List<PolygonAnnotation> Polygons, List<PolylineAnnotation> Polylines, List<CircleAnnotation> Circles, List<OrientedBoundingBoxAnnotation> OrientedBoxes) FromYoloFormat(IEnumerable<string> lines, int imageWidth, int imageHeight, List<string> classNames);
}
