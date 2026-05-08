using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="ImageNavigationHandler"/>, covering initial state,
/// navigation commands, image loading, and CanNavigate behavior.
/// </summary>
public class ImageNavigationHandlerTests
{
    private static (ImageNavigationHandler handler, Mock<IAnnotationService> mockService, AnnotationContext ctx, AnnotationProject project) CreateHandler(int annotationCount = 0)
    {
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Classes = new List<AnnotationClass> { new() { Index = 0, Name = "object" } },
            Annotations = Enumerable.Range(0, annotationCount)
                .Select(i => new AnnotationData { ImagePath = $"image_{i}.jpg" })
                .ToList()
        };

        var mockService = new Mock<IAnnotationService>();
        mockService.Setup(s => s.UpdateAnnotationAsync(It.IsAny<AnnotationProject>(), It.IsAny<AnnotationData>()))
            .Returns(Task.CompletedTask);

        string? statusMessage = null;
        AnnotationData? currentAnnotation = null;

        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => currentAnnotation,
            SetCurrentAnnotation = data => currentAnnotation = data,
            GetTotalImages = () => project.Annotations.Count,
            SetStatusMessage = msg => statusMessage = msg,
            UpdateBoxesList = () => { },
            UpdateStatistics = () => { },
        };

        var handler = new ImageNavigationHandler(mockService.Object, ctx);
        return (handler, mockService, ctx, project);
    }

    // ── Initial state ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that CurrentImageIndex defaults to -1.
    /// </summary>
    [Fact]
    public void CurrentImageIndex_DefaultsToMinusOne()
    {
        // Arrange & Act
        var (handler, _, _, _) = CreateHandler();

        // Assert
        handler.CurrentImageIndex.Should().Be(-1);
    }

    /// <summary>
    /// Verifies that CurrentImage defaults to null.
    /// </summary>
    [Fact]
    public void CurrentImage_DefaultsToNull()
    {
        // Arrange & Act
        var (handler, _, _, _) = CreateHandler();

        // Assert
        handler.CurrentImage.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ImageNavigationText returns "无图像" when project is null.
    /// </summary>
    [Fact]
    public void ImageNavigationText_WhenProjectNull_ReturnsNoImage()
    {
        // Arrange
        var mockService = new Mock<IAnnotationService>();
        var ctx = new AnnotationContext
        {
            GetProject = () => null,
            GetCurrentAnnotation = () => null,
            GetTotalImages = () => 0,
            SetStatusMessage = _ => { },
        };
        var handler = new ImageNavigationHandler(mockService.Object, ctx);

        // Assert
        handler.ImageNavigationText.Should().Be("无图像");
    }

    /// <summary>
    /// Verifies that PreviousImageCommand and NextImageCommand are initialized.
    /// </summary>
    [Fact]
    public void Commands_AreInitialized()
    {
        // Arrange & Act
        var (handler, _, _, _) = CreateHandler();

        // Assert
        handler.PreviousImageCommand.Should().NotBeNull();
        handler.NextImageCommand.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that navigation commands are disabled when there are no annotations.
    /// </summary>
    [Fact]
    public void NavigationCommands_WhenNoAnnotations_CannotExecute()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandler(annotationCount: 0);

        // Assert
        handler.PreviousImageCommand.CanExecute().Should().BeFalse();
        handler.NextImageCommand.CanExecute().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that navigation commands are enabled when annotations exist.
    /// </summary>
    [Fact]
    public void NavigationCommands_WhenAnnotationsExist_CanExecute()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandler(annotationCount: 3);
        handler.RaiseCanNavigateChanged();

        // Assert
        handler.PreviousImageCommand.CanExecute().Should().BeTrue();
        handler.NextImageCommand.CanExecute().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that RaiseCanNavigateChanged refreshes CanExecute state.
    /// </summary>
    [Fact]
    public void RaiseCanNavigateChanged_RefreshesCanExecute()
    {
        // Arrange — start with no annotations
        var project = new AnnotationProject
        {
            Annotations = new List<AnnotationData>()
        };
        var mockService = new Mock<IAnnotationService>();
        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => null,
            GetTotalImages = () => project.Annotations.Count,
            SetStatusMessage = _ => { },
        };
        var handler = new ImageNavigationHandler(mockService.Object, ctx);

        // Initially disabled
        handler.NextImageCommand.CanExecute().Should().BeFalse();

        // Act — add annotations and refresh
        project.Annotations.Add(new AnnotationData { ImagePath = "new.jpg" });
        handler.RaiseCanNavigateChanged();

        // Assert
        handler.NextImageCommand.CanExecute().Should().BeTrue();
    }

    // ── LoadImageAsync ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that LoadImageAsync with out-of-range index does nothing.
    /// </summary>
    [Fact]
    public async Task LoadImageAsync_OutOfRange_DoesNothing()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandler(annotationCount: 2);

        // Act
        await handler.LoadImageAsync(5);

        // Assert
        handler.CurrentImageIndex.Should().Be(-1);
    }

    /// <summary>
    /// Verifies that LoadImageAsync with negative index does nothing.
    /// </summary>
    [Fact]
    public async Task LoadImageAsync_NegativeIndex_DoesNothing()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandler(annotationCount: 2);

        // Act
        await handler.LoadImageAsync(-1);

        // Assert
        handler.CurrentImageIndex.Should().Be(-1);
    }

    /// <summary>
    /// Verifies that LoadImageAsync with null project does nothing.
    /// </summary>
    [Fact]
    public async Task LoadImageAsync_WhenProjectNull_DoesNothing()
    {
        // Arrange
        var mockService = new Mock<IAnnotationService>();
        var ctx = new AnnotationContext
        {
            GetProject = () => null,
            GetCurrentAnnotation = () => null,
            GetTotalImages = () => 0,
            SetStatusMessage = _ => { },
        };
        var handler = new ImageNavigationHandler(mockService.Object, ctx);

        // Act
        await handler.LoadImageAsync(0);

        // Assert
        handler.CurrentImageIndex.Should().Be(-1);
    }

    /// <summary>
    /// Verifies that LoadImageAsync updates CurrentImageIndex.
    /// </summary>
    [Fact]
    public async Task LoadImageAsync_ValidIndex_UpdatesCurrentImageIndex()
    {
        // Arrange
        var (handler, _, _, _) = CreateHandler(annotationCount: 3);

        // Act — LoadImageAsync tries to load a BitmapImage from the path,
        // which will fail for non-existent files. The index is still updated.
        await handler.LoadImageAsync(1);

        // Assert
        handler.CurrentImageIndex.Should().Be(1);
    }

    /// <summary>
    /// Verifies that LoadImageAsync calls SetCurrentAnnotation on context.
    /// </summary>
    [Fact]
    public async Task LoadImageAsync_SetsCurrentAnnotation()
    {
        // Arrange
        AnnotationData? captured = null;
        var project = new AnnotationProject
        {
            Annotations = new List<AnnotationData>
            {
                new() { ImagePath = "image_0.jpg" }
            }
        };
        var mockService = new Mock<IAnnotationService>();
        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => captured,
            SetCurrentAnnotation = data => captured = data,
            GetTotalImages = () => project.Annotations.Count,
            SetStatusMessage = _ => { },
            UpdateBoxesList = () => { },
        };
        var handler = new ImageNavigationHandler(mockService.Object, ctx);

        // Act
        await handler.LoadImageAsync(0);

        // Assert
        captured.Should().NotBeNull();
        captured!.ImagePath.Should().Be("image_0.jpg");
    }

    // ── SaveCurrentAnnotationAsync ───────────────────────────────────────────

    /// <summary>
    /// Verifies that SaveCurrentAnnotationAsync calls UpdateAnnotationAsync on the service.
    /// </summary>
    [Fact]
    public async Task SaveCurrentAnnotationAsync_CallsService()
    {
        // Arrange
        var annotation = new AnnotationData { ImagePath = "test.jpg" };
        var project = new AnnotationProject();
        var mockService = new Mock<IAnnotationService>();
        mockService.Setup(s => s.UpdateAnnotationAsync(It.IsAny<AnnotationProject>(), It.IsAny<AnnotationData>()))
            .Returns(Task.CompletedTask);

        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => annotation,
            SetStatusMessage = _ => { },
            UpdateStatistics = () => { },
        };
        var handler = new ImageNavigationHandler(mockService.Object, ctx);

        // Act
        await handler.SaveCurrentAnnotationAsync();

        // Assert
        mockService.Verify(s => s.UpdateAnnotationAsync(project, annotation), Times.Once);
    }

    /// <summary>
    /// Verifies that SaveCurrentAnnotationAsync does nothing when project is null.
    /// </summary>
    [Fact]
    public async Task SaveCurrentAnnotationAsync_WhenProjectNull_DoesNothing()
    {
        // Arrange
        var mockService = new Mock<IAnnotationService>();
        var ctx = new AnnotationContext
        {
            GetProject = () => null,
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
        };
        var handler = new ImageNavigationHandler(mockService.Object, ctx);

        // Act
        await handler.SaveCurrentAnnotationAsync();

        // Assert
        mockService.Verify(s => s.UpdateAnnotationAsync(It.IsAny<AnnotationProject>(), It.IsAny<AnnotationData>()), Times.Never);
    }
}
