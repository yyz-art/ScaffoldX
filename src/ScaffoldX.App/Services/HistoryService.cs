using System.IO;
using System.Text.Json;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// <see cref="IHistoryService"/> 的默认实现，将历史记录以 JSON 格式持久化到
/// %APPDATA%/ScaffoldX/history.json。
/// </summary>
public class HistoryService : IHistoryService
{
    private static readonly string _historyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScaffoldX",
        "history.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>
    /// 从持久化存储异步加载全部历史记录，按创建时间降序排列。
    /// 文件不存在时返回空列表。
    /// </summary>
    /// <returns>历史记录列表。</returns>
    public async Task<List<ProjectHistory>> LoadAsync()
    {
        if (!File.Exists(_historyFilePath))
        {
            return new List<ProjectHistory>();
        }

        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            string json = await File.ReadAllTextAsync(_historyFilePath).ConfigureAwait(false);
            var list = JsonSerializer.Deserialize<List<ProjectHistory>>(json, _jsonOptions)
                       ?? new List<ProjectHistory>();
            return list.OrderByDescending(h => h.CreatedAt).ToList();
        }
        catch (JsonException)
        {
            return new List<ProjectHistory>();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// 将新的历史条目追加保存到持久化存储。
    /// </summary>
    /// <param name="entry">要保存的历史条目。</param>
    public async Task SaveAsync(ProjectHistory entry)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            List<ProjectHistory> list = await LoadInternalAsync().ConfigureAwait(false);
            list.Add(entry);
            await WriteAsync(list).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// 按项目名称删除对应的历史条目。若不存在则静默忽略。
    /// </summary>
    /// <param name="projectName">要删除的项目名称。</param>
    public async Task DeleteAsync(string projectName)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            List<ProjectHistory> list = await LoadInternalAsync().ConfigureAwait(false);
            int removed = list.RemoveAll(h =>
                string.Equals(h.ProjectName, projectName, StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                await WriteAsync(list).ConfigureAwait(false);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// 更新已存在的历史条目（按 ProjectName 匹配）。若不存在则追加。
    /// </summary>
    /// <param name="entry">包含更新内容的历史条目。</param>
    public async Task UpdateAsync(ProjectHistory entry)
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            List<ProjectHistory> list = await LoadInternalAsync().ConfigureAwait(false);
            int index = list.FindIndex(h =>
                string.Equals(h.ProjectName, entry.ProjectName, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                list[index] = entry;
            }
            else
            {
                list.Add(entry);
            }

            await WriteAsync(list).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// 不加锁地从文件读取历史列表，供内部已持锁的方法调用。
    /// </summary>
    private static async Task<List<ProjectHistory>> LoadInternalAsync()
    {
        if (!File.Exists(_historyFilePath))
        {
            return new List<ProjectHistory>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(_historyFilePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<ProjectHistory>>(json, _jsonOptions)
                   ?? new List<ProjectHistory>();
        }
        catch (JsonException)
        {
            return new List<ProjectHistory>();
        }
    }

    /// <summary>
    /// 将历史列表序列化并写入文件，目录不存在时自动创建。
    /// </summary>
    /// <param name="list">要写入的历史列表。</param>
    private static async Task WriteAsync(List<ProjectHistory> list)
    {
        string? dir = Path.GetDirectoryName(_historyFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(list, _jsonOptions);
        await File.WriteAllTextAsync(_historyFilePath, json).ConfigureAwait(false);
    }
}
