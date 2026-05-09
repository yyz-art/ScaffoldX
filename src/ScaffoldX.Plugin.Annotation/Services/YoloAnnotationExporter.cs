using System.IO;
using ScaffoldX.Plugin.Annotation.Models;

namespace ScaffoldX.Plugin.Annotation.Services;

public class YoloAnnotationExporter
{
    public List<string> ToYoloFormat(AnnotationData annotation)
    {
        return annotation.Boxes.Select(b =>
            $"{b.ClassIndex} {b.CenterX:F6} {b.CenterY:F6} {b.Width:F6} {b.Height:F6}"
        ).ToList();
    }

    public async Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8)
    {
        var trainImagesDir = Path.Combine(outputPath, "images", "train");
        var valImagesDir = Path.Combine(outputPath, "images", "val");
        var trainLabelsDir = Path.Combine(outputPath, "labels", "train");
        var valLabelsDir = Path.Combine(outputPath, "labels", "val");

        Directory.CreateDirectory(trainImagesDir);
        Directory.CreateDirectory(valImagesDir);
        Directory.CreateDirectory(trainLabelsDir);
        Directory.CreateDirectory(valLabelsDir);

        var annotations = project.Annotations;
        var trainCount = (int)Math.Ceiling(annotations.Count * trainValSplit);

        for (var i = 0; i < annotations.Count; i++)
        {
            var annotation = annotations[i];
            var isTrain = i < trainCount;
            var imagesDir = isTrain ? trainImagesDir : valImagesDir;
            var labelsDir = isTrain ? trainLabelsDir : valLabelsDir;

            var srcImagePath = Path.Combine(project.ProjectDirectory, "images", annotation.ImagePath);
            if (File.Exists(srcImagePath))
                File.Copy(srcImagePath, Path.Combine(imagesDir, annotation.ImagePath), overwrite: true);

            var labelFileName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
            var labelPath = Path.Combine(labelsDir, $"{labelFileName}.txt");
            var lines = ToYoloFormat(annotation);
            await File.WriteAllLinesAsync(labelPath, lines);
        }

        var yaml = GenerateDataYaml(project, outputPath);
        await File.WriteAllTextAsync(Path.Combine(outputPath, "data.yaml"), yaml);
    }

    private static string GenerateDataYaml(AnnotationProject project, string outputPath)
    {
        var classNames = string.Join(", ", project.Classes.Select(c => $"'{c.Name}'"));
        return $"path: {outputPath.Replace('\\', '/')}\ntrain: images/train\nval: images/val\nnc: {project.Classes.Count}\nnames: [{classNames}]\n";
    }
}
