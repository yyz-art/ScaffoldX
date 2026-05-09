using System.Drawing;

namespace ScaffoldX.Plugin.Annotation.Models;

public class AnnotationData
{
    public string ImagePath { get; set; } = string.Empty;
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public List<BoundingBoxAnnotation> Boxes { get; set; } = [];
    public List<PolygonAnnotation> Polygons { get; set; } = [];
    public List<OrientedBoundingBoxAnnotation> OrientedBoxes { get; set; } = [];
    public List<PolylineAnnotation> Polylines { get; set; } = [];
    public List<CircleAnnotation> Circles { get; set; } = [];
    public List<SegmentationAnnotation> Segmentations { get; set; } = [];
}

public class BoundingBoxAnnotation
{
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class PolygonAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public List<PointF> Points { get; set; } = [];
}

public class OrientedBoundingBoxAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Angle { get; set; }
}

public class PolylineAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public List<PointF> Points { get; set; } = [];
    public bool IsClosed { get; set; }
}

public class CircleAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public float CenterX { get; set; }
    public float CenterY { get; set; }
    public float Radius { get; set; }
}

public class AnnotationClass
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#FF0000";
}

public class AnnotationProject
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectDirectory { get; set; } = string.Empty;
    public List<AnnotationClass> Classes { get; set; } = [];
    public List<AnnotationData> Annotations { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
}

public class SegmentationAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public List<PointF> Polygon { get; set; } = [];
    public byte[,]? Mask { get; set; }
}

public enum AutoLabelingMode
{
    Detection,
    Segmentation,
    Classification
}
