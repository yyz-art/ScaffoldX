using System.Windows.Media.Imaging;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 标注工具的共享上下文，封装所有 handler 共需的状态和回调。
/// 替代 AnnotationViewModel 构造函数中 64 个 Func/Action 参数。
/// </summary>
public class AnnotationContext
{
    // ── 只读状态 ──────────────────────────────────────────────────────────

    /// <summary>获取当前标注项目。</summary>
    public required Func<AnnotationProject?> GetProject { get; init; }

    /// <summary>获取当前图像的标注数据。</summary>
    public required Func<AnnotationData?> GetCurrentAnnotation { get; init; }

    /// <summary>获取当前显示的图像。</summary>
    public Func<BitmapImage?> GetCurrentImage { get; init; } = () => null;

    /// <summary>获取当前图像索引。</summary>
    public Func<int> GetCurrentImageIndex { get; init; } = () => 0;

    /// <summary>获取图像总数。</summary>
    public Func<int> GetTotalImages { get; init; } = () => 0;

    /// <summary>获取当前选中的类别索引。</summary>
    public Func<int> GetSelectedClassIndex { get; init; } = () => 0;

    /// <summary>获取折线标注数量。</summary>
    public Func<int> GetPolylineCount { get; init; } = () => 0;

    /// <summary>获取圆形标注数量。</summary>
    public Func<int> GetCircleCount { get; init; } = () => 0;

    // ── 状态写入 ──────────────────────────────────────────────────────────

    /// <summary>设置当前标注数据。</summary>
    public Action<AnnotationData?> SetCurrentAnnotation { get; init; } = _ => { };

    // ── UI 刷新 ───────────────────────────────────────────────────────────

    /// <summary>设置状态栏消息。</summary>
    public required Action<string> SetStatusMessage { get; init; }

    /// <summary>刷新标注框列表。</summary>
    public Action UpdateBoxesList { get; init; } = () => { };

    /// <summary>刷新统计信息。</summary>
    public Action UpdateStatistics { get; init; } = () => { };

    /// <summary>刷新类别分布。</summary>
    public Action UpdateClassDistribution { get; init; } = () => { };

    /// <summary>刷新类别列表。</summary>
    public Action UpdateClassesList { get; init; } = () => { };

    // ── 操作回调 ──────────────────────────────────────────────────────────

    /// <summary>推送撤销快照。</summary>
    public Action PushUndoSnapshot { get; init; } = () => { };

    /// <summary>加载第一张图像。</summary>
    public Func<Task> LoadFirstImage { get; init; } = () => Task.CompletedTask;

    /// <summary>按索引加载图像。</summary>
    public Func<int, Task> LoadImageAsync { get; init; } = _ => Task.CompletedTask;

    // ── 绘图状态 ──────────────────────────────────────────────────────────

    /// <summary>绘图状态管理器（多边形/OBB 共享）。</summary>
    public DrawingStateManager DrawingState { get; init; } = new();

    /// <summary>查询是否处于 OBB 模式。</summary>
    public Func<bool> GetIsObbMode { get; init; } = () => false;

    /// <summary>查询是否处于多边形模式。</summary>
    public Func<bool> GetIsPolygonMode { get; init; } = () => false;

    /// <summary>禁用 OBB 模式。</summary>
    public Action DisableObbMode { get; init; } = () => { };

    /// <summary>禁用多边形模式。</summary>
    public Action DisablePolygonMode { get; init; } = () => { };
}
