using System.Drawing;
using System.IO;
using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for MOT (Multiple Object Tracking) Challenge export functionality
/// in AnnotationService. MOT format outputs a single gt.txt file with lines in the
/// format: frame_id,track_id,x,y,w,h,confidence,class_id,visibility.
/// </summary>
public class AnnotationServiceMotTests : IDisposable
{
    private readonly AnnotationService _sut = new();
    private readonly string _tempDirectory;

    /// <summary>
    /// Sets up a temporary directory for test output files.
    /// </summary>
    public AnnotationServiceMotTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ScaffoldX_MotTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Verifies that ExportMotDatasetAsync creates a gt.txt file containing annotation records
    /// for each annotated frame.
    /// </summary>
    [Fact]
    public async Task ExportMotDataset_WithAnnotations_CreatesGtFile()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDirectory, "mot_output");
        var project = new AnnotationProject
        {
            ProjectName = "MotTest",
            ProjectDirectory = _tempDirectory,
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "car" },
                new() { Index = 1, Name = "pedestrian" }
            },
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "/frames/frame001.jpg",
                    ImageWidth = 1920,
                    ImageHeight = 1080,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "car", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 },
                        new() { ClassIndex = 1, ClassName = "pedestrian", CenterX = 0.3, CenterY = 0.4, Width = 0.05, Height = 0.15 }
                    }
                },
                new()
                {
                    ImagePath = "/frames/frame002.jpg",
                    ImageWidth = 1920,
                    ImageHeight = 1080,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "car", CenterX = 0.52, CenterY = 0.51, Width = 0.2, Height = 0.3 }
                    }
                }
            }
        };

        // Act
        await _sut.ExportMotDatasetAsync(project, outputDir);

        // Assert
        var gtPath = Path.Combine(outputDir, "gt.txt");
        File.Exists(gtPath).Should().BeTrue("MOT export should create gt.txt");

        var lines = await File.ReadAllLinesAsync(gtPath);
        lines.Should().HaveCount(3, "project has 3 bounding boxes across 2 frames");
    }

    /// <summary>
    /// Verifies that the MOT gt.txt format matches the expected MOT Challenge convention:
    /// frame_id,track_id,x,y,w,h,confidence,class_id,visibility
    /// </summary>
    [Fact]
    public async Task ExportMotDataset_FormatIsCorrect()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDirectory, "mot_format");
        var project = new AnnotationProject
        {
            ProjectName = "MotFormatTest",
            ProjectDirectory = _tempDirectory,
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "car" }
            },
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "/frames/frame001.jpg",
                    ImageWidth = 1000,
                    ImageHeight = 500,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "car", CenterX = 0.5, CenterY = 0.5, Width = 0.1, Height = 0.2 }
                    }
                }
            }
        };

        // Act
        await _sut.ExportMotDatasetAsync(project, outputDir);

        // Assert
        var gtPath = Path.Combine(outputDir, "gt.txt");
        var lines = await File.ReadAllLinesAsync(gtPath);
        lines.Should().HaveCount(1);

        var parts = lines[0].Split(',');
        parts.Should().HaveCount(9, "MOT format has 9 comma-separated fields");

        // frame_id (1-based)
        int.TryParse(parts[0], out var frameId).Should().BeTrue();
        frameId.Should().Be(1, "first frame should have frame_id = 1");

        // track_id
        int.TryParse(parts[1], out var trackId).Should().BeTrue();
        trackId.Should().BeGreaterThanOrEqualTo(1, "track_id should be >= 1");

        // x, y, w, h (absolute pixel coordinates, comma-separated)
        double.TryParse(parts[2], out _).Should().BeTrue("x should be a valid number");
        double.TryParse(parts[3], out _).Should().BeTrue("y should be a valid number");
        double.TryParse(parts[4], out _).Should().BeTrue("w should be a valid number");
        double.TryParse(parts[5], out _).Should().BeTrue("h should be a valid number");

        // confidence
        int.TryParse(parts[6], out var confidence).Should().BeTrue();
        confidence.Should().Be(1, "default confidence is 1");

        // class_id
        int.TryParse(parts[7], out var classId).Should().BeTrue();
        classId.Should().Be(0, "class_id matches the class index");

        // visibility
        int.TryParse(parts[8], out var visibility).Should().BeTrue();
        visibility.Should().Be(1, "default visibility is 1");
    }

    /// <summary>
    /// Verifies that ExportMotDatasetAsync creates an empty gt.txt (with no lines) when the
    /// project has no annotations.
    /// </summary>
    [Fact]
    public async Task ExportMotDataset_WithNoAnnotations_CreatesEmptyFile()
    {
        // Arrange
        var outputDir = Path.Combine(_tempDirectory, "mot_empty");
        var project = new AnnotationProject
        {
            ProjectName = "EmptyMot",
            ProjectDirectory = _tempDirectory,
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "car" }
            },
            Annotations = new List<AnnotationData>()
        };

        // Act
        await _sut.ExportMotDatasetAsync(project, outputDir);

        // Assert
        var gtPath = Path.Combine(outputDir, "gt.txt");
        File.Exists(gtPath).Should().BeTrue("gt.txt should be created even with no annotations");

        var content = await File.ReadAllTextAsync(gtPath);
        content.Should().BeEmpty("gt.txt should be empty when there are no annotations");
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
