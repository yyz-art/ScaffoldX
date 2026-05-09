namespace ScaffoldX.Plugin.Management.Models;

public sealed class ProjectHistoryRecord
{
    public string ProjectName { get; init; } = string.Empty;
    public string ProjectType { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastOpenedAt { get; init; }
    public List<string> Tags { get; init; } = [];
}
