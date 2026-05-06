using System.Drawing;
using FluentAssertions;
using ScaffoldX.App.Models;
using Xunit;

namespace ScaffoldX.App.Tests.Models;

/// <summary>
/// Unit tests for PolylineAnnotation and CircleAnnotation models,
/// verifying default values, property assignment, and integration with AnnotationData.
/// </summary>
public class PolylineCircleAnnotationTests
{
    /// <summary>
    /// Verifies that a new PolylineAnnotation has correct default property values.
    /// Id should be auto-generated, numeric fields zeroed, collections initialized.
    /// </summary>
    [Fact]
    public void PolylineAnnotation_DefaultValues_AreCorrect()
    {
        // Act
        var polyline = new PolylineAnnotation();

        // Assert
        polyline.Id.Should().NotBeNullOrEmpty("Id should be auto-generated as a GUID");
        Guid.TryParse(polyline.Id, out _).Should().BeTrue("Id should be a valid GUID string");
        polyline.ClassIndex.Should().Be(0, "default ClassIndex should be 0");
        polyline.ClassName.Should().Be(string.Empty, "default ClassName should be empty string");
        polyline.Points.Should().NotBeNull("Points list should be initialized");
        polyline.Points.Should().BeEmpty("default Points list should be empty");
        polyline.IsClosed.Should().BeFalse("default IsClosed should be false (open polyline)");
    }

    /// <summary>
    /// Verifies that a new CircleAnnotation has correct default property values.
    /// Id should be auto-generated, center and radius zeroed, ClassName empty.
    /// </summary>
    [Fact]
    public void CircleAnnotation_DefaultValues_AreCorrect()
    {
        // Act
        var circle = new CircleAnnotation();

        // Assert
        circle.Id.Should().NotBeNullOrEmpty("Id should be auto-generated as a GUID");
        Guid.TryParse(circle.Id, out _).Should().BeTrue("Id should be a valid GUID string");
        circle.ClassIndex.Should().Be(0, "default ClassIndex should be 0");
        circle.ClassName.Should().Be(string.Empty, "default ClassName should be empty string");
        circle.CenterX.Should().Be(0f, "default CenterX should be 0");
        circle.CenterY.Should().Be(0f, "default CenterY should be 0");
        circle.Radius.Should().Be(0f, "default Radius should be 0");
    }

    /// <summary>
    /// Verifies that AnnotationData includes Polylines and Circles collections
    /// that initialize as empty lists alongside existing annotation types.
    /// </summary>
    [Fact]
    public void AnnotationData_IncludesPolylinesAndCircles()
    {
        // Act
        var annotationData = new AnnotationData();

        // Assert — existing collections
        annotationData.Boxes.Should().NotBeNull("Boxes should be initialized");
        annotationData.Polygons.Should().NotBeNull("Polygons should be initialized");
        annotationData.OrientedBoxes.Should().NotBeNull("OrientedBoxes should be initialized");

        // Assert — new Phase 6 collections
        annotationData.Polylines.Should().NotBeNull("Polylines should be initialized");
        annotationData.Polylines.Should().BeEmpty("default Polylines list should be empty");
        annotationData.Circles.Should().NotBeNull("Circles should be initialized");
        annotationData.Circles.Should().BeEmpty("default Circles list should be empty");
    }

    /// <summary>
    /// Verifies that points can be added to and removed from a PolylineAnnotation.
    /// </summary>
    [Fact]
    public void PolylineAnnotation_PointManipulation_Works()
    {
        // Arrange
        var polyline = new PolylineAnnotation
        {
            ClassIndex = 1,
            ClassName = "edge"
        };

        var point1 = new PointF(0.1f, 0.2f);
        var point2 = new PointF(0.3f, 0.4f);
        var point3 = new PointF(0.5f, 0.6f);

        // Act — add points
        polyline.Points.Add(point1);
        polyline.Points.Add(point2);
        polyline.Points.Add(point3);

        // Assert — points added
        polyline.Points.Should().HaveCount(3);
        polyline.Points[0].X.Should().BeApproximately(0.1f, 0.001f);
        polyline.Points[0].Y.Should().BeApproximately(0.2f, 0.001f);
        polyline.Points[1].X.Should().BeApproximately(0.3f, 0.001f);
        polyline.Points[1].Y.Should().BeApproximately(0.4f, 0.001f);
        polyline.Points[2].X.Should().BeApproximately(0.5f, 0.001f);
        polyline.Points[2].Y.Should().BeApproximately(0.6f, 0.001f);

        // Act — remove middle point
        polyline.Points.RemoveAt(1);

        // Assert — point removed
        polyline.Points.Should().HaveCount(2);
        polyline.Points[0].X.Should().BeApproximately(0.1f, 0.001f);
        polyline.Points[1].X.Should().BeApproximately(0.5f, 0.001f);
    }

