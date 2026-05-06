using System.IO;
using FluentAssertions;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for ModelZooService, covering model catalog retrieval, download status checks, and path resolution.
/// </summary>
public class ModelZooServiceTests : IDisposable
{
    private readonly ModelZooService _sut;
    private readonly string _tempCacheDir;

    public ModelZooServiceTests()
    {
        // Use a unique temp directory to avoid polluting the real cache
        _tempCacheDir = Path.Combine(Path.GetTempPath(), $"ModelZooTests_{Guid.NewGuid():N}");
        _sut = new ModelZooService(baseUrl: "https://example.com/models");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempCacheDir))
        {
            try { Directory.Delete(_tempCacheDir, recursive: true); }
            catch { /* best effort cleanup */ }
        }
    }

    [Fact]
    public void GetAvailableModels_ReturnsNonEmptyList()
    {
        // Act
        var models = _sut.GetAvailableModels();

        // Assert
        models.Should().NotBeEmpty("the built-in model catalog should contain at least one model");
    }

    [Fact]
    public void GetAvailableModels_ContainsYoloV8Models()
    {
        // Act
        var models = _sut.GetAvailableModels();
        var modelIds = models.Select(m => m.Id).ToList();

        // Assert
        modelIds.Should().Contain("yolov8n", "YOLOv8 Nano should be in the catalog");
        modelIds.Should().Contain("yolov8s", "YOLOv8 Small should be in the catalog");
        modelIds.Should().Contain("yolov8m", "YOLOv8 Medium should be in the catalog");
        modelIds.Should().Contain("yolov8l", "YOLOv8 Large should be in the catalog");
        modelIds.Should().Contain("yolov8x", "YOLOv8 Extra Large should be in the catalog");
    }

    [Fact]
    public void IsModelDownloaded_NonExistentModel_ReturnsFalse()
    {
        // Act
        var result = _sut.IsModelDownloaded("non_existent_model_xyz");

        // Assert
        result.Should().BeFalse("a model that was never downloaded should not be reported as downloaded");
    }

    [Fact]
    public void GetModelPath_NonExistentModel_ReturnsNull()
    {
        // Act
        var path = _sut.GetModelPath("non_existent_model_xyz");

        // Assert
        path.Should().BeNull("a model that was never downloaded should have no local path");
    }

    [Fact]
    public void GetAvailableModels_AllModelsHaveRequiredFields()
    {
        // Act
        var models = _sut.GetAvailableModels();

        // Assert
        foreach (var model in models)
        {
            model.Id.Should().NotBeNullOrWhiteSpace("each model must have an Id");
            model.Name.Should().NotBeNullOrWhiteSpace("each model must have a Name");
            model.Description.Should().NotBeNullOrWhiteSpace("each model must have a Description");
            model.SizeBytes.Should().BeGreaterThan(0, "each model must have a positive size");
            model.DownloadUrl.Should().NotBeNullOrWhiteSpace("each model must have a DownloadUrl");
            model.Category.Should().NotBeNullOrWhiteSpace("each model must have a Category");
        }
    }

    [Fact]
    public void GetAvailableModels_ContainsBothDetectionAndSegmentationModels()
    {
        // Act
        var models = _sut.GetAvailableModels();
        var categories = models.Select(m => m.Category).Distinct().ToList();

        // Assert
        categories.Should().Contain("Detection", "the catalog should include detection models");
        categories.Should().Contain("Segmentation", "the catalog should include segmentation models");
    }

    [Fact]
    public void IsModelDownloaded_ModelIdWithSpecialCharacters_ReturnsFalse()
    {
        // Act
        var result = _sut.IsModelDownloaded("../../etc/passwd");

        // Assert
        result.Should().BeFalse("path traversal attempts should not find a model");
    }

    [Fact]
    public void GetModelPath_ModelIdWithSpecialCharacters_ReturnsNull()
    {
        // Act
        var path = _sut.GetModelPath("../../etc/passwd");

        // Assert
        path.Should().BeNull("path traversal attempts should not return a valid path");
    }
}
