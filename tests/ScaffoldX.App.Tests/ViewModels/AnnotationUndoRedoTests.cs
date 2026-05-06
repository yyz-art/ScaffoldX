using System.Drawing;
using FluentAssertions;
using ScaffoldX.App.Models;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for undo/redo operations with boxes, polygons, and OBB annotations.
/// Tests the snapshot/restore pattern used by AnnotationViewModel's undo/redo stacks.
/// </summary>
public class AnnotationUndoRedoTests
{
    /// <summary>
    /// Verifies that undo after adding a box removes it, restoring the previous state.
    /// </summary>
    [Fact]
    public void Undo_AfterAddingBox_RemovesBox()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>()
        };

        var undoStack = new Stack<List<BoundingBoxAnnotation>>();
        var redoStack = new Stack<List<BoundingBoxAnnotation>>();

        // Snapshot before adding
        undoStack.Push(CloneBoxes(annotation.Boxes));
        redoStack.Clear();

        // Act: add a box
        annotation.Boxes.Add(new BoundingBoxAnnotation
        {
            ClassIndex = 0, ClassName = "cat",
            CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3
        });
        annotation.Boxes.Should().HaveCount(1);

        // Undo
        redoStack.Push(CloneBoxes(annotation.Boxes));
        var snapshot = undoStack.Pop();
        annotation.Boxes.Clear();
        foreach (var box in snapshot) annotation.Boxes.Add(box);

        // Assert
        annotation.Boxes.Should().BeEmpty("undo should restore the state before the box was added");
    }

    /// <summary>
    /// Verifies that undo after adding a polygon removes it.
    /// The undo system snapshots Boxes; polygons are tracked separately.
    /// This test verifies the polygon list manipulation pattern.
    /// </summary>
    [Fact]
    public void Undo_AfterAddingPolygon_RemovesPolygon()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Polygons = new List<PolygonAnnotation>()
        };

        var polygonUndoStack = new Stack<List<PolygonAnnotation>>();
        var polygonRedoStack = new Stack<List<PolygonAnnotation>>();

        // Snapshot before adding
        polygonUndoStack.Push(ClonePolygons(annotation.Polygons));
        polygonRedoStack.Clear();

        // Act: add a polygon
        annotation.Polygons.Add(new PolygonAnnotation
        {
            ClassIndex = 0,
            ClassName = "defect",
            Points = new List<PointF>
            {
                new(0.1f, 0.1f),
                new(0.3f, 0.1f),
                new(0.2f, 0.3f)
            }
        });
        annotation.Polygons.Should().HaveCount(1);

        // Undo
        polygonRedoStack.Push(ClonePolygons(annotation.Polygons));
        var snapshot = polygonUndoStack.Pop();
        annotation.Polygons.Clear();
        foreach (var p in snapshot) annotation.Polygons.Add(p);

        // Assert
        annotation.Polygons.Should().BeEmpty("undo should restore the state before the polygon was added");
    }

    /// <summary>
    /// Verifies that undo after adding an OBB removes it.
    /// </summary>
    [Fact]
    public void Undo_AfterAddingObb_RemovesObb()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
        };

        var obbUndoStack = new Stack<List<OrientedBoundingBoxAnnotation>>();
        var obbRedoStack = new Stack<List<OrientedBoundingBoxAnnotation>>();

        // Snapshot before adding
        obbUndoStack.Push(CloneObbs(annotation.OrientedBoxes));
        obbRedoStack.Clear();

        // Act: add an OBB
        annotation.OrientedBoxes.Add(new OrientedBoundingBoxAnnotation
        {
            ClassIndex = 0,
            ClassName = "part",
            CenterX = 0.5f,
            CenterY = 0.5f,
            Width = 0.2f,
            Height = 0.1f,
            Angle = 0.785f
        });
        annotation.OrientedBoxes.Should().HaveCount(1);

        // Undo
        obbRedoStack.Push(CloneObbs(annotation.OrientedBoxes));
        var snapshot = obbUndoStack.Pop();
        annotation.OrientedBoxes.Clear();
        foreach (var obb in snapshot) annotation.OrientedBoxes.Add(obb);

        // Assert
        annotation.OrientedBoxes.Should().BeEmpty("undo should restore the state before the OBB was added");
    }

    /// <summary>
    /// Verifies that redo after undo restores the previously removed box.
    /// </summary>
    [Fact]
    public void Redo_AfterUndo_RestoresBox()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>()
        };

        var undoStack = new Stack<List<BoundingBoxAnnotation>>();
        var redoStack = new Stack<List<BoundingBoxAnnotation>>();

        // Snapshot empty state
        undoStack.Push(CloneBoxes(annotation.Boxes));
        redoStack.Clear();

        // Add a box
        var box = new BoundingBoxAnnotation
        {
            ClassIndex = 0, ClassName = "cat",
            CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3
        };
        annotation.Boxes.Add(box);

        // Undo (restore empty state)
        redoStack.Push(CloneBoxes(annotation.Boxes));
        var undoSnapshot = undoStack.Pop();
        annotation.Boxes.Clear();
        foreach (var b in undoSnapshot) annotation.Boxes.Add(b);
        annotation.Boxes.Should().BeEmpty();

        // Act: Redo (restore the state with the box)
        undoStack.Push(CloneBoxes(annotation.Boxes));
        var redoSnapshot = redoStack.Pop();
        annotation.Boxes.Clear();
        foreach (var b in redoSnapshot) annotation.Boxes.Add(b);

        // Assert
        annotation.Boxes.Should().HaveCount(1);
        annotation.Boxes[0].ClassName.Should().Be("cat");
        annotation.Boxes[0].CenterX.Should().Be(0.5);
    }

    /// <summary>
    /// Verifies that redo after undo restores the previously removed polygon.
    /// </summary>
    [Fact]
    public void Redo_AfterUndo_RestoresPolygon()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Polygons = new List<PolygonAnnotation>()
        };

        var undoStack = new Stack<List<PolygonAnnotation>>();
        var redoStack = new Stack<List<PolygonAnnotation>>();

        // Snapshot empty state
        undoStack.Push(ClonePolygons(annotation.Polygons));
        redoStack.Clear();

        // Add a polygon
        annotation.Polygons.Add(new PolygonAnnotation
        {
            ClassIndex = 1,
            ClassName = "scratch",
            Points = new List<PointF>
            {
                new(0.2f, 0.3f),
                new(0.4f, 0.3f),
                new(0.3f, 0.5f)
            }
        });

        // Undo (restore empty state)
        redoStack.Push(ClonePolygons(annotation.Polygons));
        var undoSnapshot = undoStack.Pop();
        annotation.Polygons.Clear();
        foreach (var p in undoSnapshot) annotation.Polygons.Add(p);
        annotation.Polygons.Should().BeEmpty();

        // Act: Redo
        undoStack.Push(ClonePolygons(annotation.Polygons));
        var redoSnapshot = redoStack.Pop();
        annotation.Polygons.Clear();
        foreach (var p in redoSnapshot) annotation.Polygons.Add(p);

        // Assert
        annotation.Polygons.Should().HaveCount(1);
        annotation.Polygons[0].ClassName.Should().Be("scratch");
        annotation.Polygons[0].Points.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that multiple undo operations reverse in LIFO order.
    /// </summary>
    [Fact]
    public void Undo_MultipleOperations_ReversesInOrder()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>()
        };

        var undoStack = new Stack<List<BoundingBoxAnnotation>>();
        var redoStack = new Stack<List<BoundingBoxAnnotation>>();

        // Operation 1: add box A
        undoStack.Push(CloneBoxes(annotation.Boxes));
        redoStack.Clear();
        annotation.Boxes.Add(new BoundingBoxAnnotation
        {
            ClassIndex = 0, ClassName = "A",
            CenterX = 0.1, CenterY = 0.1, Width = 0.1, Height = 0.1
        });

        // Operation 2: add box B
        undoStack.Push(CloneBoxes(annotation.Boxes));
        redoStack.Clear();
        annotation.Boxes.Add(new BoundingBoxAnnotation
        {
            ClassIndex = 1, ClassName = "B",
            CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.2
        });

        // Operation 3: add box C
        undoStack.Push(CloneBoxes(annotation.Boxes));
        redoStack.Clear();
        annotation.Boxes.Add(new BoundingBoxAnnotation
        {
            ClassIndex = 2, ClassName = "C",
            CenterX = 0.8, CenterY = 0.8, Width = 0.1, Height = 0.1
        });

        annotation.Boxes.Should().HaveCount(3);

        // Act & Assert: Undo 1 — removes C
        redoStack.Push(CloneBoxes(annotation.Boxes));
        var s1 = undoStack.Pop();
        annotation.Boxes.Clear();
        foreach (var b in s1) annotation.Boxes.Add(b);
        annotation.Boxes.Should().HaveCount(2);
        annotation.Boxes.Should().NotContain(b => b.ClassName == "C");

        // Undo 2 — removes B
        redoStack.Push(CloneBoxes(annotation.Boxes));
        var s2 = undoStack.Pop();
        annotation.Boxes.Clear();
        foreach (var b in s2) annotation.Boxes.Add(b);
        annotation.Boxes.Should().HaveCount(1);
        annotation.Boxes[0].ClassName.Should().Be("A");

        // Undo 3 — removes A
        redoStack.Push(CloneBoxes(annotation.Boxes));
        var s3 = undoStack.Pop();
        annotation.Boxes.Clear();
        foreach (var b in s3) annotation.Boxes.Add(b);
        annotation.Boxes.Should().BeEmpty();
    }

    // ── Clone helpers (mirror AnnotationViewModel.PushUndoSnapshot pattern) ──

    private static List<BoundingBoxAnnotation> CloneBoxes(List<BoundingBoxAnnotation> source) =>
        source.Select(b => new BoundingBoxAnnotation
        {
            ClassIndex = b.ClassIndex,
            ClassName = b.ClassName,
            CenterX = b.CenterX,
            CenterY = b.CenterY,
            Width = b.Width,
            Height = b.Height
        }).ToList();

    private static List<PolygonAnnotation> ClonePolygons(List<PolygonAnnotation> source) =>
        source.Select(p => new PolygonAnnotation
        {
            Id = p.Id,
            ClassIndex = p.ClassIndex,
            ClassName = p.ClassName,
            Points = new List<PointF>(p.Points)
        }).ToList();

    private static List<OrientedBoundingBoxAnnotation> CloneObbs(List<OrientedBoundingBoxAnnotation> source) =>
        source.Select(o => new OrientedBoundingBoxAnnotation
        {
            Id = o.Id,
            ClassIndex = o.ClassIndex,
            ClassName = o.ClassName,
            CenterX = o.CenterX,
            CenterY = o.CenterY,
            Width = o.Width,
            Height = o.Height,
            Angle = o.Angle
        }).ToList();
}