    /// <summary>
    /// Verifies that CircleAnnotation radius and center can be assigned and retrieved.
    /// </summary>
    [Fact]
    public void CircleAnnotation_RadiusAssignment_Works()
    {
        // Arrange & Act
        var circle = new CircleAnnotation
        {
            ClassIndex = 2,
            ClassName = "hole",
            CenterX = 0.5f,
            CenterY = 0.5f,
            Radius = 0.15f
        };

        // Assert
        circle.ClassIndex.Should().Be(2);
        circle.ClassName.Should().Be("hole");
        circle.CenterX.Should().BeApproximately(0.5f, 0.0001f);
        circle.CenterY.Should().BeApproximately(0.5f, 0.0001f);
        circle.Radius.Should().BeApproximately(0.15f, 0.0001f);
    }

    /// <summary>
    /// Verifies that each PolylineAnnotation instance gets a unique Id.
    /// </summary>
    [Fact]
    public void PolylineAnnotation_MultipleInstances_HaveUniqueIds()
    {
        // Act
        var polyline1 = new PolylineAnnotation();
        var polyline2 = new PolylineAnnotation();

        // Assert
        polyline1.Id.Should().NotBe(polyline2.Id, "each instance should have a unique GUID");
    }

    /// <summary>
    /// Verifies that each CircleAnnotation instance gets a unique Id.
    /// </summary>
    [Fact]
    public void CircleAnnotation_MultipleInstances_HaveUniqueIds()
    {
        // Act
        var circle1 = new CircleAnnotation();
        var circle2 = new CircleAnnotation();

        // Assert
        circle1.Id.Should().NotBe(circle2.Id, "each instance should have a unique GUID");
    }

    /// <summary>
    /// Verifies that AnnotationData supports all five annotation types simultaneously.
    /// </summary>
    [Fact]
    public void AnnotationData_SupportsAllFiveAnnotationTypes_Simultaneously()
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
            },
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>
            {
                new()
                {
                    ClassIndex = 2,
                    ClassName = "rotated_defect",
                    CenterX = 0.4f,
                    CenterY = 0.6f,
                    Width = 0.15f,
                    Height = 0.1f,
                    Angle = 0.7854f
                }
            },
            Polylines = new List<PolylineAnnotation>
            {
                new()
                {
                    ClassIndex = 3,
                    ClassName = "edge",
                    IsClosed = false,
                    Points = new List<PointF>
                    {
                        new(0.1f, 0.1f),
                        new(0.5f, 0.3f),
                        new(0.9f, 0.1f)
                    }
                }
            },
            Circles = new List<CircleAnnotation>
            {
                new()
                {
                    ClassIndex = 4,
                    ClassName = "hole",
                    CenterX = 0.5f,
                    CenterY = 0.5f,
                    Radius = 0.1f
                }
            }
        };

        // Assert
        annotationData.Boxes.Should().HaveCount(1);
        annotationData.Polygons.Should().HaveCount(1);
        annotationData.OrientedBoxes.Should().HaveCount(1);
        annotationData.Polylines.Should().HaveCount(1);
        annotationData.Circles.Should().HaveCount(1);
        annotationData.Polylines[0].ClassName.Should().Be("edge");
        annotationData.Polylines[0].Points.Should().HaveCount(3);
        annotationData.Polylines[0].IsClosed.Should().BeFalse();
        annotationData.Circles[0].ClassName.Should().Be("hole");
        annotationData.Circles[0].Radius.Should().BeApproximately(0.1f, 0.0001f);
    }
}
