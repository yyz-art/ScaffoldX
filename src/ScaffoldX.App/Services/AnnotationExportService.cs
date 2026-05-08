using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// Orchestrates export operations for annotation projects across multiple formats
/// (YOLO, COCO, Pascal VOC). Extracted from AnnotationViewModel to separate
/// export coordination from UI state management.
/// </summary>
public class AnnotationExportService
{
    private readonly IAnnotationExporter _annotationExporter;
    private readonly ILogger _logger = Log.ForContext<AnnotationExportService>();

    /// <summary>
    /// Initializes the export service with the annotation exporter.
    /// </summary>
    /// <param name="annotationExporter">The annotation exporter that performs the actual export.</param>
    public AnnotationExportService(IAnnotationExporter annotationExporter)
    {
        _annotationExporter = annotationExporter;
    }

    /// <summary>
    /// Validates that the project has exportable annotations.
    /// </summary>
    /// <param name="project">The annotation project to validate.</param>
    /// <returns>True if the project has annotations to export.</returns>
    public static bool CanExport(AnnotationProject? project)
        => project is { Annotations.Count: > 0 };

    /// <summary>
    /// Exports the project in YOLO format to the specified directory.
    /// </summary>
    /// <param name="project">The annotation project.</param>
    /// <param name="outputDirectory">The target output directory.</param>
    /// <returns>True if export succeeded, false otherwise.</returns>
    public async Task<bool> ExportYoloAsync(AnnotationProject project, string outputDirectory)
    {
        try
        {
            await _annotationExporter.ExportYoloDatasetAsync(project, outputDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 YOLO 数据集失败");
            return false;
        }
    }

    /// <summary>
    /// Exports the project in COCO JSON format to the specified directory.
    /// </summary>
    /// <param name="project">The annotation project.</param>
    /// <param name="outputDirectory">The target output directory.</param>
    /// <returns>True if export succeeded, false otherwise.</returns>
    public async Task<bool> ExportCocoAsync(AnnotationProject project, string outputDirectory)
    {
        try
        {
            await _annotationExporter.ExportCocoDatasetAsync(project, outputDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 COCO 数据集失败");
            return false;
        }
    }

    /// <summary>
    /// Exports the project in Pascal VOC XML format to the specified directory.
    /// </summary>
    /// <param name="project">The annotation project.</param>
    /// <param name="outputDirectory">The target output directory.</param>
    /// <returns>True if export succeeded, false otherwise.</returns>
    public async Task<bool> ExportVocAsync(AnnotationProject project, string outputDirectory)
    {
        try
        {
            await _annotationExporter.ExportVocDatasetAsync(project, outputDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 VOC 数据集失败");
            return false;
        }
    }

    /// <summary>
    /// Exports the project in DOTA format (oriented bounding boxes) to the specified directory.
    /// </summary>
    /// <param name="project">The annotation project.</param>
    /// <param name="outputDirectory">The target output directory.</param>
    /// <returns>True if export succeeded, false otherwise.</returns>
    public async Task<bool> ExportDotAsync(AnnotationProject project, string outputDirectory)
    {
        try
        {
            await _annotationExporter.ExportDotDatasetAsync(project, outputDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 DOTA 数据集失败");
            return false;
        }
    }

    /// <summary>
    /// Exports the project in MOT Challenge format (multi-object tracking) to the specified directory.
    /// </summary>
    /// <param name="project">The annotation project.</param>
    /// <param name="outputDirectory">The target output directory.</param>
    /// <returns>True if export succeeded, false otherwise.</returns>
    public async Task<bool> ExportMotAsync(AnnotationProject project, string outputDirectory)
    {
        try
        {
            await _annotationExporter.ExportMotDatasetAsync(project, outputDirectory);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 MOT 数据集失败");
            return false;
        }
    }
}
