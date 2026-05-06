using System.Drawing;
using System.Windows.Media.Imaging;
using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="Sam3LabelingCommandHandler"/> using mocked <see cref="IAutoLabelingService"/>.
/// </summary>
public class Sam3LabelingHandlerTests
{
    private readonly Mock<IAutoLabelingService> _mockService;
    private AnnotationData? _currentAnnotation;
    private AnnotationProject? _currentProject;
    private string _statusMessage = string.Empty;
    private int _undoSnapshotCount;
    private int _updateBoxesCount;
    private int _updateClassDistCount;
    private int _updateStatsCount;

    public Sam3LabelingHandlerTests()
    {
        _mockService = new Mock<IAutoLabelingService>();
        _currentAnnotation = new AnnotationData { ImagePath = "test.jpg" };
        _currentProject = new AnnotationProject
        {
            Annotations = new List<AnnotationData> { _currentAnnotation }
        };
    }

    private Sam3LabelingCommandHandler CreateHandler()
    {
        return new Sam3LabelingCommandHandler(
            _mockService.Object,
            () => _currentAnnotation,
            () => _currentProject,
            () => null, // getCurrentImage
            msg => _statusMessage = msg,
            () => _undoSnapshotCount++,
            () => _updateBoxesCount++,
            () => _updateClassDistCount++,
            () => _updateStatsCount++);
    }

    // ── 初始状态 ──────────────────────────────────────────────────────────

    [Fact]
    public void CurrentPromptMode_InitiallyPoint()
    {
        var handler = CreateHandler();

        handler.CurrentPromptMode.Should().Be(Sam3PromptMode.Point);
    }

    [Fact]
    public void PromptPoints_InitiallyEmpty()
    {
        var handler = CreateHandler();

        handler.PromptPoints.Should().BeEmpty();
    }

    [Fact]
    public void TextPromptInput_InitiallyEmpty()
    {
        var handler = CreateHandler();

        handler.TextPromptInput.Should().BeEmpty();
    }

    [Fact]
    public void IsProcessing_InitiallyFalse()
    {
        var handler = CreateHandler();

        handler.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public void ConfidenceThreshold_DefaultIs0_5()
    {
        var handler = CreateHandler();

        handler.ConfidenceThreshold.Should().Be(0.5f);
    }

    [Fact]
    public void CurrentMaskPreview_InitiallyNull()
    {
        var handler = CreateHandler();

        handler.CurrentMaskPreview.Should().BeNull();
    }

    [Fact]
    public void HasMaskPreview_WhenNoMask_IsFalse()
    {
        var handler = CreateHandler();

        handler.HasMaskPreview.Should().BeFalse();
    }

    [Fact]
    public void ReferenceImagePath_InitiallyNull()
    {
        var handler = CreateHandler();

        handler.ReferenceImagePath.Should().BeNull();
    }

    // ── 属性设置 ──────────────────────────────────────────────────────────

    [Fact]
    public void CurrentPromptMode_CanBeSetToBox()
    {
        var handler = CreateHandler();

        handler.CurrentPromptMode = Sam3PromptMode.Box;

        handler.CurrentPromptMode.Should().Be(Sam3PromptMode.Box);
    }

    [Fact]
    public void TextPromptInput_CanBeSet()
    {
        var handler = CreateHandler();

        handler.TextPromptInput = "cat, dog";

        handler.TextPromptInput.Should().Be("cat, dog");
    }

    [Fact]
    public void ConfidenceThreshold_ClampsToValidRange()
    {
        var handler = CreateHandler();

        handler.ConfidenceThreshold = 0.01f;
        handler.ConfidenceThreshold.Should().Be(0.1f, "values below 0.1 are clamped");

        handler.ConfidenceThreshold = 1.5f;
        handler.ConfidenceThreshold.Should().Be(0.95f, "values above 0.95 are clamped");
    }

    // ── IsSam3ModelLoaded ─────────────────────────────────────────────────

    [Fact]
    public void IsSam3ModelLoaded_WhenNotSegmentationMode_IsFalse()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Detection);

        var handler = CreateHandler();

