using FluentAssertions;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// CoordinateMapper 工具类单元测试，覆盖坐标变换和常量定义。
/// </summary>
public class CoordinateMapperTests
{
    // ── MinBoxSize ──────────────────────────────────────────────────────────

    [Fact]
    public void MinBoxSize_ShouldBe5()
    {
        CoordinateMapper.MinBoxSize.Should().Be(5f);
    }

    // ── ToAbsolute (pair) ───────────────────────────────────────────────────

    [Fact]
    public void ToAbsolute_Pair_ShouldConvertNormalizedToAbsolute()
    {
        // Arrange & Act
        var (x, y) = CoordinateMapper.ToAbsolute(0.5, 0.25, 640, 480);

        // Assert
        x.Should().Be(320.0);
        y.Should().Be(120.0);
    }

    [Fact]
    public void ToAbsolute_Pair_ShouldReturnZero_ForZeroInput()
    {
        var (x, y) = CoordinateMapper.ToAbsolute(0.0, 0.0, 1920, 1080);

        x.Should().Be(0.0);
        y.Should().Be(0.0);
    }

    [Fact]
    public void ToAbsolute_Pair_ShouldReturnFullSize_ForNormalizedOne()
    {
        var (x, y) = CoordinateMapper.ToAbsolute(1.0, 1.0, 640, 480);

        x.Should().Be(640.0);
        y.Should().Be(480.0);
    }

    // ── ToAbsolute (single, double) ─────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 640, 0.0)]
    [InlineData(0.5, 640, 320.0)]
    [InlineData(1.0, 640, 640.0)]
    [InlineData(0.25, 480, 120.0)]
    public void ToAbsolute_SingleDouble_ShouldConvertCorrectly(double normalized, int dimension, double expected)
    {
        CoordinateMapper.ToAbsolute(normalized, dimension).Should().Be(expected);
    }

    // ── ToAbsolute (single, float) ──────────────────────────────────────────

    [Theory]
    [InlineData(0.0f, 640, 0.0f)]
    [InlineData(0.5f, 640, 320.0f)]
    [InlineData(1.0f, 640, 640.0f)]
    public void ToAbsolute_SingleFloat_ShouldConvertCorrectly(float normalized, int dimension, float expected)
    {
        CoordinateMapper.ToAbsolute(normalized, dimension).Should().Be(expected);
    }

    // ── ToNormalized (pair) ─────────────────────────────────────────────────

    [Fact]
    public void ToNormalized_Pair_ShouldConvertAbsoluteToNormalized()
    {
        var (x, y) = CoordinateMapper.ToNormalized(320.0, 120.0, 640, 480);

        x.Should().Be(0.5);
        y.Should().Be(0.25);
    }

    [Fact]
    public void ToNormalized_Pair_ShouldBeInverseOfToAbsolute()
    {
        // Arrange
        const double normX = 0.375;
        const double normY = 0.625;
        const int width = 1920;
        const int height = 1080;

        // Act
        var (absX, absY) = CoordinateMapper.ToAbsolute(normX, normY, width, height);
        var (backX, backY) = CoordinateMapper.ToNormalized(absX, absY, width, height);

        // Assert
        backX.Should().BeApproximately(normX, 1e-10);
        backY.Should().BeApproximately(normY, 1e-10);
    }

    // ── ToNormalized (single) ───────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 640, 0.0)]
    [InlineData(320.0, 640, 0.5)]
    [InlineData(640.0, 640, 1.0)]
    public void ToNormalized_Single_ShouldConvertCorrectly(double absolute, int dimension, double expected)
    {
        CoordinateMapper.ToNormalized(absolute, dimension).Should().Be(expected);
    }

    // ── ToAbsoluteBbox (double) ─────────────────────────────────────────────

