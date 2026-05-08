using Prism.Mvvm;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 图像状态子 ViewModel，管理缩放级别。
/// 图像导航状态（CurrentImage、CurrentImageIndex）仍由 ImageNavigationHandler 持有。
/// </summary>
public class ImageStateVM : BindableBase
{
    private double _zoomLevel = 1.0;

    /// <summary>当前缩放级别（1.0 = 100%）。</summary>
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (SetProperty(ref _zoomLevel, Math.Clamp(value, 0.1, 10.0)))
                RaisePropertyChanged(nameof(ZoomLevelText));
        }
    }

    /// <summary>缩放级别显示文本。</summary>
    public string ZoomLevelText => $"{ZoomLevel * 100:F0}%";

    /// <summary>重置缩放至 100%。</summary>
    public void ResetZoom() => ZoomLevel = 1.0;
}
