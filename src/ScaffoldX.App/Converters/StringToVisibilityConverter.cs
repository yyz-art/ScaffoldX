using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScaffoldX.App.Converters;

/// <summary>
/// 将字符串转换为 Visibility：非空字符串 → Visible，空字符串 → Collapsed。
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 非空字符串返回 Visible，否则返回 Collapsed。
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>不支持反向转换。</summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// 将字符串转换为反向 Visibility：空字符串 → Visible，非空字符串 → Collapsed。
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class InverseStringToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 空字符串返回 Visible，非空字符串返回 Collapsed。
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>不支持反向转换。</summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// 将 bool 转换为反向 Visibility：false → Visible，true → Collapsed。
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// false 返回 Visible，true 返回 Collapsed。
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>不支持反向转换。</summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
