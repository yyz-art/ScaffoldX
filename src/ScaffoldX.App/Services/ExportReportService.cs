using System.IO;
using System.Text;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// Generates an HTML statistics report for an annotation project, including per-class
/// distribution and export format details.
/// </summary>
public interface IExportReportService
{
    /// <summary>
    /// Generates an HTML report and writes it to the specified export path.
    /// </summary>
    /// <param name="project">The annotation project to report on.</param>
    /// <param name="exportPath">The directory where the report HTML file will be saved.</param>
    /// <returns>The full path to the generated HTML report file.</returns>
    Task<string> GenerateReportAsync(AnnotationProject project, string exportPath);
}

/// <summary>
/// Generates a self-contained HTML report summarizing annotation project statistics,
/// including image counts, annotation type breakdowns, and per-class distribution.
/// </summary>
public class ExportReportService : IExportReportService
{
    private readonly ILogger _logger = Log.ForContext<ExportReportService>();

    /// <inheritdoc />
    public async Task<string> GenerateReportAsync(AnnotationProject project, string exportPath)
    {
        Directory.CreateDirectory(exportPath);

        var html = BuildHtmlReport(project);
        var filePath = Path.Combine(exportPath, $"{SanitizeFileName(project.ProjectName)}_report.html");

        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);

