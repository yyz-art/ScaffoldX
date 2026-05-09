using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Plugins;

namespace ScaffoldX.Plugin.Scaffold.Services;

public interface ITemplateEngine
{
    string Render(string template, Dictionary<string, object> variables);
}

public interface IValidationService
{
    ValidationResult ValidateProjectName(string name);
    ValidationResult ValidateOutputPath(string path, string projectName);
    string ToPascalCase(string input);
    ValidationResult ValidateIpAddress(string ip);
    ValidationResult ValidatePort(int port);
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string ErrorMessage { get; }

    public ValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Valid { get; } = new(true);
    public static ValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}

public class ValidationService : IValidationService
{
    private static readonly Regex ProjectNameRegex = new(@"^[A-Za-z][A-Za-z0-9_]{0,49}$", RegexOptions.Compiled);

    public ValidationResult ValidateProjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Invalid("项目名称不能为空。");
        if (!ProjectNameRegex.IsMatch(name))
            return ValidationResult.Invalid("项目名称须以字母开头，仅含字母、数字、下划线，且长度不超过 50 个字符。");
        return ValidationResult.Valid;
    }

    public ValidationResult ValidateOutputPath(string path, string projectName)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ValidationResult.Invalid("输出路径不能为空。");
        if (!Directory.Exists(path))
            return ValidationResult.Invalid($"输出路径不存在：{path}");
        var targetDir = Path.Combine(path, projectName);
        if (Directory.Exists(targetDir))
            return ValidationResult.Invalid($"目标目录已存在：{targetDir}");
        return ValidationResult.Valid;
    }

    public string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var segments = input.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Text.StringBuilder(input.Length);

        foreach (var segment in segments)
        {
            if (segment.Length == 0) continue;
            result.Append(char.ToUpperInvariant(segment[0]));
            if (segment.Length > 1)
            {
                var remainder = segment[1..];
                result.Append(remainder == remainder.ToUpperInvariant()
                    ? remainder.ToLowerInvariant()
                    : remainder);
            }
        }

        return result.ToString();
    }

    public ValidationResult ValidateIpAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return ValidationResult.Invalid("IP 地址不能为空。");
        if (!System.Net.IPAddress.TryParse(ip, out var parsed) ||
            parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return ValidationResult.Invalid($"无效的 IPv4 地址格式：{ip}");
        return ValidationResult.Valid;
    }

    public ValidationResult ValidatePort(int port)
    {
        if (port < 1 || port > 65535)
            return ValidationResult.Invalid($"端口号须在 1–65535 范围内，当前值：{port}");
        return ValidationResult.Valid;
    }
}
