using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="UndoRedoHandler"/>, covering snapshot push/restore,
/// undo/redo operations, and command CanExecute states.
/// </summary>
public class UndoRedoHandlerTests
{
    private static (UndoRedoHandler handler, AnnotationContext ctx, AnnotationData annotation) CreateHandlerWithAnnotation(List<BoundingBoxAnnotation>? boxes = null)
    {
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            Boxes = boxes ?? new List<BoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, ClassName = "object", CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.2 }
            }
        };

        string? statusMessage = null;
        int updateBoxesCallCount = 0;
        int updateClassDistCallCount = 0;

        var ctx = new AnnotationContext
        {
            GetProject = () => new AnnotationProject(),
            GetCurrentAnnotation = () => annotation,
            SetStatusMessage = msg => statusMessage = msg,
            UpdateBoxesList = () => updateBoxesCallCount++,
            UpdateClassDistribution = () => updateClassDistCallCount++,
        };

        var handler = new UndoRedoHandler(ctx);
        return (handler, ctx, annotation);
    }

    // ── Initial state ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that UndoCommand is initialized.
    /// </summary>
    [Fact]
    public void UndoCommand_IsInitialized()
    {
        // Arrange & Act
        var (handler, _, _) = CreateHandlerWithAnnotation();

        // Assert
        handler.UndoCommand.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that RedoCommand is initialized.
    /// </summary>
    [Fact]
    public void RedoCommand_IsInitialized()
    {
        // Arrange & Act
        var (handler, _, _) = CreateHandlerWithAnnotation();

        // Assert
        handler.RedoCommand.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that UndoCommand CanExecute is false when no snapshots have been pushed.
    /// </summary>
    [Fact]
    public void UndoCommand_CanExecute_InitiallyFalse()
    {
        // Arrange & Act
        var (handler, _, _) = CreateHandlerWithAnnotation();

        // Assert
        handler.UndoCommand.CanExecute().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RedoCommand CanExecute is false when no undo has been performed.
    /// </summary>
    [Fact]
    public void RedoCommand_CanExecute_InitiallyFalse()
    {
        // Arrange & Act
        var (handler, _, _) = CreateHandlerWithAnnotation();

        // Assert
        handler.RedoCommand.CanExecute().Should().BeFalse();
    }

    // ── PushUndoSnapshot ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that PushUndoSnapshot makes UndoCommand executable.
    /// </summary>
    [Fact]
    public void PushUndoSnapshot_MakesUndoCommandExecutable()
    {
        // Arrange
        var (handler, _, _) = CreateHandlerWithAnnotation();

        // Act
        handler.PushUndoSnapshot();

        // Assert
        handler.UndoCommand.CanExecute().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that PushUndoSnapshot with null current annotation does nothing.
    /// </summary>
    [Fact]
    public void PushUndoSnapshot_WhenAnnotationNull_DoesNothing()
    {
        // Arrange
        var ctx = new AnnotationContext
        {
            GetProject = () => new AnnotationProject(),
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
            UpdateBoxesList = () => { },
            UpdateClassDistribution = () => { },
        };
        var handler = new UndoRedoHandler(ctx);

        // Act
        handler.PushUndoSnapshot();

        // Assert
        handler.UndoCommand.CanExecute().Should().BeFalse();
    }

    // ── Undo/Redo ────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that Undo restores the previous box state.
    /// </summary>
    [Fact]
    public void Undo_RestoresPreviousBoxState()
    {
        // Arrange
        var (handler, _, annotation) = CreateHandlerWithAnnotation();
        handler.PushUndoSnapshot(); // snapshot with 1 box

        // Modify: add a second box
        annotation.Boxes.Add(new BoundingBoxAnnotation
        {
            ClassIndex = 1, ClassName = "person", CenterX = 0.3, CenterY = 0.3, Width = 0.1, Height = 0.1
        });
        annotation.Boxes.Should().HaveCount(2);

        // Act
        handler.UndoCommand.Execute();

        // Assert — should be restored to 1 box
        annotation.Boxes.Should().HaveCount(1);
        annotation.Boxes[0].ClassName.Should().Be("object");
    }

    /// <summary>
    /// Verifies that Undo with no current annotation does nothing.
    /// </summary>
    [Fact]
    public void Undo_WhenAnnotationNull_DoesNothing()
    {
        // Arrange
        var ctx = new AnnotationContext
        {
            GetProject = () => new AnnotationProject(),
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
            UpdateBoxesList = () => { },
            UpdateClassDistribution = () => { },
        };
        var handler = new UndoRedoHandler(ctx);

        // Act
        handler.UndoCommand.Execute();

        // Assert — no exception
    }

    /// <summary>
    /// Verifies that Redo after Undo restores the undone state.
    /// </summary>
    [Fact]
    public void Redo_AfterUndo_RestoresUndoneState()
    {
        // Arrange
        var (handler, _, annotation) = CreateHandlerWithAnnotation();
        handler.PushUndoSnapshot(); // snapshot: 1 box

        // Modify: add a second box
        annotation.Boxes.Add(new BoundingBoxAnnotation
        {
            ClassIndex = 1, ClassName = "person", CenterX = 0.3, CenterY = 0.3, Width = 0.1, Height = 0.1
        });

        // Undo — back to 1 box
        handler.UndoCommand.Execute();
        annotation.Boxes.Should().HaveCount(1);

        // Act — Redo should restore 2 boxes
        handler.RedoCommand.Execute();

        // Assert
        annotation.Boxes.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that Undo with empty undo stack does nothing (CanExecute is false).
    /// </summary>
    [Fact]
    public void Undo_WithEmptyStack_DoesNotExecute()
    {
        // Arrange
        var (handler, _, _) = CreateHandlerWithAnnotation();

        // Assert
        handler.UndoCommand.CanExecute().Should().BeFalse();

        // Act — should not throw
        handler.UndoCommand.Execute();
    }

    /// <summary>
    /// Verifies that Redo with empty redo stack does nothing (CanExecute is false).
    /// </summary>
    [Fact]
    public void Redo_WithEmptyStack_DoesNotExecute()
    {
        // Arrange
        var (handler, _, _) = CreateHandlerWithAnnotation();

        // Assert
        handler.RedoCommand.CanExecute().Should().BeFalse();

        // Act — should not throw
        handler.RedoCommand.Execute();
    }

    /// <summary>
    /// Verifies that pushing a new snapshot after undo clears the redo stack.
    /// </summary>
    [Fact]
    public void PushSnapshot_AfterUndo_ClearsRedoStack()
    {
        // Arrange
        var (handler, _, annotation) = CreateHandlerWithAnnotation();
        handler.PushUndoSnapshot();
        annotation.Boxes.Add(new BoundingBoxAnnotation { ClassIndex = 1, ClassName = "person" });
        handler.UndoCommand.Execute();

        // Redo should be available now
        handler.RedoCommand.CanExecute().Should().BeTrue();

        // Act — push new snapshot clears redo
        handler.PushUndoSnapshot();

        // Assert
        handler.RedoCommand.CanExecute().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Undo restores polygon annotations as well.
    /// </summary>
    [Fact]
    public void Undo_RestoresPolygonState()
    {
        // Arrange
        var annotation = new AnnotationData { ImagePath = "test.jpg" };
        var ctx = new AnnotationContext
        {
            GetProject = () => new AnnotationProject(),
            GetCurrentAnnotation = () => annotation,
            SetStatusMessage = _ => { },
            UpdateBoxesList = () => { },
            UpdateClassDistribution = () => { },
        };
        var handler = new UndoRedoHandler(ctx);

        handler.PushUndoSnapshot();

        // Add a polygon
        annotation.Polygons.Add(new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "object",
            Points = new List<System.Drawing.PointF>
            {
                new(0.1f, 0.1f), new(0.5f, 0.1f), new(0.3f, 0.5f)
            }
        });
        annotation.Polygons.Should().HaveCount(1);

        // Act
        handler.UndoCommand.Execute();

        // Assert
        annotation.Polygons.Should().BeEmpty();
    }
}
