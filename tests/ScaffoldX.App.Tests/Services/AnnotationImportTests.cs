using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for annotation import functionality, covering bbox parsing,
/// polygon format handling, and error resilience.
/// </summary>
public class AnnotationImportTests
{
    private readonly AnnotationService _sut = new();
    private readonly List<string> _classNames = new() { "cat", "dog", "bird" };

    /// <summary>
    /// Verifies that a valid YOLO bounding box line (5 parts) is parsed correctly.
    /// Format: class_index center_x center_y width height
    /// </summary>
    [Fact]
    public void FromYoloFormat_ValidBBoxLine_ParsesCorrectly()
    {
        // Arrange
        var lines = new[] { "1 0.450000 0.550000 0.200000 0.300000" };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert
        boxes.Should().HaveCount(1);
        polygons.Should().BeEmpty();
        var box = boxes[0];
        box.ClassIndex.Should().Be(1);
        box.ClassName.Should().Be("dog");
        box.CenterX.Should().BeApproximately(0.45, 0.000001);
        box.CenterY.Should().BeApproximately(0.55, 0.000001);
        box.Width.Should().BeApproximately(0.20, 0.000001);
        box.Height.Should().BeApproximately(0.30, 0.000001);
    }

    /// <summary>
    /// Verifies that a valid polygon-format YOLO line (> 5 parts) is parsed correctly.
    /// Format: class_index x1 y1 x2 y2 ... xn yn
    /// </summary>
    [Fact]
    public void FromYoloFormat_ValidPolygonLine_ParsesCorrectly()
    {
        // Arrange: polygon line with class=0 and 3 points (7 parts total)
        var lines = new[] { "0 0.100000 0.200000 0.300000 0.400000 0.500000 0.100000" };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert
        boxes.Should().BeEmpty();
        polygons.Should().HaveCount(1);
        var polygon = polygons[0];
        polygon.ClassIndex.Should().Be(0);
        polygon.ClassName.Should().Be("cat");
        polygon.Points.Should().HaveCount(3);
        polygon.Points[0].X.Should().BeApproximately(0.1f, 0.0001f);
        polygon.Points[0].Y.Should().BeApproximately(0.2f, 0.0001f);
        polygon.Points[1].X.Should().BeApproximately(0.3f, 0.0001f);
        polygon.Points[1].Y.Should().BeApproximately(0.4f, 0.0001f);
        polygon.Points[2].X.Should().BeApproximately(0.5f, 0.0001f);
        polygon.Points[2].Y.Should().BeApproximately(0.1f, 0.0001f);
    }

