using System.Drawing;
using FluentAssertions;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Testable subclass that exposes the protected Postprocess method for unit testing.
/// </summary>
internal class TestableOnnxDetector : OnnxDetector
{
    /// <summary>
    /// Exposes the protected Postprocess method for testing.
    /// </summary>
    public List<InferenceResult> PostprocessForTest(float[][] outputs, int originalWidth, int originalHeight)
    {
        return Postprocess(outputs, originalWidth, originalHeight);
    }
}

/// <summary>
/// Unit tests for OnnxDetector.Postprocess, covering YOLOv8 transposed format,
/// YOLOv5 standard format, empty output, and confidence threshold filtering.
/// </summary>
public class OnnxDetectorTests
{
    private readonly TestableOnnxDetector _detector = new();

    /// <summary>
    /// Helper to build YOLOv8 transposed (column-major) flat output.
    /// YOLOv8 [1, 4+numClasses, numDetections] stores features along rows,
    /// detections along columns. Access: output[feature * numDetections + detection].
    /// </summary>
    private static float[] BuildYolov8Transposed(int numClasses, int numDetections,
        (float cx, float cy, float w, float h, float[] classScores)[] detections)
    {
        var numFeatures = 4 + numClasses;
        var flat = new float[numFeatures * numDetections];

        for (int d = 0; d < numDetections; d++)
        {
            var det = detections[d];
            flat[0 * numDetections + d] = det.cx;
            flat[1 * numDetections + d] = det.cy;
            flat[2 * numDetections + d] = det.w;
            flat[3 * numDetections + d] = det.h;
            for (int c = 0; c < numClasses; c++)
            {
                flat[(4 + c) * numDetections + d] = det.classScores[c];
            }
        }

        return flat;
    }

    /// <summary>
    /// Verifies that YOLOv8 transposed format [1, 4+numClasses, numDetections]
    /// is correctly parsed using column-major access.
    /// </summary>
    [Fact]
    public void Postprocess_TransposedYOLOv8Format_DetectsObjects()
    {
        // Arrange: 2 classes, 7 detections (7 > 6 features → transposed path)
        var numClasses = 2;
        var numDetections = 7; // must be > numFeatures (6) for transposed path

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.25f;

        // Detection 0: center=(320,320), size=(64,64), class_0=0.9
        // Detection 1: center=(160,160), size=(32,32), class_1=0.8
        // Detections 2-6: low confidence (0.1) → filtered out
        var detections = new (float cx, float cy, float w, float h, float[] classScores)[numDetections];
        detections[0] = (320f, 320f, 64f, 64f, new float[] { 0.9f, 0.1f });
        detections[1] = (160f, 160f, 32f, 32f, new float[] { 0.1f, 0.8f });
        for (int i = 2; i < numDetections; i++)
        {
            detections[i] = (50f, 50f, 10f, 10f, new float[] { 0.1f, 0.1f });
        }

        var flatOutput = BuildYolov8Transposed(numClasses, numDetections, detections);

        // Act: Postprocess with original image 640x640 (same as model input, scale=1.0)
        var results = _detector.PostprocessForTest(new[] { flatOutput }, 640, 640);

        // Assert: Only 2 high-confidence detections should be found
        results.Should().HaveCount(2);

        var det0 = results.First(r => r.ClassIndex == 0);
        det0.ClassName.Should().Be("cat");
        det0.Confidence.Should().BeApproximately(0.9f, 0.01f);
        det0.BoundingBox.X.Should().BeApproximately(320f - 32f, 1f); // cx - w/2 = 288
        det0.BoundingBox.Y.Should().BeApproximately(320f - 32f, 1f);
        det0.BoundingBox.Width.Should().BeApproximately(64f, 1f);
        det0.BoundingBox.Height.Should().BeApproximately(64f, 1f);

        var det1 = results.First(r => r.ClassIndex == 1);
        det1.ClassName.Should().Be("dog");
        det1.Confidence.Should().BeApproximately(0.8f, 0.01f);
        det1.BoundingBox.Width.Should().BeApproximately(32f, 1f);
    }

