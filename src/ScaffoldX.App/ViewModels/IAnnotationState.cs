using System.Windows.Media.Imaging;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// Read-only query interface for annotation state.
/// Handlers depend on this instead of raw Func delegates for type safety and testability.
/// </summary>
public interface IAnnotationState
{
    /// <summary>Gets the current annotation project.</summary>
    AnnotationProject? GetProject();

    /// <summary>Gets the annotation data for the current image.</summary>
    AnnotationData? GetCurrentAnnotation();

    /// <summary>Gets the currently displayed image.</summary>
    BitmapImage? GetCurrentImage();

    /// <summary>Gets the index of the current image in the project.</summary>
    int GetCurrentImageIndex();

    /// <summary>Gets the total number of images in the project.</summary>
    int GetTotalImages();

    /// <summary>Gets the index of the currently selected annotation class.</summary>
    int GetSelectedClassIndex();

    /// <summary>Gets the number of polyline annotations on the current image.</summary>
    int GetPolylineCount();

    /// <summary>Gets the number of circle annotations on the current image.</summary>
    int GetCircleCount();

    /// <summary>Gets whether OBB (oriented bounding box) drawing mode is active.</summary>
    bool GetIsObbMode();

    /// <summary>Gets whether polygon drawing mode is active.</summary>
    bool GetIsPolygonMode();

    /// <summary>Gets the shared drawing state manager for polygon and OBB modes.</summary>
    DrawingStateManager DrawingState { get; }
}
