using FluentAssertions;
using ScaffoldX.App.Models;
using Xunit;

namespace ScaffoldX.App.Tests.Models;

/// <summary>
/// Unit tests for the OrientedBoundingBoxAnnotation model and its relationship with AnnotationData.
/// </summary>
public class OrientedBoundingBoxAnnotationTests
{
    /// <summary>
    /// Verifies that a new OrientedBoundingBoxAnnotation has correct default property values.
    /// Angle should default to 0, numeric fields to 0, Id auto-generated.
    /// </summary>
    [Fact]
    public void OrientedBoundingBox_DefaultValues_AreCorrect()
    {
        // Act
        var obb = new OrientedBoundingBoxAnnotation();

        // Assert
        obb.Id.Should().NotBeNullOrEmpty("Id should be auto-generated as a GUID");
        Guid.TryParse(obb.Id, out _).Should().BeTrue("Id should be a valid GUID string");
        obb.ClassIndex.Should().Be(0, "default ClassIndex should be 0");
        obb.ClassName.Should().Be(string.Empty, "default ClassName should be empty string");
        obb.CenterX.Should().Be(0f, "default CenterX should be 0");
        obb.CenterY.Should().Be(0f, "default CenterY should be 0");
        obb.Width.Should().Be(0f, "default Width should be 0");
        obb.Height.Should().Be(0f, "default Height should be 0");
        obb.Angle.Should().Be(0f, "default Angle should be 0");
    }

    /// <summary>
    /// Verifies that AnnotationData includes an OrientedBoxes property that initializes as an empty list.
    /// </summary>
    [Fact]
    public void AnnotationData_IncludesOrientedBoxes()
    {
        // Act
        var annotationData = new AnnotationData();

        // Assert
        annotationData.OrientedBoxes.Should().NotBeNull("OrientedBoxes should be initialized");
        annotationData.OrientedBoxes.Should().BeEmpty("default OrientedBoxes list should be empty");
    }

    /// <summary>
    /// Verifies that each OrientedBoundingBoxAnnotation instance gets a unique Id.
    /// </summary>
    [Fact]
    public void OrientedBoundingBox_MultipleInstances_HaveUniqueIds()
    {
        // Act
        var obb1 = new OrientedBoundingBoxAnnotation();
        var obb2 = new OrientedBoundingBoxAnnotation();
        var obb3 = new OrientedBoundingBoxAnnotation();

        // Assert
        var ids = new[] { obb1.Id, obb2.Id, obb3.Id };
        ids.Should().OnlyHaveUniqueItems("each instance should have a unique GUID");
    }

    /// <summary>
    /// Verifies that AnnotationData supports all three annotation types simultaneously.
    /// </summary>
    [Fact]
    public void AnnotationData_SupportsAllAnnotationTypes_Simultaneously()
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
                    Points = new List<System.Drawing.PointF>
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
            }
        };

        // Assert
        annotationData.Boxes.Should().HaveCount(1);
        annotationData.Polygons.Should().HaveCount(1);
        annotationData.OrientedBoxes.Should().HaveCount(1);
        annotationData.OrientedBoxes[0].ClassName.Should().Be("rotated_defect");
        annotationData.OrientedBoxes[0].Angle.Should().BeApproximately(0.7854f, 0.0001f);
    }

    /// <summary>
    /// Verifies that OrientedBoundingBoxAnnotation properties can be set and retrieved correctly.
    /// </summary>
    [Fact]
    public void OrientedBoundingBox_PropertyAssignment_WorksCorrectly()
    {
        // Arrange & Act
        var obb = new OrientedBoundingBoxAnnotation
        {
            ClassIndex = 3,
            ClassName = "tilted_object",
            CenterX = 0.35f,
            CenterY = 0.65f,
            Width = 0.25f,
            Height = 0.15f,
            Angle = 1.0472f
        };

        // Assert
        obb.ClassIndex.Should().Be(3);
        obb.ClassName.Should().Be("tilted_object");
        obb.CenterX.Should().BeApproximately(0.35f, 0.0001f);
        obb.CenterY.Should().BeApproximately(0.65f, 0.0001f);
        obb.Width.Should().BeApproximately(0.25f, 0.0001f);
        obb.Height.Should().BeApproximately(0.15f, 0.0001f);
        obb.Angle.Should().BeApproximately(1.0472f, 0.0001f);
    }
}
