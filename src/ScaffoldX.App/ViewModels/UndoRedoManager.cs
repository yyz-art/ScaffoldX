namespace ScaffoldX.App.ViewModels;

/// <summary>
/// Generic undo/redo manager that maintains LIFO stacks of snapshots.
/// Pushing a new snapshot clears the redo stack (branching history).
/// </summary>
/// <typeparam name="T">The snapshot type stored on each stack.</typeparam>
public class UndoRedoManager<T>
{
    private readonly Stack<T> _undoStack = new();
    private readonly Stack<T> _redoStack = new();

    /// <summary>Whether an undo operation is available.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Whether a redo operation is available.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Pushes a snapshot onto the undo stack and clears the redo stack.
    /// Call this before each user-initiated mutation.
    /// </summary>
    /// <param name="snapshot">The state snapshot to save.</param>
    public void PushSnapshot(T snapshot)
    {
        _undoStack.Push(snapshot);
        _redoStack.Clear();
    }

    /// <summary>
    /// Pops the most recent snapshot from the undo stack,
    /// pushes the provided current state onto the redo stack,
    /// and returns the snapshot to restore.
    /// </summary>
    /// <param name="currentState">The current state to save on the redo stack.</param>
    /// <returns>The snapshot to restore, or default if the undo stack is empty.</returns>
    public T? Undo(T currentState)
    {
        if (_undoStack.Count == 0) return default;
        _redoStack.Push(currentState);
        return _undoStack.Pop();
    }

    /// <summary>
    /// Pops the most recent snapshot from the redo stack,
    /// pushes the provided current state onto the undo stack,
    /// and returns the snapshot to restore.
    /// </summary>
    /// <param name="currentState">The current state to save on the undo stack.</param>
    /// <returns>The snapshot to restore, or default if the redo stack is empty.</returns>
    public T? Redo(T currentState)
    {
        if (_redoStack.Count == 0) return default;
        _undoStack.Push(currentState);
        return _redoStack.Pop();
    }

    /// <summary>Clears both undo and redo stacks.</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
