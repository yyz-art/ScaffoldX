using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScaffoldX.Plugin.Scaffold.ViewModels;

public sealed class Step3ViewModel : INotifyPropertyChanged
{
    private bool _hasSiemensS7;
    private bool _hasMitsubishiQ;
    private bool _hasModbusTcp;
    private bool _hasOpcUa;
    private bool _hasHikVision;
    private bool _hasDaHua;
    private bool _hasYoloDetection;
    private bool _hasSam3Segmentation;
    private bool _hasUserManagement;
    private bool _hasThemeSwitcher;

    public bool HasSiemensS7
    {
        get => _hasSiemensS7;
        set { _hasSiemensS7 = value; OnPropertyChanged(); }
    }

    public bool HasMitsubishiQ
    {
        get => _hasMitsubishiQ;
        set { _hasMitsubishiQ = value; OnPropertyChanged(); }
    }

    public bool HasModbusTcp
    {
        get => _hasModbusTcp;
        set { _hasModbusTcp = value; OnPropertyChanged(); }
    }

    public bool HasOpcUa
    {
        get => _hasOpcUa;
        set { _hasOpcUa = value; OnPropertyChanged(); }
    }

    public bool HasHikVision
    {
        get => _hasHikVision;
        set { _hasHikVision = value; OnPropertyChanged(); }
    }

    public bool HasDaHua
    {
        get => _hasDaHua;
        set { _hasDaHua = value; OnPropertyChanged(); }
    }

    public bool HasYoloDetection
    {
        get => _hasYoloDetection;
        set { _hasYoloDetection = value; OnPropertyChanged(); }
    }

    public bool HasSam3Segmentation
    {
        get => _hasSam3Segmentation;
        set { _hasSam3Segmentation = value; OnPropertyChanged(); }
    }

    public bool HasUserManagement
    {
        get => _hasUserManagement;
        set { _hasUserManagement = value; OnPropertyChanged(); }
    }

    public bool HasThemeSwitcher
    {
        get => _hasThemeSwitcher;
        set { _hasThemeSwitcher = value; OnPropertyChanged(); }
    }

    public bool HasAnyDriver => HasSiemensS7 || HasMitsubishiQ || HasModbusTcp || HasOpcUa;
    public bool HasAnyVision => HasHikVision || HasDaHua || HasYoloDetection || HasSam3Segmentation;
    public bool HasAnySystemModule => HasUserManagement || HasThemeSwitcher;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
