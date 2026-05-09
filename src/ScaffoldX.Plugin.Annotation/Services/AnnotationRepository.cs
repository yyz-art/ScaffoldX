using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScaffoldX.Plugin.Annotation.Models;

namespace ScaffoldX.Plugin.Annotation.Services;

public class AnnotationRepository : IAnnotationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new PointFJsonConverter() }
    };

    public async Task<AnnotationProject> CreateProjectAsync(string projectName, string projectDirectory, List<AnnotationClass> classes)
    {
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(Path.Combine(projectDirectory, "images"));
        Directory.CreateDirectory(Path.Combine(projectDirectory, "labels"));

        var project = new AnnotationProject
        {
            ProjectName = projectName,
            ProjectDirectory = projectDirectory,
            Classes = classes,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        await SaveClassesAsync(project);
        return project;
    }

    public async Task<AnnotationProject> LoadProjectAsync(string projectFilePath)
    {
        var classesPath = Path.Combine(projectFilePath, "classes.json");
        var json = await File.ReadAllTextAsync(classesPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var project = new AnnotationProject
        {
            ProjectName = root.GetProperty("projectName").GetString()!,
            ProjectDirectory = projectFilePath,
            Classes = JsonSerializer.Deserialize<List<AnnotationClass>>(
                root.GetProperty("classes").GetRawText(), JsonOptions)!,
            CreatedAt = root.GetProperty("createdAt").GetDateTime(),
            ModifiedAt = root.GetProperty("modifiedAt").GetDateTime(),
        };

        var labelsDir = Path.Combine(projectFilePath, "labels");
        if (Directory.Exists(labelsDir))
        {
            foreach (var file in Directory.GetFiles(labelsDir, "*.json"))
            {
                var labelJson = await File.ReadAllTextAsync(file);
                var annotation = JsonSerializer.Deserialize<AnnotationData>(labelJson, JsonOptions);
                if (annotation is not null)
                    project.Annotations.Add(annotation);
            }
        }

        return project;
    }

    public async Task SaveProjectAsync(AnnotationProject project)
    {
        project.ModifiedAt = DateTime.Now;
        await SaveClassesAsync(project);

        var labelsDir = Path.Combine(project.ProjectDirectory, "labels");
        Directory.CreateDirectory(labelsDir);

        foreach (var annotation in project.Annotations)
            await SaveAnnotationAsync(labelsDir, annotation);
    }

    public async Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths)
    {
        var imagesDir = Path.Combine(project.ProjectDirectory, "images");
        Directory.CreateDirectory(imagesDir);

        foreach (var path in imagePaths)
        {
            var fileName = Path.GetFileName(path);
            File.Copy(path, Path.Combine(imagesDir, fileName), overwrite: true);
            project.Annotations.Add(new AnnotationData { ImagePath = fileName });
        }

        project.ModifiedAt = DateTime.Now;
        await SaveClassesAsync(project);
    }

    public async Task UpdateAnnotationAsync(AnnotationProject project, AnnotationData annotation)
    {
        var labelsDir = Path.Combine(project.ProjectDirectory, "labels");
        Directory.CreateDirectory(labelsDir);
        await SaveAnnotationAsync(labelsDir, annotation);

        var idx = project.Annotations.FindIndex(a => a.ImagePath == annotation.ImagePath);
        if (idx >= 0)
            project.Annotations[idx] = annotation;
        else
            project.Annotations.Add(annotation);

        project.ModifiedAt = DateTime.Now;
        await SaveClassesAsync(project);
    }

    private async Task SaveClassesAsync(AnnotationProject project)
    {
        var path = Path.Combine(project.ProjectDirectory, "classes.json");
        var json = JsonSerializer.Serialize(new
        {
            project.ProjectName,
            project.Classes,
            project.CreatedAt,
            project.ModifiedAt
        }, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private async Task SaveAnnotationAsync(string labelsDir, AnnotationData annotation)
    {
        var name = Path.GetFileNameWithoutExtension(annotation.ImagePath);
        var path = Path.Combine(labelsDir, $"{name}.json");
        var json = JsonSerializer.Serialize(annotation, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}

internal sealed class PointFJsonConverter : JsonConverter<PointF>
{
    public override PointF Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        reader.Read();
        float x = 0, y = 0;
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            var propName = reader.GetString()!;
            reader.Read();
            switch (propName.ToUpperInvariant())
            {
                case "X": x = reader.GetSingle(); break;
                case "Y": y = reader.GetSingle(); break;
                default: reader.Skip(); break;
            }
            reader.Read();
        }
        return new PointF(x, y);
    }

    public override void Write(Utf8JsonWriter writer, PointF value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}
