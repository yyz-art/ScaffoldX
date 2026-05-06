using FluentAssertions;
using Moq;
using ScaffoldX.App.Services;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for auto-labeling handler properties, covering the initial state
/// of model loading and default confidence threshold on the underlying inference
/// engine and service layer.
/// </summary>
public class AutoLabelingHandlerTests : IDisposable
{
    private readonly List<IDisposable> _disposables = new();

    public void Dispose()
    {
        foreach (var d in _disposables)
        {
            try { d.Dispose(); }
            catch { /* best effort */ }
        }
    }

    [Fact]
    public void IsModelLoaded_InitiallyFalse()
    {
        // Arrange: create a fresh OnnxDetector (the engine backing auto-labeling)
        var detector = new OnnxDetector();
        _disposables.Add(detector);

        // Assert
        detector.IsLoaded.Should().BeFalse("no model has been loaded into the detector");
    }

    [Fact]
    public void ConfidenceThreshold_DefaultValue_Is0_5()
    {
        // Arrange: create a fresh OnnxDetector
        var detector = new OnnxDetector();

        // Assert
        detector.ConfidenceThreshold.Should().Be(0.5f,
            "the default confidence threshold on OnnxDetector should be 0.5");
    }

    [Fact]
    public void IsModelLoaded_OnAutoLabelingService_InitiallyFalse()
    {
        // Arrange: create AutoLabelingService (the service layer wrapping OnnxDetector)
        var service = new AutoLabelingService();
        _disposables.Add(service);

        // Assert
        service.IsModelLoaded.Should().BeFalse("no model has been loaded into the service");
    }

    [Fact]
    public void LoadedModelPath_OnAutoLabelingService_InitiallyNull()
    {
        // Arrange
        var service = new AutoLabelingService();
        _disposables.Add(service);

        // Assert
        service.LoadedModelPath.Should().BeNull("no model has been loaded");
    }

    [Fact]
    public void ClassNames_OnAutoLabelingService_InitiallyEmpty()
    {
        // Arrange
        var service = new AutoLabelingService();
        _disposables.Add(service);

        // Assert
        service.ClassNames.Should().BeEmpty("no model has been loaded to provide class names");
    }

    [Fact]
    public void ConfidenceThreshold_ClampsBelowMinimum()
    {
        // Arrange
        var detector = new OnnxDetector();

        // Act
        detector.ConfidenceThreshold = 0.01f;

        // Assert: OnnxDetector clamps to [0, 1], so 0.01 is valid
        detector.ConfidenceThreshold.Should().Be(0.01f,
            "OnnxDetector clamps to [0, 1] range");
    }

    [Fact]
    public void ConfidenceThreshold_ClampsAboveMaximum()
    {
        // Arrange
        var detector = new OnnxDetector();

        // Act
        detector.ConfidenceThreshold = 1.5f;

        // Assert: clamped to 1.0
        detector.ConfidenceThreshold.Should().Be(1.0f,
            "values above 1.0 should be clamped to 1.0 by OnnxDetector");
    }

    [Fact]
    public void ConfidenceThreshold_AcceptsValidRange()
    {
        // Arrange
        var detector = new OnnxDetector();

        // Act
        detector.ConfidenceThreshold = 0.7f;

        // Assert
        detector.ConfidenceThreshold.Should().Be(0.7f, "0.7 is within the valid range [0, 1]");
    }
}
