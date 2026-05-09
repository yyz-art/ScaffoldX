using System.Drawing;
using ScaffoldX.Plugin.Annotation.Models;
using ScaffoldX.Plugin.Annotation.Vision.Services;

namespace ScaffoldX.Plugin.Annotation.Vision.Services;

public sealed class Sam3SegmentorStub : ISam3SegmentationEngine
{
    public bool IsModelLoaded => false;

    public Task LoadModelAsync(string modelDirectory, CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "SAM3 推理引擎未安装。请安装 ScaffoldX.Plugin.Annotation.Vision.TorchSharp 包以启用 SAM3 分割功能。");
    }

    public void UnloadModel() { }

    public Task<List<SegmentationAnnotation>> SegmentByTextAsync(
        string imagePath, IEnumerable<string> textPrompts, float threshold = 0.5f, CancellationToken ct = default)
    {
        throw new NotImplementedException("SAM3 推理引擎未安装。");
    }

    public Task<SegmentationAnnotation> SegmentByPointsAsync(
        string imagePath, IEnumerable<PointF> positivePoints, IEnumerable<PointF> negativePoints, CancellationToken ct = default)
    {
        throw new NotImplementedException("SAM3 推理引擎未安装。");
    }

    public Task<List<SegmentationAnnotation>> SegmentByReferenceAsync(
        string imagePath, string referenceImagePath, float threshold = 0.5f, CancellationToken ct = default)
    {
        throw new NotImplementedException("SAM3 推理引擎未安装。");
    }
}
