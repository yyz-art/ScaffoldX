using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using ScaffoldX.Core.TemplateProcessing;

namespace ScaffoldX.App.Services;

/// <summary>
/// <see cref="IValidationService"/> 的默认实现，提供项目名称、路径、IP 地址和端口的校验逻辑。
/// </summary>
public class ValidationService : IValidationService
{
    private static readonly Regex _projectNameRegex =
        new(@"^[A-Za-z][A-Za-z0-9_]{0,49}$", RegexOptions.Compiled);

    /// <summary>
    /// 验证项目名称是否符合规范（^[A-Za-z][A-Za-z0-9_]{0,49}$）。
    /// </summary>
    /// <param name="name">待验证的项目名称。</param>
    /// <returns>验证结果。</returns>
    public ValidationResult ValidateProjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Invalid("项目名称不能为空。");
        }

        if (!_projectNameRegex.IsMatch(name))
        {
            return ValidationResult.Invalid(
                "项目名称须以字母开头，仅含字母、数字、下划线，且长度不超过 50 个字符。");
        }

        return ValidationResult.Valid;
    }

    /// <summary>
    /// 验证输出路径的存在性、可写性，以及目标子目录是否已存在同名文件夹。
    /// </summary>
    /// <param name="path">输出根目录路径。</param>
    /// <param name="projectName">项目名称，用于检测同名子目录冲突。</param>
    /// <returns>验证结果。</returns>
    public ValidationResult ValidateOutputPath(string path, string projectName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Invalid("输出路径不能为空。");
        }

        if (!Directory.Exists(path))
        {
            return ValidationResult.Invalid($"输出路径不存在：{path}");
        }

        string targetDir = Path.Combine(path, projectName);
        if (Directory.Exists(targetDir))
        {
            return ValidationResult.Invalid($"目标目录已存在，请更换项目名称或输出路径：{targetDir}");
        }

        try
        {
            string probe = Path.Combine(path, $".scaffoldx_write_probe_{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            return ValidationResult.Invalid($"输出路径不可写：{ex.Message}");
        }

        return ValidationResult.Valid;
    }

    /// <summary>
    /// 将任意字符串转换为 PascalCase，委托给 <see cref="VariableResolver.ToPascalCase"/>。
    /// </summary>
    /// <param name="input">原始输入字符串。</param>
    /// <returns>PascalCase 格式的字符串。</returns>
    public string ToPascalCase(string input) => VariableResolver.ToPascalCase(input);

    /// <summary>
    /// 验证 IPv4 地址格式是否合法。
    /// </summary>
    /// <param name="ip">待验证的 IP 地址字符串。</param>
    /// <returns>验证结果。</returns>
    public ValidationResult ValidateIpAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return ValidationResult.Invalid("IP 地址不能为空。");
        }

        if (!IPAddress.TryParse(ip, out IPAddress? parsed) ||
            parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return ValidationResult.Invalid($"无效的 IPv4 地址格式：{ip}");
        }

        return ValidationResult.Valid;
    }

    /// <summary>
    /// 验证端口号是否在有效范围（1–65535）内。
    /// </summary>
    /// <param name="port">待验证的端口号。</param>
    /// <returns>验证结果。</returns>
    public ValidationResult ValidatePort(int port)
    {
        if (port < 1 || port > 65535)
        {
            return ValidationResult.Invalid($"端口号须在 1–65535 范围内，当前值：{port}");
        }

        return ValidationResult.Valid;
    }
}
