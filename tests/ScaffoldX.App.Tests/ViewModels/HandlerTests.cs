using FluentAssertions;
using Moq;
using Prism.Commands;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for handler initial state, covering ExportCommandHandler, ReviewCommandHandler,
/// PolygonDrawingHandler, and ObbDrawingHandler. Verifies that each handler initializes with
/// correct default property values and command availability.
/// </summary>
public class HandlerTests
{
    /// <summary>
    /// Verifies that ExportCommandHandler initializes with correct default state:
    /// all export and import commands are available and properly initialized.
    /// </summary>
    [Fact]
    public void ExportCommandHandler_InitialState()
    {
        // Arrange
        var mockAnnotationService = new Mock<IAnnotationService>();
        var mockVideoFrameService = new Mock<IVideoFrameService>();

        var handler = new ExportCommandHandler(
            mockAnnotationService.Object,
            mockVideoFrameService.Object,
            getProject: () => null,
            getCurrentAnnotation: () => null,
            getCurrentImageIndex: () => -1,
            loadFirstImage: () => Task.CompletedTask,
            setStatusMessage: _ => { },
            updateBoxesList: () => { },
            updateStatistics: () => { });

        // Assert: all export commands should be initialized
        handler.ExportYoloCommand.Should().NotBeNull("ExportYoloCommand should be initialized");
        handler.ExportCocoCommand.Should().NotBeNull("ExportCocoCommand should be initialized");
        handler.ExportVocCommand.Should().NotBeNull("ExportVocCommand should be initialized");
        handler.ExportDotCommand.Should().NotBeNull("ExportDotCommand should be initialized");
        handler.ExportMotCommand.Should().NotBeNull("ExportMotCommand should be initialized");

        // Import commands should be initialized
        handler.ImportAnnotationsCommand.Should().NotBeNull("ImportAnnotationsCommand should be initialized");
        handler.ImportVideoCommand.Should().NotBeNull("ImportVideoCommand should be initialized");
    }

    /// <summary>
    /// Verifies that ReviewCommandHandler initializes with correct default state:
    /// empty summary text, zero unannotated count, no unannotated images flagged.
    /// </summary>
    [Fact]
    public void ReviewCommandHandler_InitialState()
    {
        // Arrange
        var handler = new ReviewCommandHandler(
            getProject: () => null,
            getCurrentImageIndex: () => -1,
            loadImageAsync: _ => Task.CompletedTask,
            setStatusMessage: _ => { },
            updateStatistics: () => { },
            getPolylineCount: () => 0,
            getCircleCount: () => 0);

        // Assert
        handler.ReviewSummaryText.Should().BeEmpty("no review has been performed yet");
        handler.UnannotatedImageCount.Should().Be(0, "no project is loaded");
        handler.HasUnannotatedImages.Should().BeFalse("no unannotated images when no project");

        // Commands should be initialized
        handler.GotoNextUnannotatedCommand.Should().NotBeNull("GotoNextUnannotatedCommand should be initialized");
        handler.RefreshReviewSummaryCommand.Should().NotBeNull("RefreshReviewSummaryCommand should be initialized");
    }

    /// <summary>
    /// Verifies that PolygonDrawingHandler initializes with correct default state:
    /// polygon mode disabled, empty vertex collection, all commands initialized.
    /// </summary>
    [Fact]
    public void PolygonDrawingHandler_InitialState()
    {
        // Arrange
        var drawingState = new DrawingStateManager();

        var handler = new PolygonDrawingHandler(
            drawingState,
            getProject: () => null,
            getCurrentAnnotation: () => null,
            getCurrentImage: () => null,
            getSelectedClassIndex: () => 0,
            getIsObbMode: () => false,
            disableObbMode: () => { },
            setStatusMessage: _ => { },
            pushUndoSnapshot: () => { },
            updateBoxesList: () => { },
            updateClassDistribution: () => { });

        // Assert
        handler.IsPolygonMode.Should().BeFalse("polygon mode should be disabled initially");
        handler.PolygonModeButtonText.Should().Be("多边形", "button text should indicate polygon mode is off");
        handler.CurrentPolygonPoints.Should().BeEmpty("no vertices should exist initially");

        // Commands should be initialized
        handler.TogglePolygonModeCommand.Should().NotBeNull("TogglePolygonModeCommand should be initialized");
        handler.FinishPolygonCommand.Should().NotBeNull("FinishPolygonCommand should be initialized");
        handler.CancelPolygonCommand.Should().NotBeNull("CancelPolygonCommand should be initialized");
        handler.PolygonMouseDownCommand.Should().NotBeNull("PolygonMouseDownCommand should be initialized");
        handler.PolygonDoubleClickCommand.Should().NotBeNull("PolygonDoubleClickCommand should be initialized");

        // Drawing state should be clean
        drawingState.IsDrawingPolygon.Should().BeFalse("drawing state should not be drawing initially");
    }

    /// <summary>
    /// Verifies that ObbDrawingHandler initializes with correct default state:
    /// OBB mode disabled, zero center/size/angle, all commands initialized.
    /// </summary>
    [Fact]
    public void ObbDrawingHandler_InitialState()
    {
        // Arrange
        var drawingState = new DrawingStateManager();

        var handler = new ObbDrawingHandler(
            drawingState,
            getProject: () => null,
            getCurrentAnnotation: () => null,
            getCurrentImage: () => null,
            getSelectedClassIndex: () => 0,
            getIsPolygonMode: () => false,
            disablePolygonMode: () => { },
            setStatusMessage: _ => { },
            pushUndoSnapshot: () => { },
            updateBoxesList: () => { },
            updateClassDistribution: () => { });

        // Assert
        handler.IsObbMode.Should().BeFalse("OBB mode should be disabled initially");
        handler.ObbModeButtonText.Should().Be("OBB", "button text should indicate OBB mode is off");
        handler.IsDrawingObb.Should().BeFalse("should not be drawing OBB initially");
        handler.IsRotatingObb.Should().BeFalse("should not be rotating OBB initially");
        handler.ObbSize.Should().Be(default(System.Windows.Size), "OBB size should be zero initially");
        handler.ObbAngle.Should().Be(0, "OBB angle should be zero initially");

        // Commands should be initialized
        handler.ToggleObbModeCommand.Should().NotBeNull("ToggleObbModeCommand should be initialized");
        handler.FinishObbCommand.Should().NotBeNull("FinishObbCommand should be initialized");
        handler.CancelObbCommand.Should().NotBeNull("CancelObbCommand should be initialized");
        handler.ObbMouseDownCommand.Should().NotBeNull("ObbMouseDownCommand should be initialized");
        handler.ObbMouseMoveCommand.Should().NotBeNull("ObbMouseMoveCommand should be initialized");
        handler.ObbMouseUpCommand.Should().NotBeNull("ObbMouseUpCommand should be initialized");

        // Drawing state should be clean
        drawingState.IsDrawingObb.Should().BeFalse("drawing state should not be drawing OBB initially");
        drawingState.IsRotatingObb.Should().BeFalse("drawing state should not be rotating OBB initially");
    }
}
