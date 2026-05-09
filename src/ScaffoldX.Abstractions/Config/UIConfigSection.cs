namespace ScaffoldX.Abstractions.Config;

public sealed class UIConfigSection : IConfigSection
{
    public string NavigationStyle { get; set; } = "LeftSidebar";
    public string DefaultTheme { get; set; } = "IndustrialDark";
    public string DefaultLanguage { get; set; } = "zh-CN";
    public bool EnableLocalization { get; set; }

    public string SectionId => "Scaffold.UI";
    public string DisplayName => "UI 配置";

    public Dictionary<string, object> GetDefaults()
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["NavigationStyle"] = NavigationStyle,
            ["DefaultTheme"] = DefaultTheme,
            ["DefaultLanguage"] = DefaultLanguage,
            ["EnableLocalization"] = EnableLocalization,
        };
    }

    public IReadOnlyList<ValidationError> Validate() => [];
}
