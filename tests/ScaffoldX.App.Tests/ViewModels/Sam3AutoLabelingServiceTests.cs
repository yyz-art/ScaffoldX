using System.Drawing;
using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using System.IO;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for SAM 3 auto-labeling functionality via <see cref="IAutoLabelingService"/> mocks,
/// covering model lifecycle, text/point/reference segmentation, and batch operations.
/// </summary>
public class Sam3AutoLabelingServiceTests
{
    private readonly Mock<IAutoLabelingService> _mockService;

    public Sam3AutoLabelingServiceTests()
    {
        _mockService = new Mock<IAutoLabelingService>();
    }

    // ── 初始状态 ──────────────────────────────────────────────────────────

    [Fact]
    public void IsModelLoaded_InitiallyFalse()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(false);

        _mockService.Object.IsModelLoaded.Should().BeFalse();
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

    // ── SAM 3 模型加载 ───────────────────────────────────────────────────

    [Fact]
    public async Task LoadSam3ModelAsync_SetsCurrentModeToSegmentation()
    {
        _mockService.Setup(s => s.LoadSam3ModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);

        await _mockService.Object.LoadSam3ModelAsync("/models/sam3");

        _mockService.Object.IsModelLoaded.Should().BeTrue();
        _mockService.Object.CurrentMode.Should().Be(AutoLabelingMode.Segmentation);
    }

    [Fact]
    public async Task LoadSam3ModelAsync_InvalidPath_ThrowsException()
    {
        _mockService.Setup(s => s.LoadSam3ModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("模型目录不存在"));

        var act = () => _mockService.Object.LoadSam3ModelAsync("/invalid/path");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    // ── 模型卸载 ──────────────────────────────────────────────────────────

    [Fact]
    public void UnloadModel_ResetsState()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(false);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Detection);

        _mockService.Object.UnloadModel();

        _mockService.Object.IsModelLoaded.Should().BeFalse();
    }

    // ── 文本分割 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SegmentByTextAsync_WhenModelLoaded_ReturnsResults()
    {
        var expected = new List<SegmentationAnnotation>
        {
            new() { ClassName = "cat", Confidence = 0.9f },
            new() { ClassName = "dog", Confidence = 0.85f }
        };

        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);
        _mockService.Setup(s => s.SegmentByTextAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _mockService.Object.SegmentByTextAsync("test.jpg", new[] { "cat", "dog" });

        result.Should().HaveCount(2);
        result[0].ClassName.Should().Be("cat");
        result[1].ClassName.Should().Be("dog");
    }

    [Fact]
    public async Task SegmentByTextAsync_EmptyPrompts_ReturnsEmpty()
    {
        _mockService.Setup(s => s.SegmentByTextAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SegmentationAnnotation>());

        var result = await _mockService.Object.SegmentByTextAsync("test.jpg", Array.Empty<string>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SegmentByTextAsync_ModelNotLoaded_ThrowsException()
    {
        _mockService.Setup(s => s.SegmentByTextAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SAM 3 模型未加载"));

        var act = () => _mockService.Object.SegmentByTextAsync("test.jpg", new[] { "cat" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── 点提示分割 ────────────────────────────────────────────────────────

    [Fact]
    public async Task SegmentByPointsAsync_WithValidPoints_ReturnsResult()
    {
        var expected = new SegmentationAnnotation
        {
            ClassName = "point_segment",
            Confidence = 0.95f,
            Mask = new byte[,]
            {
                { 0, 1, 0 },
                { 1, 1, 1 },
                { 0, 1, 0 }
            }
        };

        _mockService.Setup(s => s.SegmentByPointsAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<PointF>>(),
                It.IsAny<IEnumerable<PointF>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _mockService.Object.SegmentByPointsAsync("test.jpg",
            new[] { new PointF(0.5f, 0.5f) },
            Array.Empty<PointF>());

        result.ClassName.Should().Be("point_segment");
        result.Confidence.Should().Be(0.95f);
        result.Mask.Should().NotBeNull();
    }

    [Fact]
    public async Task SegmentByPointsAsync_EmptyPoints_ThrowsArgumentException()
    {
        _mockService.Setup(s => s.SegmentByPointsAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<PointF>>(),
                It.IsAny<IEnumerable<PointF>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("至少需要一个提示点"));

        var act = () => _mockService.Object.SegmentByPointsAsync("test.jpg",
            Array.Empty<PointF>(), Array.Empty<PointF>());

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SegmentByPointsAsync_WithNegativePoints_ExcludesRegions()
    {
        var expected = new SegmentationAnnotation { ClassName = "partial" };

        _mockService.Setup(s => s.SegmentByPointsAsync(
                "test.jpg",
                It.Is<IEnumerable<PointF>>(p => p.Count() == 1),
                It.Is<IEnumerable<PointF>>(n => n.Count() == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _mockService.Object.SegmentByPointsAsync("test.jpg",
            new[] { new PointF(0.5f, 0.5f) },
            new[] { new PointF(0.1f, 0.1f) });

        result.ClassName.Should().Be("partial");
    }

    // ── 参考图分割 ────────────────────────────────────────────────────────

    [Fact]
    public async Task SegmentByReferenceAsync_WithValidPaths_ReturnsResults()
    {
        var expected = new List<SegmentationAnnotation>
        {
            new() { ClassName = "reference_match", Confidence = 0.88f }
        };

        _mockService.Setup(s => s.SegmentByReferenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _mockService.Object.SegmentByReferenceAsync(
            "target.jpg", "reference.jpg", 0.5f);

        result.Should().HaveCount(1);
        result[0].ClassName.Should().Be("reference_match");
    }

    [Fact]
    public async Task SegmentByReferenceAsync_SimilarImages_HighConfidence()
    {
        var expected = new List<SegmentationAnnotation>
        {
            new() { ClassName = "match", Confidence = 0.95f }
        };

        _mockService.Setup(s => s.SegmentByReferenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _mockService.Object.SegmentByReferenceAsync(
            "same_object_1.jpg", "same_object_2.jpg", 0.5f);

        result[0].Confidence.Should().BeGreaterThan(0.9f);
    }

    // ── 批量检测（原有功能） ─────────────────────────────────────────────

    [Fact]
    public async Task DetectBatchAsync_ReturnsResultsPerImage()
    {
        var imagePaths = new[] { "img1.jpg", "img2.jpg" };
        var expected = new Dictionary<string, List<BoundingBoxAnnotation>>
        {
            ["img1.jpg"] = new() { new() { ClassName = "cat" } },
            ["img2.jpg"] = new()
        };

        _mockService.Setup(s => s.DetectBatchAsync(imagePaths, 0.5f, It.IsAny<IProgress<(int, int)>>()))
            .ReturnsAsync(expected);

        var result = await _mockService.Object.DetectBatchAsync(imagePaths, 0.5f);

        result.Should().HaveCount(2);
        result["img1.jpg"].Should().HaveCount(1);
        result["img2.jpg"].Should().BeEmpty();
    }
}
