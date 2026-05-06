using System.Drawing;
using FluentAssertions;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Testable subclass that simulates YOLOv8-seg model output parsing.
/// Extends OnnxDetector to handle mask output alongside detection output.
/// YOLOv8-seg produces two outputs: [1, 4+numClasses, numDetections] for detection
/// and [1, numDetections, maskDims, maskH, maskW] for mask coefficients.
/// </summary>
internal class TestableOnnxDetectorSeg : OnnxDetector
{
    /// <summary>
    /// Postprocess with mask output support. If outputs contains more than one tensor,
    /// the second tensor is treated as mask coefficients and stored in InferenceResult.Mask.
    /// </summary>
    public List<InferenceResult> PostprocessWithMaskForTest(float[][] outputs, int originalWidth, int originalHeight)
    {
        if (outputs.Length < 2)
        {
            // No mask output — fall back to standard detection
            return Postprocess(outputs, originalWidth, originalHeight);
        }

        // Process detection output (first tensor) using base class
        var detections = Postprocess(new[] { outputs[0] }, originalWidth, originalHeight);

        // Extract mask coefficients from second tensor
        // outputs[1] shape: [numDetections, maskDims] flattened
        var maskTensor = outputs[1];
        var numDetections = detections.Count;

        if (maskTensor.Length > 0 && numDetections > 0)
        {
            var maskDims = maskTensor.Length / Math.Max(numDetections, 1);

            for (int i = 0; i < numDetections && i * maskDims < maskTensor.Length; i++)
            {
                var maskCoeffs = new float[maskDims];
                Array.Copy(maskTensor, i * maskDims, maskCoeffs, 0, maskDims);
                detections[i].Mask = maskCoeffs;
            }
        }

        return detections;
    }
}

/// <summary>
/// Unit tests for YOLOv8-seg model output parsing, covering mask extraction,
/// detection + mask coexistence, and backward compatibility (detection-only mode).
/// </summary>
public class OnnxDetectorSegTests
{
    private readonly TestableOnnxDetectorSeg _detector = new();

    /// <summary>
    /// Helper to build YOLOv8 transposed (column-major) flat output for detection.
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
    /// Verifies that when mask output is present alongside detection output,
    /// the mask coefficients are extracted and stored in InferenceResult.Mask.
    /// </summary>
    [Fact]
    public void Postprocess_WithMaskOutput_ParsesSegmentation()
    {
        // Arrange: 2 classes, 8 raw detections (3 high-confidence survive NMS)
        var numClasses = 2;
        var numDetections = 8; // must be > numFeatures (6) for transposed path
        var expectedSurviving = 3; // 3 high-confidence detections survive threshold
        var maskDims = 32;

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.25f;

        // Build detection tensor
        var detections = new (float cx, float cy, float w, float h, float[] classScores)[numDetections];
        detections[0] = (320f, 320f, 64f, 64f, new float[] { 0.9f, 0.1f });
        detections[1] = (160f, 160f, 32f, 32f, new float[] { 0.1f, 0.85f });
        detections[2] = (480f, 240f, 48f, 48f, new float[] { 0.7f, 0.2f });
        for (int i = 3; i < numDetections; i++)
        {
            detections[i] = (50f, 50f, 10f, 10f, new float[] { 0.1f, 0.1f });
        }

        var detectionOutput = BuildYolov8Transposed(numClasses, numDetections, detections);

        // Build mask coefficient tensor sized for surviving detections only
        var maskOutput = new float[expectedSurviving * maskDims];
        for (int d = 0; d < expectedSurviving; d++)
        {
            for (int m = 0; m < maskDims; m++)
            {
                maskOutput[d * maskDims + m] = (d + 1) * 0.1f + m * 0.01f;
            }
        }

        // Act
        var results = _detector.PostprocessWithMaskForTest(
            new[] { detectionOutput, maskOutput }, 640, 640);

        // Assert: 3 high-confidence detections
        results.Should().HaveCount(3);

        // Verify mask coefficients are present on each detection
        foreach (var result in results)
        {
            result.Mask.Should().NotBeNull("mask coefficients should be extracted from second output");
            result.Mask.Should().HaveCount(maskDims, "mask coefficients should have maskDims elements");
        }

        // Verify detection data is correct
        var det0 = results.First(r => r.ClassIndex == 0);
        det0.ClassName.Should().Be("cat");
        det0.Confidence.Should().BeApproximately(0.9f, 0.01f);

        var det1 = results.First(r => r.ClassIndex == 1);
        det1.ClassName.Should().Be("dog");
        det1.Confidence.Should().BeApproximately(0.85f, 0.01f);
    }

