using System.Globalization;
using FluentAssertions;
using ScaffoldX.App.Constants;
using ScaffoldX.App.Converters;
using Xunit;

namespace ScaffoldX.App.Tests.Converters;

/// <summary>
/// Unit tests for <see cref="RadiansToDegreesConverter"/>.
/// </summary>
public class RadiansToDegreesConverterTests
{
    private readonly RadiansToDegreesConverter _converter = new();
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    // ── Convert (radians → degrees) ─────────────────────────────────────────

    [Fact]
    public void Convert_FloatZero_ReturnsZero()
    {
        var result = _converter.Convert(0f, typeof(double), null!, _culture);

        result.Should().Be(0.0);
    }

    [Fact]
    public void Convert_FloatPi_Returns180()
    {
        var result = _converter.Convert((float)Math.PI, typeof(double), null!, _culture);

        ((double)result).Should().BeApproximately(180.0, 0.1);
    }

    [Fact]
    public void Convert_DoublePi_Returns180()
    {
        var result = _converter.Convert(Math.PI, typeof(double), null!, _culture);

        ((double)result).Should().BeApproximately(180.0, 0.1);
    }

    [Fact]
    public void Convert_FloatHalfPi_Returns90()
    {
        var result = _converter.Convert((float)(Math.PI / 2), typeof(double), null!, _culture);

        ((double)result).Should().BeApproximately(90.0, 0.1);
    }

    [Fact]
    public void Convert_NonNumericValue_ReturnsZero()
    {
        var result = _converter.Convert("not a number", typeof(double), null!, _culture);

        result.Should().Be(0.0);
    }

    [Fact]
    public void Convert_NullValue_ReturnsZero()
    {
        var result = _converter.Convert(null!, typeof(double), null!, _culture);

        result.Should().Be(0.0);
    }

    [Fact]
    public void Convert_RoundsToOneDecimal()
    {
        // 1 radian ≈ 57.2958 degrees
        var result = _converter.Convert(1f, typeof(double), null!, _culture);

        ((double)result).Should().Be(Math.Round(1.0 * MathConstants.RadiansToDegrees, 1));
    }

    // ── ConvertBack (degrees → radians) ─────────────────────────────────────

    [Fact]
    public void ConvertBack_DoubleZero_ReturnsZeroFloat()
    {
        var result = _converter.ConvertBack(0.0, typeof(float), null!, _culture);

        result.Should().Be(0f);
    }

    [Fact]
    public void ConvertBack_Double180_ReturnsPiAsFloat()
    {
        var result = _converter.ConvertBack(180.0, typeof(float), null!, _culture);

        ((float)result).Should().BeApproximately((float)Math.PI, 0.001f);
    }

    [Fact]
    public void ConvertBack_Double90_ReturnsHalfPiAsFloat()
    {
        var result = _converter.ConvertBack(90.0, typeof(float), null!, _culture);

        ((float)result).Should().BeApproximately((float)(Math.PI / 2), 0.001f);
    }

    [Fact]
    public void ConvertBack_NonDoubleValue_ReturnsZeroFloat()
    {
        var result = _converter.ConvertBack("invalid", typeof(float), null!, _culture);

        result.Should().Be(0f);
    }

    // ── Round-trip ──────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_FloatRadians_ReturnsOriginalValue()
    {
        float original = 1.234f;

        var degrees = _converter.Convert(original, typeof(double), null!, _culture);
        var radians = _converter.ConvertBack(degrees, typeof(float), null!, _culture);

        ((float)radians).Should().BeApproximately(original, 0.01f);
    }
}
