using System.IO;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 图像导航处理器，管理图像切换、加载和当前标注保存。
/// </summary>
public class ImageNavigationHandler : BindableBase
{
    private readonly IAnnotationService _annotationService;
    private readonly Func<AnnotationProject?> _getProject;
    private readonly Func<AnnotationData?> _getCurrentAnnotation;
    private readonly Action<AnnotationData?> _setCurrentAnnotation;
    private readonly Func<int> _getTotalImages;
    private readonly Action<string> _setStatusMessage;
    private readonly Action _updateBoxesList;
    private readonly Action _updateStatistics;
    private readonly ILogger _logger = Log.ForContext<ImageNavigationHandler>();

    private int _currentImageIndex = -1;
    private BitmapImage? _currentImage;

    /// <summary>
    /// 初始化图像导航处理器。
    /// </summary>
    /// <param name="annotationService">标注服务。</param>
    /// <param name="getProject">获取当前项目的回调。</param>
    /// <param name="getCurrentAnnotation">获取当前标注数据的回调。</param>
    /// <param name="setCurrentAnnotation">设置当前标注数据的回调。</param>
    /// <param name="getTotalImages">获取总图像数的回调。</param>
    /// <param name="setStatusMessage">设置状态消息的回调。</param>
    /// <param name="updateBoxesList">更新边界框列表的回调。</param>
    /// <param name="updateStatistics">更新统计信息的回调。</param>
    public ImageNavigationHandler(
        IAnnotationService annotationService,
        Func<AnnotationProject?> getProject,
        Func<AnnotationData?> getCurrentAnnotation,
        Action<AnnotationData?> setCurrentAnnotation,
        Func<int> getTotalImages,
        Action<string> setStatusMessage,
        Action updateBoxesList,
        Action updateStatistics)
    {
        _annotationService = annotationService;
        _getProject = getProject;
        _getCurrentAnnotation = getCurrentAnnotation;
        _setCurrentAnnotation = setCurrentAnnotation;
        _getTotalImages = getTotalImages;
        _setStatusMessage = setStatusMessage;
        _updateBoxesList = updateBoxesList;
        _updateStatistics = updateStatistics;

        PreviousImageCommand = new DelegateCommand(ExecutePreviousImage, CanNavigateImage);
        NextImageCommand = new DelegateCommand(ExecuteNextImage, CanNavigateImage);
    }

    /// <summary>当前图像在列表中的索引。</summary>
    public int CurrentImageIndex
    {
        get => _currentImageIndex;
        set
        {
            if (SetProperty(ref _currentImageIndex, value))
            {
                RaisePropertyChanged(nameof(ImageNavigationText));
            }
        }
    }

    /// <summary>当前显示的图像。</summary>
    public BitmapImage? CurrentImage
    {
        get => _currentImage;
        private set => SetProperty(ref _currentImage, value);
    }

    /// <summary>图像导航文字。</summary>
    public string ImageNavigationText => _getProject() == null
        ? "无图像"
        : $"{CurrentImageIndex + 1} / {_getTotalImages()}";

    /// <summary>上一张图像命令。</summary>
    public DelegateCommand PreviousImageCommand { get; }

    /// <summary>下一张图像命令。</summary>
    public DelegateCommand NextImageCommand { get; }

    /// <summary>
    /// 判断是否可以进行图像导航。
    /// </summary>
    private bool CanNavigateImage()
    {
        var project = _getProject();
        return project != null && project.Annotations.Count > 0;
    }

    /// <summary>
    /// 切换到上一张图像。
    /// </summary>
    private async void ExecutePreviousImage()
    {
        try
        {
            if (CurrentImageIndex > 0)
            {
                await SaveCurrentAnnotationAsync();
                await LoadImageAsync(CurrentImageIndex - 1);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "切换上一张图像失败");
            _setStatusMessage($"导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换到下一张图像。
    /// </summary>
    private async void ExecuteNextImage()
    {
        try
        {
            var project = _getProject();
            if (project != null && CurrentImageIndex < project.Annotations.Count - 1)
            {
                await SaveCurrentAnnotationAsync();
                await LoadImageAsync(CurrentImageIndex + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "切换下一张图像失败");
            _setStatusMessage($"导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载指定索引的图像及其标注数据。
    /// </summary>
    /// <param name="index">图像索引。</param>
    public Task LoadImageAsync(int index)
    {
        var project = _getProject();
        if (project == null || index < 0 || index >= project.Annotations.Count)
            return Task.CompletedTask;

        CurrentImageIndex = index;
        _setCurrentAnnotation(project.Annotations[index]);

        var currentAnnotation = _getCurrentAnnotation();
        if (currentAnnotation == null) return Task.CompletedTask;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(currentAnnotation.ImagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            CurrentImage = bitmap;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载图像失败: {ImagePath}", currentAnnotation.ImagePath);
            CurrentImage = null;
            _setStatusMessage($"加载图像失败: {ex.Message}");
            return Task.CompletedTask;
        }

        if (currentAnnotation.ImageWidth == 0 || currentAnnotation.ImageHeight == 0)
        {
            currentAnnotation.ImageWidth = CurrentImage.PixelWidth;
            currentAnnotation.ImageHeight = CurrentImage.PixelHeight;
        }

        _updateBoxesList();
        _setStatusMessage($"图像 {index + 1}/{project.Annotations.Count}: {Path.GetFileName(currentAnnotation.ImagePath)}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存当前图像的标注数据。
    /// </summary>
    public async Task SaveCurrentAnnotationAsync()
    {
        var project = _getProject();
        var currentAnnotation = _getCurrentAnnotation();
        if (project == null || currentAnnotation == null) return;

        await _annotationService.UpdateAnnotationAsync(project, currentAnnotation);
        _updateStatistics();
    }
}
