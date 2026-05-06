using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for auto-labeling service contract, covering initial state
/// and SAM 3 integration via mocked IAutoLabelingService.
/// </summary>
public class AutoLabelingHandlerTests
{
    private readonly Mock<IAutoLabelingService> _mockService;

    public AutoLabelingHandlerTests()
    {
        _mockService = new Mock<IAutoLabelingService>();
    }

    [Fact]
    public void IsModelLoaded_InitiallyFalse()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(false);
        _mockService.Object.IsModelLoaded.Should().BeFalse("no model has been loaded");
    }

    [Fact]
    public void CurrentMode_InitiallyDetection()
    {
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Detection);
        _mockService.Object.CurrentMode.Should().Be(AutoLabelingMode.Detection);
    }

    [Fact]
    public void LoadedModelPath_InitiallyNull()
    {
        _mockService.Setup(s => s.LoadedModelPath).Returns((string?)null);
        _mockService.Object.LoadedModelPath.Should().BeNull();
    }

    [Fact]
    public void ClassNames_InitiallyEmpty()
    {
        _mockService.Setup(s => s.ClassNames).Returns(Array.Empty<string>());
        _mockService.Object.ClassNames.Should().BeEmpty();
    }

    [Fact]
    public void LoadSam3ModelAsync_SetsCurrentModeToSegmentation()
    {
        _mockService.Setup(s => s.LoadSam3ModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);

        _mockService.Object.LoadSam3ModelAsync("models/sam3").GetAwaiter().GetResult();

        _mockService.Object.CurrentMode.Should().Be(AutoLabelingMode.Segmentation);
    }

    [Fact]
    public void SegmentByTextAsync_WhenModelLoaded_ReturnsResults()
    {
        var seg = new SegmentationAnnotation { ClassName = "cat", Confidence = 0.9f };
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);
        _mockService.Setup(s => s.SegmentByTextAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SegmentationAnnotation> { seg });

        var result = _mockService.Object.SegmentByTextAsync("test.jpg", new[] { "cat" }).GetAwaiter().GetResult();

        result.Should().HaveCount(1);
        result[0].ClassName.Should().Be("cat");
    }

    [Fact]
    public void SegmentByPointsAsync_WhenModelLoaded_ReturnsResult()
    {
        var seg = new SegmentationAnnotation { ClassName = "point_segment" };
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.SegmentByPointsAsync(
            It.IsAny<string>(), It.IsAny<IEnumerable<System.Drawing.PointF>>(),
            It.IsAny<IEnumerable<System.Drawing.PointF>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(seg);

        var result = _mockService.Object.SegmentByPointsAsync("test.jpg",
            new[] { new System.Drawing.PointF(0.5f, 0.5f) },
            Array.Empty<System.Drawing.PointF>()).GetAwaiter().GetResult();

        result.ClassName.Should().Be("point_segment");
    }
}
