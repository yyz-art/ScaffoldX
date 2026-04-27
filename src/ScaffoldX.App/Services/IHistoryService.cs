using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// 项目生成历史记录服务契约，负责历史条目的持久化读写。
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// 从持久化存储异步加载全部历史记录，按创建时间降序排列。
    /// 文件不存在时返回空列表。
    /// </summary>
    /// <returns>历史记录列表。</returns>
    Task<List<ProjectHistory>> LoadAsync();

    /// <summary>
    /// 将新的历史条目追加保存到持久化存储。
    /// </summary>
    /// <param name="entry">要保存的历史条目。</param>
    Task SaveAsync(ProjectHistory entry);

    /// <summary>
    /// 按项目名称删除对应的历史条目。
    /// 若不存在则静默忽略。
    /// </summary>
    /// <param name="projectName">要删除的项目名称。</param>
    Task DeleteAsync(string projectName);

    /// <summary>
    /// 更新已存在的历史条目（按 <see cref="ProjectHistory.ProjectName"/> 匹配）。
    /// 若不存在则追加。
    /// </summary>
    /// <param name="entry">包含更新内容的历史条目。</param>
    Task UpdateAsync(ProjectHistory entry);
}
