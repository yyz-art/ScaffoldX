using System.Drawing;
using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using ScaffoldX.App.ViewModels;
using Xunit;

namespace ScaffoldX.App.Tests.ViewModels;

/// <summary>
/// Tests for the sub-ViewModels extracted from AnnotationViewModel:
/// ImageStateVM, AnnotationStateVM, ClassStateVM, and ProjectCommandHandler.
/// </summary>
public class SubViewModelTests
{
    // ── ImageStateVM ────────────────────────────────────────────────────────

    [Fact]
    public void ImageStateVM_ZoomLevel_DefaultsToOne()
    {
        var vm = new ImageStateVM();

        vm.ZoomLevel.Should().Be(1.0, "default zoom should be 1.0 (100%)");
    }

    [Fact]
    public void ImageStateVM_ZoomLevelText_AtDefault_Shows100Percent()
    {
        var vm = new ImageStateVM();

        vm.ZoomLevelText.Should().Be("100%");
    }

    [Fact]
    public void ImageStateVM_ZoomLevel_ClampsAtMinimum()
    {
        var vm = new ImageStateVM();

        vm.ZoomLevel = 0.01;

        vm.ZoomLevel.Should().BeGreaterThanOrEqualTo(0.1, "zoom should not go below 0.1");
    }

    [Fact]
    public void ImageStateVM_ZoomLevel_ClampsAtMaximum()
    {
        var vm = new ImageStateVM();

        vm.ZoomLevel = 50.0;

        vm.ZoomLevel.Should().BeLessThanOrEqualTo(10.0, "zoom should not exceed 10.0");
    }

    [Fact]
    public void ImageStateVM_ResetZoom_ReturnsToOne()
    {
        var vm = new ImageStateVM();
        vm.ZoomLevel = 3.5;

        vm.ResetZoom();

        vm.ZoomLevel.Should().Be(1.0);
        vm.ZoomLevelText.Should().Be("100%");
    }

    [Fact]
    public void ImageStateVM_ZoomLevelText_UpdatesWithZoom()
    {
        var vm = new ImageStateVM();

        vm.ZoomLevel = 2.5;

        vm.ZoomLevelText.Should().Be("250%");
    }

    // ── ClassStateVM ────────────────────────────────────────────────────────

    [Fact]
    public void ClassStateVM_Classes_InitiallyEmpty()
    {
        var vm = new ClassStateVM();

        vm.Classes.Should().BeEmpty("no classes loaded initially");
    }

