using System.Drawing;
using System.Drawing.Imaging;
using FluentAssertions;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Testable subclass that exposes the protected Preprocess method for unit testing.
/// </summary>
internal class TestableDetectorForPreprocess : OnnxDetector
{
    /// <summary>
    /// Exposes the protected Preprocess method for testing.
    /// </summary>
    public float[] PreprocessForTest(Bitmap image) => Preprocess(image);

    /// <summary>
    /// Allows setting InputWidth/InputHeight for testing with smaller dimensions.
    /// </summary>
    public void SetInputDimensions(int width, int height)
    {
        InputWidth = width;
        InputHeight = height;
    }
}

/// <summary>
/// Unit tests for BitmapData preprocessing: output shape, pixel normalization, and edge cases.
/// </summary>
public class PreprocessPerformanceTests
{
    /// <summary>
    /// Verifies that preprocessing a small image produces a float array with the correct shape [1,3,H,W].
    /// </summary>
    [Fact]
    public void Preprocess_SmallImage_ProducesCorrectShape()
    {
        // Arrange: use small dimensions to keep the test fast
        using var detector = new TestableDetectorForPreprocess();
        detector.SetInputDimensions(8, 8);

        using var image = new Bitmap(16, 16, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(image))
        {
            g.Clear(Color.Red);
        }

        // Act
        var result = detector.PreprocessForTest(image);

        // Assert: flat array length should be 1 * 3 * H * W
        var expectedLength = 1 * 3 * 8 * 8; // 192
        result.Should().HaveCount(expectedLength,
            "output shape should be [1, 3, {0}, {1}] = {2} elements", 8, 8, expectedLength);
    }

    /// <summary>
    /// Verifies that all pixel values in the preprocessed output are normalized to [0, 1] range.
    /// </summary>
    [Fact]
    public void Preprocess_SmallImage_PixelValuesNormalized()
    {
        // Arrange
        using var detector = new TestableDetectorForPreprocess();
        detector.SetInputDimensions(4, 4);

        // Create an image with a known color (128, 64, 200)
        using var image = new Bitmap(4, 4, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(image))
        {
            g.Clear(Color.FromArgb(128, 64, 200));
        }

        // Act
        var result = detector.PreprocessForTest(image);

        // Assert: all values should be in [0, 1] since input is 0-255 divided by 255
        result.Should().AllSatisfy(v =>
        {
            v.Should().BeGreaterThanOrEqualTo(0f, "pixel / 255 should be >= 0");
            v.Should().BeLessThanOrEqualTo(1f, "pixel / 255 should be <= 1");
        });

        // Verify specific channel values
        // Channel layout: [R plane (16 values), G plane (16 values), B plane (16 values)]
        var pixelCount = 4 * 4; // 16
        var expectedR = 128f / 255f;
        var expectedG = 64f / 255f;
        var expectedB = 200f / 255f;

        // All R values should be ~128/255
        result[0].Should().BeApproximately(expectedR, 0.005f);
        // All G values should be ~64/255
        result[pixelCount].Should().BeApproximately(expectedG, 0.005f);
        // All B values should be ~200/255
        result[pixelCount * 2].Should().BeApproximately(expectedB, 0.005f);
    }

    /// <summary>
    /// Verifies that preprocessing a solid black image produces all-zero values.
    /// </summary>
    [Fact]
    public void Preprocess_BlackImage_AllZeros()
    {
        // Arrange
        using var detector = new TestableDetectorForPreprocess();
        detector.SetInputDimensions(4, 4);

        using var image = new Bitmap(4, 4, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(image))
        {
            g.Clear(Color.Black); // RGB = (0, 0, 0)
        }

        // Act
        var result = detector.PreprocessForTest(image);

        // Assert: all values should be 0.0 (0 / 255 = 0)
        result.Should().AllSatisfy(v =>
            v.Should().Be(0f, "black pixels (0) divided by 255 should be 0"));
    }

    /// <summary>
    /// Verifies that preprocessing a solid white image produces all-one values.
    /// </summary>
    [Fact]
    public void Preprocess_WhiteImage_AllOnes()
    {
        // Arrange
        using var detector = new TestableDetectorForPreprocess();
        detector.SetInputDimensions(4, 4);

        using var image = new Bitmap(4, 4, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(image))
        {
            g.Clear(Color.White); // RGB = (255, 255, 255)
        }

        // Act
        var result = detector.PreprocessForTest(image);

        // Assert: all values should be 1.0 (255 / 255 = 1)
        result.Should().AllSatisfy(v =>
            v.Should().Be(1f, "white pixels (255) divided by 255 should be 1"));
    }
}
