using System.Text.Json;
using ScaffoldX.Plugin.Annotation.Models;
using ScaffoldX.Plugin.Annotation.Services;
using Xunit;

namespace ScaffoldX.Plugin.Annotation.Tests.Services;

public class CocoAnnotationExporterTests : IDisposable
{
    private readonly List<string> _tempDirs = [];
    private readonly CocoAnnotationExporter _exporter = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_CocoTest_{Guid.NewGuid():N}");
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

    private static AnnotationProject CreateSampleProject()
    {
        return new AnnotationProject
        {
            ProjectName = "test_coco",
            ProjectDirectory = "",
            Classes =
            [
                new AnnotationClass { Index = 0, Name = "cat" },
                new AnnotationClass { Index = 1, Name = "dog" }
            ],
            Annotations =
            [
                new AnnotationData
                {
                    ImagePath = "img001.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes =
                    [
                        new BoundingBoxAnnotation
                        {
                            ClassIndex = 0, ClassName = "cat",
                            CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3
                        }
                    ]
                }
            ]
        };
    }

    [Fact]
    public async Task ExportCocoDatasetAsync_生成cocoJson()
    {
        var dir = CreateTempDir();
        var exportDir = Path.Combine(dir, "export");

        await _exporter.ExportCocoDatasetAsync(CreateSampleProject(), exportDir);

        var cocoPath = Path.Combine(exportDir, "coco.json");
        Assert.True(File.Exists(cocoPath));

        var json = await File.ReadAllTextAsync(cocoPath);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("info", out _));
        Assert.True(doc.RootElement.TryGetProperty("images", out _));
        Assert.True(doc.RootElement.TryGetProperty("annotations", out _));
        Assert.True(doc.RootElement.TryGetProperty("categories", out _));
    }

    [Fact]
    public async Task ExportCocoDatasetAsync_包含类别()
    {
        var dir = CreateTempDir();
        var exportDir = Path.Combine(dir, "export");

        await _exporter.ExportCocoDatasetAsync(CreateSampleProject(), exportDir);

        var json = await File.ReadAllTextAsync(Path.Combine(exportDir, "coco.json"));
        using var doc = JsonDocument.Parse(json);
        var categories = doc.RootElement.GetProperty("categories");

        Assert.Equal(2, categories.GetArrayLength());
        Assert.Equal("cat", categories[0].GetProperty("name").GetString());
        Assert.Equal("dog", categories[1].GetProperty("name").GetString());
        Assert.Equal(1, categories[0].GetProperty("id").GetInt32());
        Assert.Equal(2, categories[1].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task ExportCocoDatasetAsync_包含图片信息()
    {
        var dir = CreateTempDir();
        var exportDir = Path.Combine(dir, "export");

        await _exporter.ExportCocoDatasetAsync(CreateSampleProject(), exportDir);

        var json = await File.ReadAllTextAsync(Path.Combine(exportDir, "coco.json"));
        using var doc = JsonDocument.Parse(json);
        var images = doc.RootElement.GetProperty("images");

        Assert.Equal(1, images.GetArrayLength());
        Assert.Equal("img001.jpg", images[0].GetProperty("file_name").GetString());
        Assert.Equal(640, images[0].GetProperty("width").GetInt32());
        Assert.Equal(480, images[0].GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task ExportCocoDatasetAsync_包含标注()
    {
        var dir = CreateTempDir();
        var exportDir = Path.Combine(dir, "export");

        await _exporter.ExportCocoDatasetAsync(CreateSampleProject(), exportDir);

        var json = await File.ReadAllTextAsync(Path.Combine(exportDir, "coco.json"));
        using var doc = JsonDocument.Parse(json);
        var annotations = doc.RootElement.GetProperty("annotations");

        Assert.Equal(1, annotations.GetArrayLength());
        Assert.Equal(1, annotations[0].GetProperty("image_id").GetInt32());
        Assert.Equal(1, annotations[0].GetProperty("category_id").GetInt32());
        Assert.Equal(0, annotations[0].GetProperty("iscrowd").GetInt32());
    }

    [Fact]
    public async Task ExportCocoDatasetAsync_bbox坐标计算()
    {
        var dir = CreateTempDir();
        var exportDir = Path.Combine(dir, "export");

        await _exporter.ExportCocoDatasetAsync(CreateSampleProject(), exportDir);

        var json = await File.ReadAllTextAsync(Path.Combine(exportDir, "coco.json"));
        using var doc = JsonDocument.Parse(json);
        var bbox = doc.RootElement.GetProperty("annotations")[0].GetProperty("bbox");

        var x = bbox[0].GetDouble();
        var y = bbox[1].GetDouble();
        var w = bbox[2].GetDouble();
        var h = bbox[3].GetDouble();

        Assert.Equal(256, x, 0.01);
        Assert.Equal(168, y, 0.01);
        Assert.Equal(128, w, 0.01);
        Assert.Equal(144, h, 0.01);

        var area = doc.RootElement.GetProperty("annotations")[0].GetProperty("area").GetDouble();
        Assert.Equal(128 * 144, area, 0.01);
    }
}