    [Fact]
    public void ClassStateVM_UpdateClassesList_WithProject_PopulatesClasses()
    {
        var vm = new ClassStateVM();
        var project = new AnnotationProject
        {
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "cat", Color = "#FF0000" },
                new() { Index = 1, Name = "dog", Color = "#00FF00" }
            }
        };

        vm.UpdateClassesList(project);

        vm.Classes.Should().HaveCount(2);
        vm.Classes[0].Name.Should().Be("cat");
        vm.Classes[1].Name.Should().Be("dog");
    }

    [Fact]
    public void ClassStateVM_UpdateClassesList_WithNull_ClearsClasses()
    {
        var vm = new ClassStateVM();
        var project = new AnnotationProject
        {
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "cat", Color = "#FF0000" }
            }
        };
        vm.UpdateClassesList(project);
        vm.Classes.Should().HaveCount(1);

        vm.UpdateClassesList(null);

        vm.Classes.Should().BeEmpty("null project should clear classes");
    }

    [Fact]
    public void ClassStateVM_UpdateClassesList_CalledTwice_ReplacesClasses()
    {
        var vm = new ClassStateVM();
        var project1 = new AnnotationProject
        {
            Classes = new List<AnnotationClass> { new() { Index = 0, Name = "old" } }
        };
        var project2 = new AnnotationProject
        {
            Classes = new List<AnnotationClass>
            {
                new() { Index = 0, Name = "new1" },
                new() { Index = 1, Name = "new2" }
            }
        };

        vm.UpdateClassesList(project1);
        vm.UpdateClassesList(project2);

        vm.Classes.Should().HaveCount(2);
        vm.Classes[0].Name.Should().Be("new1");
    }

    // ── AnnotationStateVM ───────────────────────────────────────────────────

    [Fact]
    public void AnnotationStateVM_Project_InitiallyNull()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());

        vm.Project.Should().BeNull();
    }

    [Fact]
    public void AnnotationStateVM_StatusMessage_DefaultsToReady()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());

        vm.StatusMessage.Should().Be("就绪");
    }

    [Fact]
    public void AnnotationStateVM_CurrentAnnotation_InitiallyNull()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());

        vm.CurrentAnnotation.Should().BeNull();
        vm.HasBoxes.Should().BeFalse();
    }

    [Fact]
    public void AnnotationStateVM_SetCurrentAnnotation_RaisesHasBoxesChanged()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => { if (e.PropertyName != null) changedProperties.Add(e.PropertyName); };

        vm.CurrentAnnotation = new AnnotationData
        {
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassName = "cat" }
            }
        };

        changedProperties.Should().Contain("HasBoxes");
        changedProperties.Should().Contain("CurrentBoxes");
    }

    [Fact]
    public void AnnotationStateVM_UpdateBoxesList_WithAnnotations_PopulatesCollections()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.CurrentAnnotation = new AnnotationData
        {
            Boxes = new List<BoundingBoxAnnotation>
            {
                new() { ClassName = "cat", ClassIndex = 0 },
                new() { ClassName = "dog", ClassIndex = 1 }
            },
            Polygons = new List<PolygonAnnotation>
            {
                new() { ClassName = "defect", Points = new List<PointF> { new(0.1f, 0.1f) } }
            },
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation>()
        };

        vm.UpdateBoxesList();

        vm.CurrentBoxes.Should().HaveCount(2, "two bounding boxes");
        vm.AllAnnotations.Should().HaveCount(3, "two boxes + one polygon");
        vm.CurrentBoxCount.Should().Be(2);
        vm.HasBoxes.Should().BeTrue();
    }

    [Fact]
    public void AnnotationStateVM_UpdateBoxesList_WithNullAnnotation_ClearsAll()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.CurrentAnnotation = null;

        vm.UpdateBoxesList();

        vm.CurrentBoxes.Should().BeEmpty();
        vm.AllAnnotations.Should().BeEmpty();
        vm.CurrentBoxCount.Should().Be(0);
        vm.HasBoxes.Should().BeFalse();
    }

    [Fact]
    public void AnnotationStateVM_UpdateStatistics_ComputesCorrectCounts()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.Project = new AnnotationProject
        {
            ProjectName = "TestProject",
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    Boxes = new List<BoundingBoxAnnotation> { new() { ClassName = "cat" } },
                    Polygons = new List<PolygonAnnotation>()
                },
                new()
                {
                    Boxes = new List<BoundingBoxAnnotation>(),
                    Polygons = new List<PolygonAnnotation>()
                },
                new()
                {
                    Boxes = new List<BoundingBoxAnnotation>(),
                    Polygons = new List<PolygonAnnotation> { new() { ClassName = "defect", Points = new List<PointF>() } }
                }
            }
        };

        vm.UpdateStatistics();

        vm.TotalImages.Should().Be(3);
        vm.AnnotatedImages.Should().Be(2, "img1 has box, img3 has polygon");
        vm.AnnotatedImageCount.Should().Be(2);
        vm.ProjectName.Should().Be("TestProject");
    }

    [Fact]
    public void AnnotationStateVM_UpdateAnnotationStatistics_CountsAllTypes()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.CurrentAnnotation = new AnnotationData
        {
            Boxes = new List<BoundingBoxAnnotation> { new(), new() },
            Polygons = new List<PolygonAnnotation> { new() },
            OrientedBoxes = new List<OrientedBoundingBoxAnnotation> { new() },
            Polylines = new List<PolylineAnnotation>(),
            Circles = new List<CircleAnnotation>(),
            Segmentations = new List<SegmentationAnnotation> { new() }
        };

        vm.UpdateAnnotationStatistics();

        vm.TotalBoxCount.Should().Be(2);
        vm.TotalPolygonCount.Should().Be(1);
        vm.TotalObbCount.Should().Be(1);
        vm.TotalPolylineCount.Should().Be(0);
        vm.TotalCircleCount.Should().Be(0);
        vm.TotalAnnotationCount.Should().Be(5, "2 boxes + 1 polygon + 1 OBB + 1 segmentation");
    }

    [Fact]
    public void AnnotationStateVM_UpdateAnnotationStatistics_WithNull_ClearsAll()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.CurrentAnnotation = null;

        vm.UpdateAnnotationStatistics();

        vm.TotalBoxCount.Should().Be(0);
        vm.TotalPolygonCount.Should().Be(0);
        vm.TotalObbCount.Should().Be(0);
        vm.TotalAnnotationCount.Should().Be(0);
    }

    [Fact]
    public void AnnotationStateVM_UpdateClassDistribution_ComputesDistribution()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.Project = new AnnotationProject
        {
            Annotations = new List<AnnotationData>
            {
                new()
                {
                    Boxes = new List<BoundingBoxAnnotation>
                    {
                        new() { ClassName = "cat" },
                        new() { ClassName = "cat" },
                        new() { ClassName = "dog" }
                    }
                }
            }
        };

        vm.UpdateClassDistribution();

        vm.ClassDistributionText.Should().Contain("cat: 2");
        vm.ClassDistributionText.Should().Contain("dog: 1");
    }

    [Fact]
    public void AnnotationStateVM_UpdateClassDistribution_WithNullProject_ShowsEmpty()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.Project = null;

        vm.UpdateClassDistribution();

        vm.ClassDistributionText.Should().BeEmpty();
    }

    [Fact]
    public void AnnotationStateVM_AnnotationProgressText_WithProject_ShowsProgress()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());
        vm.Project = new AnnotationProject
        {
            Annotations = new List<AnnotationData>
            {
                new() { Boxes = new List<BoundingBoxAnnotation> { new() } },
                new() { Boxes = new List<BoundingBoxAnnotation>() }
            }
        };
        vm.UpdateStatistics();

        vm.AnnotationProgressText.Should().Contain("已标注: 1 / 2");
    }

    [Fact]
    public void AnnotationStateVM_AnnotationProgressText_WithNullProject_ShowsEmpty()
    {
        var vm = new AnnotationStateVM(new DrawingStateManager());

        vm.AnnotationProgressText.Should().BeEmpty();
    }

    // ── AnnotationViewModel facade ──────────────────────────────────────────

    [Fact]
    public void AnnotationViewModel_ExposesSubViewModels()
    {
        var mockAnnotationService = new Mock<IAnnotationService>();
        var mockAutoLabelingService = new Mock<IAutoLabelingService>();
        var mockVideoFrameService = new Mock<IVideoFrameService>();
        var mockDialogService = new Mock<IDialogService>();

        var vm = new AnnotationViewModel(
            mockAnnotationService.Object,
            mockAutoLabelingService.Object,
            mockVideoFrameService.Object,
            mockDialogService.Object);

        vm.ImageState.Should().NotBeNull("ImageState sub-VM should be exposed");
        vm.AnnotationState.Should().NotBeNull("AnnotationState sub-VM should be exposed");
        vm.ClassState.Should().NotBeNull("ClassState sub-VM should be exposed");
    }

    [Fact]
    public void AnnotationViewModel_ForwardingProperties_DelegateToSubVMs()
    {
        var mockAnnotationService = new Mock<IAnnotationService>();
        var mockAutoLabelingService = new Mock<IAutoLabelingService>();
        var mockVideoFrameService = new Mock<IVideoFrameService>();
        var mockDialogService = new Mock<IDialogService>();

        var vm = new AnnotationViewModel(
            mockAnnotationService.Object,
            mockAutoLabelingService.Object,
            mockVideoFrameService.Object,
            mockDialogService.Object);

        // ZoomLevel delegates to ImageStateVM
        vm.ZoomLevel.Should().Be(vm.ImageState.ZoomLevel);
        vm.ZoomLevelText.Should().Be(vm.ImageState.ZoomLevelText);

        // StatusMessage delegates to AnnotationStateVM
        vm.StatusMessage.Should().Be(vm.AnnotationState.StatusMessage);

        // Classes delegates to ClassStateVM
        vm.Classes.Should().BeSameAs(vm.ClassState.Classes);

        // CurrentBoxes delegates to AnnotationStateVM
        vm.CurrentBoxes.Should().BeSameAs(vm.AnnotationState.CurrentBoxes);
    }

    [Fact]
    public void AnnotationViewModel_Commands_AreNotNull()
    {
        var mockAnnotationService = new Mock<IAnnotationService>();
        var mockAutoLabelingService = new Mock<IAutoLabelingService>();
        var mockVideoFrameService = new Mock<IVideoFrameService>();
        var mockDialogService = new Mock<IDialogService>();

        var vm = new AnnotationViewModel(
            mockAnnotationService.Object,
            mockAutoLabelingService.Object,
            mockVideoFrameService.Object,
            mockDialogService.Object);

        // ProjectCommandHandler commands
        vm.NewProjectCommand.Should().NotBeNull();
        vm.OpenProjectCommand.Should().NotBeNull();
        vm.SaveProjectCommand.Should().NotBeNull();
        vm.AddImagesCommand.Should().NotBeNull();
        vm.AddFolderCommand.Should().NotBeNull();
        vm.DeleteSelectedBoxCommand.Should().NotBeNull();
        vm.ClearAllBoxesCommand.Should().NotBeNull();
        vm.ImageMouseDownCommand.Should().NotBeNull();
        vm.ImageMouseMoveCommand.Should().NotBeNull();
        vm.ImageMouseUpCommand.Should().NotBeNull();
        vm.SwitchToBboxModeCommand.Should().NotBeNull();
        vm.SwitchToPolygonModeCommand.Should().NotBeNull();
        vm.SwitchToObbModeCommand.Should().NotBeNull();
        vm.CancelDrawingCommand.Should().NotBeNull();
        vm.LoadSam3ModelCommand.Should().NotBeNull();
        vm.ResetZoomCommand.Should().NotBeNull();

        // Handler commands
        vm.PreviousImageCommand.Should().NotBeNull();
        vm.NextImageCommand.Should().NotBeNull();
        vm.AddClassCommand.Should().NotBeNull();
        vm.RemoveClassCommand.Should().NotBeNull();
        vm.UndoCommand.Should().NotBeNull();
        vm.RedoCommand.Should().NotBeNull();
        vm.ExportYoloCommand.Should().NotBeNull();
    }

    [Fact]
    public void AnnotationViewModel_PublicInterface_IsBackwardCompatible()
    {
        var mockAnnotationService = new Mock<IAnnotationService>();
        var mockAutoLabelingService = new Mock<IAutoLabelingService>();
        var mockVideoFrameService = new Mock<IVideoFrameService>();
        var mockDialogService = new Mock<IDialogService>();

        var vm = new AnnotationViewModel(
            mockAnnotationService.Object,
            mockAutoLabelingService.Object,
            mockVideoFrameService.Object,
            mockDialogService.Object);

        // All properties from the original public interface should exist
        vm.Project.Should().BeNull();
        vm.CurrentAnnotation.Should().BeNull();
        vm.CurrentImage.Should().BeNull();
        vm.CurrentImageIndex.Should().Be(-1);
        vm.IsDrawing.Should().BeFalse();
        vm.SelectedBox.Should().BeNull();
        vm.StatusMessage.Should().Be("就绪");
        vm.ProjectName.Should().BeEmpty();
        vm.TotalImages.Should().Be(0);
        vm.AnnotatedImages.Should().Be(0);
        vm.CurrentBoxCount.Should().Be(0);
        vm.HasBoxes.Should().BeFalse();
        vm.ZoomLevel.Should().Be(1.0);
        vm.IsModelLoaded.Should().BeFalse();
        vm.IsPolygonMode.Should().BeFalse();
        vm.IsObbMode.Should().BeFalse();
        vm.IsSam3ModelLoaded.Should().BeFalse();
    }
}
