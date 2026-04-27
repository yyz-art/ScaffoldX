using Serilog;

namespace ScaffoldX.Core.FileGeneration;

/// <summary>
/// 负责创建目录结构，安全处理已存在目录的情况。
/// </summary>
public class DirectoryCreator
{
    private readonly ILogger _logger = Log.ForContext<DirectoryCreator>();

    // ── 公共 API ──────────────────────────────────────────────────────────

    /// <summary>
    /// 确保指定目录存在；若不存在则递归创建，若已存在则跳过。
    /// </summary>
    /// <param name="directoryPath">要创建的目录绝对路径。</param>
    /// <exception cref="IOException">创建目录时发生 I/O 错误时抛出。</exception>
    /// <exception cref="UnauthorizedAccessException">没有权限创建目录时抛出。</exception>
    public void EnsureExists(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            _logger.Debug("目录已存在，跳过创建: {Path}", directoryPath);
            return;
        }

        try
        {
            Directory.CreateDirectory(directoryPath);
            _logger.Debug("已创建目录: {Path}", directoryPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "创建目录失败: {Path}", directoryPath);
            throw;
        }
    }

    /// <summary>
    /// 根据文件树节点递归创建所有目录。
    /// 仅处理 <see cref="NodeType.Folder"/> 类型的节点。
    /// </summary>
    /// <param name="rootPath">输出根目录的绝对路径。</param>
    /// <param name="rootNode">文件树根节点。</param>
    public void CreateFromTree(string rootPath, FileTreeNode rootNode)
    {
        EnsureExists(rootPath);
        CreateChildDirectories(rootPath, rootNode);
    }

    /// <summary>
    /// 异步版本：根据文件树节点递归创建所有目录。
    /// </summary>
    /// <param name="rootPath">输出根目录的绝对路径。</param>
    /// <param name="rootNode">文件树根节点。</param>
    public Task CreateFromTreeAsync(string rootPath, FileTreeNode rootNode)
    {
        // Directory.CreateDirectory 本身是同步的，包装为 Task 以统一异步接口
        return Task.Run(() => CreateFromTree(rootPath, rootNode));
    }

    /// <summary>
    /// 批量确保多个目录存在。
    /// </summary>
    /// <param name="directoryPaths">目录路径集合。</param>
    public void EnsureAllExist(IEnumerable<string> directoryPaths)
    {
        foreach (var path in directoryPaths)
        {
            EnsureExists(path);
        }
    }

    // ── 私有方法 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 递归遍历文件树，为所有 Folder 节点创建对应目录。
    /// </summary>
    private void CreateChildDirectories(string rootPath, FileTreeNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.NodeType != NodeType.Folder)
            {
                continue;
            }

            var fullPath = Path.Combine(rootPath, child.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            EnsureExists(fullPath);
            CreateChildDirectories(rootPath, child);
        }
    }
}
