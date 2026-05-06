using System.Drawing;
using System.Text.Json.Serialization;

namespace ScaffoldX.App.Models;

/// <summary>
/// YOLO 标注数据模型，包含图像路径和边界框标注列表。
/// </summary>
public class AnnotationData
{
    /// <summary>图像文件的绝对路径。</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>图像宽度（像素）。</summary>
    public int ImageWidth { get; set; }

    /// <summary>图像高度（像素）。</summary>
    public int ImageHeight { get; set; }

    /// <summary>该图像的所有边界框标注。</summary>
    public List<BoundingBoxAnnotation> Boxes { get; set; } = new();

    /// <summary>该图像的所有多边形标注。</summary>
    public List<PolygonAnnotation> Polygons { get; set; } = new();

    /// <summary>该图像的所有旋转边界框（OBB）标注。</summary>
    public List<OrientedBoundingBoxAnnotation> OrientedBoxes { get; set; } = new();

    /// <summary>该图像的所有折线标注。</summary>
    public List<PolylineAnnotation> Polylines { get; set; } = new();

    /// <summary>该图像的所有圆形标注。</summary>
    public List<CircleAnnotation> Circles { get; set; } = new();

    /// <summary>该图像的所有分割标注。</summary>
    public List<SegmentationAnnotation> Segmentations { get; set; } = new();
}

/// <summary>
/// 单个边界框标注，包含类别和归一化坐标。
/// </summary>
public class BoundingBoxAnnotation
{
    /// <summary>类别索引（从 0 开始）。</summary>
    public int ClassIndex { get; set; }

    /// <summary>类别名称。</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>边界框中心 X 坐标（归一化到 0-1）。</summary>
    public double CenterX { get; set; }

    /// <summary>边界框中心 Y 坐标（归一化到 0-1）。</summary>
    public double CenterY { get; set; }

    /// <summary>边界框宽度（归一化到 0-1）。</summary>
    public double Width { get; set; }

    /// <summary>边界框高度（归一化到 0-1）。</summary>
    public double Height { get; set; }
}

/// <summary>
/// 单个多边形标注，包含类别和归一化的多边形顶点坐标。
/// </summary>
public class PolygonAnnotation
{
    /// <summary>唯一标识符。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>类别索引（从 0 开始）。</summary>
    public int ClassIndex { get; set; }

    /// <summary>类别名称。</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>多边形顶点列表（归一化到 0-1 的坐标）。</summary>
    public List<PointF> Points { get; set; } = new();
}

/// <summary>
/// 单个旋转边界框（Oriented Bounding Box）标注，包含类别、归一化中心坐标、宽高和旋转角度。
/// </summary>
public class OrientedBoundingBoxAnnotation
{
    /// <summary>唯一标识符。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>类别索引（从 0 开始）。</summary>
    public int ClassIndex { get; set; }

    /// <summary>类别名称。</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>边界框中心 X 坐标（归一化到 0-1）。</summary>
    public float CenterX { get; set; }

    /// <summary>边界框中心 Y 坐标（归一化到 0-1）。</summary>
    public float CenterY { get; set; }

    /// <summary>边界框宽度（归一化到 0-1）。</summary>
    public float Width { get; set; }

    /// <summary>边界框高度（归一化到 0-1）。</summary>
    public float Height { get; set; }

    /// <summary>旋转角度（弧度，从水平方向顺时针）。</summary>
    public float Angle { get; set; }
}

/// <summary>
/// 单个折线标注，包含类别、归一化的折线顶点坐标和闭合标志。
/// </summary>
public class PolylineAnnotation
{
    /// <summary>唯一标识符。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>类别索引（从 0 开始）。</summary>
    public int ClassIndex { get; set; }

    /// <summary>类别名称。</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>折线顶点列表（归一化到 0-1 的坐标）。</summary>
    public List<PointF> Points { get; set; } = new();

    /// <summary>是否闭合（false 为折线，true 为类多边形闭合形状）。</summary>
    public bool IsClosed { get; set; }
}

/// <summary>
/// 单个圆形标注，包含类别、归一化的圆心坐标和半径。
/// </summary>
public class CircleAnnotation
{
    /// <summary>唯一标识符。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>类别索引（从 0 开始）。</summary>
    public int ClassIndex { get; set; }

    /// <summary>类别名称。</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>圆心 X 坐标（归一化到 0-1）。</summary>
    public float CenterX { get; set; }

    /// <summary>圆心 Y 坐标（归一化到 0-1）。</summary>
    public float CenterY { get; set; }

    /// <summary>半径（归一化到 0-1，相对于图像宽度）。</summary>
    public float Radius { get; set; }
}

/// <summary>
/// 标注类别定义。
/// </summary>
public class AnnotationClass
{
    /// <summary>类别索引（从 0 开始）。</summary>
    public int Index { get; set; }

    /// <summary>类别名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>类别颜色（十六进制，如 #FF0000）。</summary>
    public string Color { get; set; } = "#FF0000";
}

/// <summary>
/// 标注项目配置，包含类别定义和项目元数据。
/// </summary>
public class AnnotationProject
{
    /// <summary>项目名称。</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>项目根目录（包含图像文件夹）。</summary>
    public string ProjectDirectory { get; set; } = string.Empty;

    /// <summary>类别定义列表。</summary>
    public List<AnnotationClass> Classes { get; set; } = new();

    /// <summary>已标注的图像数据。</summary>
    public List<AnnotationData> Annotations { get; set; } = new();

