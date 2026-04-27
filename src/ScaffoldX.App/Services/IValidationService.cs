namespace ScaffoldX.App.Services;

/// <summary>
/// 输入验证服务契约，提供项目名称、路径、IP、端口等字段的校验能力。
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// 验证项目名称是否符合规范（^[A-Za-z][A-Za-z0-9_]{0,49}$）。
    /// </summary>
    /// <param name="name">待验证的项目名称。</param>
    /// <returns>验证结果。</returns>
    ValidationResult ValidateProjectName(string name);

    /// <summary>
    /// 验证输出路径的存在性、可写性，以及目标子目录是否已存在同名文件夹。
    /// </summary>
    /// <param name="path">输出根目录路径。</param>
    /// <param name="projectName">项目名称，用于检测同名子目录冲突。</param>
    /// <returns>验证结果。</returns>
    ValidationResult ValidateOutputPath(string path, string projectName);

    /// <summary>
    /// 将任意字符串转换为 PascalCase，按下划线、连字符和空格分割，每段首字母大写其余小写。
    /// </summary>
    /// <param name="input">原始输入字符串。</param>
    /// <returns>PascalCase 格式的字符串。</returns>
    string ToPascalCase(string input);

    /// <summary>
    /// 验证 IPv4 地址格式是否合法。
    /// </summary>
    /// <param name="ip">待验证的 IP 地址字符串。</param>
    /// <returns>验证结果。</returns>
    ValidationResult ValidateIpAddress(string ip);

    /// <summary>
    /// 验证端口号是否在有效范围（1–65535）内。
    /// </summary>
    /// <param name="port">待验证的端口号。</param>
    /// <returns>验证结果。</returns>
    ValidationResult ValidatePort(int port);
}

/// <summary>
/// 单次字段验证的结果，包含是否通过及错误描述。
/// </summary>
public class ValidationResult
{
    /// <summary>验证是否通过。</summary>
    public bool IsValid { get; }

    /// <summary>验证失败时的错误描述；通过时为空字符串。</summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// 初始化 <see cref="ValidationResult"/>。
    /// </summary>
    /// <param name="isValid">是否通过验证。</param>
    /// <param name="errorMessage">错误描述，通过时传入空字符串。</param>
    public ValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>表示验证通过的静态实例。</summary>
    public static ValidationResult Valid { get; } = new(true);

    /// <summary>
    /// 构造一个验证失败的结果。
    /// </summary>
    /// <param name="errorMessage">错误描述。</param>
    /// <returns>失败的验证结果。</returns>
    public static ValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
