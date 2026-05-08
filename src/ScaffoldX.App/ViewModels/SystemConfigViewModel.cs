using Prism.Mvvm;
using ScaffoldX.Core.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 系统类配置 ViewModel：功能模块、登录窗口、密码策略。
/// 属性直接读写 ProjectConfig，无需手动字段拷贝。
/// </summary>
public class SystemConfigViewModel : BindableBase
{
    /// <summary>
    /// 初始化系统类配置，构建模块选项列表。
    /// </summary>
    public SystemConfigViewModel() : this(new ProjectConfig
    {
        EnableLoginWindow = true,
        EnableUserManagement = true,
        EnableRolePermission = true
    }) { }

    /// <summary>
    /// 初始化系统类配置，绑定到指定 ProjectConfig。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public SystemConfigViewModel(ProjectConfig config)
    {
        Config = config;

        ModuleOptions = new List<ModuleOption>
        {
            new() { Key = "UserManagement", DisplayName = "用户管理",   IsSelected = config.EnableUserManagement },
            new() { Key = "RolePermission", DisplayName = "角色权限",   IsSelected = config.EnableRolePermission },
            new() { Key = "SystemLog",      DisplayName = "审计日志",   IsSelected = config.EnableSystemLog },
            new() { Key = "ThemeSwitcher",  DisplayName = "主题切换",   IsSelected = config.EnableThemeSwitcher },
        };

        foreach (var m in ModuleOptions)
            m.PropertyChanged += (_, _) => SyncModuleToConfig(m);
    }

    // ── 属性 ────────────────────────────────────────────────────────────────────

    /// <summary>关联的项目配置对象，所有属性直接读写此对象。</summary>
    public ProjectConfig Config { get; private set; }

    /// <summary>
    /// 初始化或重新绑定到指定 ProjectConfig，并同步 UI 状态。
    /// </summary>
    /// <param name="config">项目配置对象。</param>
    public void Initialize(ProjectConfig config)
    {
        Config = config;
        SyncModuleOptionsFromConfig();
        RaisePropertyChanged(nameof(EnableLoginWindow));
        RaisePropertyChanged(nameof(ForcePasswordChange));
    }

    /// <summary>功能模块选项列表（CheckBox 绑定）。</summary>
    public List<ModuleOption> ModuleOptions { get; }

    /// <summary>是否生成独立登录窗口。</summary>
    public bool EnableLoginWindow
    {
        get => Config.EnableLoginWindow;
        set
        {
            Config.EnableLoginWindow = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>是否强制首次登录修改密码。</summary>
    public bool ForcePasswordChange
    {
        get => Config.ForcePasswordChange;
        set
        {
            Config.ForcePasswordChange = value;
            RaisePropertyChanged();
        }
    }

    // ── 方法 ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 获取已选功能模块的 Key 列表。
    /// </summary>
    public List<string> GetSelectedModules()
        => ModuleOptions.Where(m => m.IsSelected).Select(m => m.Key).ToList();

    /// <summary>
    /// 重置系统类配置到默认值。
    /// </summary>
    public void Reset()
    {
        EnableLoginWindow = true;
        ForcePasswordChange = false;
        foreach (var m in ModuleOptions)
            m.IsSelected = m.Key is "UserManagement" or "RolePermission";
    }

    // ── 内部同步 ────────────────────────────────────────────────────────────────

    private void SyncModuleToConfig(ModuleOption module)
    {
        Config.SetModule(module.Key, module.IsSelected);
    }

    private void SyncModuleOptionsFromConfig()
    {
        foreach (var m in ModuleOptions)
        {
            m.IsSelected = m.Key switch
            {
                "UserManagement" => Config.EnableUserManagement,
                "RolePermission" => Config.EnableRolePermission,
                "SystemLog" => Config.EnableSystemLog,
                "ThemeSwitcher" => Config.EnableThemeSwitcher,
                _ => m.IsSelected
            };
        }
    }
}
