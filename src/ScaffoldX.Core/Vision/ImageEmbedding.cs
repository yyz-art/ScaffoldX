using TorchSharp;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// 缓存的图像嵌入向量，用于 SAM 3 交互式分割。
/// 调用方负责 Dispose 释放 Tensor 内存。
/// </summary>
public sealed class ImageEmbedding : IDisposable
{
    private bool _disposed;

    /// <summary>图像特征 Tensor。</summary>
    public torch.Tensor Features { get; }

    /// <summary>原始图像宽度。</summary>
    public int OriginalWidth { get; }

    /// <summary>原始图像高度。</summary>
    public int OriginalHeight { get; }

    /// <summary>编码时的缩放宽度。</summary>
    public int ScaledWidth { get; }

    /// <summary>编码时的缩放高度。</summary>
    public int ScaledHeight { get; }

    public ImageEmbedding(torch.Tensor features, int originalWidth, int originalHeight, int scaledWidth, int scaledHeight)
    {
        Features = features ?? throw new ArgumentNullException(nameof(features));
        OriginalWidth = originalWidth;
        OriginalHeight = originalHeight;
        ScaledWidth = scaledWidth;
        ScaledHeight = scaledHeight;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Features.Dispose();
        }
    }
}
