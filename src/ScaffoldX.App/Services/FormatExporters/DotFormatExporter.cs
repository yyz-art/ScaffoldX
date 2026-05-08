using System.IO;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services.FormatExporters;

/// <summary>
/// DOTA 格式导出器。
/// </summary>
internal static class DotFormatExporter
{
    private static readonly ILogger Logger = Log.ForContext("Class", nameof(DotFormatExporter));

    /// <summary>
    /// 导出 DOTA 格式数据集。
    /// </summary>
    internal static async Task ExportDotDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        Directory.CreateDirectory(outputPath);

        foreach (var annotation in project.Annotations)
        {
            var imageName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
            var lines = new List<string>();

            foreach (var obb in annotation.OrientedBoxes)
            {
                var corners = FormatExporterHelper.ComputeObbCorners(
                    obb.CenterX, obb.CenterY, obb.Width, obb.Height, obb.Angle,
                    annotation.ImageWidth, annotation.ImageHeight);

                lines.Add($"{corners.x1:F1} {corners.y1:F1} {corners.x2:F1} {corners.y2:F1} " +
                          $"{corners.x3:F1} {corners.y3:F1} {corners.x4:F1} {corners.y4:F1} " +
                          $"{obb.ClassName} 1.0");
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

        Logger.Information("导出 DOTA 数据集完成: {Count} 张图像", project.Annotations.Count);
    }
}