    /// <summary>项目创建时间。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最后修改时间。</summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// YOLO 训练配置。
/// </summary>
public class YoloTrainingConfig
{
    /// <summary>训练数据集路径。</summary>
    public string DatasetPath { get; set; } = string.Empty;

    /// <summary>输出模型路径。</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>预训练模型路径（如 yolov8n.pt）。</summary>
    public string PretrainedModel { get; set; } = "yolov8n.pt";

    /// <summary>训练轮数。</summary>
    public int Epochs { get; set; } = 100;

    /// <summary>批次大小。</summary>
    public int BatchSize { get; set; } = 16;

    /// <summary>图像尺寸。</summary>
    public int ImageSize { get; set; } = 640;

    /// <summary>学习率。</summary>
    public double LearningRate { get; set; } = 0.01;

    /// <summary>类别数量。</summary>
    public int NumClasses { get; set; } = 1;

    /// <summary>类别名称列表。</summary>
    public List<string> ClassNames { get; set; } = new();

    /// <summary>是否使用 GPU。</summary>
    public bool UseGpu { get; set; } = true;

    /// <summary>工作线程数。</summary>
    public int Workers { get; set; } = 8;
}

/// <summary>
/// 训练进度信息。
/// </summary>
public class TrainingProgress
{
    /// <summary>当前轮次。</summary>
    public int CurrentEpoch { get; set; }

    /// <summary>总轮次。</summary>
    public int TotalEpochs { get; set; }

    /// <summary>当前损失值。</summary>
    public double Loss { get; set; }

    /// <summary>当前 mAP@0.5。</summary>
    public double Map50 { get; set; }

    /// <summary>当前 mAP@0.5:0.95。</summary>
    public double Map50_95 { get; set; }

    /// <summary>学习率。</summary>
    public double LearningRate { get; set; }

    /// <summary>已用时间。</summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>预计剩余时间。</summary>
    public TimeSpan EstimatedRemaining { get; set; }

    /// <summary>进度百分比（0-100）。</summary>
    public double ProgressPercent => TotalEpochs > 0 ? (double)CurrentEpoch / TotalEpochs * 100 : 0;

    /// <summary>状态消息。</summary>
    public string StatusMessage { get; set; } = string.Empty;
}

/// <summary>
/// 训练结果。
/// </summary>
public class TrainingResult
{
    /// <summary>是否成功。</summary>
    public bool Success { get; set; }

    /// <summary>输出模型路径。</summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>最终 mAP@0.5。</summary>
    public double FinalMap50 { get; set; }

    /// <summary>最终 mAP@0.5:0.95。</summary>
    public double FinalMap50_95 { get; set; }

    /// <summary>总训练时间。</summary>
    public TimeSpan TotalTime { get; set; }

    /// <summary>错误信息（如果失败）。</summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// 分割标注，包含掩码轮廓多边形和可选的原始掩码数据。
/// </summary>
public class SegmentationAnnotation
{
    /// <summary>唯一标识符。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>类别索引（从 0 开始）。</summary>
    public int ClassIndex { get; set; }

    /// <summary>类别名称。</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>置信度（0-1，自动标注时产生）。</summary>
    public float Confidence { get; set; }

    /// <summary>掩码轮廓多边形顶点列表（归一化到 0-1 的坐标）。</summary>
    public List<PointF> Polygon { get; set; } = new();

    /// <summary>原始分割掩码（与原图同尺寸的二值矩阵），可选。</summary>
    public byte[,]? Mask { get; set; }
}

/// <summary>
/// SAM3 点提示，用于交互式分割。
/// </summary>
public class Sam3Point
{
    /// <summary>X 坐标（归一化到 0-1）。</summary>
    public float X { get; set; }

    /// <summary>Y 坐标（归一化到 0-1）。</summary>
    public float Y { get; set; }

    /// <summary>标签：1 = 前景（包含），0 = 背景（排除）。</summary>
    public int Label { get; set; } = 1;
}

/// <summary>
/// SAM3 提示模式。
/// </summary>
public enum Sam3PromptMode
{
    /// <summary>点提示模式。</summary>
    Point,

    /// <summary>框提示模式。</summary>
    Box,

    /// <summary>点 + 框混合提示模式。</summary>
    PointAndBox,

    /// <summary>自动分割（无提示，全图分割）。</summary>
    Everything
}

/// <summary>
/// 自动标注模式。
/// </summary>
public enum AutoLabelingMode
{
    /// <summary>目标检测（YOLO 边界框）。</summary>
    Detection,

    /// <summary>实例分割（SAM3 掩码 + 多边形）。</summary>
    Segmentation,

    /// <summary>图像分类。</summary>
    Classification
}

/// <summary>
/// SAM3 模型元数据。
/// </summary>
public class Sam3ModelInfo
{
    /// <summary>模型文件目录路径。</summary>
    public string ModelDirectory { get; set; } = string.Empty;

    /// <summary>模型显示名称。</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>图像编码器模型文件名。</summary>
    public string ImageEncoderPath { get; set; } = string.Empty;

    /// <summary>提示编码器模型文件名。</summary>
    public string PromptEncoderPath { get; set; } = string.Empty;

    /// <summary>掩码解码器模型文件名。</summary>
    public string MaskDecoderPath { get; set; } = string.Empty;

    /// <summary>模型输入图像尺寸（正方形边长）。</summary>
    public int InputSize { get; set; } = 1024;

    /// <summary>是否已验证模型文件完整性。</summary>
    public bool IsValidated { get; set; }
}
