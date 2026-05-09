using System.IO;
using System.Text.Json;
using ScaffoldX.Plugin.Annotation.Models;

namespace ScaffoldX.Plugin.Annotation.Services;

public class CocoAnnotationExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        var cocoCategories = new List<object>();
        for (var i = 0; i < project.Classes.Count; i++)
        {
            var cls = project.Classes[i];
            cocoCategories.Add(new
            {
                id = cls.Index + 1,
                name = cls.Name,
                supercategory = "none"
            });
        }

        var cocoImages = new List<object>();
        var cocoAnnotations = new List<object>();
        var annotationId = 1;

        for (var imageIdx = 0; imageIdx < project.Annotations.Count; imageIdx++)
        {
            var annotation = project.Annotations[imageIdx];
            var imageId = imageIdx + 1;

            cocoImages.Add(new
            {
                id = imageId,
                file_name = annotation.ImagePath,
                width = annotation.ImageWidth,
                height = annotation.ImageHeight
            });

            foreach (var box in annotation.Boxes)
            {
                var x = (box.CenterX - box.Width / 2) * annotation.ImageWidth;
                var y = (box.CenterY - box.Height / 2) * annotation.ImageHeight;
                var w = box.Width * annotation.ImageWidth;
                var h = box.Height * annotation.ImageHeight;

                cocoAnnotations.Add(new
                {
                    id = annotationId++,
                    image_id = imageId,
                    category_id = box.ClassIndex + 1,
                    bbox = new[] { x, y, w, h },
                    area = w * h,
                    iscrowd = 0
                });
            }
        }

        var cocoJson = new
        {
            info = new
            {
                description = project.ProjectName,
                date_created = DateTime.Now.ToString("o")
            },
            images = cocoImages,
            annotations = cocoAnnotations,
            categories = cocoCategories
        };

        var json = JsonSerializer.Serialize(cocoJson, JsonOptions);
        await File.WriteAllTextAsync(Path.Combine(outputPath, "coco.json"), json);
    }
}
