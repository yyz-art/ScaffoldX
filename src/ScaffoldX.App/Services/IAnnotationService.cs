using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注服务接口，提供标注项目的创建、保存、加载和导出功能。
/// </summary>
public interface IAnnotationService
{
    /// <summary>
    /// 创建新的标注项目。
    /// </summary>
    /// <param name="projectName">项目名称。</param>
    /// <param name="projectDirectory">项目目录。</param>
    /// <param name="classes">类别定义列表。</param>
    /// <returns>创建的标注项目。</returns>
    Task<AnnotationProject> CreateProjectAsync(string projectName, string projectDirectory, List<AnnotationClass> classes);

    /// <summary>
    /// 加载已有的标注项目。
    /// </summary>
    /// <param name="projectFilePath">项目文件路径（.scaffoldx-annotation.json）。</param>
    /// <returns>加载的标注项目。</returns>
    Task<AnnotationProject> LoadProjectAsync(string projectFilePath);

    /// <summary>
    /// 保存标注项目到文件。
    /// </summary>
    /// <param name="project">要保存的标注项目。</param>
    Task SaveProjectAsync(AnnotationProject project);

    /// <summary>
    /// 添加图像到项目。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="imagePath">图像文件路径。</param>
    Task AddImageAsync(AnnotationProject project, string imagePath);

    /// <summary>
    /// 批量添加图像到项目。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="imagePaths">图像文件路径列表。</param>
    Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths);

    /// <summary>
    /// 更新图像的标注数据。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="annotation">标注数据。</param>
    Task UpdateAnnotationAsync(AnnotationProject project, AnnotationData annotation);

    /// <summary>
    /// 导出项目为 YOLO 格式的数据集。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="outputPath">输出目录。</param>
    /// <param name="trainValSplit">训练集/验证集比例（0-1，如 0.8 表示 80% 训练）。</param>
    Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8);

    /// <summary>
    /// 导出项目为 COCO JSON 格式的数据集。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="outputPath">输出目录。</param>
    Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>
    /// 导出项目为 Pascal VOC XML 格式的数据集。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="outputPath">输出目录。</param>
    Task ExportVocDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>
    /// 导出项目为 DOTA 格式的数据集（面向航拍/旋转目标检测）。
    /// 每个图像生成一个 .txt 文件，每行格式: x1 y1 x2 y2 x3 y3 x4 y4 class_name confidence。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="outputPath">输出目录。</param>
    Task ExportDotDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>
    /// 导出项目为 MOT Challenge 格式的数据集（多目标跟踪）。
    /// 生成单个 gt.txt 文件，每行格式: frame_id,track_id,x,y,w,h,confidence,class_id,visibility。
    /// </summary>
    /// <param name="project">标注项目。</param>
    /// <param name="outputPath">输出目录。</param>
    Task ExportMotDatasetAsync(AnnotationProject project, string outputPath);

    /// <summary>
    /// 将标注数据导出为 YOLO 格式的文本行。
    /// </summary>
    /// <param name="annotation">标注数据。</param>
    /// <returns>YOLO 格式的文本行列表（每行一个边界框）。</returns>
    List<string> ToYoloFormat(AnnotationData annotation);

    /// <summary>
    /// 从 YOLO 格式文本行解析标注数据，同时支持边界框、多边形、折线、圆形和旋转边界框（OBB）格式。
    /// 边界框格式: class_index center_x center_y width height（5 个数值）
    /// OBB 格式: class_index center_x center_y width height angle（7 个数值，含类别索引）
    /// 多边形格式: class_index x1 y1 x2 y2 ... xn yn（超过 7 个数值或偶数个非 OBB 值）
    /// 圆形格式: class_index center_x center_y radius（4 个数值）
    /// </summary>
    /// <param name="lines">YOLO 格式的文本行。</param>
    /// <param name="imageWidth">图像宽度。</param>
    /// <param name="imageHeight">图像高度。</param>
    /// <param name="classNames">类别名称列表。</param>
    /// <returns>包含边界框、多边形、折线、圆形和 OBB 标注的元组。</returns>
    (List<BoundingBoxAnnotation> Boxes, List<PolygonAnnotation> Polygons, List<PolylineAnnotation> Polylines, List<CircleAnnotation> Circles, List<OrientedBoundingBoxAnnotation> OrientedBoxes) FromYoloFormat(IEnumerable<string> lines, int imageWidth, int imageHeight, List<string> classNames);
}
