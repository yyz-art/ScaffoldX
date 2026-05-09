namespace ScaffoldX.Abstractions.Config;

public sealed class ScaffoldConfigSection : IConfigSection
{
    public string ProjectName { get; set; } = string.Empty;
    public string NamespacePrefix { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = "net10.0-windows";
    public string UIFramework { get; set; } = "WPF";
    public string OutputDirectory { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string SectionId => "Scaffold";
    public string DisplayName => "基础配置";

    public Dictionary<string, object> GetDefaults()
    {
        var projectNamePascal = ToPascalCase(ProjectName);
        var namespacePrefix = string.IsNullOrWhiteSpace(NamespacePrefix)
            ? projectNamePascal
            : NamespacePrefix;
        var isAvalonia = UIFramework.Equals("Avalonia", StringComparison.OrdinalIgnoreCase);

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["ScaffoldXVersion"] = "1.0.0",
            ["ProjectName"] = projectNamePascal,
            ["NamespacePrefix"] = namespacePrefix,
            ["TargetFramework"] = TargetFramework,
            ["TargetFrameworkShort"] = TargetFramework.Replace("-windows", ""),
            ["UIFramework"] = UIFramework,
            ["Author"] = Author,
            ["Company"] = Company,
            ["Description"] = Description,
            ["ProjectDescription"] = Description,
            ["Year"] = DateTime.Now.Year.ToString(),
            ["ProjectType"] = ProjectType,
            ["AppTitle"] = projectNamePascal,
            ["AppVersion"] = "1.0.0",
            ["IsWPF"] = !isAvalonia,
            ["IsAvalonia"] = isAvalonia,
            ["XamlExt"] = isAvalonia ? "axaml" : "xaml",
            ["XamlCodeBehindExt"] = isAvalonia ? ".axaml.cs" : ".xaml.cs",
            ["XamlNs"] = isAvalonia
                ? "xmlns=\"https://github.com/avaloniaui\""
                : "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
            ["XamlXNs"] = "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
            ["WindowBaseClass"] = "Window",
            ["UserControlBase"] = "UserControl",
            ["SolutionName"] = projectNamePascal,
            ["RootNamespace"] = namespacePrefix,
            ["AssemblyName"] = projectNamePascal,
        };
    }

    public IReadOnlyList<ValidationError> Validate()
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(ProjectName))
            errors.Add(new ValidationError { PropertyName = "ProjectName", ErrorMessage = "项目名称不能为空" });
        if (string.IsNullOrWhiteSpace(OutputDirectory))
            errors.Add(new ValidationError { PropertyName = "OutputDirectory", ErrorMessage = "输出目录不能为空" });
        return errors;
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
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
}
