using System.Drawing;
using ScaffoldX.Abstractions.Plugins;
using ScaffoldX.Plugin.Annotation.Models;

namespace ScaffoldX.Plugin.Annotation.Vision.Services;

public interface ISam3SegmentationEngine
{
    bool IsModelLoaded { get; }

    Task LoadModelAsync(string modelDirectory, CancellationToken ct = default);

    void UnloadModel();

    Task<List<SegmentationAnnotation>> SegmentByTextAsync(
        string imagePath, IEnumerable<string> textPrompts, float threshold = 0.5f, CancellationToken ct = default);

    Task<SegmentationAnnotation> SegmentByPointsAsync(
        string imagePath, IEnumerable<PointF> positivePoints, IEnumerable<PointF> negativePoints, CancellationToken ct = default);

    Task<List<SegmentationAnnotation>> SegmentByReferenceAsync(
        string imagePath, string referenceImagePath, float threshold = 0.5f, CancellationToken ct = default);
}
