using System.Drawing;
using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for the AnnotationInterpolationService, which linearly interpolates
/// annotations between keyframes in video annotation workflows.
/// </summary>
public class AnnotationInterpolationTests
{
    private readonly AnnotationInterpolationService _sut = new();

    // ── BoundingBox Interpolation ────────────────────────────────────────────

    /// <summary>
    /// When start and end bounding boxes are identical, interpolation with any frameCount
    /// should return identical boxes for every frame.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void InterpolateBoundingBoxes_SamePosition_ReturnsSame(int frameCount)
    {
        // Arrange
        var box = new BoundingBoxAnnotation
        {
            ClassIndex = 0,
            ClassName = "defect",
            CenterX = 0.5,
            CenterY = 0.5,
            Width = 0.2,
            Height = 0.3
        };

        // Act
        var result = _sut.InterpolateBoundingBoxes(box, box, frameCount);

        // Assert
        result.Should().HaveCount(frameCount, "should generate one annotation per frame");
        foreach (var frame in result)
        {
            frame.ClassIndex.Should().Be(0);
            frame.ClassName.Should().Be("defect");
            frame.CenterX.Should().Be(0.5);
            frame.CenterY.Should().Be(0.5);
            frame.Width.Should().Be(0.2);
            frame.Height.Should().Be(0.3);
        }
    }

    /// <summary>
    /// Verifies linear interpolation of bounding box center and dimensions
    /// between two different positions. With frameCount=3, the middle frame
    /// should be the midpoint.
    /// </summary>
    [Fact]
    public void InterpolateBoundingBoxes_LinearMovement_InterpolatesCorrectly()
    {
        // Arrange
        var start = new BoundingBoxAnnotation
        {
            ClassIndex = 0,
            ClassName = "defect",
            CenterX = 0.2,
            CenterY = 0.3,
            Width = 0.1,
            Height = 0.2
        };

        var end = new BoundingBoxAnnotation
        {
            ClassIndex = 0,
            ClassName = "defect",
            CenterX = 0.6,
            CenterY = 0.7,
            Width = 0.3,
            Height = 0.4
        };

        // Act — 3 frames: start, midpoint, end
        var result = _sut.InterpolateBoundingBoxes(start, end, 3);

        // Assert
        result.Should().HaveCount(3);

        // Frame 0 (start)
        result[0].CenterX.Should().BeApproximately(0.2, 0.0001);
        result[0].CenterY.Should().BeApproximately(0.3, 0.0001);

        // Frame 1 (midpoint)
        result[1].CenterX.Should().BeApproximately(0.4, 0.0001);
        result[1].CenterY.Should().BeApproximately(0.5, 0.0001);
        result[1].Width.Should().BeApproximately(0.2, 0.0001);
        result[1].Height.Should().BeApproximately(0.3, 0.0001);

        // Frame 2 (end)
        result[2].CenterX.Should().BeApproximately(0.6, 0.0001);
        result[2].CenterY.Should().BeApproximately(0.7, 0.0001);
        result[2].Width.Should().BeApproximately(0.3, 0.0001);
        result[2].Height.Should().BeApproximately(0.4, 0.0001);
    }

    /// <summary>
    /// Verifies that the first and last frames exactly match the start and end annotations.
    /// </summary>
    [Fact]
    public void InterpolateBoundingBoxes_Endpoints_MatchStartAndEnd()
    {
        // Arrange
        var start = new BoundingBoxAnnotation
        {
            ClassIndex = 1,
            ClassName = "scratch",
            CenterX = 0.1,
            CenterY = 0.2,
            Width = 0.05,
            Height = 0.1
        };

        var end = new BoundingBoxAnnotation
        {
            ClassIndex = 1,
            ClassName = "scratch",
            CenterX = 0.9,
            CenterY = 0.8,
            Width = 0.15,
            Height = 0.3
        };

        // Act
        var result = _sut.InterpolateBoundingBoxes(start, end, 5);

        // Assert — first frame matches start
        result[0].CenterX.Should().BeApproximately(0.1, 0.0001);
        result[0].CenterY.Should().BeApproximately(0.2, 0.0001);
        result[0].Width.Should().BeApproximately(0.05, 0.0001);
        result[0].Height.Should().BeApproximately(0.1, 0.0001);

        // Assert — last frame matches end
        result[4].CenterX.Should().BeApproximately(0.9, 0.0001);
        result[4].CenterY.Should().BeApproximately(0.8, 0.0001);
        result[4].Width.Should().BeApproximately(0.15, 0.0001);
        result[4].Height.Should().BeApproximately(0.3, 0.0001);
    }

