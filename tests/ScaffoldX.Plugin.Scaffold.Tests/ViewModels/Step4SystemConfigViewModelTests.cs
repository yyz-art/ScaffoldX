using ScaffoldX.Plugin.Scaffold.ViewModels;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class Step4SystemConfigViewModelTests
{
    private Step4SystemConfigViewModel CreateViewModel()
    {
        return new Step4SystemConfigViewModel();
    }

    #region Constructor & Default Values

    [Fact]
    public void Constructor_默认值正确()
    {
        var vm = CreateViewModel();

        // 用户管理默认值
        Assert.True(vm.EnableUserManagement);
        Assert.True(vm.EnableRoleBasedAccess);
        Assert.False(vm.EnableUserRegistration);
        Assert.True(vm.EnablePasswordPolicy);
        Assert.Equal(8, vm.PasswordMinLength);
        Assert.True(vm.EnableSessionTimeout);
        Assert.Equal("60", vm.SessionTimeoutMinutes);

        // 主题默认值
        Assert.Equal(ThemeMode.System, vm.SelectedTheme);
        Assert.False(vm.EnableAutoThemeSwitch);
        Assert.Equal(AccentColor.Blue, vm.AccentColor);
        Assert.True(vm.EnableAnimations);

        // 高级设置默认值
        Assert.True(vm.EnableLogging);
        Assert.True(vm.EnableAutoUpdate);
        Assert.False(vm.EnableTelemetry);
        Assert.True(vm.EnableCrashReporting);
        Assert.Equal("Information", vm.LogLevel);
    }

    #endregion

    #region User Management Properties

    [Fact]
    public void EnableUserManagement_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableUserManagement = false;

        Assert.False(vm.EnableUserManagement);
    }

    [Fact]
    public void EnableRoleBasedAccess_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableRoleBasedAccess = false;

        Assert.False(vm.EnableRoleBasedAccess);
    }

    [Fact]
    public void EnableUserRegistration_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableUserRegistration = true;

        Assert.True(vm.EnableUserRegistration);
    }

    [Fact]
    public void EnablePasswordPolicy_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnablePasswordPolicy = false;

        Assert.False(vm.EnablePasswordPolicy);
    }

    [Fact]
    public void PasswordMinLength_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.PasswordMinLength = 12;

        Assert.Equal(12, vm.PasswordMinLength);
    }

    [Fact]
    public void EnableSessionTimeout_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableSessionTimeout = false;

        Assert.False(vm.EnableSessionTimeout);
    }

    [Fact]
    public void SessionTimeoutMinutes_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.SessionTimeoutMinutes = "30";

        Assert.Equal("30", vm.SessionTimeoutMinutes);
    }

    #endregion

    #region Theme Properties

    [Fact]
    public void SelectedTheme_设置Light_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedTheme = ThemeMode.Light;

        Assert.Equal(ThemeMode.Light, vm.SelectedTheme);
        Assert.False(vm.IsSystemThemeSelected);
    }

    [Fact]
    public void SelectedTheme_设置Dark_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedTheme = ThemeMode.Dark;

        Assert.Equal(ThemeMode.Dark, vm.SelectedTheme);
        Assert.False(vm.IsSystemThemeSelected);
    }

    [Fact]
    public void SelectedTheme_设置System_正确存储()
    {
        var vm = CreateViewModel();

        vm.SelectedTheme = ThemeMode.System;

        Assert.Equal(ThemeMode.System, vm.SelectedTheme);
        Assert.True(vm.IsSystemThemeSelected);
    }

    [Fact]
    public void SelectLightThemeCommand_执行后_选择Light主题()
    {
        var vm = CreateViewModel();
        vm.SelectedTheme = ThemeMode.Dark;

        vm.SelectLightThemeCommand.Execute(null);

        Assert.Equal(ThemeMode.Light, vm.SelectedTheme);
    }

    [Fact]
    public void SelectDarkThemeCommand_执行后_选择Dark主题()
    {
        var vm = CreateViewModel();
        vm.SelectedTheme = ThemeMode.Light;

        vm.SelectDarkThemeCommand.Execute(null);

        Assert.Equal(ThemeMode.Dark, vm.SelectedTheme);
    }

    [Fact]
    public void SelectSystemThemeCommand_执行后_选择System主题()
    {
        var vm = CreateViewModel();
        vm.SelectedTheme = ThemeMode.Light;

        vm.SelectSystemThemeCommand.Execute(null);

        Assert.Equal(ThemeMode.System, vm.SelectedTheme);
    }

    [Fact]
    public void EnableAutoThemeSwitch_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableAutoThemeSwitch = true;

        Assert.True(vm.EnableAutoThemeSwitch);
    }

    [Fact]
    public void EnableAnimations_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableAnimations = false;

        Assert.False(vm.EnableAnimations);
    }

    #endregion

    #region Accent Color Properties

    [Fact]
    public void AccentColor_设置Blue_正确存储()
    {
        var vm = CreateViewModel();

        vm.AccentColor = AccentColor.Blue;

        Assert.Equal(AccentColor.Blue, vm.AccentColor);
        Assert.True(vm.IsAccentBlue);
        Assert.False(vm.IsAccentRed);
    }

    [Fact]
    public void AccentColor_设置Red_正确存储()
    {
        var vm = CreateViewModel();

        vm.AccentColor = AccentColor.Red;

        Assert.Equal(AccentColor.Red, vm.AccentColor);
        Assert.True(vm.IsAccentRed);
        Assert.False(vm.IsAccentBlue);
    }

    [Fact]
    public void AccentColor_设置Green_正确存储()
    {
        var vm = CreateViewModel();

        vm.AccentColor = AccentColor.Green;

        Assert.Equal(AccentColor.Green, vm.AccentColor);
        Assert.True(vm.IsAccentGreen);
    }

    [Fact]
    public void AccentColor_设置Purple_正确存储()
    {
        var vm = CreateViewModel();

        vm.AccentColor = AccentColor.Purple;

        Assert.Equal(AccentColor.Purple, vm.AccentColor);
        Assert.True(vm.IsAccentPurple);
    }

    [Fact]
    public void AccentColor_设置Orange_正确存储()
    {
        var vm = CreateViewModel();

        vm.AccentColor = AccentColor.Orange;

        Assert.Equal(AccentColor.Orange, vm.AccentColor);
        Assert.True(vm.IsAccentOrange);
    }

    [Fact]
    public void AccentColor_设置Teal_正确存储()
    {
        var vm = CreateViewModel();

        vm.AccentColor = AccentColor.Teal;

        Assert.Equal(AccentColor.Teal, vm.AccentColor);
        Assert.True(vm.IsAccentTeal);
    }

    [Fact]
    public void IsAccentBlue_设置True_切换到Blue()
    {
        var vm = CreateViewModel();
        vm.AccentColor = AccentColor.Red;

        vm.IsAccentBlue = true;

        Assert.Equal(AccentColor.Blue, vm.AccentColor);
    }

    [Fact]
    public void IsAccentRed_设置True_切换到Red()
    {
        var vm = CreateViewModel();

        vm.IsAccentRed = true;

        Assert.Equal(AccentColor.Red, vm.AccentColor);
    }

    [Fact]
    public void IsAccentGreen_设置True_切换到Green()
    {
        var vm = CreateViewModel();

        vm.IsAccentGreen = true;

        Assert.Equal(AccentColor.Green, vm.AccentColor);
    }

    [Fact]
    public void IsAccentPurple_设置True_切换到Purple()
    {
        var vm = CreateViewModel();

        vm.IsAccentPurple = true;

        Assert.Equal(AccentColor.Purple, vm.AccentColor);
    }

    [Fact]
    public void IsAccentOrange_设置True_切换到Orange()
    {
        var vm = CreateViewModel();

        vm.IsAccentOrange = true;

        Assert.Equal(AccentColor.Orange, vm.AccentColor);
    }

    [Fact]
    public void IsAccentTeal_设置True_切换到Teal()
    {
        var vm = CreateViewModel();

        vm.IsAccentTeal = true;

        Assert.Equal(AccentColor.Teal, vm.AccentColor);
    }

    #endregion

    #region Advanced Settings Properties

    [Fact]
    public void EnableLogging_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableLogging = false;

        Assert.False(vm.EnableLogging);
    }

    [Fact]
    public void EnableAutoUpdate_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableAutoUpdate = false;

        Assert.False(vm.EnableAutoUpdate);
    }

    [Fact]
    public void EnableTelemetry_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableTelemetry = true;

        Assert.True(vm.EnableTelemetry);
    }

    [Fact]
    public void EnableCrashReporting_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.EnableCrashReporting = false;

        Assert.False(vm.EnableCrashReporting);
    }

    [Fact]
    public void LogLevel_设置值_正确存储()
    {
        var vm = CreateViewModel();

        vm.LogLevel = "Debug";

        Assert.Equal("Debug", vm.LogLevel);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void SetProjectType_Collection_所有系统配置可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.True(vm.IsUserManagementVisible);
        Assert.True(vm.IsLoggingVisible);
        Assert.True(vm.IsThemeVisible);
    }

    [Fact]
    public void SetProjectType_Vision_所有系统配置可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.Vision);

        Assert.True(vm.IsUserManagementVisible);
        Assert.True(vm.IsLoggingVisible);
        Assert.True(vm.IsThemeVisible);
    }

    [Fact]
    public void SetProjectType_System_所有系统配置可见()
    {
        var vm = CreateViewModel();

        vm.SetProjectType(ProjectTypeCategory.System);

        Assert.True(vm.IsUserManagementVisible);
        Assert.True(vm.IsLoggingVisible);
        Assert.True(vm.IsThemeVisible);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsValid_始终返回True()
    {
        var vm = CreateViewModel();

        Assert.True(vm.IsValid);
    }

    [Fact]
    public void IsValid_修改配置后_仍然返回True()
    {
        var vm = CreateViewModel();

        vm.EnableUserManagement = false;
        vm.SelectedTheme = ThemeMode.Dark;
        vm.AccentColor = AccentColor.Red;
        vm.EnableLogging = false;

        Assert.True(vm.IsValid);
    }

    #endregion

    #region Property Changed Tests

    [Fact]
    public void PropertyChanged_用户管理属性变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.EnableUserManagement = false;
        vm.EnableRoleBasedAccess = false;
        vm.PasswordMinLength = 10;

        Assert.Contains(nameof(vm.EnableUserManagement), propertyChangedEvents);
        Assert.Contains(nameof(vm.EnableRoleBasedAccess), propertyChangedEvents);
        Assert.Contains(nameof(vm.PasswordMinLength), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_主题选择变更_触发相关属性通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.SelectedTheme = ThemeMode.Dark;

        Assert.Contains(nameof(vm.SelectedTheme), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsSystemThemeSelected), propertyChangedEvents);
        Assert.Contains(nameof(vm.LightThemeCardBrush), propertyChangedEvents);
        Assert.Contains(nameof(vm.DarkThemeCardBrush), propertyChangedEvents);
        Assert.Contains(nameof(vm.SystemThemeCardBrush), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_强调色变更_触发相关属性通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.AccentColor = AccentColor.Red;

        Assert.Contains(nameof(vm.AccentColor), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsAccentBlue), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsAccentRed), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsAccentGreen), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsAccentPurple), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsAccentOrange), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsAccentTeal), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_SetProjectType_触发可见性通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.SetProjectType(ProjectTypeCategory.Collection);

        Assert.Contains(nameof(vm.IsUserManagementVisible), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsLoggingVisible), propertyChangedEvents);
        Assert.Contains(nameof(vm.IsThemeVisible), propertyChangedEvents);
    }

    [Fact]
    public void PropertyChanged_高级设置变更_触发通知()
    {
        var vm = CreateViewModel();
        var propertyChangedEvents = new List<string>();
        vm.PropertyChanged += (_, e) => propertyChangedEvents.Add(e.PropertyName!);

        vm.EnableLogging = false;
        vm.EnableAutoUpdate = false;
        vm.LogLevel = "Debug";

        Assert.Contains(nameof(vm.EnableLogging), propertyChangedEvents);
        Assert.Contains(nameof(vm.EnableAutoUpdate), propertyChangedEvents);
        Assert.Contains(nameof(vm.LogLevel), propertyChangedEvents);
    }

    #endregion

    #region Card Brush Tests

    [Fact]
    public void LightThemeCardBrush_选择Light时_不为Null()
    {
        var vm = CreateViewModel();
        vm.SelectedTheme = ThemeMode.Light;

        Assert.NotNull(vm.LightThemeCardBrush);
    }

    [Fact]
    public void DarkThemeCardBrush_选择Dark时_不为Null()
    {
        var vm = CreateViewModel();
        vm.SelectedTheme = ThemeMode.Dark;

        Assert.NotNull(vm.DarkThemeCardBrush);
    }

    [Fact]
    public void SystemThemeCardBrush_选择System时_不为Null()
    {
        var vm = CreateViewModel();
        vm.SelectedTheme = ThemeMode.System;

        Assert.NotNull(vm.SystemThemeCardBrush);
    }

    #endregion
}