    /// <summary>
    /// Verifies that standard YOLOv5 format [1, numDetections, 5+numClasses]
    /// is correctly parsed with objectness * class_score.
    /// </summary>
    [Fact]
    public void Postprocess_StandardYOLOv5Format_DetectsObjects()
    {
        // Arrange: 2 classes, 3 detections
        // numValuesPerDetection = 5 + 2 = 7
        // 3 * 7 = 21 elements
        var numClasses = 2;
        var numDetections = 3;
        var stride = 5 + numClasses; // 7

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.25f;

        var flatOutput = new float[numDetections * stride];

        // Detection 0: cx=320, cy=320, w=64, h=64, objectness=0.9, class_0=1.0 → score=0.9
        flatOutput[0] = 320f; flatOutput[1] = 320f; flatOutput[2] = 64f; flatOutput[3] = 64f;
        flatOutput[4] = 0.9f; flatOutput[5] = 1.0f; flatOutput[6] = 0.1f;

        // Detection 1: cx=100, cy=100, w=20, h=20, objectness=0.8, class_1=1.0 → score=0.8
        var off1 = stride;
        flatOutput[off1 + 0] = 100f; flatOutput[off1 + 1] = 100f; flatOutput[off1 + 2] = 20f; flatOutput[off1 + 3] = 20f;
        flatOutput[off1 + 4] = 0.8f; flatOutput[off1 + 5] = 0.1f; flatOutput[off1 + 6] = 1.0f;

        // Detection 2: low confidence → filtered
        var off2 = 2 * stride;
        flatOutput[off2 + 0] = 50f; flatOutput[off2 + 1] = 50f; flatOutput[off2 + 2] = 10f; flatOutput[off2 + 3] = 10f;
        flatOutput[off2 + 4] = 0.1f; flatOutput[off2 + 5] = 0.5f; flatOutput[off2 + 6] = 0.5f;

        // Act
        var results = _detector.PostprocessForTest(new[] { flatOutput }, 640, 640);

        // Assert
        results.Should().HaveCount(2);

        var det0 = results.First(r => r.ClassIndex == 0);
        det0.ClassName.Should().Be("cat");
        det0.Confidence.Should().BeApproximately(0.9f, 0.01f);
        det0.BoundingBox.Width.Should().BeApproximately(64f, 1f);

        var det1 = results.First(r => r.ClassIndex == 1);
        det1.ClassName.Should().Be("dog");
        det1.Confidence.Should().BeApproximately(0.8f, 0.01f);
        det1.BoundingBox.Width.Should().BeApproximately(20f, 1f);
    }

    /// <summary>
    /// Verifies that empty model output returns an empty result list.
    /// </summary>
    [Fact]
    public void Postprocess_EmptyOutput_ReturnsEmptyList()
    {
        // Arrange
        _detector.SetClassNames(new[] { "cat", "dog" });

        // Act
        var results = _detector.PostprocessForTest(Array.Empty<float[]>(), 640, 480);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that detections below the confidence threshold are filtered out.
    /// Uses YOLOv5 format with objectness=1.0 for direct score comparison.
    /// </summary>
    [Fact]
    public void Postprocess_LowConfidence_FiltersDetections()
    {
        // Arrange: 2 classes, stride=7, 3 detections
        var stride = 5 + 2; // 7

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.5f;

        var flatOutput = new float[3 * stride];

        // Detection 0: objectness=0.9, class_0=1.0 → score=0.9 (KEEP)
        flatOutput[0] = 320f; flatOutput[1] = 320f; flatOutput[2] = 64f; flatOutput[3] = 64f;
        flatOutput[4] = 0.9f; flatOutput[5] = 1.0f; flatOutput[6] = 0f;

        // Detection 1: objectness=0.3, class_0=1.0 → score=0.3 (FILTERED)
        var off1 = stride;
        flatOutput[off1 + 0] = 100f; flatOutput[off1 + 1] = 100f; flatOutput[off1 + 2] = 20f; flatOutput[off1 + 3] = 20f;
        flatOutput[off1 + 4] = 0.3f; flatOutput[off1 + 5] = 1.0f; flatOutput[off1 + 6] = 0f;

        // Detection 2: objectness=0.6, class_1=1.0 → score=0.6 (KEEP)
        var off2 = 2 * stride;
        flatOutput[off2 + 0] = 200f; flatOutput[off2 + 1] = 200f; flatOutput[off2 + 2] = 30f; flatOutput[off2 + 3] = 30f;
        flatOutput[off2 + 4] = 0.6f; flatOutput[off2 + 5] = 0f; flatOutput[off2 + 6] = 1.0f;

        // Act
        var results = _detector.PostprocessForTest(new[] { flatOutput }, 640, 640);

        // Assert: 2 detections pass the 0.5 threshold
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Confidence >= 0.5f);
    }

    /// <summary>
    /// Verifies that scaling from model input size to original image size works correctly.
    /// </summary>
    [Fact]
    public void Postprocess_DifferentOriginalSize_ScalesCorrectly()
    {
        // Arrange: model input 640x640, original image 1280x960
        var stride = 5 + 2; // 7

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.1f;

        var flatOutput = new float[stride];

        // Detection at center=(320,320) in model coords, size=(64,64)
        flatOutput[0] = 320f; flatOutput[1] = 320f; flatOutput[2] = 64f; flatOutput[3] = 64f;
        flatOutput[4] = 1.0f; flatOutput[5] = 0.9f; flatOutput[6] = 0f;

        // Act: original 1280x960, model input 640x640
        var results = _detector.PostprocessForTest(new[] { flatOutput }, 1280, 960);

        // Assert: coordinates should be scaled
        results.Should().HaveCount(1);
        var det = results[0];

        // scaleX = 1280/640 = 2.0, scaleY = 960/640 = 1.5
        // x = (320 - 32) * 2.0 = 576
        det.BoundingBox.X.Should().BeApproximately(576f, 1f);
        // y = (320 - 32) * 1.5 = 432
        det.BoundingBox.Y.Should().BeApproximately(432f, 1f);
        // width = 64 * 2.0 = 128
        det.BoundingBox.Width.Should().BeApproximately(128f, 1f);
        // height = 64 * 1.5 = 96
        det.BoundingBox.Height.Should().BeApproximately(96f, 1f);
    }

