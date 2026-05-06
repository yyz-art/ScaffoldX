using System.Windows;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// Manages raw drawing state for polygon and OBB (oriented bounding box) modes.
/// The AnnotationViewModel delegates mutable state tracking here while keeping
/// the bindable property wrappers and RaisePropertyChanged calls itself.
/// </summary>
public class DrawingStateManager
{
    // ── Polygon state ──────────────────────────────────────────────────────────

    /// <summary>Whether a polygon is currently being drawn (vertices being added).</summary>
    public bool IsDrawingPolygon { get; set; }

    // ── OBB state ──────────────────────────────────────────────────────────────

    /// <summary>Whether the OBB size is being defined (drag phase).</summary>
    public bool IsDrawingObb { get; set; }

    /// <summary>Whether the OBB angle is being set (rotation phase).</summary>
    public bool IsRotatingObb { get; set; }

    /// <summary>OBB center point in screen coordinates.</summary>
    public Point ObbCenter { get; set; }

    /// <summary>OBB size in screen coordinates.</summary>
    public Size ObbSize { get; set; }

    /// <summary>OBB rotation angle in radians.</summary>
    public double ObbAngle { get; set; }

    // ── Bounding box state ─────────────────────────────────────────────────────

    /// <summary>Drag start point for bounding box drawing.</summary>
    public Point DrawStartPoint { get; set; }

    /// <summary>Drag end point for bounding box drawing.</summary>
    public Point DrawEndPoint { get; set; }

    /// <summary>
    /// Resets all drawing state to defaults.
    /// </summary>
    public void ResetDrawingState()
    {
        IsDrawingPolygon = false;
        IsDrawingObb = false;
        IsRotatingObb = false;
        ObbCenter = default;
        ObbSize = default;
        ObbAngle = 0;
        DrawStartPoint = default;
        DrawEndPoint = default;
    }

    /// <summary>
    /// Determines which drawing mode is active and resets it.
    /// Returns a string indicating which mode was cancelled, or null if nothing was active.
    /// </summary>
    /// <returns>The name of the cancelled mode ("polygon", "obb", "bbox") or null.</returns>
    public string? CancelDrawing()
    {
        if (IsDrawingPolygon)
        {
            IsDrawingPolygon = false;
            return "polygon";
        }

        if (IsDrawingObb || IsRotatingObb)
        {
            IsDrawingObb = false;
            IsRotatingObb = false;
            ObbSize = default;
            ObbAngle = 0;
            return "obb";
        }

        return null;
    }
}
