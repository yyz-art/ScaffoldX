using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// Action callback interface for annotation operations.
/// Handlers depend on this instead of raw Action/Func delegates for type safety and testability.
/// </summary>
public interface IAnnotationActions
{
    /// <summary>Sets the annotation data for the current image.</summary>
    void SetCurrentAnnotation(AnnotationData? data);

    /// <summary>Sets a status bar message.</summary>
    void SetStatusMessage(string message);

    /// <summary>Refreshes the annotation boxes list in the UI.</summary>
    void UpdateBoxesList();

    /// <summary>Refreshes annotation statistics in the UI.</summary>
    void UpdateStatistics();

    /// <summary>Refreshes the class distribution display.</summary>
    void UpdateClassDistribution();

    /// <summary>Refreshes the annotation class list in the UI.</summary>
    void UpdateClassesList();

    /// <summary>Pushes a snapshot of the current annotation state for undo.</summary>
    void PushUndoSnapshot();

    /// <summary>Loads the first image in the project.</summary>
    Task LoadFirstImage();

    /// <summary>Loads the image at the specified index.</summary>
    Task LoadImageAsync(int index);

    /// <summary>Disables OBB drawing mode.</summary>
    void DisableObbMode();

    /// <summary>Disables polygon drawing mode.</summary>
    void DisablePolygonMode();
}
