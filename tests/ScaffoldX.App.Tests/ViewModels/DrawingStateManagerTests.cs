using System.Windows;
using FluentAssertions;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="DrawingStateManager"/>, verifying initial state,
/// reset behaviour, and cancel-drawing mode detection.
/// </summary>
public class DrawingStateManagerTests
{
    private readonly DrawingStateManager _manager = new();

    /// <summary>
    /// Verifies that a freshly created DrawingStateManager has no active drawing state.
    /// </summary>
    [Fact]
    public void InitialState_NothingDrawing()
    {
        // Assert
        _manager.IsDrawingPolygon.Should().BeFalse();
        _manager.IsDrawingObb.Should().BeFalse();
        _manager.IsRotatingObb.Should().BeFalse();
        _manager.DrawStartPoint.Should().Be(default(Point));
        _manager.DrawEndPoint.Should().Be(default(Point));
    }

    /// <summary>
    /// Verifies that ResetDrawingState clears all drawing flags and coordinates.
    /// </summary>
    [Fact]
    public void ResetDrawingState_ClearsAllFlags()
    {
        // Arrange — set all state to non-default values
        _manager.IsDrawingPolygon = true;
        _manager.IsDrawingObb = true;
        _manager.IsRotatingObb = true;
        _manager.ObbCenter = new Point(100, 200);
        _manager.ObbSize = new Size(50, 60);
        _manager.ObbAngle = 1.57;
        _manager.DrawStartPoint = new Point(10, 20);
        _manager.DrawEndPoint = new Point(30, 40);

        // Act
        _manager.ResetDrawingState();

        // Assert
        _manager.IsDrawingPolygon.Should().BeFalse();
        _manager.IsDrawingObb.Should().BeFalse();
        _manager.IsRotatingObb.Should().BeFalse();
        _manager.ObbCenter.Should().Be(default(Point));
        _manager.ObbSize.Should().Be(default(Size));
        _manager.ObbAngle.Should().Be(0);
        _manager.DrawStartPoint.Should().Be(default(Point));
        _manager.DrawEndPoint.Should().Be(default(Point));
    }

    /// <summary>
    /// Verifies that CancelDrawing clears the active polygon mode and returns "polygon".
    /// </summary>
    [Fact]
    public void CancelDrawing_ClearsPolygonMode()
    {
        // Arrange
        _manager.IsDrawingPolygon = true;

        // Act
        var result = _manager.CancelDrawing();

        // Assert
        result.Should().Be("polygon");
        _manager.IsDrawingPolygon.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelDrawing clears the active OBB mode and returns "obb".
    /// </summary>
    [Fact]
    public void CancelDrawing_ClearsObbMode()
    {
        // Arrange
        _manager.IsDrawingObb = true;
        _manager.IsRotatingObb = true;
        _manager.ObbSize = new Size(50, 60);
        _manager.ObbAngle = 1.57;

        // Act
        var result = _manager.CancelDrawing();

        // Assert
        result.Should().Be("obb");
        _manager.IsDrawingObb.Should().BeFalse();
        _manager.IsRotatingObb.Should().BeFalse();
        _manager.ObbSize.Should().Be(default(Size));
        _manager.ObbAngle.Should().Be(0);
    }

    /// <summary>
    /// Verifies that CancelDrawing returns null when nothing is being drawn.
    /// </summary>
    [Fact]
    public void CancelDrawing_WhenNothingActive_ReturnsNull()
    {
        // Act
        var result = _manager.CancelDrawing();

        // Assert
        result.Should().BeNull();
    }
}