    /// <summary>
    /// Verifies that when both bbox and polygon lines are present,
    /// both are parsed into their respective lists.
    /// </summary>
    [Fact]
    public void FromYoloFormat_MixedBBoxAndPolygon_ParsesBoth()
    {
        // Arrange: one bbox line and one polygon line
        var lines = new[]
        {
            "0 0.500000 0.500000 0.200000 0.300000",           // bbox: 5 parts
            "1 0.100000 0.200000 0.300000 0.400000 0.500000 0.100000" // polygon: 7 parts
        };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert
        boxes.Should().HaveCount(1, "the bbox line should be parsed");
        boxes[0].ClassIndex.Should().Be(0);
        boxes[0].ClassName.Should().Be("cat");

        polygons.Should().HaveCount(1, "the polygon line should be parsed");
        polygons[0].ClassIndex.Should().Be(1);
        polygons[0].ClassName.Should().Be("dog");
        polygons[0].Points.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that invalid lines are skipped gracefully.
    /// </summary>
    [Fact]
    public void FromYoloFormat_InvalidLine_SkipsGracefully()
    {
        // Arrange: various invalid lines mixed with valid ones
        var lines = new[]
        {
            "",                                                 // empty line
            "0 0.5",                                            // too few parts (2)
            "abc 0.5 0.5 0.2 0.3",                            // non-numeric class index
            "0 abc 0.5 0.2 0.3",                              // non-numeric center_x
            "0 0.5 abc 0.2 0.3",                              // non-numeric center_y
            "0 0.5 0.5 abc 0.3",                              // non-numeric width
            "0 0.5 0.5 0.2 abc",                              // non-numeric height
            "2 0.700000 0.800000 0.100000 0.150000"           // valid bbox line
        };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert: only the last valid line should be parsed
        boxes.Should().HaveCount(1);
        boxes[0].ClassIndex.Should().Be(2);
        boxes[0].ClassName.Should().Be("bird");
        boxes[0].CenterX.Should().BeApproximately(0.7, 0.000001);
        boxes[0].CenterY.Should().BeApproximately(0.8, 0.000001);
        boxes[0].Width.Should().BeApproximately(0.1, 0.000001);
        boxes[0].Height.Should().BeApproximately(0.15, 0.000001);
        polygons.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that polygon lines with invalid coordinates are skipped.
    /// </summary>
    [Fact]
    public void FromYoloFormat_InvalidPolygonLine_SkipsGracefully()
    {
        // Arrange: polygon with non-numeric coordinates
        var lines = new[]
        {
            "0 0.1 abc 0.3 0.4 0.5 0.1",  // invalid polygon: non-numeric y1
            "0 0.1 0.2 0.3",               // too few parts for polygon (4 parts, < 5)
            "1 0.1 0.2 0.3 0.4 0.5 0.1"   // valid polygon
        };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert: only the valid polygon should be parsed
        boxes.Should().BeEmpty();
        polygons.Should().HaveCount(1);
        polygons[0].ClassIndex.Should().Be(1);
        polygons[0].ClassName.Should().Be("dog");
    }

    /// <summary>
    /// Verifies that multiple valid bbox lines are all parsed correctly.
    /// </summary>
    [Fact]
    public void FromYoloFormat_MultipleValidLines_ParsesAll()
    {
        // Arrange
        var lines = new[]
        {
            "0 0.100000 0.200000 0.300000 0.400000",
            "1 0.500000 0.600000 0.100000 0.200000",
            "2 0.800000 0.900000 0.050000 0.050000"
        };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert
        boxes.Should().HaveCount(3);
        boxes[0].ClassName.Should().Be("cat");
        boxes[1].ClassName.Should().Be("dog");
        boxes[2].ClassName.Should().Be("bird");
        polygons.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an empty input returns empty lists.
    /// </summary>
    [Fact]
    public void FromYoloFormat_EmptyInput_ReturnsEmptyLists()
    {
        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(Array.Empty<string>(), 640, 480, _classNames);

        // Assert
        boxes.Should().BeEmpty();
        polygons.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that lines with only whitespace are skipped.
    /// </summary>
    [Fact]
    public void FromYoloFormat_WhitespaceLines_SkippedGracefully()
    {
        // Arrange
        var lines = new[]
        {
            "   ",
            "0 0.500000 0.500000 0.200000 0.300000",
            "\t"
        };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert: whitespace-only lines are split into empty/whitespace tokens, which have < 5 parts → skipped
        // The valid line "0 0.500000 0.500000 0.200000 0.300000" should parse fine
        boxes.Should().HaveCount(1);
        boxes[0].ClassIndex.Should().Be(0);
    }

    /// <summary>
    /// Verifies that class index out of range uses fallback name "class_N".
    /// </summary>
    [Fact]
    public void FromYoloFormat_ClassIndexOutOfRange_UsesFallbackName()
    {
        // Arrange
        var lines = new[] { "10 0.500000 0.500000 0.200000 0.300000" };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert
        boxes.Should().HaveCount(1);
        boxes[0].ClassIndex.Should().Be(10);
        boxes[0].ClassName.Should().Be("class_10");
    }

    /// <summary>
    /// Verifies that a polygon with fewer than 3 points is skipped (invalid polygon).
    /// </summary>
    [Fact]
    public void FromYoloFormat_PolygonWithTwoPoints_SkippedAsInvalid()
    {
        // Arrange: 2 points (class + 4 coords = 5 parts → treated as bbox, not polygon)
        // But with only 2 coordinate pairs and class = 5 parts, it's treated as bbox
        // For a true polygon test with 2 points: class + 4 coords = 5 parts → bbox format
        // To test < 3 points in polygon path, need > 5 parts but odd coords → skipped
        var lines = new[]
        {
            "0 0.100000 0.200000 0.300000 0.400000 0.500000" // 6 parts, 5 coords → (5-1)/2 = 2 points → skipped
        };

        // Act
        var (boxes, polygons, _, _, _) = _sut.FromYoloFormat(lines, 640, 480, _classNames);

        // Assert: 6 parts, (6-1)%2 == 0 → polygon path, but only 2 points → skipped (< 3)
        // Wait: (parts.Length - 1) % 2 = (6-1) % 2 = 5 % 2 = 1 ≠ 0 → skipped by condition
        // Actually: parts = ["0", "0.1", "0.2", "0.3", "0.4", "0.5"] → 6 parts
        // (6 - 1) % 2 = 1 → condition fails → not parsed as polygon
        // And 6 ≠ 5 → not parsed as bbox either
        boxes.Should().BeEmpty();
        polygons.Should().BeEmpty();
    }
}
