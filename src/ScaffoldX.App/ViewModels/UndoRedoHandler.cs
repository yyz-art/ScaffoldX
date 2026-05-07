using System.Drawing;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 撤销/重做处理器，管理标注状态快照的撤销和重做操作。
/// </summary>
public class UndoRedoHandler : BindableBase
{
    private readonly UndoRedoManager<AnnotationSnapshot> _undoRedoManager = new();
    private readonly AnnotationContext _ctx;

    /// <summary>
    /// 标注数据快照，用于撤销/重做操作。
    /// </summary>
    internal record AnnotationSnapshot(
        List<BoundingBoxAnnotation> Boxes,
        List<PolygonAnnotation> Polygons,
        List<OrientedBoundingBoxAnnotation> OrientedBoxes);

    /// <summary>
    /// 初始化撤销/重做处理器。
    /// </summary>
    public UndoRedoHandler(AnnotationContext ctx)
    {
        _ctx = ctx;

        UndoCommand = new DelegateCommand(ExecuteUndo, CanUndo);
        RedoCommand = new DelegateCommand(ExecuteRedo, CanRedo);
    }

    /// <summary>撤销命令。</summary>
    public DelegateCommand UndoCommand { get; }

    /// <summary>重做命令。</summary>
    public DelegateCommand RedoCommand { get; }

    /// <summary>
    /// 推送当前标注数据的快照到撤销栈。
    /// </summary>
    public void PushUndoSnapshot()
    {
        var currentAnnotation = _ctx.GetCurrentAnnotation();
        if (currentAnnotation == null) return;
        _undoRedoManager.PushSnapshot(CloneSnapshot(currentAnnotation));
    }

    /// <summary>
    /// 判断是否可以撤销。
    /// </summary>
    private bool CanUndo() => _undoRedoManager.CanUndo && _ctx.GetCurrentAnnotation() != null;

    /// <summary>
    /// 执行撤销操作，恢复到上一个快照状态。
    /// </summary>
    private void ExecuteUndo()
    {
        var currentAnnotation = _ctx.GetCurrentAnnotation();
        if (currentAnnotation == null) return;

        var snapshot = _undoRedoManager.Undo(CloneSnapshot(currentAnnotation));
        if (snapshot == null) return;

        RestoreSnapshot(currentAnnotation, snapshot);
        _ctx.UpdateBoxesList();
        _ctx.UpdateClassDistribution();
        _ctx.SetStatusMessage("已撤销");
    }

    /// <summary>
    /// 判断是否可以重做。
    /// </summary>
    private bool CanRedo() => _undoRedoManager.CanRedo && _ctx.GetCurrentAnnotation() != null;

    /// <summary>
    /// 执行重做操作，恢复到下一个快照状态。
    /// </summary>
    private void ExecuteRedo()
    {
        var currentAnnotation = _ctx.GetCurrentAnnotation();
        if (currentAnnotation == null) return;

        var snapshot = _undoRedoManager.Redo(CloneSnapshot(currentAnnotation));
        if (snapshot == null) return;

        RestoreSnapshot(currentAnnotation, snapshot);
        _ctx.UpdateBoxesList();
        _ctx.UpdateClassDistribution();
        _ctx.SetStatusMessage("已重做");
    }

    /// <summary>
    /// 克隆当前标注数据为快照。
    /// </summary>
    /// <param name="annotation">要克隆的标注数据。</param>
    private static AnnotationSnapshot CloneSnapshot(AnnotationData annotation) => new(
        Boxes: annotation.Boxes.Select(b => new BoundingBoxAnnotation
        {
            ClassIndex = b.ClassIndex,
            ClassName = b.ClassName,
            CenterX = b.CenterX,
            CenterY = b.CenterY,
            Width = b.Width,
            Height = b.Height
        }).ToList(),
        Polygons: annotation.Polygons.Select(p => new PolygonAnnotation
        {
            Id = p.Id,
            ClassIndex = p.ClassIndex,
            ClassName = p.ClassName,
            Points = p.Points.Select(pt => new PointF(pt.X, pt.Y)).ToList()
        }).ToList(),
        OrientedBoxes: annotation.OrientedBoxes.Select(o => new OrientedBoundingBoxAnnotation
        {
            Id = o.Id,
            ClassIndex = o.ClassIndex,
            ClassName = o.ClassName,
            CenterX = o.CenterX,
            CenterY = o.CenterY,
            Width = o.Width,
            Height = o.Height,
            Angle = o.Angle
        }).ToList()
    );

    /// <summary>
    /// 将快照数据恢复到目标标注数据。
    /// </summary>
    /// <param name="target">恢复目标。</param>
    /// <param name="snapshot">要恢复的快照。</param>
    private static void RestoreSnapshot(AnnotationData target, AnnotationSnapshot snapshot)
    {
        target.Boxes.Clear();
        target.Boxes.AddRange(snapshot.Boxes);

        target.Polygons.Clear();
        target.Polygons.AddRange(snapshot.Polygons);

        target.OrientedBoxes.Clear();
        target.OrientedBoxes.AddRange(snapshot.OrientedBoxes);
    }
}
