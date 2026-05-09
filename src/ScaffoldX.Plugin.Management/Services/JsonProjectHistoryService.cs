using System.IO;
using System.Text.Json;
using ScaffoldX.Plugin.Management.Models;

namespace ScaffoldX.Plugin.Management.Services;

public sealed class JsonProjectHistoryService : IProjectHistoryService
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonProjectHistoryService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScaffoldX", "history.json"))
    {
    }

    public JsonProjectHistoryService(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null) Directory.CreateDirectory(dir);
    }

    public async Task AddRecordAsync(ProjectHistoryRecord record)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadInternalAsync();
            records.Add(record);
            await WriteInternalAsync(records);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<ProjectHistoryRecord>> GetAllRecordsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await ReadInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteRecordAsync(string outputPath)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadInternalAsync();
            records.RemoveAll(r => r.OutputPath == outputPath);
            await WriteInternalAsync(records);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<ProjectHistoryRecord>> GetRecentRecordsAsync(int count)
    {
        await _lock.WaitAsync();
        try
        {
            var records = await ReadInternalAsync();
            return records
                .OrderByDescending(r => r.LastOpenedAt != default ? r.LastOpenedAt : r.CreatedAt)
                .Take(count)
                .ToList()
                .AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await WriteInternalAsync([]);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<ProjectHistoryRecord>> ReadInternalAsync()
    {
        if (!File.Exists(_filePath)) return [];
        var json = await File.ReadAllTextAsync(_filePath);
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<ProjectHistoryRecord>>(json, _jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task WriteInternalAsync(List<ProjectHistoryRecord> records)
    {
        var json = JsonSerializer.Serialize(records, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
