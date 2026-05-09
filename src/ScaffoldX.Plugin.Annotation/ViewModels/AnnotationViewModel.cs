using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ScaffoldX.Plugin.Annotation.Models;

namespace ScaffoldX.Plugin.Annotation.ViewModels;

public sealed class AnnotationViewModel : INotifyPropertyChanged
{
    private AnnotationProject? _project;
    private AnnotationData? _currentAnnotation;
    private string _statusMessage = "就绪";

    public AnnotationProject? Project
    {
        get => _project;
        set => SetField(ref _project, value);
    }

    public AnnotationData? CurrentAnnotation
    {
        get => _currentAnnotation;
        set => SetField(ref _currentAnnotation, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ObservableCollection<AnnotationClass> Classes { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
