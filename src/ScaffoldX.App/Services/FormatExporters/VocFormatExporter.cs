using System.IO;
using System.Xml.Linq;
using ScaffoldX.App.Constants;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services.FormatExporters;

/// <summary>
/// Pascal VOC XML 格式导出器。
/// </summary>
internal static class VocFormatExporter
{
    private static readonly ILogger Logger = Log.ForContext("Class", nameof(VocFormatExporter));

    /// <summary>
    /// 导出 Pascal VOC XML 格式数据集。
    /// </summary>
    internal static async Task ExportVocDatasetAsync(AnnotationProject project, string outputPath)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));

        var imagesDir = Path.Combine(outputPath, "JPEGImages");
        var annotationsDir = Path.Combine(outputPath, "Annotations");
        var imageSetsDir = Path.Combine(outputPath, "ImageSets", "Main");
        Directory.CreateDirectory(imagesDir);
        Directory.CreateDirectory(annotationsDir);
        Directory.CreateDirectory(imageSetsDir);

        var imageNames = new List<string>();

        foreach (var annotation in project.Annotations)
        {
            var imageName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
            var imageExt = Path.GetExtension(annotation.ImagePath);
            imageNames.Add(imageName);

            if (File.Exists(annotation.ImagePath))
            {
                File.Copy(annotation.ImagePath, Path.Combine(imagesDir, $"{imageName}{imageExt}"), true);
            }

            var objectElements = BuildObjectElements(annotation);

            var xml = new XDocument(
                new XElement("annotation",
                    new XElement("folder", Path.GetFileName(outputPath)),
                    new XElement("filename", $"{imageName}{imageExt}"),
                    new XElement("size",
                        new XElement("width", annotation.ImageWidth),
                        new XElement("height", annotation.ImageHeight),
                        new XElement("depth", 3)),
                    new XElement("segmented", 0),
                    objectElements));

            await File.WriteAllTextAsync(
                Path.Combine(annotationsDir, $"{imageName}.xml"),
                xml.ToString());
        }

        var trainTxt = string.Join("\n", imageNames);
        await File.WriteAllTextAsync(Path.Combine(imageSetsDir, "train.txt"), trainTxt);

        Logger.Information("导出 Pascal VOC 数据集完成: {Count} 张图像", imageNames.Count);
    }

    private static List<XElement> BuildObjectElements(AnnotationData annotation)
    {
        var objectElements = new List<XElement>();

        foreach (var box in annotation.Boxes)
        {
            var (vAbsX, vAbsY, vAbsW, vAbsH) = CoordinateMapper.ToAbsoluteBbox(
                box.CenterX, box.CenterY, box.Width, box.Height,
                annotation.ImageWidth, annotation.ImageHeight);
            var absXmin = CoordinateMapper.Clamp((int)vAbsX, 0, annotation.ImageWidth);
            var absYmin = CoordinateMapper.Clamp((int)vAbsY, 0, annotation.ImageHeight);
            var absXmax = CoordinateMapper.Clamp((int)(vAbsX + vAbsW), 0, annotation.ImageWidth);
            var absYmax = CoordinateMapper.Clamp((int)(vAbsY + vAbsH), 0, annotation.ImageHeight);

            objectElements.Add(new XElement("object",
                new XElement("name", box.ClassName),
                new XElement("pose", "Unspecified"),
                new XElement("truncated", 0),
                new XElement("difficult", 0),
                new XElement("bndbox",
                    new XElement("xmin", absXmin),
                    new XElement("ymin", absYmin),
                    new XElement("xmax", absXmax),
                    new XElement("ymax", absYmax))));
        }

        foreach (var polygon in annotation.Polygons)
        {
            var pointElements = new List<XElement>();
            foreach (var p in polygon.Points)
            {
                var px = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth), 0, annotation.ImageWidth);
                var py = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight), 0, annotation.ImageHeight);
                pointElements.Add(new XElement("x", px));
                pointElements.Add(new XElement("y", py));
            }

            objectElements.Add(new XElement("object",
                new XElement("name", polygon.ClassName),
                new XElement("pose", "Unspecified"),
                new XElement("truncated", 0),
                new XElement("difficult", 0),
                new XElement("polygon", pointElements)));
        }

        foreach (var obb in annotation.OrientedBoxes)
        {
            var absCx = Math.Round(CoordinateMapper.ToAbsolute(obb.CenterX, annotation.ImageWidth), 2);
            var absCy = Math.Round(CoordinateMapper.ToAbsolute(obb.CenterY, annotation.ImageHeight), 2);
            var absW = Math.Round(CoordinateMapper.ToAbsolute(obb.Width, annotation.ImageWidth), 2);
            var absH = Math.Round(CoordinateMapper.ToAbsolute(obb.Height, annotation.ImageHeight), 2);
            var angleDeg = Math.Round(obb.Angle * MathConstants.RadiansToDegrees, 2);

            objectElements.Add(new XElement("object",
                new XElement("name", obb.ClassName),
                new XElement("pose", "Unspecified"),
                new XElement("truncated", 0),
                new XElement("difficult", 0),
                new XElement("robndbox",
                    new XElement("cx", absCx),
                    new XElement("cy", absCy),
                    new XElement("w", absW),
                    new XElement("h", absH),
                    new XElement("angle", angleDeg))));
        }

        foreach (var polyline in annotation.Polylines)
        {
            var pointElements = new List<XElement>();
            foreach (var p in polyline.Points)
            {
                var px = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.X, annotation.ImageWidth), 0, annotation.ImageWidth);
                var py = CoordinateMapper.Clamp((int)CoordinateMapper.ToAbsolute(p.Y, annotation.ImageHeight), 0, annotation.ImageHeight);
                pointElements.Add(new XElement("x", px));
                pointElements.Add(new XElement("y", py));
            }

            objectElements.Add(new XElement("object",
                new XElement("name", polyline.ClassName),
                new XElement("pose", "Unspecified"),
                new XElement("truncated", 0),
                new XElement("difficult", 0),
                new XElement("polyline", pointElements)));
        }

        foreach (var circle in annotation.Circles)
        {
            var absCx = Math.Round(CoordinateMapper.ToAbsolute(circle.CenterX, annotation.ImageWidth), 2);
            var absCy = Math.Round(CoordinateMapper.ToAbsolute(circle.CenterY, annotation.ImageHeight), 2);
            var absR = Math.Round(CoordinateMapper.ToAbsolute(circle.Radius, annotation.ImageWidth), 2);

            objectElements.Add(new XElement("object",
                new XElement("name", circle.ClassName),
                new XElement("pose", "Unspecified"),
                new XElement("truncated", 0),
                new XElement("difficult", 0),
                new XElement("circle",
                    new XElement("cx", absCx),
                    new XElement("cy", absCy),
                    new XElement("r", absR))));
        }

        return objectElements;
    }
}
