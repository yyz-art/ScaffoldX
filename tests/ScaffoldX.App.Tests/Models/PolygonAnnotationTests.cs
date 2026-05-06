using System.Drawing;
using FluentAssertions;
using ScaffoldX.App.Models;
using Xunit;

namespace ScaffoldX.App.Tests.Models;

/// <summary>
/// Unit tests for the PolygonAnnotation model and its relationship with AnnotationData.
/// </summary>
public class PolygonAnnotationTests
{
    /// <summary>
    /// Verifies that a new PolygonAnnotation has correct default property values.
    /// </summary>
    [Fact]
    public void PolygonAnnotation_DefaultValues_AreCorrect()
    {
        // Act
        var polygon = new PolygonAnnotation();

        // Assert
        polygon.Id.Should().NotBeNullOrEmpty("Id should be auto-generated as a GUID");
        Guid.TryParse(polygon.Id, out _).Should().BeTrue("Id should be a valid GUID string");
        polygon.ClassIndex.Should().Be(0, "default ClassIndex should be 0");
        polygon.ClassName.Should().Be(string.Empty, "default ClassName should be empty string");
        polygon.Points.Should().NotBeNull("Points list should be initialized");
        polygon.Points.Should().BeEmpty("default Points list should be empty");
    }

    /// <summary>
    /// Verifies that adding points to the Points list works correctly.
    /// </summary>
    [Fact]
    public void PolygonAnnotation_AddPoints_WorksCorrectly()
    {
        // Arrange
        var polygon = new PolygonAnnotation
        {
            ClassIndex = 1,
            ClassName = "defect"
        };

        var point1 = new PointF(0.1f, 0.2f);
        var point2 = new PointF(0.3f, 0.4f);
        var point3 = new PointF(0.5f, 0.1f);

        // Act
        polygon.Points.Add(point1);
        polygon.Points.Add(point2);
        polygon.Points.Add(point3);

        // Assert
        polygon.Points.Should().HaveCount(3);
        polygon.Points[0].X.Should().BeApproximately(0.1f, 0.001f);
        polygon.Points[0].Y.Should().BeApproximately(0.2f, 0.001f);
        polygon.Points[1].X.Should().BeApproximately(0.3f, 0.001f);
        polygon.Points[1].Y.Should().BeApproximately(0.4f, 0.001f);
        polygon.Points[2].X.Should().BeApproximately(0.5f, 0.001f);
        polygon.Points[2].Y.Should().BeApproximately(0.1f, 0.001f);
    }

    /// <summary>
    /// Verifies that AnnotationData includes a Polygons property that initializes as an empty list.
    /// </summary>
    [Fact]
    public void AnnotationData_IncludesPolygons()
    {
        // Act
        var annotationData = new AnnotationData();

        // Assert
        annotationData.Polygons.Should().NotBeNull("Polygons should be initialized");
        annotationData.Polygons.Should().BeEmpty("default Polygons list should be empty");
    }

    /// <summary>
    /// Verifies that AnnotationData supports both Boxes and Polygons simultaneously.
    /// </summary>
    [Fact]
    public void AnnotationData_SupportsBoxesAndPolygons_Simultaneously()
    {
        // Arrange & Act
        var annotationData = new AnnotationData
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
                    ClassName = "scratch",
                    Points = new List<PointF>
                    {
                        new(0.1f, 0.2f),
                        new(0.3f, 0.4f),
                        new(0.5f, 0.2f)
                    }
                }
            }
        };

        // Assert
        annotationData.Boxes.Should().HaveCount(1);
        annotationData.Polygons.Should().HaveCount(1);
        annotationData.Polygons[0].Points.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that each PolygonAnnotation instance gets a unique Id.
    /// </summary>
    [Fact]
    public void PolygonAnnotation_MultipleInstances_HaveUniqueIds()
    {
        // Act
        var polygon1 = new PolygonAnnotation();
        var polygon2 = new PolygonAnnotation();

        // Assert
        polygon1.Id.Should().NotBe(polygon2.Id, "each instance should have a unique GUID");
    }
}
