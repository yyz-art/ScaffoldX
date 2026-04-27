using Serilog;

namespace ScaffoldX.Core.FileGeneration;

/// <summary>
/// 负责将渲染后的内容写入文件系统。
/// .cs 文件使用 UTF-8 with BOM，其他文件使用 UTF-8 without BOM。
/// </summary>
public class FileWriter
{
    private readonly ILogger _logger = Log.ForContext<FileWriter>();

    private static readonly System.Text.Encoding Utf8WithBom    = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private static readonly System.Text.Encoding Utf8WithoutBom = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // ── 公共 API ──────────────────────────────────────────────────────────

    /// <summary>
    /// 将内容异步写入指定文件路径。
    /// 根据文件扩展名自动选择编码：.cs 文件使用 UTF-8 BOM，其余使用 UTF-8 无 BOM。
    /// 若目标目录不存在，会自动创建。
    /// </summary>
    /// <param name="filePath">目标文件的绝对路径。</param>
    /// <param name="content">要写入的文本内容。</param>
    /// <param name="overwrite">
    /// 若为 <c>true</c>（默认），文件已存在时覆盖；
    /// 若为 <c>false</c>，文件已存在时跳过写入。
    /// </param>
    /// <returns>实际是否写入了文件（<c>false</c> 表示因 overwrite=false 而跳过）。</returns>
    /// <exception cref="IOException">写入文件时发生 I/O 错误时抛出。</exception>
    /// <exception cref="UnauthorizedAccessException">没有写入权限时抛出。</exception>
    public async Task<bool> WriteAsync(string filePath, string content, bool overwrite = true)
    {
        if (!overwrite && File.Exists(filePath))
        {
            _logger.Debug("文件已存在，跳过写入: {FilePath}", filePath);
            return false;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.Debug("已创建目录: {Directory}", directory);
        }

        var encoding = SelectEncoding(filePath);

        try
        {
            await File.WriteAllTextAsync(filePath, content, encoding);
            _logger.Debug("已写入文件 [{Encoding}]: {FilePath}", encoding.EncodingName, filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "写入文件失败: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// 批量异步写入多个文件。
    /// </summary>
    /// <param name="files">文件路径与内容的键值对集合。</param>
    /// <param name="overwrite">是否覆盖已存在的文件。</param>
    /// <returns>实际写入的文件数量。</returns>
    public async Task<int> WriteManyAsync(
        IEnumerable<KeyValuePair<string, string>> files,
        bool overwrite = true)
    {
        var count = 0;

        foreach (var (filePath, content) in files)
        {
            var written = await WriteAsync(filePath, content, overwrite);
            if (written)
            {
                count++;
            }
        }

        _logger.Information("批量写入完成，共写入 {Count} 个文件", count);
        return count;
    }

    /// <summary>
    /// 将二进制内容异步写入指定文件路径（用于图标、图片等二进制资源）。
    /// </summary>
    /// <param name="filePath">目标文件的绝对路径。</param>
    /// <param name="data">要写入的字节数组。</param>
    /// <param name="overwrite">是否覆盖已存在的文件。</param>
    /// <returns>实际是否写入了文件。</returns>
    public async Task<bool> WriteBinaryAsync(string filePath, byte[] data, bool overwrite = true)
    {
        if (!overwrite && File.Exists(filePath))
        {
            _logger.Debug("二进制文件已存在，跳过写入: {FilePath}", filePath);
            return false;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            await File.WriteAllBytesAsync(filePath, data);
            _logger.Debug("已写入二进制文件: {FilePath} ({Size} bytes)", filePath, data.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "写入二进制文件失败: {FilePath}", filePath);
            throw;
        }
    }

    // ── 私有方法 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 根据文件扩展名选择合适的编码。
    /// .cs 文件使用 UTF-8 with BOM（Visual Studio 默认），其余使用 UTF-8 without BOM。
    /// </summary>
    private static System.Text.Encoding SelectEncoding(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".cs" => Utf8WithBom,
            _     => Utf8WithoutBom,
        };
    }
}
