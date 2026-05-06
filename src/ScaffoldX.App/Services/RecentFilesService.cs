using System.IO;
using System.Text.Json;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// Persists a list of recently opened file paths to a JSON file in the application base directory.
/// Maintains a maximum of 10 entries with most-recent-first ordering.
/// </summary>
public class RecentFilesService : IRecentFilesService
{
    private const int MaxRecentFiles = 10;
    private static readonly string RecentFilesPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_files.json");

    private readonly ILogger _logger = Log.ForContext<RecentFilesService>();
    private readonly List<string> _files;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes the service by loading the persisted recent files list from disk.
    /// </summary>
    public RecentFilesService()
    {
        _files = LoadFromDisk();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRecentFiles()
    {
        lock (_lock)
        {
            return _files.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void AddRecentFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var fullPath = Path.GetFullPath(path);

        lock (_lock)
        {
            _files.Remove(fullPath);
            _files.Insert(0, fullPath);

            if (_files.Count > MaxRecentFiles)
                _files.RemoveRange(MaxRecentFiles, _files.Count - MaxRecentFiles);

            SaveToDisk();
        }

        _logger.Debug("已添加最近文件: {Path}", fullPath);
    }

    /// <inheritdoc />
    public void ClearRecentFiles()
    {
        lock (_lock)
        {
            _files.Clear();
            SaveToDisk();
        }

        _logger.Debug("已清空最近文件列表");
    }

    /// <summary>
    /// Loads the recent files list from the JSON file on disk.
    /// </summary>
    private static List<string> LoadFromDisk()
    {
        try
        {
            if (!File.Exists(RecentFilesPath))
                return [];

            var json = File.ReadAllText(RecentFilesPath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Persists the current recent files list to the JSON file on disk.
    /// </summary>
    private void SaveToDisk()
    {
        try
        {
            var json = JsonSerializer.Serialize(_files, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RecentFilesPath, json);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "保存最近文件列表失败: {Path}", RecentFilesPath);
        }
    }
}
