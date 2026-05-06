using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for polygon annotation YOLO export functionality in AnnotationService.
/// </summary>
public class AnnotationServicePolygonTests
{
    private readonly AnnotationService _sut = new();

    /// <summary>
    /// Verifies that ToYoloFormat produces the correct YOLO segmentation format for polygon annotations:
    /// class_index x1 y1 x2 y2 ... xn yn (normalized coordinates).
    /// </summary>
    [Fact]
    public void ToYoloFormat_PolygonAnnotation_CorrectFormat()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Polygons = new List<PolygonAnnotation>
            {
                new()
                {
                    ClassIndex = 0,
                    ClassName = "defect",
                    Points = new List<PointF>
                    {
                        new(0.1f, 0.2f),
                        new(0.3f, 0.4f),
                        new(0.5f, 0.1f)
                    }
                }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().StartWith("0 ", "first value should be class index 0");

        // Parse and verify coordinates
        var parts = result[0].Split(' ');
        parts.Should().HaveCount(7, "format is: class_index + 3 points * 2 coords = 7 values");
        parts[0].Should().Be("0");
        float.Parse(parts[1]).Should().BeApproximately(0.1f, 0.0001f);
        float.Parse(parts[2]).Should().BeApproximately(0.2f, 0.0001f);
        float.Parse(parts[3]).Should().BeApproximately(0.3f, 0.0001f);
        float.Parse(parts[4]).Should().BeApproximately(0.4f, 0.0001f);
        float.Parse(parts[5]).Should().BeApproximately(0.5f, 0.0001f);
        float.Parse(parts[6]).Should().BeApproximately(0.1f, 0.0001f);
    }

    /// <summary>
    /// Verifies that ToYoloFormat correctly handles multiple polygons with different class indices.
    /// </summary>
    [Fact]
    public void ToYoloFormat_MultiplePolygons_ProducesMultipleLines()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Polygons = new List<PolygonAnnotation>
            {
                new()
                {
                    ClassIndex = 0,
                    Points = new List<PointF> { new(0.1f, 0.2f), new(0.3f, 0.4f), new(0.5f, 0.1f) }
                },
                new()
                {
                    ClassIndex = 2,
                    Points = new List<PointF> { new(0.6f, 0.7f), new(0.8f, 0.9f), new(0.7f, 0.6f), new(0.6f, 0.8f) }
                }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().StartWith("0 ");
        result[1].Should().StartWith("2 ");

        // Verify second polygon has 4 points (8 coords + class = 9 values)
        var parts = result[1].Split(' ');
        parts.Should().HaveCount(9);
    }

