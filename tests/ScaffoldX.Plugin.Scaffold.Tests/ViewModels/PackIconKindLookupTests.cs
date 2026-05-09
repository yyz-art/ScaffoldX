using MaterialDesignThemes.Wpf;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.ViewModels;

public class PackIconKindLookupTests
{
    [Theory]
    [InlineData("Chip")]
    [InlineData("Memory")]
    [InlineData("Lan")]
    [InlineData("LanConnect")]
    [InlineData("Cloud")]
    [InlineData("Camera")]
    [InlineData("CameraOutline")]
    [InlineData("Database")]
    [InlineData("Eye")]
    [InlineData("EyeOutline")]
    [InlineData("Brain")]
    [InlineData("Palette")]
    [InlineData("DesktopClassic")]
    [InlineData("AccountGroup")]
    [InlineData("AccountMultiple")]
    [InlineData("ClipboardCheck")]
    [InlineData("WeatherSunny")]
    [InlineData("WeatherNight")]
    [InlineData("Cog")]
    [InlineData("CogOutline")]
    [InlineData("Settings")]
    [InlineData("Shield")]
    [InlineData("Timer")]
    [InlineData("CalendarClock")]
    [InlineData("Percent")]
    [InlineData("Shape")]
    [InlineData("FileDocument")]
    [InlineData("FormatColorFill")]
    [InlineData("InformationCircle")]
    [InlineData("CheckCircle")]
    [InlineData("CloseCircle")]
    [InlineData("AlertCircle")]
    [InlineData("PlayCircleOutline")]
    [InlineData("ImageFilterCenterFocusStrong")]
    public void CheckIconValidity(string iconName)
    {
        var isValid = Enum.TryParse<PackIconKind>(iconName, out var kind);
        Assert.True(isValid, $"PackIconKind.{iconName} 不存在");
    }
}
