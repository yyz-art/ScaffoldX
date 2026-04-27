namespace ScaffoldX.App.Models;

/// <summary>
/// 项目生成历史记录条目，持久化到 %APPDATA%/ScaffoldX/history.json。
/// </summary>
public class ProjectHistory
{
    /// <summary>项目名称，作为历史记录的唯一标识键。</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>项目类型："Collection" | "Vision" | "System"。</summary>
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>生成输出目录的绝对路径。</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>目标框架标识，如 "net8.0-windows"。</summary>
    public string TargetFramework { get; set; } = string.Empty;

    /// <summary>UI 框架："WPF" 或 "Avalonia"。</summary>
    public string UIFramework { get; set; } = string.Empty;

    /// <summary>项目生成完成的 UTC 时间。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>生成时使用的完整 <see cref="ProjectConfig"/> 序列化 JSON，用于重放或导出。</summary>
    public string ConfigJson { get; set; } = string.Empty;
}
