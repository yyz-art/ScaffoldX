using ScaffoldX.Plugin.Annotation.Models;
using ScaffoldX.Plugin.Annotation.Services;
using Xunit;

namespace ScaffoldX.Plugin.Annotation.Tests.Services;

public class YoloAnnotationExporterTests : IDisposable
{
    private readonly List<string> _tempDirs = [];
    private readonly YoloAnnotationExporter _exporter = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_YoloTest_{Guid.NewGuid():N}");
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
    public void ToYoloFormat_单框转换()
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
                    ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3
                }
            ]
        };

        var lines = _exporter.ToYoloFormat(annotation);

        Assert.Single(lines);
        Assert.Equal("0 0.500000 0.500000 0.200000 0.300000", lines[0]);
    }

    [Fact]
    public void ToYoloFormat_多框转换()
    {
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes =
            [
                new BoundingBoxAnnotation { ClassIndex = 0, CenterX = 0.1, CenterY = 0.2, Width = 0.3, Height = 0.4 },
                new BoundingBoxAnnotation { ClassIndex = 1, CenterX = 0.5, CenterY = 0.6, Width = 0.1, Height = 0.2 }
            ]
        };

        var lines = _exporter.ToYoloFormat(annotation);

        Assert.Equal(2, lines.Count);
        Assert.StartsWith("0 ", lines[0]);
        Assert.StartsWith("1 ", lines[1]);
    }

    [Fact]
    public void ToYoloFormat_无框返回空列表()
    {
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480
        };

        var lines = _exporter.ToYoloFormat(annotation);

        Assert.Empty(lines);
    }

    [Fact]
    public void ToYoloFormat_格式化精度()
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
                    ClassIndex = 2, CenterX = 0.123456789, CenterY = 0.987654321, Width = 0.111111, Height = 0.222222
                }
            ]
        };

        var lines = _exporter.ToYoloFormat(annotation);

        Assert.Single(lines);
        var parts = lines[0].Split(' ');
        Assert.Equal(5, parts.Length);
        Assert.Equal("2", parts[0]);
        Assert.Equal("0.123457", parts[1]);
        Assert.Equal("0.987654", parts[2]);
    }

    [Fact]
    public async Task ExportYoloDatasetAsync_创建目录结构()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "project");
        var exportDir = Path.Combine(dir, "export");

        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "images"));
        Directory.CreateDirectory(Path.Combine(projectDir, "labels"));

        var project = new AnnotationProject
        {
            ProjectName = "test",
            ProjectDirectory = projectDir,
            Classes = [new AnnotationClass { Index = 0, Name = "cat" }],
            Annotations =
            [
                new AnnotationData
                {
                    ImagePath = "img1.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = [new BoundingBoxAnnotation { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }]
                }
            ]
        };

        await _exporter.ExportYoloDatasetAsync(project, exportDir);

        Assert.True(Directory.Exists(Path.Combine(exportDir, "images", "train")));
        Assert.True(Directory.Exists(Path.Combine(exportDir, "images", "val")));
        Assert.True(Directory.Exists(Path.Combine(exportDir, "labels", "train")));
        Assert.True(Directory.Exists(Path.Combine(exportDir, "labels", "val")));
    }

    [Fact]
    public async Task ExportYoloDatasetAsync_生成dataYaml()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "project");
        var exportDir = Path.Combine(dir, "export");

        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "images"));
        Directory.CreateDirectory(Path.Combine(projectDir, "labels"));

        var project = new AnnotationProject
        {
            ProjectName = "test",
            ProjectDirectory = projectDir,
            Classes =
            [
                new AnnotationClass { Index = 0, Name = "cat" },
                new AnnotationClass { Index = 1, Name = "dog" }
            ],
            Annotations = []
        };

        await _exporter.ExportYoloDatasetAsync(project, exportDir);

        var yamlPath = Path.Combine(exportDir, "data.yaml");
        Assert.True(File.Exists(yamlPath));

        var yaml = await File.ReadAllTextAsync(yamlPath);
        Assert.Contains("nc: 2", yaml);
        Assert.Contains("'cat'", yaml);
        Assert.Contains("'dog'", yaml);
        Assert.Contains("train: images/train", yaml);
        Assert.Contains("val: images/val", yaml);
    }

    [Fact]
    public async Task ExportYoloDatasetAsync_训练验证分割()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "project");
        var exportDir = Path.Combine(dir, "export");

        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "images"));
        Directory.CreateDirectory(Path.Combine(projectDir, "labels"));

        var annotations = new List<AnnotationData>();
        for (var i = 0; i < 5; i++)
        {
            annotations.Add(new AnnotationData
            {
                ImagePath = $"img{i}.jpg",
                ImageWidth = 640,
                ImageHeight = 480,
                Boxes = [new BoundingBoxAnnotation { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.1, Height = 0.1 }]
            });
        }

        var project = new AnnotationProject
        {
            ProjectName = "test",
            ProjectDirectory = projectDir,
            Classes = [new AnnotationClass { Index = 0, Name = "obj" }],
            Annotations = annotations
        };

        await _exporter.ExportYoloDatasetAsync(project, exportDir, 0.8);

        var trainLabels = Directory.GetFiles(Path.Combine(exportDir, "labels", "train"));
        var valLabels = Directory.GetFiles(Path.Combine(exportDir, "labels", "val"));
        Assert.Equal(4, trainLabels.Length);
        Assert.Equal(1, valLabels.Length);
    }

    [Fact]
    public async Task ExportYoloDatasetAsync_写入标签文件()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "project");
        var exportDir = Path.Combine(dir, "export");

        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "images"));
        Directory.CreateDirectory(Path.Combine(projectDir, "labels"));

        var project = new AnnotationProject
        {
            ProjectName = "test",
            ProjectDirectory = projectDir,
            Classes = [new AnnotationClass { Index = 0, Name = "cat" }],
            Annotations =
            [
                new AnnotationData
                {
                    ImagePath = "photo.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = [new BoundingBoxAnnotation { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }]
                }
            ]
        };

        await _exporter.ExportYoloDatasetAsync(project, exportDir);

        var labelFile = Path.Combine(exportDir, "labels", "train", "photo.txt");
        Assert.True(File.Exists(labelFile));

        var lines = await File.ReadAllLinesAsync(labelFile);
        Assert.Single(lines);
        Assert.Equal("0 0.500000 0.500000 0.200000 0.300000", lines[0]);
    }

    [Fact]
    public async Task ExportYoloDatasetAsync_复制图片文件()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "project");
        var exportDir = Path.Combine(dir, "export");

        Directory.CreateDirectory(projectDir);
        var imagesDir = Path.Combine(projectDir, "images");
        Directory.CreateDirectory(imagesDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "labels"));

        await File.WriteAllTextAsync(Path.Combine(imagesDir, "sample.jpg"), "fake image data");

        var project = new AnnotationProject
        {
            ProjectName = "test",
            ProjectDirectory = projectDir,
            Classes = [new AnnotationClass { Index = 0, Name = "cat" }],
            Annotations =
            [
                new AnnotationData
                {
                    ImagePath = "sample.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = [new BoundingBoxAnnotation { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }]
                }
            ]
        };

        await _exporter.ExportYoloDatasetAsync(project, exportDir);

        Assert.True(File.Exists(Path.Combine(exportDir, "images", "train", "sample.jpg")));
    }
}
