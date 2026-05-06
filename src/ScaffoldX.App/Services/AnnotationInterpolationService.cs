using System.Drawing;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// Provides linear interpolation between annotation frames for bounding boxes and polygons,
/// enabling semi-automatic annotation of intermediate video frames.
/// </summary>
public interface IAnnotationInterpolationService
{
    /// <summary>
    /// Linearly interpolates bounding box parameters between a start and end annotation.
    /// </summary>
    /// <param name="start">The bounding box at the first frame.</param>
    /// <param name="end">The bounding box at the last frame.</param>
    /// <param name="frameCount">Total number of frames to generate (including start and end).</param>
    /// <returns>A list of interpolated bounding box annotations, one per frame.</returns>
    List<BoundingBoxAnnotation> InterpolateBoundingBoxes(
        BoundingBoxAnnotation start, BoundingBoxAnnotation end, int frameCount);

    /// <summary>
    /// Point-by-point linear interpolation between a start and end polygon annotation.
    /// Both polygons must have the same number of points.
    /// </summary>
    /// <param name="start">The polygon at the first frame.</param>
    /// <param name="end">The polygon at the last frame.</param>
    /// <param name="frameCount">Total number of frames to generate (including start and end).</param>
    /// <returns>A list of interpolated polygon annotations, one per frame.</returns>
    List<PolygonAnnotation> InterpolatePolygons(
        PolygonAnnotation start, PolygonAnnotation end, int frameCount);
}

/// <summary>
/// Implements annotation interpolation using linear interpolation for both bounding boxes
/// (center + size model) and polygons (per-vertex interpolation).
/// </summary>
public class AnnotationInterpolationService : IAnnotationInterpolationService
{
    private readonly ILogger _logger = Log.ForContext<AnnotationInterpolationService>();

    /// <inheritdoc />
    public List<BoundingBoxAnnotation> InterpolateBoundingBoxes(
        BoundingBoxAnnotation start, BoundingBoxAnnotation end, int frameCount)
    {
        if (frameCount < 2)
            throw new ArgumentOutOfRangeException(nameof(frameCount), "Frame count must be at least 2.");

        var result = new List<BoundingBoxAnnotation>(frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            double t = (double)i / (frameCount - 1);
            result.Add(new BoundingBoxAnnotation
            {
                ClassIndex = start.ClassIndex,
                ClassName = start.ClassName,
                CenterX = Lerp(start.CenterX, end.CenterX, t),
                CenterY = Lerp(start.CenterY, end.CenterY, t),
                Width = Lerp(start.Width, end.Width, t),
                Height = Lerp(start.Height, end.Height, t)
            });
        }

        _logger.Debug("插值生成 {Count} 帧边界框标注", frameCount);
        return result;
    }

    /// <inheritdoc />
    public List<PolygonAnnotation> InterpolatePolygons(
        PolygonAnnotation start, PolygonAnnotation end, int frameCount)
    {
        if (frameCount < 2)
            throw new ArgumentOutOfRangeException(nameof(frameCount), "Frame count must be at least 2.");

        if (start.Points.Count != end.Points.Count)
            throw new ArgumentException(
                $"Polygon point counts must match: start has {start.Points.Count}, end has {end.Points.Count}.");

        var result = new List<PolygonAnnotation>(frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            double t = (double)i / (frameCount - 1);
            var points = new List<PointF>(start.Points.Count);

            for (int j = 0; j < start.Points.Count; j++)
            {
                points.Add(new PointF(
                    (float)Lerp(start.Points[j].X, end.Points[j].X, t),
                    (float)Lerp(start.Points[j].Y, end.Points[j].Y, t)));
            }

            result.Add(new PolygonAnnotation
            {
                ClassIndex = start.ClassIndex,
                ClassName = start.ClassName,
                Points = points
            });
        }

        _logger.Debug("插值生成 {Count} 帧多边形标注（{Points} 个顶点）", frameCount, start.Points.Count);
        return result;
    }

    /// <summary>
    /// Performs linear interpolation between two values.
    /// </summary>
    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
