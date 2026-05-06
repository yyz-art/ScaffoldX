using System.IO;
using FluentAssertions;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for <see cref="VideoFrameService"/>, covering input validation
/// and error-path behaviour for video info retrieval and frame extraction.
/// </summary>
public class VideoFrameServiceTests
{
    private readonly VideoFrameService _service = new();

    /// <summary>
    /// Verifies that GetVideoInfoAsync throws FileNotFoundException for a non-existent file.
    /// </summary>
    [Fact]
    public async Task GetVideoInfoAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.mp4");

        // Act
        var act = () => _service.GetVideoInfoAsync(nonExistentPath);

        // Assert
        (await act.Should().ThrowExactlyAsync<FileNotFoundException>())
            .WithMessage("*视频文件不存在*");
    }

    /// <summary>
    /// Verifies that ExtractFramesAsync throws FileNotFoundException for a non-existent file.
    /// </summary>
    [Fact]
    public async Task ExtractFramesAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.mp4");
        var outputDir = Path.GetTempPath();

        // Act
        var act = () => _service.ExtractFramesAsync(nonExistentPath, outputDir);

        // Assert
        (await act.Should().ThrowExactlyAsync<FileNotFoundException>())
            .WithMessage("*视频文件不存在*");
    }

    /// <summary>
    /// Verifies that GetVideoInfoAsync throws ArgumentException for a null or empty path.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetVideoInfoAsync_InvalidPath_ThrowsArgumentException(string? invalidPath)
    {
        // Act
        var act = () => _service.GetVideoInfoAsync(invalidPath!);

        // Assert
        (await act.Should().ThrowExactlyAsync<ArgumentException>())
            .WithParameterName("videoPath");
    }
}
