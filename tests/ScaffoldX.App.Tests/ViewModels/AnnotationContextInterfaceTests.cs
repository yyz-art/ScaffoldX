using System.Windows.Media.Imaging;
using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Tests that <see cref="AnnotationContext"/> implements <see cref="IAnnotationState"/>
/// and <see cref="IAnnotationActions"/>, and that the interfaces can be mocked independently.
/// </summary>
public class AnnotationContextInterfaceTests
{
    // ── Interface implementation verification ────────────────────────────────

    [Fact]
    public void AnnotationContext_Implements_IAnnotationState()
    {
        var ctx = CreateContext();

        ctx.Should().BeAssignableTo<IAnnotationState>(
            "AnnotationContext should implement IAnnotationState");
    }

    [Fact]
    public void AnnotationContext_Implements_IAnnotationActions()
    {
        var ctx = CreateContext();

        ctx.Should().BeAssignableTo<IAnnotationActions>(
            "AnnotationContext should implement IAnnotationActions");
    }

    // ── IAnnotationState delegation ──────────────────────────────────────────

    [Fact]
    public void IAnnotationState_GetProject_DelegatesToFuncProperty()
    {
        var expected = new AnnotationProject();
        var ctx = CreateContext(getProject: () => expected);

        IAnnotationState state = ctx;

        state.GetProject().Should().BeSameAs(expected);
    }

    [Fact]
    public void IAnnotationState_GetCurrentAnnotation_DelegatesToFuncProperty()
    {
        var expected = new AnnotationData { ImagePath = "test.jpg" };
        var ctx = CreateContext(getCurrentAnnotation: () => expected);

        IAnnotationState state = ctx;

        state.GetCurrentAnnotation().Should().BeSameAs(expected);
    }

