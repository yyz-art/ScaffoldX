using System.Drawing;
using System.IO;
using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for DOTA (Dataset for Object deTection in Aerial images) export functionality
/// in AnnotationService. DOTA format outputs 4-corner polygon coordinates with class name
/// and confidence score per line.
/// </summary>
public class AnnotationServiceDotaTests : IDisposable
{
    private readonly AnnotationService _sut = new();
    private readonly string _tempDirectory;

    /// <summary>
    /// Sets up a temporary directory for test output files.
    /// </summary>
    public AnnotationServiceDotaTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ScaffoldX_DotaTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Verifies that ExportDotDatasetAsync creates per-image .txt files containing OBB annotations
    /// converted to 4-corner polygon format with class name and confidence.
    /// </summary>
    [Fact]
    public async Task ExportDotDataset_WithObbAnnotations_CreatesFiles()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDirectory, "dota_output");
        var project = new AnnotationProject
        {
            ProjectName = "DotaTest",
            ProjectDirectory = _tempDirectory,
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "ship", Color = "#FF0000" },
                new() { Index = 1, Name = "plane", Color = "#00FF00" }
            },
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "/images/frame001.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 0,
                            ClassName = "ship",
                            CenterX = 0.5f,
                            CenterY = 0.5f,
                            Width = 0.2f,
                            Height = 0.1f,
                            Angle = 0.0f
                        }
                    }
                },
                new()
                {
                    ImagePath = "/images/frame002.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 1,
                            ClassName = "plane",
                            CenterX = 0.3f,
                            CenterY = 0.4f,
                            Width = 0.15f,
                            Height = 0.08f,
                            Angle = 0.7854f
                        }
                    }
                }
            }
        };

        // Act
        await _sut.ExportDotDatasetAsync(project, outputDir);

        // Assert: per-image .txt files should be created
        var file1 = Path.Combine(outputDir, "frame001.txt");
        var file2 = Path.Combine(outputDir, "frame002.txt");

        File.Exists(file1).Should().BeTrue("DOTA export should create a .txt file per image");
        File.Exists(file2).Should().BeTrue("DOTA export should create a .txt file per image");

        var lines1 = await File.ReadAllLinesAsync(file1);
        lines1.Should().HaveCount(1, "frame001 has one OBB annotation");
        lines1[0].Should().Contain("ship", "DOTA format includes class name");
        lines1[0].Should().Contain("1.0", "DOTA format includes confidence score");

        var lines2 = await File.ReadAllLinesAsync(file2);
        lines2.Should().HaveCount(1, "frame002 has one OBB annotation");
        lines2[0].Should().Contain("plane", "DOTA format includes class name");
    }

    /// <summary>
    /// Verifies that ExportDotDatasetAsync includes a classes.txt file listing all class names.
    /// </summary>
    [Fact]
    public async Task ExportDotDataset_IncludesClassNames()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDirectory, "dota_classes");
        var project = new AnnotationProject
        {
            ProjectName = "DotaClassTest",
            ProjectDirectory = _tempDirectory,
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "vehicle" },
                new() { Index = 1, Name = "building" },
                new() { Index = 2, Name = "tree" }
            },
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "/images/test.jpg",
                    ImageWidth = 100,
                    ImageHeight = 100,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "vehicle", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
                    }
                }
            }
        };

        // Act
        await _sut.ExportDotDatasetAsync(project, outputDir);

        // Assert
        var classesPath = Path.Combine(outputDir, "classes.txt");
        File.Exists(classesPath).Should().BeTrue("DOTA export should create classes.txt");

        var classNames = await File.ReadAllLinesAsync(classesPath);
        classNames.Should().HaveCount(3);
        classNames.Should().Contain("vehicle");
        classNames.Should().Contain("building");
        classNames.Should().Contain("tree");
    }

    /// <summary>
    /// Verifies that ExportDotDatasetAsync creates the output directory even when the project
    /// has no annotations, and produces an empty classes.txt.
    /// </summary>
    [Fact]
    public async Task ExportDotDataset_WithNoAnnotations_CreatesEmptyDir()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDirectory, "dota_empty");
        var project = new AnnotationProject
        {
            ProjectName = "EmptyDota",
            ProjectDirectory = _tempDirectory,
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "object" }
            },
            Annotations = new List<AnnotationData>()
        };

        // Act
        await _sut.ExportDotDatasetAsync(project, outputDir);

        // Assert
        Directory.Exists(outputDir).Should().BeTrue("output directory should be created");

        var txtFiles = Directory.GetFiles(outputDir, "*.txt");
        // Only classes.txt should exist (no per-image .txt files)
        txtFiles.Should().HaveCount(1, "only classes.txt should exist when there are no annotations");
        Path.GetFileName(txtFiles[0]).Should().Be("classes.txt");

        var classNames = await File.ReadAllLinesAsync(txtFiles[0]);
        classNames.Should().Contain("object");
    }

    /// <summary>
    /// Cleans up the temporary directory after each test.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