    /// <summary>
    /// Verifies that frameCount less than 2 throws ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void InterpolateBoundingBoxes_FrameCountLessThan2_ThrowsArgumentOutOfRange(int frameCount)
    {
        // Arrange
        var box = new BoundingBoxAnnotation { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 };

        // Act
        Action act = () => _sut.InterpolateBoundingBoxes(box, box, frameCount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Polygon Interpolation ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that interpolating polygons with different point counts throws
    /// ArgumentException, since point-to-point mapping requires equal cardinality.
    /// </summary>
    [Fact]
    public void InterpolatePolygons_DifferentPointCount_ThrowsArgumentException()
    {
        // Arrange
        var start = new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "region",
            Points = new List<PointF>
            {
                new(0.1f, 0.2f),
                new(0.3f, 0.4f),
                new(0.5f, 0.2f)
            }
        };

        var end = new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "region",
            Points = new List<PointF>
            {
                new(0.2f, 0.3f),
                new(0.4f, 0.5f)
            }
        };

        // Act
        Action act = () => _sut.InterpolatePolygons(start, end, 3);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*point*",
                "polygons with different point counts cannot be interpolated point-by-point");
    }

    /// <summary>
    /// Verifies that interpolating two identical polygons returns identical polygons
    /// for every frame.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void InterpolatePolygons_SamePoints_InterpolatesCorrectly(int frameCount)
    {
        // Arrange
        var polygon = new PolygonAnnotation
        {
            ClassIndex = 1,
            ClassName = "scratch",
            Points = new List<PointF>
            {
                new(0.1f, 0.2f),
                new(0.3f, 0.4f),
                new(0.5f, 0.1f)
            }
        };

        // Act
        var result = _sut.InterpolatePolygons(polygon, polygon, frameCount);

        // Assert
        result.Should().HaveCount(frameCount);
        foreach (var frame in result)
        {
            frame.ClassIndex.Should().Be(1);
            frame.ClassName.Should().Be("scratch");
            frame.Points.Should().HaveCount(3);
            frame.Points[0].X.Should().BeApproximately(0.1f, 0.001f);
            frame.Points[0].Y.Should().BeApproximately(0.2f, 0.001f);
            frame.Points[1].X.Should().BeApproximately(0.3f, 0.001f);
            frame.Points[1].Y.Should().BeApproximately(0.4f, 0.001f);
            frame.Points[2].X.Should().BeApproximately(0.5f, 0.001f);
            frame.Points[2].Y.Should().BeApproximately(0.1f, 0.001f);
        }
    }

    /// <summary>
    /// Verifies that interpolating two different polygons with the same point count
    /// produces correct midpoint values for each point (frameCount=3).
    /// </summary>
    [Fact]
    public void InterpolatePolygons_DifferentPoints_InterpolatesMidpointCorrectly()
    {
        // Arrange
        var start = new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "region",
            Points = new List<PointF>
            {
                new(0.0f, 0.0f),
                new(1.0f, 0.0f),
                new(1.0f, 1.0f)
            }
        };

        var end = new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "region",
            Points = new List<PointF>
            {
                new(0.2f, 0.2f),
                new(0.8f, 0.2f),
                new(0.8f, 0.8f)
            }
        };

        // Act — 3 frames: start, midpoint, end
        var result = _sut.InterpolatePolygons(start, end, 3);

        // Assert — midpoint frame
        result.Should().HaveCount(3);
        result[1].Points.Should().HaveCount(3);
        result[1].Points[0].X.Should().BeApproximately(0.1f, 0.001f);
        result[1].Points[0].Y.Should().BeApproximately(0.1f, 0.001f);
        result[1].Points[1].X.Should().BeApproximately(0.9f, 0.001f);
        result[1].Points[1].Y.Should().BeApproximately(0.1f, 0.001f);
        result[1].Points[2].X.Should().BeApproximately(0.9f, 0.001f);
        result[1].Points[2].Y.Should().BeApproximately(0.9f, 0.001f);
    }

    /// <summary>
    /// Verifies that the first and last frames exactly match the start and end polygons.
    /// </summary>
    [Fact]
    public void InterpolatePolygons_Endpoints_MatchStartAndEnd()
    {
        // Arrange
        var start = new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "region",
            Points = new List<PointF>
            {
                new(0.1f, 0.1f),
                new(0.5f, 0.1f),
                new(0.5f, 0.5f)
            }
        };

        var end = new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "region",
            Points = new List<PointF>
            {
                new(0.3f, 0.3f),
                new(0.7f, 0.3f),
                new(0.7f, 0.7f)
            }
        };

        // Act
        var result = _sut.InterpolatePolygons(start, end, 5);

        // Assert — first frame matches start
        result[0].Points[0].X.Should().BeApproximately(0.1f, 0.001f);
        result[0].Points[0].Y.Should().BeApproximately(0.1f, 0.001f);

        // Assert — last frame matches end
        result[4].Points[0].X.Should().BeApproximately(0.3f, 0.001f);
        result[4].Points[0].Y.Should().BeApproximately(0.3f, 0.001f);
    }

    /// <summary>
    /// Verifies that frameCount less than 2 throws ArgumentOutOfRangeException for polygons.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void InterpolatePolygons_FrameCountLessThan2_ThrowsArgumentOutOfRange(int frameCount)
    {
        // Arrange
        var polygon = new PolygonAnnotation
        {
            ClassIndex = 0,
            Points = new List<PointF> { new(0.1f, 0.2f), new(0.3f, 0.4f) }
        };

        // Act
        Action act = () => _sut.InterpolatePolygons(polygon, polygon, frameCount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
