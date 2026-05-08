using System.IO;
using System.Text.Json;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services.FormatExporters;

/// <summary>
/// COCO JSON 格式导出器。
/// </summary>
internal static class CocoFormatExporter
{
    private static readonly ILogger Logger = Log.ForContext("Class", nameof(CocoFormatExporter));

    /// <summary>
    /// 导出 COCO JSON 格式数据集。
    /// </summary>
    internal static async Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        Directory.CreateDirectory(outputPath);

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

            foreach (var box in annotation.Boxes)
            {
                var (absX, absY, absW, absH) = CoordinateMapper.ToAbsoluteBbox(
                    box.CenterX, box.CenterY, box.Width, box.Height,
                    annotation.ImageWidth, annotation.ImageHeight);

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = box.ClassIndex + 1,
                    bbox = new[] { Math.Round(absX, 2), Math.Round(absY, 2), Math.Round(absW, 2), Math.Round(absH, 2) },
                    area = Math.Round(absW * absH, 2),
                    segmentation = Array.Empty<double[]>(),
                    iscrowd = 0
                });
            }

            foreach (var polygon in annotation.Polygons)
            {
                var segCoords = new List<double>();
                foreach (var pt in polygon.Points)
                {
                    var (ptAbsX, ptAbsY) = CoordinateMapper.ToAbsolute(pt.X, pt.Y, annotation.ImageWidth, annotation.ImageHeight);
                    segCoords.Add(Math.Round(ptAbsX, 2));
                    segCoords.Add(Math.Round(ptAbsY, 2));
                }

                var xs = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polygon.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var polyAbsX = xs.Min();
                var polyAbsY = ys.Min();
                var polyAbsW = xs.Max() - xs.Min();
                var polyAbsH = ys.Max() - ys.Min();

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = polygon.ClassIndex + 1,
                    bbox = new[] { Math.Round(polyAbsX, 2), Math.Round(polyAbsY, 2), Math.Round(polyAbsW, 2), Math.Round(polyAbsH, 2) },
                    area = Math.Round(polyAbsW * polyAbsH, 2),
                    segmentation = new[] { segCoords.ToArray() },
                    iscrowd = 0
                });
            }

            foreach (var polyline in annotation.Polylines)
            {
                var segCoords = new List<double>();
                foreach (var pt in polyline.Points)
                {
                    var (ptAbsX, ptAbsY) = CoordinateMapper.ToAbsolute(pt.X, pt.Y, annotation.ImageWidth, annotation.ImageHeight);
                    segCoords.Add(Math.Round(ptAbsX, 2));
                    segCoords.Add(Math.Round(ptAbsY, 2));
                }

                var xs = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth)).ToList();
                var ys = polyline.Points.Select(p => CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight)).ToList();
                var plAbsX = xs.Min();
                var plAbsY = ys.Min();
                var plAbsW = xs.Max() - xs.Min();
                var plAbsH = ys.Max() - ys.Min();

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = polyline.ClassIndex + 1,
                    bbox = new[] { Math.Round(plAbsX, 2), Math.Round(plAbsY, 2), Math.Round(plAbsW, 2), Math.Round(plAbsH, 2) },
                    area = Math.Round(plAbsW * plAbsH, 2),
                    segmentation = new[] { segCoords.ToArray() },
                    iscrowd = 0
                });
            }

            foreach (var circle in annotation.Circles)
            {
                var absCx = CoordinateMapper.ToAbsolute(circle.CenterX, annotation.ImageWidth);
                var absCy = CoordinateMapper.ToAbsolute(circle.CenterY, annotation.ImageHeight);
                var absR = CoordinateMapper.ToAbsolute(circle.Radius, annotation.ImageWidth);

                annotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId + 1,
                    category_id = circle.ClassIndex + 1,
                    bbox = new[] { Math.Round(absCx - absR, 2), Math.Round(absCy - absR, 2), Math.Round(absR * 2, 2), Math.Round(absR * 2, 2) },
                    area = Math.Round(Math.PI * absR * absR, 2),
                    segmentation = Array.Empty<double[]>(),
                    iscrowd = 0
                });
            }
        }

        var cocoJson = new { images, annotations, categories };
        var json = JsonSerializer.Serialize(cocoJson, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputPath, "annotations.json"), json);

        Logger.Information("导出 COCO 数据集完成: {ImageCount} 张图像, {AnnotationCount} 个标注",
            images.Count, annotations.Count);
    }
}
