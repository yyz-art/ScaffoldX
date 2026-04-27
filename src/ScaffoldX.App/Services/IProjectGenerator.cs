using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// 项目生成器契约，负责根据 <see cref="ProjectConfig"/> 生成完整的工业上位机项目骨架。
/// </summary>
public interface IProjectGenerator
{
    /// <summary>
    /// 异步执行项目生成流程：验证配置 → 构建变量上下文 → 选择模板 → 渲染模板 → 后处理 → 记录历史。
    /// </summary>
    /// <param name="config">项目生成配置。</param>
    /// <param name="progress">进度回调，用于向 UI 层报告当前步骤和百分比。</param>
    /// <returns>生成结果，包含成功/失败状态、输出路径、文件数量及耗时。</returns>
    Task<GenerationResult> GenerateAsync(ProjectConfig config, IProgress<GenerationProgress> progress);
}
