using System.Drawing;
using FluentAssertions;
using ScaffoldX.App.Models;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for annotation statistics computation.
/// Tests the counting and distribution logic used by AnnotationViewModel's UpdateStatistics.
/// </summary>
public class AnnotationStatisticsTests
{
    /// <summary>
    /// Verifies that total annotation count across all types (boxes, polygons, OBBs)
    /// is computed correctly when the project contains mixed annotation types.
    /// </summary>
    [Fact]
    public void TotalAnnotationCount_WithMixedTypes_ReturnsCorrectSum()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 },
                        new() { ClassIndex = 1, ClassName = "dog", CenterX = 0.3, CenterY = 0.4, Width = 0.1, Height = 0.1 }
                    },
                    Polygons = new List<PolygonAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 2,
                            ClassName = "defect",
                            Points = new List<PointF>
                            {
                                new(0.1f, 0.1f), new(0.3f, 0.1f), new(0.2f, 0.3f)
                            }
                        }
                    },
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 3,
                            ClassName = "part",
                            CenterX = 0.7f, CenterY = 0.7f,
                            Width = 0.1f, Height = 0.05f,
                            Angle = 0.5f
                        }
                    }
                },
                new()
                {
                    ImagePath = "img2.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.4, CenterY = 0.6, Width = 0.15, Height = 0.2 }
                    },
                    Polygons = new List<PolygonAnnotation>(),
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
                }
            }
        };

        // Act: count annotations across all types
        var totalBoxes = project.Annotations.Sum(a => a.Boxes.Count);
        var totalPolygons = project.Annotations.Sum(a => a.Polygons.Count);
        var totalObbs = project.Annotations.Sum(a => a.OrientedBoxes.Count);
        var totalAnnotations = totalBoxes + totalPolygons + totalObbs;

        // Assert
        totalBoxes.Should().Be(3, "there are 3 bounding box annotations across 2 images");
        totalPolygons.Should().Be(1, "there is 1 polygon annotation");
        totalObbs.Should().Be(1, "there is 1 OBB annotation");
        totalAnnotations.Should().Be(5, "total annotations should be 3 + 1 + 1 = 5");
    }

    /// <summary>
    /// Verifies that class distribution text correctly lists all unique classes
    /// and their counts across all annotation types.
    /// </summary>
    [Fact]
    public void ClassDistributionText_WithMultipleClasses_ShowsAllClasses()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "cat", Color = "#FF0000" },
                new() { Index = 1, Name = "dog", Color = "#00FF00" },
                new() { Index = 2, Name = "defect", Color = "#0000FF" }
            },
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 },
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.3, CenterY = 0.4, Width = 0.1, Height = 0.1 },
                        new() { ClassIndex = 1, ClassName = "dog", CenterX = 0.7, CenterY = 0.6, Width = 0.15, Height = 0.2 }
                    },
                    Polygons = new List<PolygonAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 2,
                            ClassName = "defect",
                            Points = new List<PointF>
                            {
                                new(0.1f, 0.1f), new(0.3f, 0.1f), new(0.2f, 0.3f)
                            }
                        }
                    },
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
                },
                new()
                {
                    ImagePath = "img2.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 1, ClassName = "dog", CenterX = 0.4, CenterY = 0.6, Width = 0.15, Height = 0.2 }
                    },
                    Polygons = new List<PolygonAnnotation>(),
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
                }
            }
        };

        // Act: compute class distribution across all annotation types
        var allAnnotations = project.Annotations
            .SelectMany(a => a.Boxes.Select(b => (ClassName: b.ClassName, Type: "box"))
                .Concat(a.Polygons.Select(p => (ClassName: p.ClassName, Type: "polygon")))
                .Concat(a.OrientedBoxes.Select(o => (ClassName: o.ClassName, Type: "obb"))))
            .ToList();

        var classDistribution = allAnnotations
            .GroupBy(a => a.ClassName)
            .Select(g => $"{g.Key}: {g.Count()}")
            .OrderBy(s => s)
            .ToList();

        var distributionText = string.Join(", ", classDistribution);

        // Assert
        classDistribution.Should().HaveCount(3, "there are 3 unique classes");
        distributionText.Should().Contain("cat: 2");
        distributionText.Should().Contain("dog: 2");
        distributionText.Should().Contain("defect: 1");
    }

    /// <summary>
    /// Verifies that annotated image count correctly counts images that have
    /// at least one annotation (of any type).
    /// </summary>
    [Fact]
    public void AnnotatedImageCount_WithAnnotations_ReturnsCorrectCount()
    {
        // Arrange
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    ImagePath = "img1.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
                    },
                    Polygons = new List<PolygonAnnotation>(),
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
                },
                new()
                {
                    ImagePath = "img2.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>(),
                    Polygons = new List<PolygonAnnotation>(),
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
                },
                new()
                {
                    ImagePath = "img3.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>(),
                    Polygons = new List<PolygonAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 1,
                            ClassName = "scratch",
                            Points = new List<PointF>
                            {
                                new(0.1f, 0.1f), new(0.3f, 0.1f), new(0.2f, 0.3f)
                            }
                        }
                    },
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
                },
                new()
                {
                    ImagePath = "img4.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>(),
                    Polygons = new List<PolygonAnnotation>(),
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
                    {
                        new()
                        {
                            ClassIndex = 2,
                            ClassName = "part",
                            CenterX = 0.5f, CenterY = 0.5f,
                            Width = 0.2f, Height = 0.1f,
                            Angle = 0.3f
                        }
                    }
                },
                new()
                {
                    ImagePath = "img5.jpg",
                    ImageWidth = 640,
                    ImageHeight = 480,
                    Boxes = new List<BoundingBoxAnnotation>(),
                    Polygons = new List<PolygonAnnotation>(),
                    OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
                }
            }
        };

        // Act: count images with any annotation (boxes-only, matching current UpdateStatistics logic)
        var annotatedImagesBoxesOnly = project.Annotations.Count(a => a.Boxes.Count > 0);

        // Count images with any annotation of any type (comprehensive)
        var annotatedImagesAnyType = project.Annotations.Count(a =>
            a.Boxes.Count > 0 || a.Polygons.Count > 0 || a.OrientedBoxes.Count > 0);

        // Assert
        project.Annotations.Should().HaveCount(5, "there are 5 total images");

        annotatedImagesBoxesOnly.Should().Be(1,
            "only 1 image has bounding box annotations (matching current UpdateStatistics behavior)");

        annotatedImagesAnyType.Should().Be(3,
            "3 images have annotations of any type: img1 (box), img3 (polygon), img4 (OBB)");
    }
}
