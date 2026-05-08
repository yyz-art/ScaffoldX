using System.IO;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services.FormatExporters;

/// <summary>
/// MOT Challenge 格式导出器。
/// </summary>
internal static class MotFormatExporter
{
    private static readonly ILogger Logger = Log.ForContext("Class", nameof(MotFormatExporter));

    /// <summary>
    /// 导出 MOT Challenge 格式数据集。
    /// </summary>
    internal static async Task ExportMotDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        Directory.CreateDirectory(outputPath);

        var gtLines = new List<string>();

        var classNameToId = project.Classes
            .Select((c, i) => (c.Name, Id: i))
            .ToDictionary(x => x.Name, x => x.Id);

        for (int frameId = 1; frameId <= project.Annotations.Count; frameId++)
        {
            var annotation = project.Annotations[frameId - 1];
            int trackId = 1;

            foreach (var box in annotation.Boxes)
            {
                var (mAbsX, mAbsY, mAbsW, mAbsH) = CoordinateMapper.ToAbsoluteBbox(
                    box.CenterX, box.CenterY, box.Width, box.Height,
                    annotation.ImageWidth, annotation.ImageHeight);
                var classId = classNameToId.GetValueOrDefault(box.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{mAbsX:F1},{mAbsY:F1},{mAbsW:F1},{mAbsH:F1},1,{classId},1");
                trackId++;
            }

            foreach (var polygon in annotation.Polygons)
            {
                if (polygon.Points.Count == 0) continue;

                var xs = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var minX = xs.Min();
                var minY = ys.Min();
                var w = xs.Max() - minX;
                var h = ys.Max() - minY;
                var classId = classNameToId.GetValueOrDefault(polygon.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{minX:F1},{minY:F1},{w:F1},{h:F1},1,{classId},1");
                trackId++;
            }

            foreach (var obb in annotation.OrientedBoxes)
            {
                var corners = FormatExporterHelper.ComputeObbCorners(
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

            foreach (var polyline in annotation.Polylines)
            {
                if (polyline.Points.Count == 0) continue;

                var xs = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var minX = xs.Min();
                var minY = ys.Min();
                var w = xs.Max() - minX;
                var h = ys.Max() - minY;
                var classId = classNameToId.GetValueOrDefault(polyline.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{minX:F1},{minY:F1},{w:F1},{h:F1},1,{classId},1");
                trackId++;
            }

            foreach (var circle in annotation.Circles)
            {
                var absCx = CoordinateMapper.ToAbsolute(circle.CenterX, annotation.ImageWidth);
                var absCy = CoordinateMapper.ToAbsolute(circle.CenterY, annotation.ImageHeight);
                var absR = CoordinateMapper.ToAbsolute(circle.Radius, annotation.ImageWidth);
                var classId = classNameToId.GetValueOrDefault(circle.ClassName, 0);

                gtLines.Add($"{frameId},{trackId},{absCx - absR:F1},{absCy - absR:F1},{absR * 2:F1},{absR * 2:F1},1,{classId},1");
                trackId++;
            }
        }

        await File.WriteAllLinesAsync(Path.Combine(outputPath, "gt.txt"), gtLines);

        Logger.Information("导出 MOT 数据集完成: {FrameCount} 帧, {LineCount} 条记录",
            project.Annotations.Count, gtLines.Count);
    }
}
