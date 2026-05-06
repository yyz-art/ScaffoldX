using System.IO;
using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for the ExportReportService, which generates HTML summary reports
/// for annotation projects including class distribution and project metadata.
/// </summary>
public class ExportReportTests : IDisposable
{
    private readonly ExportReportService _sut = new();
    private readonly string _tempDirectory;

    /// <summary>
    /// Sets up a temporary directory for test output files.
    /// </summary>
    public ExportReportTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ScaffoldX_ReportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Verifies that GenerateReport creates an HTML file when the project has annotations.
    /// </summary>
    [Fact]
    public async Task GenerateReport_WithAnnotations_CreatesHtmlFile()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var filePath = await _sut.GenerateReportAsync(project, _tempDirectory);

        // Assert
        filePath.Should().NotBeNullOrEmpty("report generation should return the file path");
        File.Exists(filePath).Should().BeTrue("HTML report file should be created on disk");

        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("<html", "output should be valid HTML");
        content.Should().Contain("</html>", "output should contain closing HTML tag");
    }

    /// <summary>
    /// Verifies that the generated report includes the project name.
    /// </summary>
    [Fact]
    public async Task GenerateReport_IncludesProjectName()
    {
        // Arrange
        var project = CreateSampleProject();
        project.ProjectName = "MyDefectDetection";

        // Act
        var filePath = await _sut.GenerateReportAsync(project, _tempDirectory);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("MyDefectDetection",
            "report should include the project name");
    }

    /// <summary>
    /// Verifies that the generated report includes class distribution statistics.
    /// </summary>
    [Fact]
    public async Task GenerateReport_IncludesClassDistribution()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var filePath = await _sut.GenerateReportAsync(project, _tempDirectory);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("defect",
            "report should list class names from the project");
        content.Should().Contain("scratch",
            "report should list all class names");
        content.Should().Contain("dent",
            "report should list all class names");
    }

    /// <summary>
    /// Verifies that the report includes annotation type counts (boundary boxes, polygons, oriented boxes).
    /// </summary>
    [Fact]
    public async Task GenerateReport_IncludesAnnotationTypeCounts()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var filePath = await _sut.GenerateReportAsync(project, _tempDirectory);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);
        content.Should().Contain("图像总数", "report should show total image count");
        content.Should().Contain("标注总数", "report should show total annotation count");
        content.Should().Contain("边界框", "report should show bounding box count");
        content.Should().Contain("多边形", "report should show polygon count");
    }

    /// <summary>
    /// Verifies that the report filename is derived from the project name.
    /// </summary>
    [Fact]
    public async Task GenerateReport_FileNameIncludesProjectName()
    {
        // Arrange
        var project = CreateSampleProject();
        project.ProjectName = "TestProject";

        // Act
        var filePath = await _sut.GenerateReportAsync(project, _tempDirectory);

        // Assert
        Path.GetFileName(filePath).Should().Contain("TestProject",
            "report filename should include the project name");
        Path.GetFileName(filePath).Should().EndWith("_report.html",
            "report filename should end with _report.html");
    }

    /// <summary>
    /// Creates a sample AnnotationProject with multiple classes and annotations for testing.
    /// </summary>
    private static AnnotationProject CreateSampleProject()
    {
        return new AnnotationProject
        {
            ProjectName = "TestProject",
            ProjectDirectory = "/tmp/test",
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "defect", Color = "#FF0000" },
                new() { Index = 1, Name = "scratch", Color = "#00FF00" },
                new() { Index = 2, Name = "dent", Color = "#0000FF" }
            },
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "image1.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "defect", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 },
                        new() { ClassIndex = 1, ClassName = "scratch", CenterX = 0.3, CenterY = 0.4, Width = 0.1, Height = 0.15 }
                    }
                },
                new()
                {
                    ImagePath = "image2.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 2, ClassName = "dent", CenterX = 0.7, CenterY = 0.6, Width = 0.15, Height = 0.2 }
                    }
                }
            }
        };
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
                // Best-effort cleanup; ignore failures in test teardown
            }
        }
    }
}
