using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// Review handler, managing annotation review summary and unannotated image navigation.
/// </summary>
public class ReviewCommandHandler : BindableBase
{
    private readonly Func<AnnotationProject?> _getProject;
    private readonly Func<int> _getCurrentImageIndex;
    private readonly Func<int, Task> _loadImageAsync;
    private readonly Action<string> _setStatusMessage;
    private readonly Action _updateStatistics;
    private readonly Func<int> _getPolylineCount;
    private readonly Func<int> _getCircleCount;

    private string _reviewSummaryText = string.Empty;
    private int _unannotatedImageCount;
    private bool _hasUnannotatedImages;

    /// <summary>
    /// Initializes the review handler.
    /// </summary>
    /// <param name="getProject">Callback to get the current project.</param>
    /// <param name="getCurrentImageIndex">Callback to get the current image index.</param>
    /// <param name="loadImageAsync">Callback to load an image by index.</param>
    /// <param name="setStatusMessage">Callback to set the status message.</param>
    /// <param name="updateStatistics">Callback to update project statistics in the ViewModel.</param>
    /// <param name="getPolylineCount">Callback to get the current image polyline count.</param>
    /// <param name="getCircleCount">Callback to get the current image circle count.</param>
    public ReviewCommandHandler(
        Func<AnnotationProject?> getProject,
        Func<int> getCurrentImageIndex,
        Func<int, Task> loadImageAsync,
        Action<string> setStatusMessage,
        Action updateStatistics,
        Func<int> getPolylineCount,
        Func<int> getCircleCount)
    {
        _getProject = getProject;
        _getCurrentImageIndex = getCurrentImageIndex;
        _loadImageAsync = loadImageAsync;
        _setStatusMessage = setStatusMessage;
        _updateStatistics = updateStatistics;
        _getPolylineCount = getPolylineCount;
        _getCircleCount = getCircleCount;

        GotoNextUnannotatedCommand = new DelegateCommand(ExecuteGotoNextUnannotated, () => HasUnannotatedImages);
        RefreshReviewSummaryCommand = new DelegateCommand(ExecuteRefreshReviewSummary);
    }

    /// <summary>Navigate to the next unannotated image command.</summary>
    public DelegateCommand GotoNextUnannotatedCommand { get; }

    /// <summary>Refresh the review summary command.</summary>
    public DelegateCommand RefreshReviewSummaryCommand { get; }

    /// <summary>Formatted review summary text with counts and percentages.</summary>
    public string ReviewSummaryText
    {
        get => _reviewSummaryText;
        private set => SetProperty(ref _reviewSummaryText, value);
    }

    /// <summary>Number of images without any annotations.</summary>
    public int UnannotatedImageCount
    {
        get => _unannotatedImageCount;
        private set => SetProperty(ref _unannotatedImageCount, value);
    }

    /// <summary>Whether there are any unannotated images in the project.</summary>
    public bool HasUnannotatedImages
    {
        get => _hasUnannotatedImages;
        private set => SetProperty(ref _hasUnannotatedImages, value);
    }

    /// <summary>
    /// Updates the review summary by recomputing annotation counts, unannotated image detection,
    /// and generating the formatted summary text.
    /// </summary>
    public void UpdateReviewSummary()
    {
        var project = _getProject();

        if (project == null)
        {
            ReviewSummaryText = string.Empty;
            UnannotatedImageCount = 0;
            HasUnannotatedImages = false;
            return;
        }

        var totalImages = project.Annotations.Count;
        var annotatedCount = project.Annotations.Count(a => IsAnnotated(a));
        UnannotatedImageCount = totalImages - annotatedCount;
        HasUnannotatedImages = UnannotatedImageCount > 0;

        var percentage = totalImages > 0 ? annotatedCount * 100.0 / totalImages : 0;

        var totalBoxes = 0;
        var totalPolygons = 0;
        var totalObbs = 0;
        var distribution = new Dictionary<string, int>();

        foreach (var annotation in project.Annotations)
        {
            totalBoxes += annotation.Boxes.Count;
            totalPolygons += annotation.Polygons.Count;
            totalObbs += annotation.OrientedBoxes.Count;

            foreach (var box in annotation.Boxes)
                distribution[box.ClassName] = distribution.GetValueOrDefault(box.ClassName) + 1;
            foreach (var polygon in annotation.Polygons)
                distribution[polygon.ClassName] = distribution.GetValueOrDefault(polygon.ClassName) + 1;
            foreach (var obb in annotation.OrientedBoxes)
                distribution[obb.ClassName] = distribution.GetValueOrDefault(obb.ClassName) + 1;
        }

        var totalAnnotations = totalBoxes + totalPolygons + totalObbs;

        var classLine = distribution.Count == 0
            ? "暂无类别数据"
            : string.Join(", ", distribution.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}"));

        ReviewSummaryText =
            $"总图像: {totalImages}\n" +
            $"已标注: {annotatedCount} ({percentage:F1}%)\n" +
            $"未标注: {UnannotatedImageCount}\n" +
            $"总标注: {totalAnnotations} (边界框: {totalBoxes}, 多边形: {totalPolygons}, OBB: {totalObbs})\n" +
            $"类别分布: {classLine}";

        GotoNextUnannotatedCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Navigates to the first unannotated image in the project.
    /// </summary>
    private async void ExecuteGotoNextUnannotated()
    {
        var project = _getProject();
        if (project == null) return;

        var currentIndex = _getCurrentImageIndex();
        var startIndex = currentIndex + 1;

        for (int i = 0; i < project.Annotations.Count; i++)
        {
            var checkIndex = (startIndex + i) % project.Annotations.Count;
            if (!IsAnnotated(project.Annotations[checkIndex]))
            {
                await _loadImageAsync(checkIndex);
                _setStatusMessage($"已跳转到未标注图像 #{checkIndex + 1}");
                return;
            }
        }

        _setStatusMessage("所有图像均已标注");
    }

    /// <summary>
    /// Refreshes the review summary by updating statistics and recomputing the review data.
    /// </summary>
    private void ExecuteRefreshReviewSummary()
    {
        _updateStatistics();
        UpdateReviewSummary();
        _setStatusMessage("审查摘要已刷新");
    }

    /// <summary>
    /// Determines whether an annotation has any content (boxes, polygons, OBBs, polylines, or circles).
    /// </summary>
    /// <param name="annotation">The annotation data to check.</param>
    /// <returns>True if the annotation has any content.</returns>
    private static bool IsAnnotated(AnnotationData annotation)
        => annotation.Boxes.Count > 0
           || annotation.Polygons.Count > 0
           || annotation.OrientedBoxes.Count > 0
           || annotation.Polylines.Count > 0
           || annotation.Circles.Count > 0;
}
