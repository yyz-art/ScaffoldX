using FluentAssertions;
using ScaffoldX.App.Models;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="ClassManagementHandler"/>, covering initial state,
/// add/remove class operations, class selection, and edge cases.
/// </summary>
public class ClassManagementHandlerTests
{
    /// <summary>
    /// Creates an AnnotationContext with a project containing the specified classes.
    /// </summary>
    private static (ClassManagementHandler handler, AnnotationContext ctx, AnnotationProject project) CreateHandlerWithClasses(List<AnnotationClass>? classes = null)
    {
        var project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Classes = classes ?? new List<AnnotationClass>
            {
                new() { Index = 0, Name = "object", Color = "#FF0000" }
            }
        };

        string? statusMessage = null;
        int updateClassesCallCount = 0;

        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => null,
            SetStatusMessage = msg => statusMessage = msg,
            UpdateClassesList = () => updateClassesCallCount++,
        };

        var handler = new ClassManagementHandler(ctx);
        return (handler, ctx, project);
    }

    // ── Initial state ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that SelectedClassIndex defaults to 0.
    /// </summary>
    [Fact]
    public void SelectedClassIndex_DefaultsToZero()
    {
        // Arrange & Act
        var (handler, _, _) = CreateHandlerWithClasses();

        // Assert
        handler.SelectedClassIndex.Should().Be(0);
    }

    /// <summary>
    /// Verifies that CurrentClassName returns the first class name when index is 0.
    /// </summary>
    [Fact]
    public void CurrentClassName_ReturnsFirstClassName()
    {
        // Arrange & Act
        var (handler, _, _) = CreateHandlerWithClasses();

        // Assert
        handler.CurrentClassName.Should().Be("object");
    }

    /// <summary>
    /// Verifies that CurrentClassName returns empty string when project is null.
    /// </summary>
    [Fact]
    public void CurrentClassName_WhenProjectNull_ReturnsEmpty()
    {
        // Arrange
        var ctx = new AnnotationContext
        {
            GetProject = () => null,
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
        };
        var handler = new ClassManagementHandler(ctx);

        // Act & Assert
        handler.CurrentClassName.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that commands are initialized and not null.
    /// </summary>
    [Fact]
    public void Commands_AreInitialized()
    {
        // Arrange & Act
        var (handler, _, _) = CreateHandlerWithClasses();

        // Assert
        handler.AddClassCommand.Should().NotBeNull();
        handler.RemoveClassCommand.Should().NotBeNull();
        handler.SelectClassCommand.Should().NotBeNull();
    }

    // ── AddClassCommand ──────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that AddClassCommand adds a new class to the project.
    /// </summary>
    [Fact]
    public void AddClassCommand_AddsNewClass()
    {
        // Arrange
        var (handler, _, project) = CreateHandlerWithClasses();
        var initialCount = project.Classes.Count;

        // Act
        handler.AddClassCommand.Execute();

        // Assert
        project.Classes.Should().HaveCount(initialCount + 1);
        project.Classes.Last().Name.Should().Be("class_1");
        project.Classes.Last().Index.Should().Be(1);
    }

    /// <summary>
    /// Verifies that AddClassCommand assigns a color to the new class.
    /// </summary>
    [Fact]
    public void AddClassCommand_AssignsColorToNewClass()
    {
        // Arrange
        var (handler, _, project) = CreateHandlerWithClasses();

        // Act
        handler.AddClassCommand.Execute();

        // Assert
        project.Classes.Last().Color.Should().NotBeNullOrEmpty();
        project.Classes.Last().Color.Should().StartWith("#");
    }

    /// <summary>
    /// Verifies that AddClassCommand calls UpdateClassesList on context.
    /// </summary>
    [Fact]
    public void AddClassCommand_CallsUpdateClassesList()
    {
        // Arrange
        int updateCallCount = 0;
        var project = new AnnotationProject
        {
            Classes = new List<AnnotationClass> { new() { Index = 0, Name = "obj" } }
        };
        var ctx = new AnnotationContext
        {
            GetProject = () => project,
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
            UpdateClassesList = () => updateCallCount++,
        };
        var handler = new ClassManagementHandler(ctx);

        // Act
        handler.AddClassCommand.Execute();

        // Assert
        updateCallCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that AddClassCommand does nothing when project is null.
    /// </summary>
    [Fact]
    public void AddClassCommand_WhenProjectNull_DoesNothing()
    {
        // Arrange
        var ctx = new AnnotationContext
        {
            GetProject = () => null,
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
            UpdateClassesList = () => { },
        };
        var handler = new ClassManagementHandler(ctx);

        // Act
        handler.AddClassCommand.Execute();

        // Assert — no exception thrown
    }

    // ── RemoveClassCommand ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that RemoveClassCommand removes the last class when more than one exists.
    /// </summary>
    [Fact]
    public void RemoveClassCommand_RemovesLastClass()
    {
        // Arrange
        var classes = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "object" },
            new() { Index = 1, Name = "person" }
        };
        var (handler, _, project) = CreateHandlerWithClasses(classes);

        // Act
        handler.RemoveClassCommand.Execute();

        // Assert
        project.Classes.Should().HaveCount(1);
        project.Classes[0].Name.Should().Be("object");
    }

    /// <summary>
    /// Verifies that RemoveClassCommand does not remove the last remaining class.
    /// </summary>
    [Fact]
    public void RemoveClassCommand_WhenOnlyOneClass_DoesNotRemove()
    {
        // Arrange
        var (handler, _, project) = CreateHandlerWithClasses();

        // Act
        handler.RemoveClassCommand.Execute();

        // Assert
        project.Classes.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that RemoveClassCommand CanExecute is false when only one class.
    /// </summary>
    [Fact]
    public void RemoveClassCommand_CanExecute_WhenOnlyOneClass_ReturnsFalse()
    {
        // Arrange
        var (handler, _, _) = CreateHandlerWithClasses();

        // Assert
        handler.RemoveClassCommand.CanExecute().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RemoveClassCommand CanExecute is true when multiple classes.
    /// </summary>
    [Fact]
    public void RemoveClassCommand_CanExecute_WhenMultipleClasses_ReturnsTrue()
    {
        // Arrange
        var classes = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "object" },
            new() { Index = 1, Name = "person" }
        };
        var (handler, _, _) = CreateHandlerWithClasses(classes);

        // Assert
        handler.RemoveClassCommand.CanExecute().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that RemoveClassCommand does nothing when project is null.
    /// </summary>
    [Fact]
    public void RemoveClassCommand_WhenProjectNull_DoesNothing()
    {
        // Arrange
        var ctx = new AnnotationContext
        {
            GetProject = () => null,
            GetCurrentAnnotation = () => null,
            SetStatusMessage = _ => { },
            UpdateClassesList = () => { },
        };
        var handler = new ClassManagementHandler(ctx);

        // Act
        handler.RemoveClassCommand.Execute();

        // Assert — no exception thrown
    }

    // ── SelectClassCommand ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that SelectClassCommand updates SelectedClassIndex.
    /// </summary>
    [Fact]
    public void SelectClassCommand_UpdatesSelectedClassIndex()
    {
        // Arrange
        var classes = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "object" },
            new() { Index = 1, Name = "person" },
            new() { Index = 2, Name = "car" }
        };
        var (handler, _, _) = CreateHandlerWithClasses(classes);

        // Act
        handler.SelectClassCommand.Execute(2);

        // Assert
        handler.SelectedClassIndex.Should().Be(2);
    }

    /// <summary>
    /// Verifies that SelectClassCommand with out-of-range index does nothing.
    /// </summary>
    [Fact]
    public void SelectClassCommand_OutOfRange_DoesNothing()
    {
        // Arrange
        var (handler, _, _) = CreateHandlerWithClasses();

        // Act
        handler.SelectClassCommand.Execute(5);

        // Assert
        handler.SelectedClassIndex.Should().Be(0);
    }

    /// <summary>
    /// Verifies that SelectClassCommand with negative index does nothing.
    /// </summary>
    [Fact]
    public void SelectClassCommand_NegativeIndex_DoesNothing()
    {
        // Arrange
        var (handler, _, _) = CreateHandlerWithClasses();

        // Act
        handler.SelectClassCommand.Execute(-1);

        // Assert
        handler.SelectedClassIndex.Should().Be(0);
    }

    /// <summary>
    /// Verifies that SelectClassCommand with null does nothing.
    /// </summary>
    [Fact]
    public void SelectClassCommand_Null_DoesNothing()
    {
        // Arrange
        var (handler, _, _) = CreateHandlerWithClasses();

        // Act
        handler.SelectClassCommand.Execute(null);

        // Assert
        handler.SelectedClassIndex.Should().Be(0);
    }

    // ── CurrentClassName ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that CurrentClassName updates when SelectedClassIndex changes.
    /// </summary>
    [Fact]
    public void CurrentClassName_UpdatesWithSelectedIndex()
    {
        // Arrange
        var classes = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "object" },
            new() { Index = 1, Name = "person" }
        };
        var (handler, _, _) = CreateHandlerWithClasses(classes);

        // Act
        handler.SelectedClassIndex = 1;

        // Assert
        handler.CurrentClassName.Should().Be("person");
    }
}
