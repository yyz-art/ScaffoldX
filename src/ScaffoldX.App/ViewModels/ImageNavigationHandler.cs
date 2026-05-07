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
    private readonly AnnotationContext _ctx;
    private readonly ILogger _logger = Log.ForContext<ImageNavigationHandler>();

    private int _currentImageIndex = -1;
    private BitmapImage? _currentImage;

    /// <summary>
    /// 初始化图像导航处理器。
    /// </summary>
    public ImageNavigationHandler(IAnnotationService annotationService, AnnotationContext ctx)
    {
        _annotationService = annotationService;
        _ctx = ctx;

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
    public string ImageNavigationText => _ctx.GetProject() == null
        ? "无图像"
        : $"{CurrentImageIndex + 1} / {_ctx.GetTotalImages()}";

    /// <summary>上一张图像命令。</summary>
    public DelegateCommand PreviousImageCommand { get; }

    /// <summary>下一张图像命令。</summary>
    public DelegateCommand NextImageCommand { get; }

    /// <summary>
    /// 判断是否可以进行图像导航。
    /// </summary>
    private bool CanNavigateImage()
    {
        var project = _ctx.GetProject();
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
            _ctx.SetStatusMessage($"导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换到下一张图像。
    /// </summary>
    private async void ExecuteNextImage()
    {
        try
        {
            var project = _ctx.GetProject();
            if (project != null && CurrentImageIndex < project.Annotations.Count - 1)
            {
                await SaveCurrentAnnotationAsync();
                await LoadImageAsync(CurrentImageIndex + 1);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "切换下一张图像失败");
            _ctx.SetStatusMessage($"导航失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 通知导航命令重新评估 CanExecute。
    /// </summary>
    public void RaiseCanNavigateChanged()
    {
        PreviousImageCommand.RaiseCanExecuteChanged();
        NextImageCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 加载指定索引的图像及其标注数据。
    /// </summary>
    /// <param name="index">图像索引。</param>
    public Task LoadImageAsync(int index)
    {
        var project = _ctx.GetProject();
        if (project == null || index < 0 || index >= project.Annotations.Count)
            return Task.CompletedTask;

        CurrentImageIndex = index;
        _ctx.SetCurrentAnnotation(project.Annotations[index]);

        var currentAnnotation = _ctx.GetCurrentAnnotation();
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
            _ctx.SetStatusMessage($"加载图像失败: {ex.Message}");
            return Task.CompletedTask;
        }

        if (currentAnnotation.ImageWidth == 0 || currentAnnotation.ImageHeight == 0)
        {
            currentAnnotation.ImageWidth = CurrentImage.PixelWidth;
            currentAnnotation.ImageHeight = CurrentImage.PixelHeight;
        }

        _ctx.UpdateBoxesList();
        _ctx.SetStatusMessage($"图像 {index + 1}/{project.Annotations.Count}: {Path.GetFileName(currentAnnotation.ImagePath)}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存当前图像的标注数据。
    /// </summary>
    public async Task SaveCurrentAnnotationAsync()
    {
        var project = _ctx.GetProject();
        var currentAnnotation = _ctx.GetCurrentAnnotation();
        if (project == null || currentAnnotation == null) return;

        await _annotationService.UpdateAnnotationAsync(project, currentAnnotation);
        _ctx.UpdateStatistics();
    }
}
