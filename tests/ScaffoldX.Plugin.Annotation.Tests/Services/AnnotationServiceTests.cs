using ScaffoldX.Plugin.Annotation.Models;
using ScaffoldX.Plugin.Annotation.Services;
using Xunit;

namespace ScaffoldX.Plugin.Annotation.Tests.Services;

public class AnnotationServiceTests : IDisposable
{
    private readonly List<string> _tempDirs = [];
    private readonly AnnotationService _service = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_SvcTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            if (Directory.Exists(dir))
                try { Directory.Delete(dir, true); } catch { }
    }

    [Fact]
    public async Task CreateProjectAsync_委托给Repository()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "svc_project");

        var project = await _service.CreateProjectAsync("svc_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "obj" }
        ]);

        Assert.Equal("svc_project", project.ProjectName);
        Assert.True(Directory.Exists(Path.Combine(projectDir, "images")));
        Assert.True(Directory.Exists(Path.Combine(projectDir, "labels")));
        Assert.True(File.Exists(Path.Combine(projectDir, "classes.json")));
    }

    [Fact]
    public void ToYoloFormat_委托给Exporter()
    {
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes =
            [
                new BoundingBoxAnnotation
                {
                    ClassIndex = 1, CenterX = 0.25, CenterY = 0.75, Width = 0.1, Height = 0.2
                }
            ]
        };

        var lines = _service.ToYoloFormat(annotation);

        Assert.Single(lines);
        Assert.Equal("1 0.250000 0.750000 0.100000 0.200000", lines[0]);
    }

    [Fact]
    public async Task ExportCocoDatasetAsync_委托给CocoExporter()
    {
        var dir = CreateTempDir();
        var exportDir = Path.Combine(dir, "coco_export");

        var project = new AnnotationProject
        {
            ProjectName = "svc_coco",
            ProjectDirectory = "",
            Classes = [new AnnotationClass { Index = 0, Name = "item" }],
            Annotations =
            [
                new AnnotationData
                {
                    ImagePath = "pic.jpg", ImageWidth = 320, ImageHeight = 240,
                    Boxes = [new BoundingBoxAnnotation { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.3, Height = 0.4 }]
                }
            ]
        };

        await _service.ExportCocoDatasetAsync(project, exportDir);

        Assert.True(File.Exists(Path.Combine(exportDir, "coco.json")));
    }
}
