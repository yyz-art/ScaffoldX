using ScaffoldX.Plugin.Annotation.Models;
using ScaffoldX.Plugin.Annotation.Services;
using Xunit;

namespace ScaffoldX.Plugin.Annotation.Tests.Services;

public class AnnotationRepositoryTests : IDisposable
{
    private readonly List<string> _tempDirs = [];
    private readonly AnnotationRepository _repo = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_RepoTest_{Guid.NewGuid():N}");
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
    public async Task CreateProjectAsync_创建目录结构()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" }
        ]);

        Assert.True(Directory.Exists(Path.Combine(projectDir, "images")));
        Assert.True(Directory.Exists(Path.Combine(projectDir, "labels")));
    }

    [Fact]
    public async Task CreateProjectAsync_生成classesJson()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" },
            new AnnotationClass { Index = 1, Name = "dog" }
        ]);

        var classesPath = Path.Combine(projectDir, "classes.json");
        Assert.True(File.Exists(classesPath));

        var json = await File.ReadAllTextAsync(classesPath);
        Assert.Contains("cat", json);
        Assert.Contains("dog", json);
    }

    [Fact]
    public async Task CreateProjectAsync_设置项目属性()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");
        var classes = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "cat" }
        };

        var project = await _repo.CreateProjectAsync("my_project", projectDir, classes);

        Assert.Equal("my_project", project.ProjectName);
        Assert.Equal(projectDir, project.ProjectDirectory);
        Assert.Single(project.Classes);
        Assert.Equal("cat", project.Classes[0].Name);
        Assert.True(project.CreatedAt <= DateTime.Now);
        Assert.True(project.ModifiedAt <= DateTime.Now);
    }

    [Fact]
    public async Task LoadProjectAsync_加载类列表()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" },
            new AnnotationClass { Index = 1, Name = "dog" }
        ]);

        var loaded = await _repo.LoadProjectAsync(projectDir);

        Assert.Equal("my_project", loaded.ProjectName);
        Assert.Equal(2, loaded.Classes.Count);
        Assert.Equal("cat", loaded.Classes[0].Name);
        Assert.Equal("dog", loaded.Classes[1].Name);
    }

    [Fact]
    public async Task LoadProjectAsync_加载标注数据()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        var project = await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "defect" }
        ]);

        project.Annotations.Add(new AnnotationData
        {
            ImagePath = "img001.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes =
            [
                new BoundingBoxAnnotation
                {
                    ClassIndex = 0, ClassName = "defect",
                    CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3
                }
            ]
        });

        await _repo.SaveProjectAsync(project);

        var loaded = await _repo.LoadProjectAsync(projectDir);

        Assert.Single(loaded.Annotations);
        Assert.Equal("img001.jpg", loaded.Annotations[0].ImagePath);
        Assert.Equal(640, loaded.Annotations[0].ImageWidth);
        Assert.Single(loaded.Annotations[0].Boxes);
        Assert.Equal(0.5, loaded.Annotations[0].Boxes[0].CenterX);
    }

    [Fact]
    public async Task LoadProjectAsync_空标注目录()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" }
        ]);

        var loaded = await _repo.LoadProjectAsync(projectDir);

        Assert.Empty(loaded.Annotations);
    }

    [Fact]
    public async Task SaveProjectAsync_保存classes和labels()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        var project = await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" }
        ]);

        project.Annotations.Add(new AnnotationData
        {
            ImagePath = "photo.jpg",
            ImageWidth = 800,
            ImageHeight = 600,
            Boxes =
            [
                new BoundingBoxAnnotation
                {
                    ClassIndex = 0, ClassName = "cat",
                    CenterX = 0.3, CenterY = 0.4, Width = 0.1, Height = 0.15
                }
            ]
        });

        await _repo.SaveProjectAsync(project);

        Assert.True(File.Exists(Path.Combine(projectDir, "classes.json")));
        Assert.True(File.Exists(Path.Combine(projectDir, "labels", "photo.json")));
    }

    [Fact]
    public async Task SaveProjectAsync_更新修改时间()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        var project = await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" }
        ]);

        var originalModified = project.ModifiedAt;
        await Task.Delay(10);
        await _repo.SaveProjectAsync(project);

        var loaded = await _repo.LoadProjectAsync(projectDir);
        Assert.True(loaded.ModifiedAt >= originalModified);
    }

    [Fact]
    public async Task AddImagesAsync_复制图片文件()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        var project = await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" }
        ]);

        var srcImage = Path.Combine(dir, "test.jpg");
        await File.WriteAllTextAsync(srcImage, "fake image");

        await _repo.AddImagesAsync(project, [srcImage]);

        Assert.True(File.Exists(Path.Combine(projectDir, "images", "test.jpg")));
    }

    [Fact]
    public async Task AddImagesAsync_创建标注条目()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        var project = await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" }
        ]);

        var src1 = Path.Combine(dir, "a.jpg");
        var src2 = Path.Combine(dir, "b.png");
        await File.WriteAllTextAsync(src1, "img1");
        await File.WriteAllTextAsync(src2, "img2");

        await _repo.AddImagesAsync(project, [src1, src2]);

        Assert.Equal(2, project.Annotations.Count);
        Assert.Equal("a.jpg", project.Annotations[0].ImagePath);
        Assert.Equal("b.png", project.Annotations[1].ImagePath);
    }

    [Fact]
    public async Task UpdateAnnotationAsync_保存标注文件()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        var project = await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "defect" }
        ]);

        var annotation = new AnnotationData
        {
            ImagePath = "sample.png",
            ImageWidth = 100,
            ImageHeight = 100,
            Boxes =
            [
                new BoundingBoxAnnotation
                {
                    ClassIndex = 0, ClassName = "defect",
                    CenterX = 0.5, CenterY = 0.5, Width = 0.1, Height = 0.1
                }
            ]
        };

        await _repo.UpdateAnnotationAsync(project, annotation);

        Assert.True(File.Exists(Path.Combine(projectDir, "labels", "sample.json")));
    }

    [Fact]
    public async Task UpdateAnnotationAsync_更新已有标注()
    {
        var dir = CreateTempDir();
        var projectDir = Path.Combine(dir, "my_project");

        var project = await _repo.CreateProjectAsync("my_project", projectDir,
        [
            new AnnotationClass { Index = 0, Name = "cat" }
        ]);

        var annotation = new AnnotationData
        {
            ImagePath = "pic.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes =
            [
                new BoundingBoxAnnotation
                {
                    ClassIndex = 0, ClassName = "cat",
                    CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.2
                }
            ]
        };

        await _repo.UpdateAnnotationAsync(project, annotation);
        Assert.Single(project.Annotations);

        annotation.Boxes.Add(new BoundingBoxAnnotation
        {
            ClassIndex = 0, ClassName = "cat",
            CenterX = 0.3, CenterY = 0.3, Width = 0.1, Height = 0.1
        });

        await _repo.UpdateAnnotationAsync(project, annotation);
        Assert.Single(project.Annotations);
        Assert.Equal(2, project.Annotations[0].Boxes.Count);
    }
}