        handler.IsSam3ModelLoaded.Should().BeFalse();
    }

    [Fact]
    public void IsSam3ModelLoaded_WhenSegmentationMode_IsTrue()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);

        var handler = CreateHandler();

        handler.IsSam3ModelLoaded.Should().BeTrue();
    }

    // ── 点提示模式 ────────────────────────────────────────────────────────

    [Fact]
    public void EnterPointMode_ClearsPointsAndMask()
    {
        var handler = CreateHandler();

        handler.EnterPointModeCommand.Execute();

        handler.CurrentPromptMode.Should().Be(Sam3PromptMode.Point);
        handler.PromptPoints.Should().BeEmpty();
        handler.CurrentMaskPreview.Should().BeNull();
        _statusMessage.Should().Contain("点提示模式");
    }

    [Fact]
    public void AddPromptPoint_Positive_AddsToList()
    {
        var handler = CreateHandler();

        handler.AddPromptPoint(0.5f, 0.5f, isPositive: true);

        handler.PromptPoints.Should().HaveCount(1);
        handler.PromptPoints[0].X.Should().Be(0.5f);
        handler.PromptPoints[0].Y.Should().Be(0.5f);
        handler.PromptPoints[0].Label.Should().Be(1);
    }

    [Fact]
    public void AddPromptPoint_Negative_AddsToList()
    {
        var handler = CreateHandler();

        handler.AddPromptPoint(0.3f, 0.7f, isPositive: false);

        handler.PromptPoints.Should().HaveCount(1);
        handler.PromptPoints[0].Label.Should().Be(0);
    }

    [Fact]
    public void ClearPoints_EmptiesListAndClearsMask()
    {
        var handler = CreateHandler();
        handler.AddPromptPoint(0.5f, 0.5f, true);
        handler.AddPromptPoint(0.3f, 0.3f, false);

        handler.ClearPointsCommand.Execute();

        handler.PromptPoints.Should().BeEmpty();
        handler.CurrentMaskPreview.Should().BeNull();
        _statusMessage.Should().Contain("已清除");
    }

    // ── 文本分割 ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SegmentByText_WithValidPrompt_CallsService()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);
        _mockService.Setup(s => s.SegmentByTextAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SegmentationAnnotation>
            {
                new() { ClassName = "cat", Confidence = 0.9f }
            });

        var handler = CreateHandler();
        handler.TextPromptInput = "cat";

        handler.SegmentByTextCommand.Execute();
        await Task.Delay(200);

        _currentAnnotation!.Segmentations.Should().HaveCount(1);
        _currentAnnotation.Segmentations[0].ClassName.Should().Be("cat");
        _undoSnapshotCount.Should().Be(1);
        _statusMessage.Should().Contain("完成");
    }

    [Fact]
    public async Task SegmentByText_MultiplePrompts_SplitsCorrectly()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);
        _mockService.Setup(s => s.SegmentByTextAsync(
                It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SegmentationAnnotation>
            {
                new() { ClassName = "cat" },
                new() { ClassName = "dog" }
            });

        var handler = CreateHandler();
        handler.TextPromptInput = "cat, dog";

        handler.SegmentByTextCommand.Execute();
        await Task.Delay(200);

        _mockService.Verify(s => s.SegmentByTextAsync(
            "test.jpg",
            It.Is<IEnumerable<string>>(p => p.Count() == 2),
            It.IsAny<float>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 掩码接受 ──────────────────────────────────────────────────────────

    [Fact]
    public void AcceptMask_WithPreview_AddsSegmentationToAnnotation()
    {
        var handler = CreateHandler();

        // Set a mask preview (simulate point-based segmentation result)
        handler.CurrentMaskPreview = new byte[,]
        {
            { 0, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 0 }
        };

        handler.AcceptMaskCommand.Execute();

        _currentAnnotation!.Segmentations.Should().HaveCount(1);
        _currentAnnotation.Segmentations[0].ClassName.Should().Be("segment");
        _currentAnnotation.Segmentations[0].Mask.Should().NotBeNull();
        handler.CurrentMaskPreview.Should().BeNull("mask should be cleared after accept");
        handler.PromptPoints.Should().BeEmpty("points should be cleared after accept");
        _undoSnapshotCount.Should().Be(1);
    }

    // ── 参考图分割 ────────────────────────────────────────────────────────

    [Fact]
    public async Task SegmentByReference_WithValidPath_CallsService()
    {
        _mockService.Setup(s => s.IsModelLoaded).Returns(true);
        _mockService.Setup(s => s.CurrentMode).Returns(AutoLabelingMode.Segmentation);
        _mockService.Setup(s => s.SegmentByReferenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SegmentationAnnotation>
            {
                new() { ClassName = "ref_obj", Confidence = 0.85f }
            });

        var handler = CreateHandler();
        handler.ReferenceImagePath = "reference.jpg";

        handler.SegmentByReferenceCommand.Execute();
        await Task.Delay(200);

        _mockService.Verify(s => s.SegmentByReferenceAsync(
            "test.jpg", "reference.jpg", It.IsAny<float>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