        _logger.Information("已生成导出报告: {FilePath}", filePath);
        return filePath;
    }

    /// <summary>
    /// Builds the complete HTML report string from the annotation project data.
    /// </summary>
    private static string BuildHtmlReport(AnnotationProject project)
    {
        int totalImages = project.Annotations.Count;
        int totalBoxes = project.Annotations.Sum(a => a.Boxes.Count);
        int totalPolygons = project.Annotations.Sum(a => a.Polygons.Count);
        int totalOrientedBoxes = project.Annotations.Sum(a => a.OrientedBoxes.Count);
        int totalAnnotations = totalBoxes + totalPolygons + totalOrientedBoxes;

        var classDistribution = ComputeClassDistribution(project);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"  <title>{EscapeHtml(project.ProjectName)} - 标注统计报告</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: 'Segoe UI', sans-serif; margin: 2rem; background: #f5f5f5; color: #333; }");
        sb.AppendLine("    .container { max-width: 900px; margin: 0 auto; background: #fff; padding: 2rem; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }");
        sb.AppendLine("    h1 { color: #1976d2; border-bottom: 2px solid #1976d2; padding-bottom: 0.5rem; }");
        sb.AppendLine("    h2 { color: #424242; margin-top: 1.5rem; }");
        sb.AppendLine("    .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin: 1rem 0; }");
        sb.AppendLine("    .card { background: #e3f2fd; padding: 1rem; border-radius: 6px; text-align: center; }");
        sb.AppendLine("    .card .value { font-size: 2rem; font-weight: bold; color: #1565c0; }");
        sb.AppendLine("    .card .label { font-size: 0.9rem; color: #616161; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 1rem; }");
        sb.AppendLine("    th, td { padding: 0.6rem 1rem; text-align: left; border-bottom: 1px solid #e0e0e0; }");
        sb.AppendLine("    th { background: #fafafa; font-weight: 600; }");
        sb.AppendLine("    tr:hover { background: #f5f5f5; }");
        sb.AppendLine("    .footer { margin-top: 2rem; font-size: 0.85rem; color: #9e9e9e; text-align: center; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"container\">");

        sb.AppendLine($"  <h1>{EscapeHtml(project.ProjectName)} - 标注统计报告</h1>");
        sb.AppendLine($"  <p>导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

        sb.AppendLine("  <h2>总览</h2>");
        sb.AppendLine("  <div class=\"summary\">");
        AppendCard(sb, totalImages, "图像总数");
        AppendCard(sb, totalAnnotations, "标注总数");
        AppendCard(sb, totalBoxes, "边界框");
        AppendCard(sb, totalPolygons, "多边形");
        AppendCard(sb, totalOrientedBoxes, "旋转框");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <h2>类别分布</h2>");
        sb.AppendLine("  <table>");
        sb.AppendLine("    <thead><tr><th>类别</th><th>边界框</th><th>多边形</th><th>旋转框</th><th>合计</th></tr></thead>");
        sb.AppendLine("    <tbody>");
        foreach (var (className, boxes, polygons, oriented) in classDistribution)
        {
            sb.AppendLine($"    <tr><td>{EscapeHtml(className)}</td><td>{boxes}</td><td>{polygons}</td><td>{oriented}</td><td>{boxes + polygons + oriented}</td></tr>");
        }
        sb.AppendLine("    </tbody>");
        sb.AppendLine("  </table>");

        sb.AppendLine("  <h2>项目信息</h2>");
        sb.AppendLine("  <table>");
        sb.AppendLine($"    <tr><td>项目名称</td><td>{EscapeHtml(project.ProjectName)}</td></tr>");
        sb.AppendLine($"    <tr><td>项目目录</td><td>{EscapeHtml(project.ProjectDirectory)}</td></tr>");
        sb.AppendLine($"    <tr><td>类别数量</td><td>{project.Classes.Count}</td></tr>");
        sb.AppendLine($"    <tr><td>创建时间</td><td>{project.CreatedAt:yyyy-MM-dd HH:mm:ss}</td></tr>");
        sb.AppendLine($"    <tr><td>最后修改</td><td>{project.ModifiedAt:yyyy-MM-dd HH:mm:ss}</td></tr>");
        sb.AppendLine("  </table>");

        sb.AppendLine("  <div class=\"footer\">由 ScaffoldX 标注工具自动生成</div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Computes per-class annotation counts across all annotation types.
    /// </summary>
    private static List<(string ClassName, int Boxes, int Polygons, int OrientedBoxes)> ComputeClassDistribution(
        AnnotationProject project)
    {
        var dict = new Dictionary<string, (int Boxes, int Polygons, int Oriented)>();

        foreach (var annotation in project.Annotations)
        {
            foreach (var box in annotation.Boxes)
            {
                var key = string.IsNullOrEmpty(box.ClassName) ? $"class_{box.ClassIndex}" : box.ClassName;
                var current = dict.GetValueOrDefault(key);
                dict[key] = (current.Boxes + 1, current.Polygons, current.Oriented);
            }

            foreach (var polygon in annotation.Polygons)
            {
                var key = string.IsNullOrEmpty(polygon.ClassName) ? $"class_{polygon.ClassIndex}" : polygon.ClassName;
                var current = dict.GetValueOrDefault(key);
                dict[key] = (current.Boxes, current.Polygons + 1, current.Oriented);
            }

            foreach (var obb in annotation.OrientedBoxes)
            {
                var key = string.IsNullOrEmpty(obb.ClassName) ? $"class_{obb.ClassIndex}" : obb.ClassName;
                var current = dict.GetValueOrDefault(key);
                dict[key] = (current.Boxes, current.Polygons, current.Oriented + 1);
            }
        }

        return dict
            .OrderBy(kv => kv.Key)
            .Select(kv => (kv.Key, kv.Value.Boxes, kv.Value.Polygons, kv.Value.Oriented))
            .ToList();
    }

    /// <summary>
    /// Appends a summary card element to the HTML builder.
    /// </summary>
    private static void AppendCard(StringBuilder sb, int value, string label)
    {
        sb.AppendLine($"    <div class=\"card\"><div class=\"value\">{value}</div><div class=\"label\">{label}</div></div>");
    }

    /// <summary>
    /// Escapes HTML special characters to prevent injection.
    /// </summary>
    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>
    /// Removes invalid file name characters from a project name.
    /// </summary>
    private static string SanitizeFileName(string name) =>
        string.Concat(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
}