    [Fact]
    public void ToAbsoluteBbox_Double_ShouldConvertCenterSizeToTopLeft()
    {
        // center=(0.5, 0.5), size=(0.4, 0.3), image=640x480
        // Expected: top-left=(0.5*640 - 0.4*640/2, 0.5*480 - 0.3*480/2) = (192, 168), size=(256, 144)
        var (x, y, w, h) = CoordinateMapper.ToAbsoluteBbox(0.5, 0.5, 0.4, 0.3, 640, 480);

        x.Should().Be(192.0);
        y.Should().Be(168.0);
        w.Should().Be(256.0);
        h.Should().Be(144.0);
    }

    [Fact]
    public void ToAbsoluteBbox_Double_ShouldHandleFullImageBox()
    {
        // center=(0.5, 0.5), size=(1.0, 1.0) → entire image
        var (x, y, w, h) = CoordinateMapper.ToAbsoluteBbox(0.5, 0.5, 1.0, 1.0, 640, 480);

        x.Should().Be(0.0);
        y.Should().Be(0.0);
        w.Should().Be(640.0);
        h.Should().Be(480.0);
    }

    [Fact]
    public void ToAbsoluteBbox_Double_ShouldHandleTopLeftCorner()
    {
        // center=(0.25, 0.25), size=(0.5, 0.5), image=100x100
        // Expected: top-left=(25-25, 25-25)=(0, 0), size=(50, 50)
        var (x, y, w, h) = CoordinateMapper.ToAbsoluteBbox(0.25, 0.25, 0.5, 0.5, 100, 100);

        x.Should().Be(0.0);
        y.Should().Be(0.0);
        w.Should().Be(50.0);
        h.Should().Be(50.0);
    }

    // ── ToAbsoluteBbox (float) ──────────────────────────────────────────────

    [Fact]
    public void ToAbsoluteBbox_Float_ShouldConvertCenterSizeToTopLeft()
    {
        var (x, y, w, h) = CoordinateMapper.ToAbsoluteBbox(0.5f, 0.5f, 0.4f, 0.3f, 640, 480);

        x.Should().Be(192.0f);
        y.Should().Be(168.0f);
        w.Should().Be(256.0f);
        h.Should().Be(144.0f);
    }

    // ── Clamp (double) ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(5.0, 0.0, 10.0, 5.0)]     // within range
    [InlineData(-1.0, 0.0, 10.0, 0.0)]    // below min
    [InlineData(15.0, 0.0, 10.0, 10.0)]   // above max
    [InlineData(0.0, 0.0, 10.0, 0.0)]     // at min
    [InlineData(10.0, 0.0, 10.0, 10.0)]   // at max
    public void Clamp_Double_ShouldClampToRange(double value, double min, double max, double expected)
    {
        CoordinateMapper.Clamp(value, min, max).Should().Be(expected);
    }

    // ── Clamp (int) ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(5, 0, 10, 5)]      // within range
    [InlineData(-1, 0, 10, 0)]     // below min
    [InlineData(15, 0, 10, 10)]    // above max
    [InlineData(0, 0, 10, 0)]      // at min
    [InlineData(10, 0, 10, 10)]    // at max
    public void Clamp_Int_ShouldClampToRange(int value, int min, int max, int expected)
    {
        CoordinateMapper.Clamp(value, min, max).Should().Be(expected);
    }

    // ── Round-trip: ToAbsolute <-> ToNormalized ─────────────────────────────

    [Theory]
    [InlineData(0.1, 0.2, 800, 600)]
    [InlineData(0.0, 0.0, 1920, 1080)]
    [InlineData(1.0, 1.0, 640, 480)]
    [InlineData(0.333, 0.667, 1024, 768)]
    public void RoundTrip_ShouldBeStable_ForPairMethods(double normX, double normY, int width, int height)
    {
        var (absX, absY) = CoordinateMapper.ToAbsolute(normX, normY, width, height);
        var (backX, backY) = CoordinateMapper.ToNormalized(absX, absY, width, height);

        backX.Should().BeApproximately(normX, 1e-10);
        backY.Should().BeApproximately(normY, 1e-10);
    }
}
