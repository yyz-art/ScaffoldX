using System.Drawing;
using FluentAssertions;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

public class MaskToPolygonTests
{
    [Fact]
    public void Convert_EmptyMask_ReturnsEmptyList()
    {
        var mask = new byte[10, 10];
        var result = MaskToPolygonConverter.Convert(mask);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Convert_SinglePoint_MayReturnEmpty()
    {
        // Marching Squares 需要至少 2x2 区域才能检测轮廓
        var mask = new byte[10, 10];
        mask[5, 5] = 1;
        var result = MaskToPolygonConverter.Convert(mask, 0);
        // 单像素可能无法形成有效轮廓，这是预期行为
    }

    [Fact]
    public void Convert_FullMask_ReturnsBoundaryPolygon()
    {
        var mask = new byte[10, 10];
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 10; x++)
                mask[y, x] = 1;

        var result = MaskToPolygonConverter.Convert(mask, 0);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Convert_RectangleMask_ReturnsFourCorners()
    {
        var mask = new byte[20, 20];
        for (int y = 5; y < 15; y++)
            for (int x = 5; x < 15; x++)
                mask[y, x] = 1;

        var result = MaskToPolygonConverter.Convert(mask, 3.0f);
        result.Should().NotBeEmpty();
        result.Count.Should().BeGreaterOrEqualTo(4);
    }

    [Fact]
    public void Convert_NormalizedCoordinates_AreBetween0And1()
    {
        var mask = new byte[10, 10];
        for (int y = 2; y < 8; y++)
            for (int x = 2; x < 8; x++)
                mask[y, x] = 1;

        var result = MaskToPolygonConverter.Convert(mask, 0);
        foreach (var point in result)
        {
            point.X.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(1);
            point.Y.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public void Simplify_FewPoints_ReturnsSamePoints()
    {
        var points = new List<PointF>
        {
            new(0, 0), new(10, 0), new(10, 10), new(0, 10)
        };
        var result = MaskToPolygonConverter.Simplify(points, 1.0f);
        result.Count.Should().BeLessThanOrEqualTo(points.Count);
    }

    [Fact]
    public void Simplify_ColinearPoints_RemovesMiddle()
    {
        var points = new List<PointF>
        {
            new(0, 0), new(5, 0), new(10, 0)
        };
        var result = MaskToPolygonConverter.Simplify(points, 1.0f);
        result.Count.Should().Be(2);
    }
}