    /// <summary>
    /// Verifies that ToYoloFormat correctly handles annotations with both boxes and polygons.
    /// </summary>
    [Fact]
    public void ToYoloFormat_MixedBoxesAndPolygons_ProducesCorrectLines()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
            },
            Polygons = new List<PolygonAnnotation>
            {
                new()
                {
                    ClassIndex = 1,
                    Points = new List<PointF> { new(0.1f, 0.2f), new(0.3f, 0.4f), new(0.5f, 0.1f) }
                }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert: box lines come first, then polygon lines
        result.Should().HaveCount(2);
        result[0].Split(' ').Should().HaveCount(5, "bbox format has 5 values");
        result[1].Split(' ').Should().HaveCount(7, "polygon format has 7 values (class + 3 points * 2)");
    }

    /// <summary>
    /// Verifies that polygon YOLO format preserves data through a conceptual round-trip.
    /// Since FromYoloFormat does not yet parse polygon lines (it skips lines with != 5 parts),
    /// this test verifies the export output matches the expected YOLO segmentation format.
    /// </summary>
    [Fact]
    public void ToYoloFormat_PolygonRoundTrip_PreservesData()
    {
        // Arrange: create a polygon with known coordinates
        var originalPoints = new List<PointF>
        {
            new(0.25f, 0.35f),
            new(0.75f, 0.35f),
            new(0.75f, 0.65f),
            new(0.25f, 0.65f)
        };

        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Polygons = new List<PolygonAnnotation>
            {
                new()
                {
                    ClassIndex = 3,
                    ClassName = "region",
                    Points = originalPoints
                }
            }
        };

        // Act
        var yoloLines = _sut.ToYoloFormat(annotation);

        // Assert: verify the exported line can be parsed back to recover coordinates
        yoloLines.Should().HaveCount(1);

        var parts = yoloLines[0].Split(' ');
        parts[0].Should().Be("3", "class index should be preserved");

        // Parse coordinates back and compare
        var recoveredPoints = new List<PointF>();
        for (int i = 1; i < parts.Length; i += 2)
        {
            recoveredPoints.Add(new PointF(
                float.Parse(parts[i]),
                float.Parse(parts[i + 1])));
        }

        recoveredPoints.Should().HaveCount(4, "should recover all 4 polygon vertices");
        for (int i = 0; i < originalPoints.Count; i++)
        {
            recoveredPoints[i].X.Should().BeApproximately(originalPoints[i].X, 0.0001f);
            recoveredPoints[i].Y.Should().BeApproximately(originalPoints[i].Y, 0.0001f);
        }
    }

    /// <summary>
    /// Verifies that polygon export uses 6-decimal precision for coordinates.
    /// </summary>
    [Fact]
    public void ToYoloFormat_Polygon_UsesSixDecimalPrecision()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            Polygons = new List<PolygonAnnotation>
            {
                new()
                {
                    ClassIndex = 0,
                    Points = new List<PointF> { new(0.123456789f, 0.987654321f) }
                }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(1);
        var parts = result[0].Split(' ');
        parts[1].Should().Be("0.123457", "X coordinate should be formatted to 6 decimal places");
        parts[2].Should().Be("0.987654", "Y coordinate should be formatted to 6 decimal places");
    }

    /// <summary>
    /// Integration test: verifies that ExportYoloDatasetAsync writes polygon annotations
    /// to label files in the correct YOLO segmentation format.
    /// </summary>
    [Fact]
    public async Task ExportYoloDataset_WithPolygons_GeneratesCorrectFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_Test_{Guid.NewGuid():N}");
        var projectDir = Path.Combine(tempDir, "project");
        var outputDir = Path.Combine(tempDir, "output");

        try
        {
            Directory.CreateDirectory(projectDir);

            // Create a minimal test image file
            var imagesDir = Path.Combine(projectDir, "images");
            Directory.CreateDirectory(imagesDir);
            var testImagePath = Path.Combine(imagesDir, "test001.jpg");

            // Create a minimal valid JPEG (1x1 pixel)
            using (var bmp = new Bitmap(100, 100))
            {
                bmp.Save(testImagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            var project = new AnnotationProject
            {
                ProjectName = "TestProject",
                ProjectDirectory = projectDir,
                Classes = new List<AnnotationClass>
                {
                    new() { Index = 0, Name = "defect" },
                    new() { Index = 1, Name = "scratch" }
                },
                Annotations = new List<AnnotationData>
                {
                    new()
                    {
                        ImagePath = testImagePath,
                        ImageWidth = 100,
                        ImageHeight = 100,
                        Boxes = new List<BoundingBoxAnnotation>(),
                        Polygons = new List<PolygonAnnotation>
                        {
                            new()
                            {
                                ClassIndex = 0,
                                ClassName = "defect",
                                Points = new List<PointF>
                                {
                                    new(0.1f, 0.2f),
                                    new(0.3f, 0.4f),
                                    new(0.5f, 0.1f)
                                }
                            }
                        }
                    }
                }
            };

            // Act
            await _sut.ExportYoloDatasetAsync(project, outputDir);

            // Assert: verify label files were created
            var trainLabelsDir = Path.Combine(outputDir, "train", "labels");
            var valLabelsDir = Path.Combine(outputDir, "val", "labels");

            // One of train/val should contain our label file
            var trainLabelPath = Path.Combine(trainLabelsDir, "test001.txt");
            var valLabelPath = Path.Combine(valLabelsDir, "test001.txt");

            var labelExistsInTrain = File.Exists(trainLabelPath);
            var labelExistsInVal = File.Exists(valLabelPath);

            (labelExistsInTrain || labelExistsInVal).Should().BeTrue(
                "label file should be created in either train or val directory");

            var labelPath = labelExistsInTrain ? trainLabelPath : valLabelPath;
            var labelContent = await File.ReadAllTextAsync(labelPath);

            // Verify polygon format in label file
            labelContent.Should().Contain("0 0.1", "polygon class index and coordinates should be in label file");
            var lines = labelContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(1, "there should be exactly one annotation line");
            var labelParts = lines[0].Trim().Split(' ');
            labelParts[0].Should().Be("0");
            labelParts.Should().HaveCount(7, "polygon with 3 points: class + 6 coords");

            // Verify data.yaml was created
            var dataYamlPath = Path.Combine(outputDir, "data.yaml");
            File.Exists(dataYamlPath).Should().BeTrue("data.yaml should be created");

            // Verify classes.txt was created
            var classesPath = Path.Combine(outputDir, "classes.txt");
            File.Exists(classesPath).Should().BeTrue("classes.txt should be created");
            var classes = await File.ReadAllLinesAsync(classesPath);
            classes.Should().Contain("defect");
            classes.Should().Contain("scratch");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
