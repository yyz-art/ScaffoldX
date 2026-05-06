using System.IO;
using FluentAssertions;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Unit tests for the RecentFilesService, which manages a bounded list of
/// recently opened project files with MRU (most-recently-used) ordering.
/// Note: The service uses Path.GetFullPath() internally, so all paths are normalized.
/// </summary>
public class RecentFilesServiceTests : IDisposable
{
    private readonly RecentFilesService _sut;
    private readonly string _tempDir;

    /// <summary>
    /// Initializes a fresh service instance for each test and cleans up any persisted state.
    /// </summary>
    public RecentFilesServiceTests()
    {
        // Use a unique temp directory for test file paths to avoid cross-test interference
        _tempDir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_RecentTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Clean up any persisted recent_files.json from previous test runs
        var recentFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_files.json");
        if (File.Exists(recentFilesPath))
            File.Delete(recentFilesPath);

        _sut = new RecentFilesService();
    }

    /// <summary>
    /// Verifies that GetRecentFiles returns an empty list when no files have been added.
    /// </summary>
    [Fact]
    public void GetRecentFiles_EmptyAtStart_ReturnsEmpty()
    {
        // Act
        var result = _sut.GetRecentFiles();

        // Assert
        result.Should().NotBeNull("result should never be null");
        result.Should().BeEmpty("no files have been added yet");
    }

    /// <summary>
    /// Verifies that adding a file places it in the recent files list.
    /// </summary>
    [Fact]
    public void AddRecentFile_AddsToList()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "MyProject.scaffoldx");

        // Act
        _sut.AddRecentFile(filePath);
        var result = _sut.GetRecentFiles();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(Path.GetFullPath(filePath));
    }

    /// <summary>
    /// Verifies that adding a duplicate file moves it to the top (MRU position)
    /// rather than creating a second entry.
    /// </summary>
    [Fact]
    public void AddRecentFile_DuplicateMovesToTop()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "ProjectA.scaffoldx");
        var file2 = Path.Combine(_tempDir, "ProjectB.scaffoldx");
        var file3 = Path.Combine(_tempDir, "ProjectC.scaffoldx");

        _sut.AddRecentFile(file1);
        _sut.AddRecentFile(file2);
        _sut.AddRecentFile(file3);

        // Act — re-add file1, should move to top
        _sut.AddRecentFile(file1);
        var result = _sut.GetRecentFiles();

        // Assert
        result.Should().HaveCount(3, "duplicate should not increase count");
        result[0].Should().Be(Path.GetFullPath(file1), "re-added file should move to MRU position");
        result[1].Should().Be(Path.GetFullPath(file3), "previously top file should shift down");
        result[2].Should().Be(Path.GetFullPath(file2), "oldest file should remain at bottom");
    }

    /// <summary>
    /// Verifies that the recent files list is capped at 10 items,
    /// with the oldest entry trimmed when the limit is exceeded.
    /// </summary>
    [Fact]
    public void AddRecentFile_Max10Items_TrimsOldest()
    {
        // Arrange — add 11 files
        for (int i = 1; i <= 11; i++)
        {
            _sut.AddRecentFile(Path.Combine(_tempDir, $"Project{i:D2}.scaffoldx"));
        }

        // Act
        var result = _sut.GetRecentFiles();

        // Assert
        result.Should().HaveCount(10, "list should be capped at 10 items");
        result[0].Should().Contain("Project11", "newest file should be at the top");
        result.Should().NotContain(f => f.Contains("Project01"),
            "oldest file (Project01) should be trimmed");
    }

    /// <summary>
    /// Verifies that ClearRecentFiles empties the entire list.
    /// </summary>
    [Fact]
    public void ClearRecentFiles_EmptiesList()
    {
        // Arrange
        _sut.AddRecentFile(Path.Combine(_tempDir, "ProjectA.scaffoldx"));
        _sut.AddRecentFile(Path.Combine(_tempDir, "ProjectB.scaffoldx"));
        _sut.GetRecentFiles().Should().HaveCount(2, "precondition: list should have 2 items");

        // Act
        _sut.ClearRecentFiles();
        var result = _sut.GetRecentFiles();

        // Assert
        result.Should().BeEmpty("list should be empty after clearing");
    }

    /// <summary>
    /// Verifies that adding multiple distinct files preserves MRU order (most recent first).
    /// </summary>
    [Fact]
    public void AddRecentFile_MultipleFiles_PreservesMruOrder()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "Alpha.scaffoldx");
        var file2 = Path.Combine(_tempDir, "Beta.scaffoldx");
        var file3 = Path.Combine(_tempDir, "Gamma.scaffoldx");

        // Act
        _sut.AddRecentFile(file1);
        _sut.AddRecentFile(file2);
        _sut.AddRecentFile(file3);

        var result = _sut.GetRecentFiles();

        // Assert — most recently added should be first
        result.Should().HaveCount(3);
        result[0].Should().Contain("Gamma");
        result[1].Should().Contain("Beta");
        result[2].Should().Contain("Alpha");
    }

    /// <summary>
    /// Verifies that ClearRecentFiles followed by AddRecentFile starts a fresh list.
    /// </summary>
    [Fact]
    public void ClearRecentFiles_ThenAdd_StartsFresh()
    {
        // Arrange
        _sut.AddRecentFile(Path.Combine(_tempDir, "Old.scaffoldx"));
        _sut.ClearRecentFiles();

        // Act
        _sut.AddRecentFile(Path.Combine(_tempDir, "New.scaffoldx"));
        var result = _sut.GetRecentFiles();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Contain("New");
        result.Should().NotContain(f => f.Contains("Old"),
            "cleared files should not reappear");
    }

    /// <summary>
    /// Cleans up the temporary directory and persisted state after each test.
    /// </summary>
    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { /* best-effort */ }
        }

        // Clean up persisted recent_files.json
        var recentFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_files.json");
        if (File.Exists(recentFilesPath))
        {
            try { File.Delete(recentFilesPath); }
            catch { /* best-effort */ }
        }
    }
}
