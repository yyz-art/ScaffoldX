using System.Windows;
using System.Windows.Controls;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.Converters;

/// <summary>
/// 根据标注类型（BoundingBoxAnnotation、PolygonAnnotation、OrientedBoundingBoxAnnotation）
/// 选择对应的 DataTemplate，用于在 ListBox 中统一显示不同类型的标注。
/// </summary>
public class AnnotationTemplateSelector : DataTemplateSelector
{
    /// <summary>边界框标注模板。</summary>
    public DataTemplate? BoundingBoxTemplate { get; set; }

    /// <summary>多边形标注模板。</summary>
    public DataTemplate? PolygonTemplate { get; set; }

    /// <summary>旋转边界框（OBB）标注模板。</summary>
    public DataTemplate? OrientedBoundingBoxTemplate { get; set; }

    /// <summary>
    /// 根据标注对象的实际类型返回对应的 DataTemplate。
    /// </summary>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container) => item switch
    {
        BoundingBoxAnnotation => BoundingBoxTemplate,
        PolygonAnnotation => PolygonTemplate,
        OrientedBoundingBoxAnnotation => OrientedBoundingBoxTemplate,
        _ => base.SelectTemplate(item, container)
    };
}
