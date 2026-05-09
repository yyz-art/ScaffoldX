namespace ScaffoldX.Abstractions.Config;

public sealed class SystemConfigSection : IConfigSection
{
    public bool EnableUserManagement { get; set; }
    public bool EnableRolePermission { get; set; }
    public bool EnableSystemLog { get; set; }
    public bool EnableThemeSwitcher { get; set; }
    public string DatabaseType { get; set; } = "SQLite";
    public bool EnableLoginWindow { get; set; }
    public bool EnableCrossPlatform { get; set; }
    public bool ForcePasswordChange { get; set; }

    public string SectionId => "Scaffold.System";
    public string DisplayName => "系统配置";

    public Dictionary<string, object> GetDefaults()
    {
        var selectedModules = new List<string>();
        if (EnableUserManagement) selectedModules.Add("UserManagement");
        if (EnableRolePermission) selectedModules.Add("RolePermission");
        if (EnableSystemLog) selectedModules.Add("SystemLog");
        if (EnableThemeSwitcher) selectedModules.Add("ThemeSwitcher");

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["EnableUserManagement"] = EnableUserManagement,
            ["EnableRolePermission"] = EnableRolePermission,
            ["EnableSystemLog"] = EnableSystemLog,
            ["EnableThemeSwitcher"] = EnableThemeSwitcher,
            ["DatabaseType"] = DatabaseType,
            ["EnableLoginWindow"] = EnableLoginWindow,
            ["EnableCrossPlatform"] = EnableCrossPlatform,
            ["ForcePasswordChange"] = ForcePasswordChange,
            ["SelectedModules"] = selectedModules,
            ["HasAnySystemModule"] = EnableUserManagement || EnableRolePermission || EnableSystemLog || EnableThemeSwitcher,
        };
    }

    public IReadOnlyList<ValidationError> Validate() => [];
}
