using System.Drawing;
using FluentAssertions;
using ScaffoldX.Core.Vision;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Minimal testable subclass of <see cref="InferenceEngineBase"/> that provides
/// stub implementations of abstract methods for robustness/guard-clause testing.
/// </summary>
internal class StubInferenceEngine : InferenceEngineBase
{
    /// <inheritdoc />
    protected override void LoadModelInternal(string modelPath)
    {
        // No-op: model loading is not under test here.
    }

    /// <inheritdoc />
    protected override float[] Preprocess(Bitmap image) => Array.Empty<float>();

    /// <inheritdoc />
    protected override float[][] RunInference(float[] input) => Array.Empty<float[]>();

    /// <inheritdoc />
    protected override List<InferenceResult> Postprocess(float[][] outputs, int originalWidth, int originalHeight) =>
        new();
}

/// <summary>
/// Unit tests verifying null-guard and precondition checks on <see cref="InferenceEngineBase"/>.
/// </summary>
public class InferenceRobustnessTests : IDisposable
{
    private readonly StubInferenceEngine _engine = new();

    /// <summary>
    /// Verifies that Run throws ArgumentNullException when the image is null.
    /// </summary>
    [Fact]
    public void Run_NullImage_ThrowsArgumentNullException()
    {
        // Arrange — load a dummy model so we pass the IsLoaded check
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, new byte[] { 0x00 });
            _engine.LoadModel(tempPath);

            // Act
            var act = () => _engine.Run(null!);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>()
               .WithParameterName("image");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Verifies that RunAsync throws ArgumentNullException when the image is null.
    /// </summary>
    [Fact]
    public async Task RunAsync_NullImage_ThrowsArgumentNullException()
    {
        // Arrange — load a dummy model so we pass the IsLoaded check
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, new byte[] { 0x00 });
            _engine.LoadModel(tempPath);

            // Act
            var act = () => _engine.RunAsync(null!);

            // Assert
            (await act.Should().ThrowExactlyAsync<ArgumentNullException>())
                .WithParameterName("image");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Verifies that Run throws InvalidOperationException when the model is not loaded.
    /// </summary>
    [Fact]
    public void Run_ModelNotLoaded_ThrowsInvalidOperationException()
    {
        // Arrange — no model loaded
        using var image = new Bitmap(64, 64);

        // Act
        var act = () => _engine.Run(image);

        // Assert
        act.Should().ThrowExactly<InvalidOperationException>()
           .WithMessage("*模型未加载*");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _engine.Dispose();
    }
}