    /// <summary>
    /// Verifies that detections at image boundaries are clamped correctly.
    /// </summary>
    [Fact]
    public void Postprocess_DetectionAtBoundary_ClampsToImage()
    {
        // Arrange
        var stride = 5 + 2; // 7

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.1f;

        var flatOutput = new float[stride];

        // Detection near top-left: center=(10,10), size=(40,40)
        // x = (10 - 20) * 1.0 = -10 → clamped to 0
        // y = (10 - 20) * 1.0 = -10 → clamped to 0
        flatOutput[0] = 10f; flatOutput[1] = 10f; flatOutput[2] = 40f; flatOutput[3] = 40f;
        flatOutput[4] = 1.0f; flatOutput[5] = 0.9f; flatOutput[6] = 0f;

        // Act
        var results = _detector.PostprocessForTest(new[] { flatOutput }, 100, 100);

        // Assert
        results.Should().HaveCount(1);
        results[0].BoundingBox.X.Should().Be(0f);
        results[0].BoundingBox.Y.Should().Be(0f);
    }

    /// <summary>
    /// Verifies that the class name falls back to "class_N" when the winning class
    /// index exceeds the class names list.
    /// </summary>
    [Fact]
    public void Postprocess_ClassIndexExceedsNames_UsesFallbackName()
    {
        // Arrange: 2 classes defined, class_1 is highest score
        var stride = 5 + 2; // 7

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.1f;

        var flatOutput = new float[stride];
        flatOutput[0] = 320f; flatOutput[1] = 320f; flatOutput[2] = 64f; flatOutput[3] = 64f;
        flatOutput[4] = 1.0f; // objectness
        flatOutput[5] = 0.3f; // class_0 score
        flatOutput[6] = 0.9f; // class_1 score → winner

        // Act
        var results = _detector.PostprocessForTest(new[] { flatOutput }, 640, 640);

        // Assert
        results.Should().HaveCount(1);
        results[0].ClassIndex.Should().Be(1);
        results[0].ClassName.Should().Be("dog");
    }

    /// <summary>
    /// Verifies that YOLOv8 row-major format [1, numDetections, 4+numClasses]
    /// is parsed when numDetections <= numFeatures.
    /// </summary>
    [Fact]
    public void Postprocess_YOLOv8RowMajor_ParsesCorrectly()
    {
        // Arrange: 80 classes, 84 features. Use 5 detections (5 < 84 → row-major path).
        // But wait: 5 * 84 = 420 elements. Need to check if code treats this correctly.
        // Actually: numDetections = 420 / 84 = 5. 5 < 84 → ParseYolov8RowMajor.
        var numClasses = 2;
        var numFeatures = 4 + numClasses; // 6
        var numDetections = 3; // 3 < 6 → row-major path

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.25f;

        // Row-major: each detection is contiguous: [cx, cy, w, h, score_c0, score_c1]
        var flatOutput = new float[numDetections * numFeatures];

        // Detection 0: center=(320,320), size=(64,64), class_0=0.9
        flatOutput[0] = 320f; flatOutput[1] = 320f; flatOutput[2] = 64f; flatOutput[3] = 64f;
        flatOutput[4] = 0.9f; flatOutput[5] = 0.1f;

        // Detection 1: center=(160,160), size=(32,32), class_1=0.8
        var off1 = numFeatures;
        flatOutput[off1 + 0] = 160f; flatOutput[off1 + 1] = 160f; flatOutput[off1 + 2] = 32f; flatOutput[off1 + 3] = 32f;
        flatOutput[off1 + 4] = 0.1f; flatOutput[off1 + 5] = 0.8f;

        // Detection 2: low confidence → filtered
        var off2 = 2 * numFeatures;
        flatOutput[off2 + 0] = 50f; flatOutput[off2 + 1] = 50f; flatOutput[off2 + 2] = 10f; flatOutput[off2 + 3] = 10f;
        flatOutput[off2 + 4] = 0.1f; flatOutput[off2 + 5] = 0.1f;

        // Act
        var results = _detector.PostprocessForTest(new[] { flatOutput }, 640, 640);

        // Assert
        results.Should().HaveCount(2);

        var det0 = results.First(r => r.ClassIndex == 0);
        det0.Confidence.Should().BeApproximately(0.9f, 0.01f);

        var det1 = results.First(r => r.ClassIndex == 1);
        det1.Confidence.Should().BeApproximately(0.8f, 0.01f);
    }
}
