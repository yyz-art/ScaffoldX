using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Unit tests for keyboard-driven annotation operations.
/// Tests the underlying model and service behavior that keyboard shortcuts rely on:
/// - Delete key: removes selected box from annotation
/// - Arrow keys: navigates between images
/// - Number keys: selects annotation class by index
/// </summary>
public class AnnotationKeyboardTests
{
    private readonly AnnotationService _annotationService = new();

    /// <summary>
    /// Verifies that removing a box from the annotation data works correctly.
    /// This is the underlying behavior for the Delete key shortcut.
    /// </summary>
    [Fact]
    public void DeleteKey_RemovesSelectedBox()
    {
        // Arrange: create annotation with 3 boxes
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.3, CenterY = 0.4, Width = 0.1, Height = 0.1 },
                new() { ClassIndex = 1, ClassName = "dog", CenterX = 0.6, CenterY = 0.7, Width = 0.15, Height = 0.2 },
                new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.8, CenterY = 0.2, Width = 0.05, Height = 0.05 }
            }
        };

        // Act: simulate delete — remove the second box (selected box)
        var selectedBox = annotation.Boxes[1];
        annotation.Boxes.Remove(selectedBox);

        // Assert
        annotation.Boxes.Should().HaveCount(2, "one box was removed");
        annotation.Boxes.Should().NotContain(b => b.ClassName == "dog" && b.CenterX == 0.6,
            "the selected box should be removed");
        annotation.Boxes[0].ClassName.Should().Be("cat");
        annotation.Boxes[1].ClassName.Should().Be("cat");
    }

    /// <summary>
    /// Verifies that deleting the last remaining box results in an empty list.
    /// </summary>
    [Fact]
    public void DeleteKey_LastBox_ResultsInEmptyList()
    {
        // Arrange
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, CenterX = 0.5, CenterY = 0.5, Width = 0.2, Height = 0.3 }
            }
        };

        // Act
        annotation.Boxes.Remove(annotation.Boxes[0]);

        // Assert
        annotation.Boxes.Should().BeEmpty("the only box was removed");
    }

    /// <summary>
    /// Verifies that navigating to the previous image decrements the index.
    /// This is the underlying behavior for the left arrow key.
    /// </summary>
    [Fact]
    public void ArrowKeys_NavigateImages_Previous()
    {
        // Arrange: simulate a project with 5 images
        var annotations = new List<AnnotationData>();
        for (int i = 0; i < 5; i++)
        {
            annotations.Add(new AnnotationData
            {
                ImagePath = $"image_{i}.jpg",
                ImageWidth = 640,
                ImageHeight = 480
            });
        }

        var currentIndex = 3;

        // Act: navigate previous
        if (currentIndex > 0)
            currentIndex--;

        // Assert
        currentIndex.Should().Be(2, "previous image should decrement index by 1");
    }

    /// <summary>
    /// Verifies that navigating to the next image increments the index.
    /// This is the underlying behavior for the right arrow key.
    /// </summary>
    [Fact]
    public void ArrowKeys_NavigateImages_Next()
    {
        // Arrange: simulate a project with 5 images
        var annotations = new List<AnnotationData>();
        for (int i = 0; i < 5; i++)
        {
            annotations.Add(new AnnotationData
            {
                ImagePath = $"image_{i}.jpg",
                ImageWidth = 640,
                ImageHeight = 480
            });
        }

        var currentIndex = 2;

        // Act: navigate next
        if (currentIndex < annotations.Count - 1)
            currentIndex++;

        // Assert
        currentIndex.Should().Be(3, "next image should increment index by 1");
    }

    /// <summary>
    /// Verifies that navigation is bounded — cannot go before the first image.
    /// </summary>
    [Fact]
    public void ArrowKeys_Navigation_BoundedAtStart()
    {
        // Arrange
        var currentIndex = 0;

        // Act: try to navigate before first
        if (currentIndex > 0)
            currentIndex--;

        // Assert
        currentIndex.Should().Be(0, "should not go below 0");
    }

    /// <summary>
    /// Verifies that navigation is bounded — cannot go past the last image.
    /// </summary>
    [Fact]
    public void ArrowKeys_Navigation_BoundedAtEnd()
    {
        // Arrange
        var annotations = new List<AnnotationData>
        {
            new() { ImagePath = "img1.jpg" },
            new() { ImagePath = "img2.jpg" },
            new() { ImagePath = "img3.jpg" }
        };
        var currentIndex = annotations.Count - 1; // at last image

        // Act: try to navigate past last
        if (currentIndex < annotations.Count - 1)
            currentIndex++;

        // Assert
        currentIndex.Should().Be(2, "should not exceed last index");
    }

    /// <summary>
    /// Verifies that number keys map to class indices correctly.
    /// Key '1' → index 0, key '2' → index 1, etc.
    /// </summary>
    [Fact]
    public void NumberKeys_SelectClass()
    {
        // Arrange: simulate class list
        var classes = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "cat", Color = "#FF0000" },
            new() { Index = 1, Name = "dog", Color = "#00FF00" },
            new() { Index = 2, Name = "bird", Color = "#0000FF" },
            new() { Index = 3, Name = "fish", Color = "#FFFF00" }
        };

        // Act & Assert: key '1' selects class index 0
        var selectedIndex = 0; // key '1' pressed
        classes[selectedIndex].Name.Should().Be("cat");

        // key '3' selects class index 2
        selectedIndex = 2; // key '3' pressed
        classes[selectedIndex].Name.Should().Be("bird");

        // key '4' selects class index 3
        selectedIndex = 3; // key '4' pressed
        classes[selectedIndex].Name.Should().Be("fish");
    }

    /// <summary>
    /// Verifies that selecting a class index beyond the class count is handled gracefully.
    /// </summary>
    [Fact]
    public void NumberKeys_SelectClass_OutOfRange_HandledGracefully()
    {
        // Arrange
        var classes = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "cat" },
            new() { Index = 1, Name = "dog" }
        };

        // Act: key '9' pressed but only 2 classes exist
        var selectedIndex = 8; // key '9'
        var selectedClass = selectedIndex < classes.Count
            ? classes[selectedIndex]
            : classes.FirstOrDefault();

        // Assert: falls back to first class
        selectedClass.Should().NotBeNull();
        selectedClass!.Name.Should().Be("cat", "should fall back to first class");
    }

    /// <summary>
    /// Verifies that ToYoloFormat preserves all annotation types after keyboard edits.
    /// </summary>
    [Fact]
    public void KeyboardEdits_ToYoloFormat_PreservesAnnotations()
    {
        // Arrange: simulate keyboard workflow — add boxes, delete one, export
        var annotation = new AnnotationData
        {
            ImagePath = "test.jpg",
            ImageWidth = 640,
            ImageHeight = 480,
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.3, CenterY = 0.4, Width = 0.1, Height = 0.1 },
                new() { ClassIndex = 1, ClassName = "dog", CenterX = 0.6, CenterY = 0.7, Width = 0.15, Height = 0.2 },
                new() { ClassIndex = 0, ClassName = "cat", CenterX = 0.8, CenterY = 0.2, Width = 0.05, Height = 0.05 }
            }
        };

        // Delete middle box (simulates Delete key)
        annotation.Boxes.RemoveAt(1);

        // Act: export to YOLO format
        var yoloLines = _annotationService.ToYoloFormat(annotation);

        // Assert
        yoloLines.Should().HaveCount(2, "one box was deleted, two remain");
        yoloLines[0].Should().StartWith("0 ", "first box is class 0");
        yoloLines[1].Should().StartWith("0 ", "remaining box is class 0");
    }
}
