using ScaffoldX.Plugin.Management.Models;
using ScaffoldX.Plugin.Management.Services;
using Xunit;

namespace ScaffoldX.Plugin.Management.Tests;

public class ProjectHistoryServiceContractTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonProjectHistoryService _service;

    public ProjectHistoryServiceContractTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new JsonProjectHistoryService(Path.Combine(_tempDir, "history.json"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task GetAllRecordsAsync_无记录_返回空列表()
    {
        var records = await _service.GetAllRecordsAsync();
        Assert.Empty(records);
    }

    [Fact]
    public async Task AddRecordAsync_添加后_GetAllRecordsAsync包含该记录()
    {
        var record = CreateRecord("TestProject", "WPF", @"C:\Projects\Test");
        await _service.AddRecordAsync(record);

        var records = await _service.GetAllRecordsAsync();
        Assert.Single(records);
        Assert.Equal("TestProject", records[0].ProjectName);
    }

    [Fact]
    public async Task AddRecordAsync_添加多条记录_按添加顺序返回()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));
        await _service.AddRecordAsync(CreateRecord("Proj2", "Web", @"C:\P2"));
        await _service.AddRecordAsync(CreateRecord("Proj3", "Console", @"C:\P3"));

        var records = await _service.GetAllRecordsAsync();
        Assert.Equal(3, records.Count);
        Assert.Equal("Proj1", records[0].ProjectName);
        Assert.Equal("Proj2", records[1].ProjectName);
        Assert.Equal("Proj3", records[2].ProjectName);
    }

    [Fact]
    public async Task DeleteRecordAsync_删除存在的记录_成功删除()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));
        await _service.AddRecordAsync(CreateRecord("Proj2", "Web", @"C:\P2"));

        await _service.DeleteRecordAsync(@"C:\P1");

        var records = await _service.GetAllRecordsAsync();
        Assert.Single(records);
        Assert.Equal("Proj2", records[0].ProjectName);
    }

    [Fact]
    public async Task DeleteRecordAsync_删除不存在的记录_无异常无变化()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));

        await _service.DeleteRecordAsync(@"C:\NotExist");

        var records = await _service.GetAllRecordsAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task GetRecentRecordsAsync_返回最近N条记录()
    {
        var now = DateTime.Now;
        await _service.AddRecordAsync(CreateRecord("Old", "WPF", @"C:\Old", now.AddDays(-3), now.AddDays(-3)));
        await _service.AddRecordAsync(CreateRecord("Mid", "Web", @"C:\Mid", now.AddDays(-2), now.AddDays(-1)));
        await _service.AddRecordAsync(CreateRecord("New", "Console", @"C:\New", now, now));

        var recent = await _service.GetRecentRecordsAsync(2);

        Assert.Equal(2, recent.Count);
        Assert.Equal("New", recent[0].ProjectName);
        Assert.Equal("Mid", recent[1].ProjectName);
    }

    [Fact]
    public async Task GetRecentRecordsAsync_请求数量超过总数_返回全部()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));
        await _service.AddRecordAsync(CreateRecord("Proj2", "Web", @"C:\P2"));

        var recent = await _service.GetRecentRecordsAsync(10);

        Assert.Equal(2, recent.Count);
    }

    [Fact]
    public async Task ClearAllAsync_清空后_无记录()
    {
        await _service.AddRecordAsync(CreateRecord("Proj1", "WPF", @"C:\P1"));
        await _service.AddRecordAsync(CreateRecord("Proj2", "Web", @"C:\P2"));

        await _service.ClearAllAsync();

        var records = await _service.GetAllRecordsAsync();
        Assert.Empty(records);
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
