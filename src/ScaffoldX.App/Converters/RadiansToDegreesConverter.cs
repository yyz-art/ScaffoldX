using System.Globalization;
using System.Windows.Data;
using ScaffoldX.App.Constants;

namespace ScaffoldX.App.Converters;

/// <summary>
/// 将弧度值转换为角度值（度），用于 OBB 标注的角度显示。
/// </summary>
[ValueConversion(typeof(float), typeof(double))]
public class RadiansToDegreesConverter : IValueConverter
{
    /// <summary>将弧度转换为角度（度）。</summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float radians)
            return Math.Round(radians * MathConstants.RadiansToDegrees, 1);
        if (value is double doubleRadians)
            return Math.Round(doubleRadians * MathConstants.RadiansToDegrees, 1);
        return 0.0;
    }

    /// <summary>将角度（度）转换为弧度。</summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double degrees)
            return (float)(degrees * MathConstants.DegreesToRadians);
        return 0f;
    }
}
