using ScaffoldX.Core.Models;

namespace ScaffoldX.Core.FileGeneration;

/// <summary>
/// 根据项目配置构建将要生成的文件树。
/// </summary>
public interface IFileTreeBuilder
{
    /// <summary>
    /// 根据项目配置构建完整的文件树。
    /// </summary>
    FileTreeNode BuildTree(ProjectConfig config);
}
