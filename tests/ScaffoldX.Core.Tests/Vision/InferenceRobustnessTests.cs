using System.Drawing;
using FluentAssertions;
using ScaffoldX.Core.Vision;
using TorchSharp;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Minimal testable subclass of <see cref="InferenceEngineBase"/> that provides
/// stub implementations of abstract methods for robustness/guard-clause testing.
/// </summary>
internal class StubInferenceEngine : InferenceEngineBase
{
    protected override void LoadModelInternal(string modelPath) { }

    protected override torch.Tensor PreprocessToTensor(Bitmap image)
        => torch.zeros(new long[] { 1, 3, 64, 64 });

    protected override torch.Tensor RunModelInference(torch.Tensor input)
        => torch.zeros(new long[] { 1, 84, 8400 });

    protected override List<InferenceResult> PostprocessResults(torch.Tensor outputs, int originalWidth, int originalHeight)
        => new();
}

/// <summary>
/// Unit tests verifying null-guard and precondition checks on <see cref="InferenceEngineBase"/>.
/// </summary>
public class InferenceRobustnessTests : IDisposable
{
    private readonly StubInferenceEngine _engine = new();

    [Fact]
    public void Run_NullImage_ThrowsArgumentNullException()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, new byte[] { 0x00 });
            _engine.LoadModel(tempPath);

            var act = () => _engine.Run(null!);
            act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("image");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task RunAsync_NullImage_ThrowsArgumentNullException()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempPath, new byte[] { 0x00 });
            _engine.LoadModel(tempPath);

            var act = () => _engine.RunAsync(null!);
            (await act.Should().ThrowExactlyAsync<ArgumentNullException>()).WithParameterName("image");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Run_ModelNotLoaded_ThrowsInvalidOperationException()
    {
        using var image = new Bitmap(64, 64);
        var act = () => _engine.Run(image);
        act.Should().ThrowExactly<InvalidOperationException>().WithMessage("*模型未加载*");
    }

    public void Dispose()
    {
        _engine.Dispose();
    }
}
