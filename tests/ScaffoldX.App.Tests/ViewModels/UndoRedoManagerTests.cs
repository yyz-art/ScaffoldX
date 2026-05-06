using FluentAssertions;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="UndoRedoManager{T}"/>, verifying snapshot push/pop
/// semantics, branching history (redo cleared on new push), and edge cases.
/// </summary>
public class UndoRedoManagerTests
{
    private readonly UndoRedoManager<string> _manager = new();

    /// <summary>
    /// Verifies that PushSnapshot adds a snapshot to the undo stack.
    /// </summary>
    [Fact]
    public void PushSnapshot_AddsToUndoStack()
    {
        // Arrange & Act
        _manager.PushSnapshot("state1");

        // Assert
        _manager.CanUndo.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Undo returns the last pushed snapshot and saves current state to redo.
    /// </summary>
    [Fact]
    public void Undo_ReturnsLastSnapshot()
    {
        // Arrange
        _manager.PushSnapshot("state1");
        _manager.PushSnapshot("state2");

        // Act
        var result = _manager.Undo("currentState");

        // Assert
        result.Should().Be("state2");
    }

    /// <summary>
    /// Verifies that Undo on an empty stack returns default.
    /// </summary>
    [Fact]
    public void Undo_EmptyStack_ReturnsDefault()
    {
        // Act
        var result = _manager.Undo("currentState");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Redo after Undo restores the previously undone snapshot.
    /// </summary>
    [Fact]
    public void Redo_AfterUndo_RestoresSnapshot()
    {
        // Arrange
        _manager.PushSnapshot("state1");
        _manager.PushSnapshot("state2");
        _manager.Undo("currentState");

        // Act
        var result = _manager.Redo("undoneState");

        // Assert
        result.Should().Be("currentState");
    }

    /// <summary>
    /// Verifies that Redo without a prior Undo returns default.
    /// </summary>
    [Fact]
    public void Redo_NoUndo_ReturnsDefault()
    {
        // Arrange
        _manager.PushSnapshot("state1");

        // Act
        var result = _manager.Redo("currentState");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that PushSnapshot clears the redo stack (branching history).
    /// </summary>
    [Fact]
    public void PushSnapshot_ClearsRedoStack()
    {
        // Arrange
        _manager.PushSnapshot("state1");
        _manager.PushSnapshot("state2");
        _manager.Undo("currentState"); // redo stack now has "currentState"
        _manager.CanRedo.Should().BeTrue();

        // Act
        _manager.PushSnapshot("newBranch");

        // Assert
        _manager.CanRedo.Should().BeFalse("pushing a new snapshot should clear the redo stack");
    }

    /// <summary>
    /// Verifies that Clear removes all snapshots from both stacks.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllSnapshots()
    {
        // Arrange
        _manager.PushSnapshot("state1");
        _manager.PushSnapshot("state2");
        _manager.Undo("currentState");

        // Act
        _manager.Clear();

        // Assert
        _manager.CanUndo.Should().BeFalse();
        _manager.CanRedo.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CanUndo returns true when the undo stack is not empty.
    /// </summary>
    [Fact]
    public void CanUndo_WhenNotEmpty_ReturnsTrue()
    {
        // Arrange
        _manager.PushSnapshot("state1");

        // Act & Assert
        _manager.CanUndo.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CanRedo returns true when the redo stack is not empty.
    /// </summary>
    [Fact]
    public void CanRedo_WhenNotEmpty_ReturnsTrue()
    {
        // Arrange
        _manager.PushSnapshot("state1");
        _manager.PushSnapshot("state2");
        _manager.Undo("currentState");

        // Act & Assert
        _manager.CanRedo.Should().BeTrue();
    }
}
