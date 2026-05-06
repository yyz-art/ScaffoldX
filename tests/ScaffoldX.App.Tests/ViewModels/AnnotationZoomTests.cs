using FluentAssertions;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Minimal zoom/pan state helper for testing coordinate conversion math.
/// This defines the expected behavior for zoom/pan functionality in the annotation tool.
/// </summary>
internal class ZoomPanState
{
    private double _zoomLevel = 1.0;
    private double _panX;
    private double _panY;

    private const double MinZoom = 0.1;
    private const double MaxZoom = 10.0;
    private const double ZoomStep = 0.25;

    /// <summary>Current zoom level (1.0 = 100%).</summary>
    public double ZoomLevel
    {
        get => _zoomLevel;
        private set => _zoomLevel = Math.Clamp(value, MinZoom, MaxZoom);
    }

    /// <summary>Pan offset X in screen pixels.</summary>
    public double PanX
    {
        get => _panX;
        private set => _panX = value;
    }

    /// <summary>Pan offset Y in screen pixels.</summary>
    public double PanY
    {
        get => _panY;
        private set => _panY = value;
    }

    /// <summary>
    /// Increases zoom level by one step.
    /// </summary>
    public void ZoomIn()
    {
        ZoomLevel += ZoomStep;
    }

    /// <summary>
    /// Decreases zoom level by one step.
    /// </summary>
    public void ZoomOut()
    {
        ZoomLevel -= ZoomStep;
    }

    /// <summary>
    /// Resets zoom to 1.0 and clears pan offset.
    /// </summary>
    public void ResetZoom()
    {
        ZoomLevel = 1.0;
        PanX = 0;
        PanY = 0;
    }

    /// <summary>
    /// Sets the pan offset.
    /// </summary>
    public void SetPan(double x, double y)
    {
        PanX = x;
        PanY = y;
    }

    /// <summary>
    /// Converts screen coordinates to image coordinates, accounting for zoom and pan.
    /// Formula: imageX = (screenX - panX) / zoomLevel
    /// </summary>
    public (double X, double Y) ScreenToImageCoordinates(double screenX, double screenY)
    {
        return ((screenX - PanX) / ZoomLevel, (screenY - PanY) / ZoomLevel);
    }

    /// <summary>
    /// Converts image coordinates to screen coordinates, accounting for zoom and pan.
    /// Formula: screenX = imageX * zoomLevel + panX
    /// </summary>
    public (double X, double Y) ImageToScreenCoordinates(double imageX, double imageY)
    {
        return (imageX * ZoomLevel + PanX, imageY * ZoomLevel + PanY);
    }
}

/// <summary>
/// Unit tests for zoom/pan functionality in the annotation tool.
/// Tests zoom level management and coordinate conversion between screen and image space.
/// </summary>
public class AnnotationZoomTests
{
    /// <summary>
    /// Verifies that ZoomIn increases the zoom level by the step amount.
    /// </summary>
    [Fact]
    public void ZoomIn_IncreasesZoomLevel()
    {
        // Arrange
        var state = new ZoomPanState();
        var initialZoom = state.ZoomLevel;

        // Act
        state.ZoomIn();

        // Assert
        state.ZoomLevel.Should().BeGreaterThan(initialZoom, "zoom in should increase zoom level");
        state.ZoomLevel.Should().BeApproximately(1.25, 0.001, "zoom level should increase by step (0.25)");
    }

    /// <summary>
    /// Verifies that multiple ZoomIn calls accumulate correctly.
    /// </summary>
    [Fact]
    public void ZoomIn_MultipleCalls_Accumulates()
    {
        // Arrange
        var state = new ZoomPanState();

        // Act
        state.ZoomIn();
        state.ZoomIn();
        state.ZoomIn();

        // Assert
        state.ZoomLevel.Should().BeApproximately(1.75, 0.001, "three zoom-in steps from 1.0 should be 1.75");
    }

    /// <summary>
    /// Verifies that ZoomOut decreases the zoom level by the step amount.
    /// </summary>
    [Fact]
    public void ZoomOut_DecreasesZoomLevel()
    {
        // Arrange
        var state = new ZoomPanState();
        state.ZoomIn(); // start at 1.25

        // Act
        state.ZoomOut();

        // Assert
        state.ZoomLevel.Should().BeApproximately(1.0, 0.001, "zoom out should return to original level");
    }

    /// <summary>
    /// Verifies that zoom level is clamped at the minimum boundary.
    /// </summary>
    [Fact]
    public void ZoomOut_ClampsAtMinimum()
    {
        // Arrange
        var state = new ZoomPanState();

        // Act: zoom out many times to hit minimum
        for (int i = 0; i < 100; i++)
            state.ZoomOut();

        // Assert
        state.ZoomLevel.Should().BeGreaterThanOrEqualTo(0.1, "zoom level should not go below minimum (0.1)");
    }

