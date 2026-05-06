using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for OBB (Oriented Bounding Box) YOLO export/import functionality in AnnotationService.
/// </summary>
public class AnnotationServiceObbTests
{
    private readonly AnnotationService _sut = new();

    /// <summary>
    /// Verifies that ToYoloFormat produces the correct YOLO OBB format for oriented bounding box annotations:
    /// class_index center_x center_y width height angle (6 values, 6-decimal precision).
    /// </summary>
    [Fact]
    public void ToYoloFormat_ObbAnnotation_CorrectFormat()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
            {
                new()
                {
                    ClassIndex = 0,
                    ClassName = "rotated_object",
                    CenterX = 0.5f,
                    CenterY = 0.5f,
                    Width = 0.2f,
                    Height = 0.1f,
                    Angle = 0.7854f
                }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(1);
        var parts = result[0].Split(' ');
        parts.Should().HaveCount(6, "OBB format is: class cx cy w h angle");
        parts[0].Should().Be("0", "class index");
        float.Parse(parts[1]).Should().BeApproximately(0.5f, 0.0001f, "center X");
        float.Parse(parts[2]).Should().BeApproximately(0.5f, 0.0001f, "center Y");
        float.Parse(parts[3]).Should().BeApproximately(0.2f, 0.0001f, "width");
        float.Parse(parts[4]).Should().BeApproximately(0.1f, 0.0001f, "height");
        float.Parse(parts[5]).Should().BeApproximately(0.7854f, 0.0001f, "angle");
    }

    /// <summary>
    /// Verifies that ToYoloFormat uses 6-decimal precision for OBB coordinates.
    /// </summary>
    [Fact]
    public void ToYoloFormat_Obb_UsesSixDecimalPrecision()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
            {
                new()
                {
                    ClassIndex = 0,
                    CenterX = 0.123456789f,
                    CenterY = 0.987654321f,
                    Width = 0.111111111f,
                    Height = 0.222222222f,
                    Angle = 0.333333333f
                }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(1);
        var parts = result[0].Split(' ');
        parts[1].Should().Be("0.123457");
        parts[2].Should().Be("0.987654");
        parts[3].Should().Be("0.111111");
        parts[4].Should().Be("0.222222");
        parts[5].Should().Be("0.333333");
    }

