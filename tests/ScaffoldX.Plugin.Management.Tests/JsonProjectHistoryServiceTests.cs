using System.Text.Json;
using ScaffoldX.Plugin.Management.Models;
using ScaffoldX.Plugin.Management.Services;
using Xunit;

namespace ScaffoldX.Plugin.Management.Tests;

public class JsonProjectHistoryServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;
    private readonly JsonProjectHistoryService _service;

    public JsonProjectHistoryServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_JsonTest_{Guid.NewGuid():N}");
        _filePath = Path.Combine(_tempDir, "history.json");
        _service = new JsonProjectHistoryService(_filePath);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void 构造函数_目录不存在_自动创建目录()
    {
        var newDir = Path.Combine(_tempDir, "sub", "deep");
        var newFile = Path.Combine(newDir, "test.json");
        Assert.False(Directory.Exists(newDir));

        var svc = new JsonProjectHistoryService(newFile);

        Assert.True(Directory.Exists(newDir));
    }

    [Fact]
    public async Task 文件不存在_GetAllRecordsAsync返回空列表()
    {
        Assert.False(File.Exists(_filePath));

        var records = await _service.GetAllRecordsAsync();

        Assert.Empty(records);
    }

    [Fact]
    public async Task AddRecordAsync_持久化到文件()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));

        Assert.True(File.Exists(_filePath));
        var json = await File.ReadAllTextAsync(_filePath);
        var deserialized = JsonSerializer.Deserialize<List<ProjectHistoryRecord>>(json);
        Assert.Single(deserialized!);
        Assert.Equal("Proj1", deserialized![0].ProjectName);
    }

    [Fact]
    public async Task 多次AddRecordAsync_文件包含所有记录()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));
        await _service.AddRecordAsync(CreateRecord("Proj2", "Web", @"C:\P2"));

        var json = await File.ReadAllTextAsync(_filePath);
        var deserialized = JsonSerializer.Deserialize<List<ProjectHistoryRecord>>(json);
        Assert.Equal(2, deserialized!.Count);
    }

    [Fact]
    public async Task DeleteRecordAsync_持久化删除到文件()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));
        await _service.AddRecordAsync(CreateRecord("Proj2", "Web", @"C:\P2"));

        await _service.DeleteRecordAsync(@"C:\P1");

        var json = await File.ReadAllTextAsync(_filePath);
        var deserialized = JsonSerializer.Deserialize<List<ProjectHistoryRecord>>(json);
        Assert.Single(deserialized!);
        Assert.Equal("Proj2", deserialized![0].ProjectName);
    }

    [Fact]
    public async Task ClearAllAsync_持久化清空到文件()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));

        await _service.ClearAllAsync();

        var json = await File.ReadAllTextAsync(_filePath);
        var deserialized = JsonSerializer.Deserialize<List<ProjectHistoryRecord>>(json);
        Assert.Empty(deserialized!);
    }

    [Fact]
    public async Task GetRecentRecordsAsync_按LastOpenedAt降序返回()
    {
        var now = DateTime.Now;
        await _service.AddRecordAsync(CreateRecord("Old", "WPF", @"C:\Old", now, now.AddDays(-2)));
        await _service.AddRecordAsync(CreateRecord("New", "Web", @"C:\New", now, now));
        await _service.AddRecordAsync(CreateRecord("Mid", "Console", @"C:\Mid", now, now.AddDays(-1)));

        var recent = await _service.GetRecentRecordsAsync(2);

        Assert.Equal(2, recent.Count);
        Assert.Equal("New", recent[0].ProjectName);
        Assert.Equal("Mid", recent[1].ProjectName);
    }

    [Fact]
    public async Task GetRecentRecordsAsync_LastOpenedAt为默认值_按CreatedAt排序()
    {
        var now = DateTime.Now;
        await _service.AddRecordAsync(CreateRecord("Newest", "WPF", @"C:\N1", now, DateTime.MinValue));
        await _service.AddRecordAsync(CreateRecord("Oldest", "Web", @"C:\N2", now.AddDays(-5), DateTime.MinValue));

        var recent = await _service.GetRecentRecordsAsync(2);

        Assert.Equal("Newest", recent[0].ProjectName);
        Assert.Equal("Oldest", recent[1].ProjectName);
    }

    [Fact]
    public async Task 并发写入_不丢数据()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _service.AddRecordAsync(CreateRecord($"Proj{i}", "WPF", $"C:\\P{i}")));
        await Task.WhenAll(tasks);

        var records = await _service.GetAllRecordsAsync();
        Assert.Equal(10, records.Count);
    }

    [Fact]
    public async Task 空文件_读取返回空列表()
    {
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(_filePath, "");

        var records = await _service.GetAllRecordsAsync();
        Assert.Empty(records);
    }

    [Fact]
    public async Task 损坏JSON_读取返回空列表()
    {
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(_filePath, "{ invalid json }}}");

        var records = await _service.GetAllRecordsAsync();
        Assert.Empty(records);
    }

    [Fact]
    public async Task 持久化_重新创建服务实例_可读取之前数据()
    {
        await _service.AddRecordAsync(CreateRecord("Persist", "WPF", @"C:\Persist"));

        var newService = new JsonProjectHistoryService(_filePath);
        var records = await newService.GetAllRecordsAsync();

        Assert.Single(records);
        Assert.Equal("Persist", records[0].ProjectName);
    }

    private static ProjectHistoryRecord CreateRecord(
        string name, string type, string path,
        DateTime? createdAt = null, DateTime? lastOpenedAt = null)
    {
        return new ProjectHistoryRecord
        {
            ProjectName = name,
            ProjectType = type,
            OutputPath = path,
            CreatedAt = createdAt ?? DateTime.Now,
            LastOpenedAt = lastOpenedAt ?? DateTime.Now,
            Tags = []
        };
    }
}
