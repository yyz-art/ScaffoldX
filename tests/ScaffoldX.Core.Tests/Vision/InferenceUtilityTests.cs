using System.Drawing;
using System.Drawing.Imaging;
using FluentAssertions;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Testable subclass that exposes the protected static utility methods from InferenceEngineBase.
/// </summary>
internal class TestableInferenceEngine : InferenceEngineBase
{
    /// <summary>
    /// Exposes the protected static ResizeImage method for testing.
    /// </summary>
    public static Bitmap ResizeImageForTest(Bitmap image, int width, int height) =>
        ResizeImage(image, width, height);

    /// <summary>
    /// Exposes the protected static CreateDetectionResult method for testing.
    /// </summary>
    public static InferenceResult CreateDetectionResultForTest(
        float cx, float cy, float w, float h,
        int maxClassIndex, float maxScore,
        int originalWidth, int originalHeight,
        float scaleX, float scaleY,
        IReadOnlyList<string> classNames) =>
        CreateDetectionResult(cx, cy, w, h, maxClassIndex, maxScore,
            originalWidth, originalHeight, scaleX, scaleY, classNames);

    // Abstract method stubs (not used in these tests)
    protected override void LoadModelInternal(string modelPath) { }
    protected override float[] Preprocess(Bitmap image) => Array.Empty<float>();
    protected override float[][] RunInference(float[] input) => Array.Empty<float[]>();
    protected override List<InferenceResult> Postprocess(float[][] outputs, int originalWidth, int originalHeight) => new();
}

/// <summary>
/// Unit tests for DRY utility methods: ResizeImage and CreateDetectionResult.
/// </summary>
public class InferenceUtilityTests
{
    /// <summary>
    /// Verifies that resizing a larger image scales it down to the target dimensions.
    /// </summary>
    [Fact]
    public void ResizeImage_LargerImage_ScalesCorrectly()
    {
        // Arrange: 200x150 image → 50x50
        using var source = new Bitmap(200, 150, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(source))
        {
            g.Clear(Color.Blue);
        }

        // Act
        using var result = TestableInferenceEngine.ResizeImageForTest(source, 50, 50);

        // Assert
        result.Width.Should().Be(50);
        result.Height.Should().Be(50);
        result.PixelFormat.Should().Be(PixelFormat.Format24bppRgb);
    }

    /// <summary>
    /// Verifies that resizing a smaller image scales it up to the target dimensions.
    /// </summary>
    [Fact]
    public void ResizeImage_SmallerImage_ScalesCorrectly()
    {
        // Arrange: 10x10 image → 100x100
        using var source = new Bitmap(10, 10, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(source))
        {
            g.Clear(Color.Green);
        }

        // Act
        using var result = TestableInferenceEngine.ResizeImageForTest(source, 100, 100);

        // Assert
        result.Width.Should().Be(100);
        result.Height.Should().Be(100);
        result.PixelFormat.Should().Be(PixelFormat.Format24bppRgb);
    }

    /// <summary>
    /// Verifies that CreateDetectionResult correctly scales and converts model coordinates
    /// to original image coordinates.
    /// </summary>
    [Fact]
    public void CreateDetectionResult_ValidInput_CreatesResult()
    {
        // Arrange: model output at center (320, 320) with size (100, 80) in 640x640 input space
        // Original image is 1280x960, so scale factors are 2.0x and 1.5x
        var classNames = new List<string> { "cat", "dog", "bird" };

        // Act
        var result = TestableInferenceEngine.CreateDetectionResultForTest(
            cx: 320f, cy: 320f, w: 100f, h: 80f,
            maxClassIndex: 1, maxScore: 0.85f,
            originalWidth: 1280, originalHeight: 960,
            scaleX: 2.0f, scaleY: 1.5f,
            classNames: classNames);

        // Assert
        result.ClassIndex.Should().Be(1);
        result.ClassName.Should().Be("dog");
        result.Confidence.Should().Be(0.85f);

        // Bounding box: x = (320 - 50) * 2 = 540, y = (320 - 40) * 1.5 = 420
        // width = 100 * 2 = 200, height = 80 * 1.5 = 120
        result.BoundingBox.X.Should().Be(540f);
        result.BoundingBox.Y.Should().Be(420f);
        result.BoundingBox.Width.Should().Be(200f);
        result.BoundingBox.Height.Should().Be(120f);
    }

    /// <summary>
    /// Verifies that CreateDetectionResult clamps coordinates to image boundaries
    /// when the detection extends beyond the original image edges.
    /// </summary>
    [Fact]
    public void CreateDetectionResult_BoundaryClamping_ClampsCorrectly()
    {
        // Arrange: detection near the top-left corner with coordinates that would go negative
        // cx=10, cy=10, w=40, h=40 in 640x640 input space
        // scaleX=1, scaleY=1 → x = (10-20)*1 = -10, y = (10-20)*1 = -10
        var classNames = new List<string> { "object" };

        // Act
        var result = TestableInferenceEngine.CreateDetectionResultForTest(
            cx: 10f, cy: 10f, w: 40f, h: 40f,
            maxClassIndex: 0, maxScore: 0.9f,
            originalWidth: 640, originalHeight: 640,
            scaleX: 1.0f, scaleY: 1.0f,
            classNames: classNames);

        // Assert: x and y should be clamped to 0
        result.BoundingBox.X.Should().Be(0f, "negative X should be clamped to 0");
        result.BoundingBox.Y.Should().Be(0f, "negative Y should be clamped to 0");

        // width = 40 * 1 = 40, but clamped: min(40, 640 - 0) = 40
        result.BoundingBox.Width.Should().Be(40f);
        // height = 40 * 1 = 40, but clamped: min(40, 640 - 0) = 40
        result.BoundingBox.Height.Should().Be(40f);
    }
}
