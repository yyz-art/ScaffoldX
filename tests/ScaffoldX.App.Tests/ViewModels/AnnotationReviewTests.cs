using System.Drawing;
using FluentAssertions;
using ScaffoldX.App.Models;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for the annotation review summary logic, covering image counts,
/// unannotated image detection, and review summary text generation.
/// Mirrors the UpdateReviewSummary logic from AnnotationViewModel to test
/// the computation independently of the Prism ViewModel infrastructure.
/// </summary>
public class AnnotationReviewTests
{
    /// <summary>
    /// Computes the review summary text from an AnnotationProject,
    /// replicating AnnotationViewModel.UpdateReviewSummary logic for testability.
    /// </summary>
    private static (string SummaryText, int UnannotatedCount, bool HasUnannotated) ComputeReviewSummary(AnnotationProject? project)
    {
        if (project == null)
            return (string.Empty, 0, false);

        var totalImages = project.Annotations.Count;
        var annotatedCount = project.Annotations.Count(a =>
            a.Boxes.Count > 0 || a.Polygons.Count > 0 || a.OrientedBoxes.Count > 0);
        var unannotatedCount = totalImages - annotatedCount;
        var percentage = totalImages > 0 ? annotatedCount * 100.0 / totalImages : 0;

        var totalBoxes = 0;
        var totalPolygons = 0;
        var totalObbs = 0;
        var distribution = new Dictionary<string, int>();

        foreach (var annotation in project.Annotations)
        {
            totalBoxes += annotation.Boxes.Count;
            totalPolygons += annotation.Polygons.Count;
            totalObbs += annotation.OrientedBoxes.Count;

            foreach (var box in annotation.Boxes)
                distribution[box.ClassName] = distribution.GetValueOrDefault(box.ClassName) + 1;
            foreach (var polygon in annotation.Polygons)
                distribution[polygon.ClassName] = distribution.GetValueOrDefault(polygon.ClassName) + 1;
            foreach (var obb in annotation.OrientedBoxes)
                distribution[obb.ClassName] = distribution.GetValueOrDefault(obb.ClassName) + 1;
        }

        var totalAnnotations = totalBoxes + totalPolygons + totalObbs;

        var classLine = distribution.Count == 0
            ? "暂无类别数据"
            : string.Join(", ", distribution.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}"));

        var summaryText =
            $"总图像: {totalImages}\n" +
            $"已标注: {annotatedCount} ({percentage:F1}%)\n" +
            $"未标注: {unannotatedCount}\n" +
            $"总标注: {totalAnnotations} (边界框: {totalBoxes}, 多边形: {totalPolygons}, OBB: {totalObbs})\n" +
            $"类别分布: {classLine}";

        return (summaryText, unannotatedCount, unannotatedCount > 0);
    }

    [Fact]
    public void ReviewSummaryText_WithAnnotations_ShowsCounts()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
                    }
                },
                new()
                {
                    ImagePath = "img2.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 1, ClassName = "dog", CenterX = 0.3, CenterY = 0.4, Width = 0.1, Height = 0.1 },
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.7, CenterY = 0.6, Width = 0.15, Height = 0.2 }
                    }
                },
                new()
                {
                    ImagePath = "img3.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>()
                }
            }
        };

        // Act
        var (summaryText, _, _) = ComputeReviewSummary(project);

        // Assert
        summaryText.Should().Contain("总图像: 3");
        summaryText.Should().Contain("已标注: 2");
        summaryText.Should().Contain("未标注: 1");
        summaryText.Should().Contain("边界框: 3");
        summaryText.Should().Contain("cat: 2");
        summaryText.Should().Contain("dog: 1");
    }

    [Fact]
    public void UnannotatedImageCount_WithMixedImages_ReturnsCorrectCount()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
                    }
                },
                new()
                {
                    ImagePath = "img2.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>()
                },
                new()
                {
                    ImagePath = "img3.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>()
                },
                new()
                {
                    ImagePath = "img4.jpg", ImageWidth = 640, ImageHeight = 480,
                    Polygons = new List<PolygonAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 1, ClassName = "scratch",
                            Points = new List<PointF> { new(0.1f, 0.1f), new(0.3f, 0.1f), new(0.2f, 0.3f) }
                        }
                    }
                },
                new()
                {
                    ImagePath = "img5.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>()
                }
            }
        };

        // Act
        var (_, unannotatedCount, _) = ComputeReviewSummary(project);

        // Assert: img1 (boxes), img4 (polygon) are annotated; img2, img3, img5 are unannotated
        unannotatedCount.Should().Be(3, "img2, img3, and img5 have no annotations");
    }

    [Fact]
    public void HasUnannotatedImages_WhenAllAnnotated_ReturnsFalse()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
                    }
                },
                new()
                {
                    ImagePath = "img2.jpg", ImageWidth = 640, ImageHeight = 480,
                    Polygons = new List<PolygonAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 1, ClassName = "scratch",
                            Points = new List<PointF> { new(0.1f, 0.1f), new(0.3f, 0.1f), new(0.2f, 0.3f) }
                        }
                    }
                }
            }
        };

        // Act
        var (_, unannotatedCount, hasUnannotated) = ComputeReviewSummary(project);

        // Assert
        hasUnannotated.Should().BeFalse("all 2 images have annotations");
        unannotatedCount.Should().Be(0);
    }

    [Fact]
    public void HasUnannotatedImages_WhenSomeUnannotated_ReturnsTrue()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
                    }
                },
                new()
                {
                    ImagePath = "img2.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>()
                }
            }
        };

        // Act
        var (_, unannotatedCount, hasUnannotated) = ComputeReviewSummary(project);

        // Assert
        hasUnannotated.Should().BeTrue("1 of 2 images is unannotated");
        unannotatedCount.Should().Be(1);
    }

    [Fact]
    public void ReviewSummaryText_WithNoProject_ReturnsEmpty()
    {
        // Act
        var (summaryText, unannotatedCount, hasUnannotated) = ComputeReviewSummary(null);

        // Assert
        summaryText.Should().BeEmpty();
        unannotatedCount.Should().Be(0);
        hasUnannotated.Should().BeFalse();
    }

    [Fact]
    public void ReviewSummaryText_IncludesAnnotationTypeDistribution()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg", ImageWidth = 640, ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "defect", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
                    },
                    Polygons = new List<PolygonAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 1, ClassName = "scratch",
                            Points = new List<PointF> { new(0.1f, 0.1f), new(0.3f, 0.1f), new(0.2f, 0.3f) }
                        }
                    },
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 2, ClassName = "part",
                            CenterX = 0.5f, CenterY = 0.5f, Width = 0.2f, Height = 0.1f, Angle = 0.3f
                        }
                    }
                }
            }
        };

        // Act
        var (summaryText, _, _) = ComputeReviewSummary(project);

        // Assert
        summaryText.Should().Contain("总标注: 3");
        summaryText.Should().Contain("边界框: 1");
        summaryText.Should().Contain("多边形: 1");
        summaryText.Should().Contain("OBB: 1");
    }
}
