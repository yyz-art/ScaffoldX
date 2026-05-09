using ScaffoldX.Plugin.Management.Models;

namespace ScaffoldX.Plugin.Management.Services;

public interface IProjectHistoryService
{
    Task AddRecordAsync(ProjectHistoryRecord record);
    Task<IReadOnlyList<ProjectHistoryRecord>> GetAllRecordsAsync();
    Task DeleteRecordAsync(string outputPath);
    Task<IReadOnlyList<ProjectHistoryRecord>> GetRecentRecordsAsync(int count);
    Task ClearAllAsync();
}