    /// <summary>
    /// Verifies that detection bounding boxes and class predictions still work correctly
    /// when mask output is present (seg model does not break detection logic).
    /// </summary>
    [Fact]
    public void Postprocess_WithMaskOutput_DetectionStillWorks()
    {
        // Arrange: 2 classes, 8 raw detections (2 high-confidence survive)
        var numClasses = 2;
        var numDetections = 8;
        var expectedSurviving = 2;
        var maskDims = 16;

        _detector.SetClassNames(new[] { "defect", "scratch" });
        _detector.ConfidenceThreshold = 0.3f;

        var detections = new (float cx, float cy, float w, float h, float[] classScores)[numDetections];
        detections[0] = (320f, 320f, 100f, 80f, new float[] { 0.95f, 0.05f });
        detections[1] = (100f, 200f, 50f, 40f, new float[] { 0.1f, 0.88f });
        for (int i = 2; i < numDetections; i++)
        {
            detections[i] = (50f, 50f, 10f, 10f, new float[] { 0.1f, 0.1f });
        }

        var detectionOutput = BuildYolov8Transposed(numClasses, numDetections, detections);

        // Mask output sized for surviving detections
        var maskOutput = new float[expectedSurviving * maskDims];
        for (int d = 0; d < expectedSurviving; d++)
            for (int m = 0; m < maskDims; m++)
                maskOutput[d * maskDims + m] = 0.5f;

        // Act
        var results = _detector.PostprocessWithMaskForTest(
            new[] { detectionOutput, maskOutput }, 640, 640);

        // Assert: verify detection correctness
        results.Should().HaveCount(2);

        var defect = results.First(r => r.ClassIndex == 0);
        defect.ClassName.Should().Be("defect");
        defect.Confidence.Should().BeApproximately(0.95f, 0.01f);
        defect.BoundingBox.Width.Should().BeApproximately(100f, 1f);
        defect.BoundingBox.Height.Should().BeApproximately(80f, 1f);

        var scratch = results.First(r => r.ClassIndex == 1);
        scratch.ClassName.Should().Be("scratch");
        scratch.Confidence.Should().BeApproximately(0.88f, 0.01f);
        scratch.BoundingBox.Width.Should().BeApproximately(50f, 1f);

        // Verify masks are attached
        defect.Mask.Should().HaveCount(maskDims);
        scratch.Mask.Should().HaveCount(maskDims);
    }

    /// <summary>
    /// Verifies backward compatibility: when only one output tensor is provided (no mask),
    /// the detector operates in detection-only mode with Mask being null.
    /// </summary>
    [Fact]
    public void Postprocess_NoMaskOutput_DetectionOnlyMode()
    {
        // Arrange: 2 classes, single output tensor (no mask)
        var numClasses = 2;
        var numDetections = 8;

        _detector.SetClassNames(new[] { "cat", "dog" });
        _detector.ConfidenceThreshold = 0.25f;

        var detections = new (float cx, float cy, float w, float h, float[] classScores)[numDetections];
        detections[0] = (320f, 320f, 64f, 64f, new float[] { 0.9f, 0.1f });
        detections[1] = (160f, 160f, 32f, 32f, new float[] { 0.1f, 0.8f });
        for (int i = 2; i < numDetections; i++)
        {
            detections[i] = (50f, 50f, 10f, 10f, new float[] { 0.1f, 0.1f });
        }

        var detectionOutput = BuildYolov8Transposed(numClasses, numDetections, detections);

        // Act: only one output tensor (detection only, no mask)
        var results = _detector.PostprocessWithMaskForTest(
            new[] { detectionOutput }, 640, 640);

        // Assert: detections work as normal
        results.Should().HaveCount(2);

        var det0 = results.First(r => r.ClassIndex == 0);
        det0.ClassName.Should().Be("cat");
        det0.Confidence.Should().BeApproximately(0.9f, 0.01f);
        det0.Mask.Should().BeNull("no mask output means Mask should be null");

        var det1 = results.First(r => r.ClassIndex == 1);
        det1.ClassName.Should().Be("dog");
        det1.Mask.Should().BeNull("no mask output means Mask should be null");
    }

    /// <summary>
    /// Verifies that empty detection output with mask output returns empty results.
    /// </summary>
    [Fact]
    public void Postprocess_EmptyDetectionWithMask_ReturnsEmpty()
    {
        // Arrange: empty detection, non-empty mask
        _detector.SetClassNames(new[] { "cat" });

        var maskOutput = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var results = _detector.PostprocessWithMaskForTest(
            new[] { Array.Empty<float>(), maskOutput }, 640, 640);

        // Assert
        results.Should().BeEmpty();
    }
}
