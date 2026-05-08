using System.Globalization;
using System.Windows;
using FluentAssertions;
using ScaffoldX.App.Converters;
using Xunit;

namespace ScaffoldX.App.Tests.Converters;

/// <summary>
/// Unit tests for <see cref="StringToVisibilityConverter"/>,
/// <see cref="InverseStringToVisibilityConverter"/>, and
/// <see cref="InverseBoolToVisibilityConverter"/>.
/// </summary>
public class StringToVisibilityConverterTests
{
    private readonly StringToVisibilityConverter _converter = new();
    private readonly InverseStringToVisibilityConverter _inverseConverter = new();
    private readonly InverseBoolToVisibilityConverter _inverseBoolConverter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    // ── StringToVisibilityConverter ─────────────────────────────────────────

    [Fact]
    public void Convert_NonEmptyString_ReturnsVisible()
    {
        // Arrange & Act
        var result = _converter.Convert("hello", typeof(Visibility), null!, _culture);

        // Assert
        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void Convert_EmptyString_ReturnsCollapsed()
    {
        var result = _converter.Convert("", typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NullValue_ReturnsCollapsed()
    {
        var result = _converter.Convert(null!, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_NonStringValue_ReturnsCollapsed()
    {
        var result = _converter.Convert(123, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void Convert_WhitespaceString_ReturnsVisible()
    {
        // Whitespace is not empty, so should be Visible
        var result = _converter.Convert("  ", typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        var act = () => _converter.ConvertBack(Visibility.Visible, typeof(string), null!, _culture);

        act.Should().Throw<NotSupportedException>();
    }

    // ── InverseStringToVisibilityConverter ──────────────────────────────────

    [Fact]
    public void InverseConvert_NonEmptyString_ReturnsCollapsed()
    {
        var result = _inverseConverter.Convert("hello", typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void InverseConvert_EmptyString_ReturnsVisible()
    {
        var result = _inverseConverter.Convert("", typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void InverseConvert_NullValue_ReturnsVisible()
    {
        var result = _inverseConverter.Convert(null!, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void InverseConvertBack_ThrowsNotSupportedException()
    {
        var act = () => _inverseConverter.ConvertBack(Visibility.Visible, typeof(string), null!, _culture);

        act.Should().Throw<NotSupportedException>();
    }

    // ── InverseBoolToVisibilityConverter ────────────────────────────────────

    [Fact]
    public void InverseBool_Convert_True_ReturnsCollapsed()
    {
        var result = _inverseBoolConverter.Convert(true, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void InverseBool_Convert_False_ReturnsVisible()
    {
        var result = _inverseBoolConverter.Convert(false, typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void InverseBool_Convert_NonBoolValue_ReturnsVisible()
    {
        var result = _inverseBoolConverter.Convert("notbool", typeof(Visibility), null!, _culture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void InverseBool_ConvertBack_ThrowsNotSupportedException()
    {
        var act = () => _inverseBoolConverter.ConvertBack(Visibility.Visible, typeof(bool), null!, _culture);

        act.Should().Throw<NotSupportedException>();
    }
}
