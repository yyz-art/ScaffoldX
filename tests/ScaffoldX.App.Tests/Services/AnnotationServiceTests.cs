using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// AnnotationService 单元测试，覆盖 YOLO 格式转换的纯函数逻辑。
/// </summary>
public class AnnotationServiceTests
{
    private readonly AnnotationService _sut = new();

    // ── ToYoloFormat ─────────────────────────────────────────────────────────

    [Fact]
    public void ToYoloFormat_ShouldReturnEmptyList_WhenNoBoxes()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>()
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToYoloFormat_ShouldReturnCorrectFormat_WhenSingleBox()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>
            {
                new()
                {
                    ClassIndex = 0,
                    CenterX = 0.5,
                    CenterY = 0.5,
                    Width = 0.2,
                    Height = 0.3
                }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("0 0.500000 0.500000 0.200000 0.300000");
    }

    [Fact]
    public void ToYoloFormat_ShouldReturnCorrectFormat_WhenMultipleBoxes()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, CenterX = 0.1, CenterY = 0.2, Width = 0.3, Height = 0.4 },
                new() { ClassIndex = 1, CenterX = 0.6, CenterY = 0.7, Width = 0.15, Height = 0.25 },
                new() { ClassIndex = 2, CenterX = 0.9, CenterY = 0.1, Width = 0.05, Height = 0.05 }
            }
        };

        // Act
        var result = _sut.ToYoloFormat(annotation);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().StartWith("0 ");
        result[1].Should().StartWith("1 ");
        result[2].Should().StartWith("2 ");
    }

    // ── FromYoloFormat ───────────────────────────────────────────────────────

    [Fact]
    public void FromYoloFormat_ShouldReturnEmptyList_WhenNoLines()
    {
        // Arrange
        var classNames = new List<string> { "cat", "dog" };

        // Act
        var result = _sut.FromYoloFormat(Array.Empty<string>(), 640, 480, classNames);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FromYoloFormat_ShouldParseCorrectly_WhenValidLine()
    {
        // Arrange
        var lines = new[] { "0 0.500000 0.500000 0.200000 0.300000" };
        var classNames = new List<string> { "cat", "dog" };

        // Act
        var result = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        result.Should().HaveCount(1);
        result[0].ClassIndex.Should().Be(0);
        result[0].ClassName.Should().Be("cat");
        result[0].CenterX.Should().Be(0.5);
        result[0].CenterY.Should().Be(0.5);
        result[0].Width.Should().Be(0.2);
        result[0].Height.Should().Be(0.3);
    }

    [Fact]
    public void FromYoloFormat_ShouldSkipInvalidLines_WhenPartsCountWrong()
    {
        // Arrange
        var lines = new[] { "0 0.5 0.5", "1 0.5 0.5 0.2 0.3" }; // 第一行只有 3 部分
        var classNames = new List<string> { "cat", "dog" };

        // Act
        var result = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        result.Should().HaveCount(1);
        result[0].ClassIndex.Should().Be(1);
    }

    [Fact]
    public void FromYoloFormat_ShouldSkipInvalidLines_WhenNonNumeric()
    {
        // Arrange
        var lines = new[] { "abc 0.5 0.5 0.2 0.3", "0 0.5 0.5 0.2 0.3" };
        var classNames = new List<string> { "cat" };

        // Act
        var result = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public void FromYoloFormat_ShouldUseFallbackClassName_WhenIndexExceedsClassCount()
    {
        // Arrange
        var lines = new[] { "5 0.5 0.5 0.2 0.3" };
        var classNames = new List<string> { "cat", "dog" }; // 只有 2 个类别

        // Act
        var result = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        result.Should().HaveCount(1);
        result[0].ClassName.Should().Be("class_5");
    }

    [Fact]
    public void FromYoloFormat_ShouldHandleMultipleLines_WhenAllValid()
    {
        // Arrange
        var lines = new[]
        {
            "0 0.1 0.2 0.3 0.4",
            "1 0.5 0.6 0.7 0.8",
            "0 0.9 0.1 0.05 0.05"
        };
        var classNames = new List<string> { "cat", "dog" };

        // Act
        var result = _sut.FromYoloFormat(lines, 640, 480, classNames);

        // Assert
        result.Should().HaveCount(3);
        result[0].ClassName.Should().Be("cat");
        result[1].ClassName.Should().Be("dog");
        result[2].ClassName.Should().Be("cat");
    }

    // ── Round-trip: ToYolo → FromYolo ────────────────────────────────────────

    [Fact]
    public void RoundTrip_ShouldPreserveData_WhenConvertingToAndFromYolo()
    {
        // Arrange
        var classNames = new List<string> { "defect", "scratch", "dent" };
        var original = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 1920,
            ImageHeight = 1080,
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, ClassName = "defect", CenterX = 0.25, CenterY = 0.35, Width = 0.1, Height = 0.2 },
                new() { ClassIndex = 2, ClassName = "dent", CenterX = 0.75, CenterY = 0.65, Width = 0.15, Height = 0.25 }
            }
        };

        // Act
        var yoloLines = _sut.ToYoloFormat(original);
        var restored = _sut.FromYoloFormat(yoloLines, 1920, 1080, classNames);

        // Assert
        restored.Should().HaveCount(2);
        restored[0].ClassIndex.Should().Be(0);
        restored[0].ClassName.Should().Be("defect");
        restored[0].CenterX.Should().Be(0.25);
        restored[1].ClassIndex.Should().Be(2);
        restored[1].ClassName.Should().Be("dent");
        restored[1].Width.Should().Be(0.15);
    }
}