    /// <summary>
    /// Verifies that zoom level is clamped at the maximum boundary.
    /// </summary>
    [Fact]
    public void ZoomIn_ClampsAtMaximum()
    {
        // Arrange
        var state = new ZoomPanState();

        // Act: zoom in many times to hit maximum
        for (int i = 0; i < 100; i++)
            state.ZoomIn();

        // Assert
        state.ZoomLevel.Should().BeLessThanOrEqualTo(10.0, "zoom level should not exceed maximum (10.0)");
    }

    /// <summary>
    /// Verifies that ResetZoom returns zoom level to 1.0 and clears pan.
    /// </summary>
    [Fact]
    public void ResetZoom_ReturnsToOne()
    {
        // Arrange
        var state = new ZoomPanState();
        state.ZoomIn();
        state.ZoomIn();
        state.SetPan(100, 200);

        // Act
        state.ResetZoom();

        // Assert
        state.ZoomLevel.Should().Be(1.0, "reset should return zoom to 1.0");
        state.PanX.Should().Be(0, "reset should clear pan X");
        state.PanY.Should().Be(0, "reset should clear pan Y");
    }

    /// <summary>
    /// Verifies that ScreenToImageCoordinates correctly converts with zoom applied.
    /// At zoom=2.0, screen (200, 200) with no pan maps to image (100, 100).
    /// </summary>
    [Fact]
    public void ScreenToImageCoordinates_WithZoom_ConvertsCorrectly()
    {
        // Arrange
        var state = new ZoomPanState();
        state.ZoomIn(); // zoom = 1.25
        state.ZoomIn(); // zoom = 1.5

        // Act
        var (imgX, imgY) = state.ScreenToImageCoordinates(300, 450);

        // Assert: imageX = (300 - 0) / 1.5 = 200, imageY = (450 - 0) / 1.5 = 300
        imgX.Should().BeApproximately(200.0, 0.001);
        imgY.Should().BeApproximately(300.0, 0.001);
    }

    /// <summary>
    /// Verifies that ScreenToImageCoordinates accounts for pan offset.
    /// </summary>
    [Fact]
    public void ScreenToImageCoordinates_WithPan_ConvertsCorrectly()
    {
        // Arrange
        var state = new ZoomPanState();
        state.ZoomIn(); // zoom = 1.25
        state.SetPan(50, 100);

        // Act
        var (imgX, imgY) = state.ScreenToImageCoordinates(300, 350);

        // Assert: imageX = (300 - 50) / 1.25 = 200, imageY = (350 - 100) / 1.25 = 200
        imgX.Should().BeApproximately(200.0, 0.001);
        imgY.Should().BeApproximately(200.0, 0.001);
    }

    /// <summary>
    /// Verifies that ScreenToImageCoordinates at zoom=1.0 with no pan is identity.
    /// </summary>
    [Fact]
    public void ScreenToImageCoordinates_AtZoomOne_IsIdentity()
    {
        // Arrange
        var state = new ZoomPanState(); // zoom = 1.0, pan = (0, 0)

        // Act
        var (imgX, imgY) = state.ScreenToImageCoordinates(150, 250);

        // Assert: at zoom=1.0 with no pan, screen coords == image coords
        imgX.Should().BeApproximately(150.0, 0.001);
        imgY.Should().BeApproximately(250.0, 0.001);
    }

    /// <summary>
    /// Verifies that ImageToScreenCoordinates is the inverse of ScreenToImageCoordinates.
    /// </summary>
    [Fact]
    public void CoordinateConversion_RoundTrip_PreservesValues()
    {
        // Arrange
        var state = new ZoomPanState();
        state.ZoomIn(); // zoom = 1.25
        state.ZoomIn(); // zoom = 1.5
        state.SetPan(30, 60);

        var originalScreenX = 400.0;
        var originalScreenY = 500.0;

        // Act: screen → image → screen
        var (imgX, imgY) = state.ScreenToImageCoordinates(originalScreenX, originalScreenY);
        var (backScreenX, backScreenY) = state.ImageToScreenCoordinates(imgX, imgY);

        // Assert: round-trip should preserve original screen coordinates
        backScreenX.Should().BeApproximately(originalScreenX, 0.001);
        backScreenY.Should().BeApproximately(originalScreenY, 0.001);
    }

    /// <summary>
    /// Verifies that zoom level defaults to 1.0.
    /// </summary>
    [Fact]
    public void ZoomLevel_DefaultsToOne()
    {
        // Act
        var state = new ZoomPanState();

        // Assert
        state.ZoomLevel.Should().Be(1.0, "default zoom should be 1.0 (100%)");
    }

    /// <summary>
    /// Verifies that pan defaults to (0, 0).
    /// </summary>
    [Fact]
    public void Pan_DefaultsToZero()
    {
        // Act
        var state = new ZoomPanState();

        // Assert
        state.PanX.Should().Be(0, "default pan X should be 0");
        state.PanY.Should().Be(0, "default pan Y should be 0");
    }
}