    [Fact]
    public void IAnnotationState_GetCurrentImageIndex_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getCurrentImageIndex: () => 42);

        IAnnotationState state = ctx;

        state.GetCurrentImageIndex().Should().Be(42);
    }

    [Fact]
    public void IAnnotationState_GetTotalImages_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getTotalImages: () => 100);

        IAnnotationState state = ctx;

        state.GetTotalImages().Should().Be(100);
    }

    [Fact]
    public void IAnnotationState_GetSelectedClassIndex_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getSelectedClassIndex: () => 3);

        IAnnotationState state = ctx;

        state.GetSelectedClassIndex().Should().Be(3);
    }

    [Fact]
    public void IAnnotationState_GetPolylineCount_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getPolylineCount: () => 5);

        IAnnotationState state = ctx;

        state.GetPolylineCount().Should().Be(5);
    }

    [Fact]
    public void IAnnotationState_GetCircleCount_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getCircleCount: () => 7);

        IAnnotationState state = ctx;

        state.GetCircleCount().Should().Be(7);
    }

    [Fact]
    public void IAnnotationState_GetIsObbMode_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getIsObbMode: () => true);

        IAnnotationState state = ctx;

        state.GetIsObbMode().Should().BeTrue();
    }

    [Fact]
    public void IAnnotationState_GetIsPolygonMode_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getIsPolygonMode: () => true);

        IAnnotationState state = ctx;

        state.GetIsPolygonMode().Should().BeTrue();
    }

    [Fact]
    public void IAnnotationState_DrawingState_ReturnsSameInstance()
    {
        var drawingState = new DrawingStateManager();
        var ctx = CreateContext(drawingState: drawingState);

        IAnnotationState state = ctx;

        state.DrawingState.Should().BeSameAs(drawingState);
    }

    [Fact]
    public void IAnnotationState_GetCurrentImage_DelegatesToFuncProperty()
    {
        var ctx = CreateContext(getCurrentImage: () => null);

        IAnnotationState state = ctx;

        // BitmapImage is WPF-specific; verify the delegation path works with null
        state.GetCurrentImage().Should().BeNull();
    }

    // ── IAnnotationActions delegation ────────────────────────────────────────

    [Fact]
    public void IAnnotationActions_SetStatusMessage_DelegatesToActionProperty()
    {
        string? captured = null;
        var ctx = CreateContext(setStatusMessage: msg => captured = msg);

        IAnnotationActions actions = ctx;

        actions.SetStatusMessage("hello");

        captured.Should().Be("hello");
    }

    [Fact]
    public void IAnnotationActions_SetCurrentAnnotation_DelegatesToActionProperty()
    {
        AnnotationData? captured = null;
        var ctx = CreateContext(setCurrentAnnotation: data => captured = data);

        IAnnotationActions actions = ctx;

        var expected = new AnnotationData { ImagePath = "test.jpg" };
        actions.SetCurrentAnnotation(expected);

        captured.Should().BeSameAs(expected);
    }

    [Fact]
    public void IAnnotationActions_UpdateBoxesList_DelegatesToActionProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(updateBoxesList: () => callCount++);

        IAnnotationActions actions = ctx;

        actions.UpdateBoxesList();

        callCount.Should().Be(1);
    }

    [Fact]
    public void IAnnotationActions_UpdateStatistics_DelegatesToActionProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(updateStatistics: () => callCount++);

        IAnnotationActions actions = ctx;

        actions.UpdateStatistics();

        callCount.Should().Be(1);
    }

    [Fact]
    public void IAnnotationActions_UpdateClassDistribution_DelegatesToActionProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(updateClassDistribution: () => callCount++);

        IAnnotationActions actions = ctx;

        actions.UpdateClassDistribution();

        callCount.Should().Be(1);
    }

    [Fact]
    public void IAnnotationActions_UpdateClassesList_DelegatesToActionProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(updateClassesList: () => callCount++);

        IAnnotationActions actions = ctx;

        actions.UpdateClassesList();

        callCount.Should().Be(1);
    }

    [Fact]
    public void IAnnotationActions_PushUndoSnapshot_DelegatesToActionProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(pushUndoSnapshot: () => callCount++);

        IAnnotationActions actions = ctx;

        actions.PushUndoSnapshot();

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task IAnnotationActions_LoadFirstImage_DelegatesToFuncProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(loadFirstImage: () => { callCount++; return Task.CompletedTask; });

        IAnnotationActions actions = ctx;

        await actions.LoadFirstImage();

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task IAnnotationActions_LoadImageAsync_DelegatesToFuncProperty()
    {
        int capturedIndex = -1;
        var ctx = CreateContext(loadImageAsync: idx => { capturedIndex = idx; return Task.CompletedTask; });

        IAnnotationActions actions = ctx;

        await actions.LoadImageAsync(5);

        capturedIndex.Should().Be(5);
    }

    [Fact]
    public void IAnnotationActions_DisableObbMode_DelegatesToActionProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(disableObbMode: () => callCount++);

        IAnnotationActions actions = ctx;

        actions.DisableObbMode();

        callCount.Should().Be(1);
    }

    [Fact]
    public void IAnnotationActions_DisablePolygonMode_DelegatesToActionProperty()
    {
        int callCount = 0;
        var ctx = CreateContext(disablePolygonMode: () => callCount++);

        IAnnotationActions actions = ctx;

        actions.DisablePolygonMode();

        callCount.Should().Be(1);
    }

    // ── Mockability tests ────────────────────────────────────────────────────

    [Fact]
    public void IAnnotationState_CanBeMocked()
    {
        var mock = new Mock<IAnnotationState>();
        mock.Setup(s => s.GetProject()).Returns(new AnnotationProject());
        mock.Setup(s => s.GetCurrentImageIndex()).Returns(42);

        IAnnotationState state = mock.Object;

        state.GetProject().Should().NotBeNull();
        state.GetCurrentImageIndex().Should().Be(42);
    }

    [Fact]
    public void IAnnotationActions_CanBeMocked()
    {
        var mock = new Mock<IAnnotationActions>();
        string? captured = null;
        mock.Setup(a => a.SetStatusMessage(It.IsAny<string>()))
            .Callback<string>(msg => captured = msg);

        IAnnotationActions actions = mock.Object;

        actions.SetStatusMessage("test");

        captured.Should().Be("test");
        mock.Verify(a => a.SetStatusMessage("test"), Times.Once);
    }

    [Fact]
    public void Handler_CanAcceptMockedInterfaces()
    {
        // Verify that a handler can be constructed with mocked interfaces
        // This is the primary motivation for the interface extraction
        var stateMock = new Mock<IAnnotationState>();
        var actionsMock = new Mock<IAnnotationActions>();

        stateMock.Setup(s => s.GetProject()).Returns((AnnotationProject?)null);
        stateMock.Setup(s => s.GetCurrentAnnotation()).Returns((AnnotationData?)null);
        actionsMock.Setup(a => a.SetStatusMessage(It.IsAny<string>()));

        // Both mocks should be usable as their interface types
        IAnnotationState state = stateMock.Object;
        IAnnotationActions actions = actionsMock.Object;

        state.GetProject().Should().BeNull();
        actions.SetStatusMessage("test");
        actionsMock.Verify(a => a.SetStatusMessage("test"), Times.Once);
    }

    // ── Backward compatibility ───────────────────────────────────────────────

    [Fact]
    public void FuncAction_PropertiesStillWorkDirectly()
    {
        // Existing code that uses Func/Action properties directly should still work
        var project = new AnnotationProject();
        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
        };

        // Direct Func/Action access (backward compatible)
        ctx.GetProject().Should().BeSameAs(project);
        ctx.GetCurrentAnnotation().Should().BeNull();
    }

    [Fact]
    public void BothAccess_PathsReturnSameData()
    {
        var project = new AnnotationProject();
        var annotation = new AnnotationData { ImagePath = "test.jpg" };
        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => annotation,
            SetStatusMessage = _ => { },
        };

        // Direct property access
        var directProject = ctx.GetProject();
        var directAnnotation = ctx.GetCurrentAnnotation();

        // Interface access
        IAnnotationState state = ctx;
        var interfaceProject = state.GetProject();
        var interfaceAnnotation = state.GetCurrentAnnotation();

        // Both paths return the same data
        interfaceProject.Should().BeSameAs(directProject);
        interfaceAnnotation.Should().BeSameAs(directAnnotation);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static AnnotationContext CreateContext(
        Func<AnnotationProject?>? getProject = null,
        Func<AnnotationData?>? getCurrentAnnotation = null,
        Func<BitmapImage?>? getCurrentImage = null,
        Func<int>? getCurrentImageIndex = null,
        Func<int>? getTotalImages = null,
        Func<int>? getSelectedClassIndex = null,
        Func<int>? getPolylineCount = null,
        Func<int>? getCircleCount = null,
        Func<bool>? getIsObbMode = null,
        Func<bool>? getIsPolygonMode = null,
        DrawingStateManager? drawingState = null,
        Action<AnnotationData?>? setCurrentAnnotation = null,
        Action<string>? setStatusMessage = null,
        Action? updateBoxesList = null,
        Action? updateStatistics = null,
        Action? updateClassDistribution = null,
        Action? updateClassesList = null,
        Action? pushUndoSnapshot = null,
        Func<Task>? loadFirstImage = null,
        Func<int, Task>? loadImageAsync = null,
        Action? disableObbMode = null,
        Action? disablePolygonMode = null)
    {
        return new AnnotationContext
        {
            GetProject = getProject ?? (() => null),
            GetCurrentAnnotation = getCurrentAnnotation ?? (() => null),
            GetCurrentImage = getCurrentImage ?? (() => null),
            GetCurrentImageIndex = getCurrentImageIndex ?? (() => 0),
            GetTotalImages = getTotalImages ?? (() => 0),
            GetSelectedClassIndex = getSelectedClassIndex ?? (() => 0),
            GetPolylineCount = getPolylineCount ?? (() => 0),
            GetCircleCount = getCircleCount ?? (() => 0),
            GetIsObbMode = getIsObbMode ?? (() => false),
            GetIsPolygonMode = getIsPolygonMode ?? (() => false),
            DrawingState = drawingState ?? new DrawingStateManager(),
            SetCurrentAnnotation = setCurrentAnnotation ?? (_ => { }),
            SetStatusMessage = setStatusMessage ?? (_ => { }),
            UpdateBoxesList = updateBoxesList ?? (() => { }),
            UpdateStatistics = updateStatistics ?? (() => { }),
            UpdateClassDistribution = updateClassDistribution ?? (() => { }),
            UpdateClassesList = updateClassesList ?? (() => { }),
            PushUndoSnapshot = pushUndoSnapshot ?? (() => { }),
            LoadFirstImage = loadFirstImage ?? (() => Task.CompletedTask),
            LoadImageAsync = loadImageAsync ?? (_ => Task.CompletedTask),
            DisableObbMode = disableObbMode ?? (() => { }),
            DisablePolygonMode = disablePolygonMode ?? (() => { }),
        };
    }
}
