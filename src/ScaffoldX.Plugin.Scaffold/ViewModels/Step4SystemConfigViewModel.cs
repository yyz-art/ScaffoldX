using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public enum ThemeMode
{
    Light,
    Dark,
    System
}

public enum AccentColor
{
    Blue,
    Red,
    Green,
    Purple,
    Orange,
    Teal
}

public sealed class Step4SystemConfigViewModel : INotifyPropertyChanged
{
    // 用户管理配置
    private bool _enableUserManagement = true;
    private bool _enableRoleBasedAccess = true;
    private bool _enableUserRegistration;
    private bool _enablePasswordPolicy = true;
    private int _passwordMinLength = 8;
    private bool _enableSessionTimeout = true;
    private string _sessionTimeoutMinutes = "60";

    // 主题配置
    private ThemeMode _selectedTheme = ThemeMode.System;
    private bool _enableAutoThemeSwitch;
    private AccentColor _accentColor = AccentColor.Blue;
    private bool _enableAnimations = true;

    // 高级设置
    private bool _enableLogging = true;
    private bool _enableAutoUpdate = true;
    private bool _enableTelemetry;
    private bool _enableCrashReporting = true;
    private string _logLevel = "Information";

    public Step4SystemConfigViewModel()
    {
        SelectLightThemeCommand = new RelayCommand(() => SelectedTheme = ThemeMode.Light);
        SelectDarkThemeCommand = new RelayCommand(() => SelectedTheme = ThemeMode.Dark);
        SelectSystemThemeCommand = new RelayCommand(() => SelectedTheme = ThemeMode.System);
    }

    #region User Management Properties

    public bool EnableUserManagement
    {
        get => _enableUserManagement;
        set
        {
            _enableUserManagement = value;
            OnPropertyChanged();
        }
    }

    public bool EnableRoleBasedAccess
    {
        get => _enableRoleBasedAccess;
        set { _enableRoleBasedAccess = value; OnPropertyChanged(); }
    }

    public bool EnableUserRegistration
    {
        get => _enableUserRegistration;
        set { _enableUserRegistration = value; OnPropertyChanged(); }
    }

    public bool EnablePasswordPolicy
    {
        get => _enablePasswordPolicy;
        set { _enablePasswordPolicy = value; OnPropertyChanged(); }
    }

    public int PasswordMinLength
    {
        get => _passwordMinLength;
        set { _passwordMinLength = value; OnPropertyChanged(); }
    }

    public bool EnableSessionTimeout
    {
        get => _enableSessionTimeout;
        set { _enableSessionTimeout = value; OnPropertyChanged(); }
    }

    public string SessionTimeoutMinutes
    {
        get => _sessionTimeoutMinutes;
        set { _sessionTimeoutMinutes = value; OnPropertyChanged(); }
    }

    #endregion

    #region Theme Properties

    public ThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            _selectedTheme = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSystemThemeSelected));
            OnPropertyChanged(nameof(LightThemeCardBrush));
            OnPropertyChanged(nameof(DarkThemeCardBrush));
            OnPropertyChanged(nameof(SystemThemeCardBrush));
        }
    }

    public bool IsSystemThemeSelected => _selectedTheme == ThemeMode.System;

    public Brush LightThemeCardBrush => GetThemeCardBrush(ThemeMode.Light);
    public Brush DarkThemeCardBrush => GetThemeCardBrush(ThemeMode.Dark);
    public Brush SystemThemeCardBrush => GetThemeCardBrush(ThemeMode.System);

    private Brush GetThemeCardBrush(ThemeMode theme)
    {
        var isSelected = _selectedTheme == theme;
        return isSelected
            ? new SolidColorBrush(Colors.DodgerBlue)
            : new SolidColorBrush(Colors.LightGray);
    }

    public ICommand SelectLightThemeCommand { get; }
    public ICommand SelectDarkThemeCommand { get; }
    public ICommand SelectSystemThemeCommand { get; }

    public bool EnableAutoThemeSwitch
    {
        get => _enableAutoThemeSwitch;
        set { _enableAutoThemeSwitch = value; OnPropertyChanged(); }
    }

    public AccentColor AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAccentBlue));
            OnPropertyChanged(nameof(IsAccentRed));
            OnPropertyChanged(nameof(IsAccentGreen));
            OnPropertyChanged(nameof(IsAccentPurple));
            OnPropertyChanged(nameof(IsAccentOrange));
            OnPropertyChanged(nameof(IsAccentTeal));
        }
    }

    public bool IsAccentBlue
    {
        get => _accentColor == AccentColor.Blue;
        set { if (value) AccentColor = AccentColor.Blue; }
    }

    public bool IsAccentRed
    {
        get => _accentColor == AccentColor.Red;
        set { if (value) AccentColor = AccentColor.Red; }
    }

    public bool IsAccentGreen
    {
        get => _accentColor == AccentColor.Green;
        set { if (value) AccentColor = AccentColor.Green; }
    }

    public bool IsAccentPurple
    {
        get => _accentColor == AccentColor.Purple;
        set { if (value) AccentColor = AccentColor.Purple; }
    }

    public bool IsAccentOrange
    {
        get => _accentColor == AccentColor.Orange;
        set { if (value) AccentColor = AccentColor.Orange; }
    }

    public bool IsAccentTeal
    {
        get => _accentColor == AccentColor.Teal;
        set { if (value) AccentColor = AccentColor.Teal; }
    }

    public bool EnableAnimations
    {
        get => _enableAnimations;
        set { _enableAnimations = value; OnPropertyChanged(); }
    }

    #endregion

    #region Advanced Settings Properties

    public bool EnableLogging
    {
        get => _enableLogging;
        set { _enableLogging = value; OnPropertyChanged(); }
    }

    public bool EnableAutoUpdate
    {
        get => _enableAutoUpdate;
        set { _enableAutoUpdate = value; OnPropertyChanged(); }
    }

    public bool EnableTelemetry
    {
        get => _enableTelemetry;
        set { _enableTelemetry = value; OnPropertyChanged(); }
    }

    public bool EnableCrashReporting
    {
        get => _enableCrashReporting;
        set { _enableCrashReporting = value; OnPropertyChanged(); }
    }

    public string LogLevel
    {
        get => _logLevel;
        set { _logLevel = value; OnPropertyChanged(); }
    }

    #endregion

    public bool IsValid => true;

    private ProjectTypeCategory _currentProjectType = ProjectTypeCategory.System;

    public void SetProjectType(ProjectTypeCategory type)
    {
        _currentProjectType = type;
        OnPropertyChanged(nameof(IsUserManagementVisible));
        OnPropertyChanged(nameof(IsLoggingVisible));
        OnPropertyChanged(nameof(IsThemeVisible));
    }

    public bool IsUserManagementVisible => _currentProjectType is ProjectTypeCategory.Collection or ProjectTypeCategory.Vision or ProjectTypeCategory.System;
    public bool IsLoggingVisible => _currentProjectType is ProjectTypeCategory.Collection or ProjectTypeCategory.Vision or ProjectTypeCategory.System;
    public bool IsThemeVisible => _currentProjectType is ProjectTypeCategory.Collection or ProjectTypeCategory.Vision or ProjectTypeCategory.System;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
