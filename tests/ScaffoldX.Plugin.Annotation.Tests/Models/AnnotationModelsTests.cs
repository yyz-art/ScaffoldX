using ScaffoldX.Plugin.Annotation.Models;
using Xunit;

namespace ScaffoldX.Plugin.Annotation.Tests.Models;

public class AnnotationModelsTests
{
    [Fact]
    public void AnnotationData_Defaults()
    {
        var data = new AnnotationData();
        Assert.Equal(string.Empty, data.ImagePath);
        Assert.Empty(data.Boxes);
        Assert.Empty(data.Polygons);
        Assert.Empty(data.OrientedBoxes);
        Assert.Empty(data.Polylines);
        Assert.Empty(data.Circles);
        Assert.Empty(data.Segmentations);
    }

    [Fact]
    public void BoundingBoxAnnotation_Properties()
    {
        var box = new BoundingBoxAnnotation
        {
            ClassIndex = 0,
            ClassName = "defect",
            CenterX = 0.5,
            CenterY = 0.5,
            Width = 0.2,
            Height = 0.3
        };
        Assert.Equal("defect", box.ClassName);
        Assert.Equal(0.5, box.CenterX);
    }

    [Fact]
    public void AnnotationProject_Defaults()
    {
        var project = new AnnotationProject();
        Assert.Equal(string.Empty, project.ProjectName);
        Assert.Empty(project.Classes);
        Assert.Empty(project.Annotations);
    }

    [Fact]
    public void AnnotationClass_Defaults()
    {
        var cls = new AnnotationClass();
        Assert.Equal("#FF0000", cls.Color);
    }

    [Fact]
    public void SegmentationAnnotation_Defaults()
    {
        var seg = new SegmentationAnnotation();
        Assert.NotEmpty(seg.Id);
        Assert.Empty(seg.Polygon);
        Assert.Null(seg.Mask);
    }

    [Fact]
    public void PolygonAnnotation_UniqueId()
    {
        var a = new PolygonAnnotation();
        var b = new PolygonAnnotation();
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void AutoLabelingMode_Values()
    {
        Assert.Equal(3, Enum.GetValues<AutoLabelingMode>().Length);
        Assert.True(Enum.IsDefined(AutoLabelingMode.Detection));
        Assert.True(Enum.IsDefined(AutoLabelingMode.Segmentation));
        Assert.True(Enum.IsDefined(AutoLabelingMode.Classification));
    }
}