    /// <summary>
    /// Verifies that ToYoloFormat handles multiple OBB annotations with different class indices.
    /// </summary>
    [Fact]
    public void ToYoloFormat_MultipleObb_ProducesMultipleLines()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, CenterX = 0.3f, CenterY = 0.4f, Width = 0.1f, Height = 0.05f, Angle = 0.5f },
                new() { ClassIndex = 2, CenterX = 0.7f, CenterY = 0.8f, Width = 0.2f, Height = 0.15f, Angle = 1.2f }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().StartWith("0 ");
        result[1].Should().StartWith("2 ");
    }

    /// <summary>
    /// Verifies that ToYoloFormat handles annotations with mixed types (boxes, polygons, OBB).
    /// </summary>
    [Fact]
    public void ToYoloFormat_MixedTypes_ProducesCorrectLines()
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
            Polygons = new List<ScaffoldX.App.Models.PolygonAnnotation>
            {
                new()
                {
                    ClassIndex = 1,
                    Points = new List<System.Drawing.PointF> { new(0.1f, 0.2f), new(0.3f, 0.4f), new(0.5f, 0.1f) }
                }
            },
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
            {
                new() { ClassIndex = 2, CenterX = 0.6f, CenterY = 0.7f, Width = 0.15f, Height = 0.1f, Angle = 0.8f }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert: box (5 values), polygon (7 values), OBB (6 values)
        result.Should().HaveCount(3);
        result[0].Split(' ').Should().HaveCount(5, "bbox format has 5 values");
        result[1].Split(' ').Should().HaveCount(7, "polygon format has 7 values (class + 3 points * 2)");
        result[2].Split(' ').Should().HaveCount(6, "OBB format has 6 values (class + cx + cy + w + h + angle)");
        result[2].Should().StartWith("2 ");
    }

    /// <summary>
    /// Verifies that FromYoloFormat correctly parses a 6-value line as an OBB annotation.
    /// Format: class_index center_x center_y width height angle (6 values total).
    /// </summary>
    [Fact]
    public void FromYoloFormat_SixValues_ParsesAsObb()
    {
        // Arrange: 6-value line = class + cx + cy + w + h + angle
        var lines = new[] { "1 0.450000 0.550000 0.200000 0.300000 0.785400" };
        var classNames = new List<string> { "cat", "dog" };

        // Act
        var (boxes, polygons, polylines, circles, orientedBoxes) = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        boxes.Should().BeEmpty();
        polygons.Should().BeEmpty();
        polylines.Should().BeEmpty();
        circles.Should().BeEmpty();
        orientedBoxes.Should().HaveCount(1);
        orientedBoxes[0].ClassIndex.Should().Be(1);
        orientedBoxes[0].ClassName.Should().Be("dog");
        orientedBoxes[0].CenterX.Should().BeApproximately(0.45f, 0.0001f);
        orientedBoxes[0].CenterY.Should().BeApproximately(0.55f, 0.0001f);
        orientedBoxes[0].Width.Should().BeApproximately(0.20f, 0.0001f);
        orientedBoxes[0].Height.Should().BeApproximately(0.30f, 0.0001f);
        orientedBoxes[0].Angle.Should().BeApproximately(0.7854f, 0.0001f);
    }

    /// <summary>
    /// Verifies that FromYoloFormat correctly handles mixed bbox and OBB lines.
    /// </summary>
    [Fact]
    public void FromYoloFormat_MixedBBoxAndObb_ParsesBoth()
    {
        // Arrange
        var lines = new[]
        {
            "0 0.500000 0.500000 0.200000 0.300000",                      // bbox: 5 parts
            "1 0.450000 0.550000 0.200000 0.300000 0.785400"            // OBB: 6 parts
        };
        var classNames = new List<string> { "cat", "dog" };

        // Act
        var (boxes, polygons, polylines, circles, orientedBoxes) = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        boxes.Should().HaveCount(1);
        boxes[0].ClassIndex.Should().Be(0);
        boxes[0].ClassName.Should().Be("cat");

        orientedBoxes.Should().HaveCount(1);
        orientedBoxes[0].ClassIndex.Should().Be(1);
        orientedBoxes[0].ClassName.Should().Be("dog");
        orientedBoxes[0].Angle.Should().BeApproximately(0.7854f, 0.0001f);

        polygons.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FromYoloFormat handles OBB with fallback class name when index exceeds class count.
    /// </summary>
    [Fact]
    public void FromYoloFormat_ObbClassIndexOutOfRange_UsesFallbackName()
    {
        // Arrange
        var lines = new[] { "5 0.500000 0.500000 0.200000 0.300000 1.000000" };
        var classNames = new List<string> { "cat", "dog" }; // only 2 classes

        // Act
        var (boxes, polygons, polylines, circles, orientedBoxes) = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        boxes.Should().BeEmpty();
        polygons.Should().BeEmpty();
        polylines.Should().BeEmpty();
        circles.Should().BeEmpty();
        orientedBoxes.Should().HaveCount(1);
        orientedBoxes[0].ClassIndex.Should().Be(5);
        orientedBoxes[0].ClassName.Should().Be("class_5");
    }

    /// <summary>
    /// Verifies that FromYoloFormat handles empty input for OBB correctly.
    /// </summary>
    [Fact]
    public void FromYoloFormat_EmptyInput_ReturnsEmptyObbList()
    {
        // Arrange
        var classNames = new List<string> { "cat" };

        // Act
        var (boxes, polygons, polylines, circles, orientedBoxes) = _sut.FromYoloFormat(Array.Empty<string>(), 640, 480, classNames);

        // Assert
        boxes.Should().BeEmpty();
        polygons.Should().BeEmpty();
        polylines.Should().BeEmpty();
        circles.Should().BeEmpty();
        orientedBoxes.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FromYoloFormat skips OBB lines with non-numeric values.
    /// </summary>
    [Fact]
    public void FromYoloFormat_InvalidObbLine_SkipsGracefully()
    {
        // Arrange: invalid OBB (non-numeric angle) mixed with valid bbox
        var lines = new[]
        {
            "0 0.500000 0.500000 0.200000 0.300000 abc",             // invalid OBB (6 parts, non-numeric angle)
            "1 0.400000 0.600000 0.150000 0.250000"                // valid bbox
        };
        var classNames = new List<string> { "cat", "dog" };

        // Act
        var (boxes, polygons, polylines, circles, orientedBoxes) = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert: invalid OBB is skipped, valid bbox is parsed
        boxes.Should().HaveCount(1);
        boxes[0].ClassIndex.Should().Be(1);
        orientedBoxes.Should().BeEmpty();
        polygons.Should().BeEmpty();
    }
}
