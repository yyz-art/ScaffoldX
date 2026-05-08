using FluentAssertions;
using ScaffoldX.Core.Vision;
using TorchSharp;
using Xunit;

namespace ScaffoldX.Core.Tests.Vision;

/// <summary>
/// Unit tests for <see cref="ImageEmbedding"/> covering construction,
/// property access, null guards, and dispose behavior.
/// </summary>
public class ImageEmbeddingTests
{
    // ── Construction & properties ───────────────────────────────────────────

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        using var tensor = torch.zeros(new long[] { 1, 256, 64, 64 });

        using var embedding = new ImageEmbedding(tensor, 1920, 1080, 1024, 512);

        embedding.Features.Should().BeSameAs(tensor);
        embedding.OriginalWidth.Should().Be(1920);
        embedding.OriginalHeight.Should().Be(1080);
        embedding.ScaledWidth.Should().Be(1024);
        embedding.ScaledHeight.Should().Be(512);
    }

    [Fact]
    public void Constructor_NullTensor_ThrowsArgumentNullException()
    {
        var act = () => new ImageEmbedding(null!, 64, 64, 64, 64);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("features");
    }

    [Fact]
    public void Constructor_ZeroDimensions_SetsProperties()
    {
        using var tensor = torch.zeros(new long[] { 1, 256, 1, 1 });

        using var embedding = new ImageEmbedding(tensor, 0, 0, 0, 0);

        embedding.OriginalWidth.Should().Be(0);
        embedding.OriginalHeight.Should().Be(0);
        embedding.ScaledWidth.Should().Be(0);
        embedding.ScaledHeight.Should().Be(0);
    }

    // ── Dispose ─────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var tensor = torch.zeros(new long[] { 1, 256, 64, 64 });
        var embedding = new ImageEmbedding(tensor, 64, 64, 64, 64);

        var act = () => embedding.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var tensor = torch.zeros(new long[] { 1, 256, 64, 64 });
        var embedding = new ImageEmbedding(tensor, 64, 64, 64, 64);

        embedding.Dispose();
        var act = () => embedding.Dispose();

        act.Should().NotThrow();
    }

    // ── IDisposable interface ───────────────────────────────────────────────

    [Fact]
    public void ImplementsIDisposable()
    {
        typeof(ImageEmbedding).Should().Implement<IDisposable>();
    }
}
