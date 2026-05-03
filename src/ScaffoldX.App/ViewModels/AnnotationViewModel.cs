using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// YOLO 标注工具 ViewModel，提供图像标注、边界框绘制和数据集导出功能。
/// </summary>
public class AnnotationViewModel : BindableBase
{
    private readonly IAnnotationService _annotationService;
    private readonly IAutoLabelingService _autoLabelingService;
    private readonly ILogger _logger = Log.ForContext<AnnotationViewModel>();

    private AnnotationProject? _project;
    private AnnotationData? _currentAnnotation;
    private BitmapImage? _currentImage;
    private int _currentImageIndex = -1;
    private int _selectedClassIndex;
    private bool _isDrawing;
    private Point _drawStartPoint;
    private Point _drawEndPoint;
    private BoundingBoxAnnotation? _selectedBox;
    private string _statusMessage = "就绪";
    private string _projectName = string.Empty;
    private int _totalImages;
    private int _annotatedImages;
    private int _currentBoxCount;
    private bool _isAutoDetecting;
    private float _confidenceThreshold = 0.5f;
    private string _loadedModelName = string.Empty;
    private int _autoDetectProgress;
    private int _autoDetectTotal;

    /// <summary>
    /// 初始化标注 ViewModel，注册命令。
    /// </summary>
    /// <param name="annotationService">标注服务。</param>
    /// <param name="autoLabelingService">自动标注服务。</param>
    public AnnotationViewModel(IAnnotationService annotationService, IAutoLabelingService autoLabelingService)
    {
        _annotationService = annotationService;
        _autoLabelingService = autoLabelingService;

        // 命令注册
        NewProjectCommand = new DelegateCommand(ExecuteNewProject);
        OpenProjectCommand = new DelegateCommand(ExecuteOpenProject);
        SaveProjectCommand = new DelegateCommand(ExecuteSaveProject);
        AddImagesCommand = new DelegateCommand(ExecuteAddImages);
        AddFolderCommand = new DelegateCommand(ExecuteAddFolder);
        ExportYoloCommand = new DelegateCommand(ExecuteExportYolo);
        ExportCocoCommand = new DelegateCommand(ExecuteExportCoco);
        ExportVocCommand = new DelegateCommand(ExecuteExportVoc);

        PreviousImageCommand = new DelegateCommand(ExecutePreviousImage, CanNavigateImage);
        NextImageCommand = new DelegateCommand(ExecuteNextImage, CanNavigateImage);

        AddClassCommand = new DelegateCommand(ExecuteAddClass);
        RemoveClassCommand = new DelegateCommand(ExecuteRemoveClass, CanRemoveClass);

        DeleteSelectedBoxCommand = new DelegateCommand(ExecuteDeleteSelectedBox, CanDeleteSelectedBox);
        ClearAllBoxesCommand = new DelegateCommand(ExecuteClearAllBoxes, () => HasBoxes);

        ImageMouseDownCommand = new DelegateCommand<Point>(ExecuteImageMouseDown);
        ImageMouseMoveCommand = new DelegateCommand<Point>(ExecuteImageMouseMove);
        ImageMouseUpCommand = new DelegateCommand<Point>(ExecuteImageMouseUp);

        // 自动标注命令
        LoadModelCommand = new DelegateCommand(ExecuteLoadModel);
        UnloadModelCommand = new DelegateCommand(ExecuteUnloadModel, () => IsModelLoaded);
        AutoDetectCurrentCommand = new DelegateCommand(ExecuteAutoDetectCurrent, CanAutoDetectCurrent);
        AutoDetectAllCommand = new DelegateCommand(ExecuteAutoDetectAll, CanAutoDetectAll);

        // 撤销/重做
        UndoCommand = new DelegateCommand(ExecuteUndo, CanUndo);
        RedoCommand = new DelegateCommand(ExecuteRedo, CanRedo);
    }

    // ── 属性 ──────────────────────────────────────────────────────────────────

    /// <summary>当前标注项目。</summary>
    public AnnotationProject? Project
    {
        get => _project;
        private set => SetProperty(ref _project, value);
    }

    /// <summary>当前图像的标注数据。</summary>
    public AnnotationData? CurrentAnnotation
    {
        get => _currentAnnotation;
        private set
        {
            if (SetProperty(ref _currentAnnotation, value))
            {
                RaisePropertyChanged(nameof(CurrentBoxes));
                RaisePropertyChanged(nameof(HasBoxes));
            }
        }
    }

    /// <summary>当前显示的图像。</summary>
    public BitmapImage? CurrentImage
    {
        get => _currentImage;
        private set => SetProperty(ref _currentImage, value);
    }

    /// <summary>当前图像在列表中的索引。</summary>
    public int CurrentImageIndex
    {
        get => _currentImageIndex;
        private set
        {
            if (SetProperty(ref _currentImageIndex, value))
            {
                RaisePropertyChanged(nameof(ImageNavigationText));
            }
        }
    }

    /// <summary>当前选中的类别索引。</summary>
    public int SelectedClassIndex
    {
        get => _selectedClassIndex;
        set => SetProperty(ref _selectedClassIndex, value);
    }

    /// <summary>是否正在绘制边界框。</summary>
    public bool IsDrawing
    {
        get => _isDrawing;
        private set => SetProperty(ref _isDrawing, value);
    }

    /// <summary>绘制起点。</summary>
    public Point DrawStartPoint
    {
        get => _drawStartPoint;
        private set => SetProperty(ref _drawStartPoint, value);
    }

    /// <summary>绘制终点。</summary>
    public Point DrawEndPoint
    {
        get => _drawEndPoint;
        private set => SetProperty(ref _drawEndPoint, value);
    }

    /// <summary>当前选中的边界框。</summary>
    public BoundingBoxAnnotation? SelectedBox
    {
        get => _selectedBox;
        set => SetProperty(ref _selectedBox, value);
    }

    /// <summary>状态消息。</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>项目名称。</summary>
    public string ProjectName
    {
        get => _projectName;
        private set => SetProperty(ref _projectName, value);
    }

    /// <summary>总图像数。</summary>
    public int TotalImages
    {
        get => _totalImages;
        private set => SetProperty(ref _totalImages, value);
    }

    /// <summary>已标注图像数。</summary>
    public int AnnotatedImages
    {
        get => _annotatedImages;
        private set => SetProperty(ref _annotatedImages, value);
    }

    /// <summary>当前图像的边界框数量。</summary>
    public int CurrentBoxCount
    {
        get => _currentBoxCount;
        private set => SetProperty(ref _currentBoxCount, value);
    }

    /// <summary>当前图像的边界框集合（用于绑定）。</summary>
    public ObservableCollection<BoundingBoxAnnotation> CurrentBoxes { get; } = new();

    /// <summary>是否有边界框。</summary>
    public bool HasBoxes => CurrentBoxes.Count > 0;

    /// <summary>类别列表。</summary>
    public ObservableCollection<AnnotationClass> Classes { get; } = new();

    /// <summary>图像导航文字。</summary>
    public string ImageNavigationText => Project == null
        ? "无图像"
        : $"{CurrentImageIndex + 1} / {TotalImages}";

    /// <summary>标注进度文字。</summary>
    public string AnnotationProgressText => Project == null
        ? string.Empty
        : $"已标注: {AnnotatedImages} / {TotalImages}";

    // ── 自动标注属性 ────────────────────────────────────────────────────────

    /// <summary>模型是否已加载。</summary>
    public bool IsModelLoaded
    {
        get => _autoLabelingService.IsModelLoaded;
        private set
        {
            RaisePropertyChanged(nameof(IsModelLoaded));
            RaisePropertyChanged(nameof(LoadedModelName));
        }
    }

    /// <summary>已加载模型名称。</summary>
    public string LoadedModelName
    {
        get
        {
            if (!_autoLabelingService.IsModelLoaded) return "未加载模型";
            return Path.GetFileName(_autoLabelingService.LoadedModelPath ?? "未知模型");
        }
    }

    /// <summary>是否正在自动检测。</summary>
    public bool IsAutoDetecting
    {
        get => _isAutoDetecting;
        private set => SetProperty(ref _isAutoDetecting, value);
    }

    /// <summary>置信度阈值。</summary>
    public float ConfidenceThreshold
    {
        get => _confidenceThreshold;
        set => SetProperty(ref _confidenceThreshold, Math.Clamp(value, 0.1f, 0.95f));
    }

    /// <summary>自动检测进度当前值。</summary>
    public int AutoDetectProgress
    {
        get => _autoDetectProgress;
        private set => SetProperty(ref _autoDetectProgress, value);
    }

    /// <summary>自动检测进度总数。</summary>
    public int AutoDetectTotal
    {
        get => _autoDetectTotal;
        private set => SetProperty(ref _autoDetectTotal, value);
    }

    /// <summary>自动检测进度文字。</summary>
    public string AutoDetectProgressText => IsAutoDetecting
        ? $"自动标注中: {AutoDetectProgress} / {AutoDetectTotal}"
        : string.Empty;

    // ── 撤销/重做 ──────────────────────────────────────────────────────────

    private readonly Stack<List<BoundingBoxAnnotation>> _undoStack = new();
    private readonly Stack<List<BoundingBoxAnnotation>> _redoStack = new();

    // ── 命令 ──────────────────────────────────────────────────────────────────

    public DelegateCommand NewProjectCommand { get; }
    public DelegateCommand OpenProjectCommand { get; }
    public DelegateCommand SaveProjectCommand { get; }
    public DelegateCommand AddImagesCommand { get; }
    public DelegateCommand ExportYoloCommand { get; }

    public DelegateCommand PreviousImageCommand { get; }
    public DelegateCommand NextImageCommand { get; }

    public DelegateCommand AddClassCommand { get; }
    public DelegateCommand RemoveClassCommand { get; }

    public DelegateCommand DeleteSelectedBoxCommand { get; }
    public DelegateCommand ClearAllBoxesCommand { get; }

    public DelegateCommand<Point> ImageMouseDownCommand { get; }
    public DelegateCommand<Point> ImageMouseMoveCommand { get; }
    public DelegateCommand<Point> ImageMouseUpCommand { get; }

    // 自动标注命令
    public DelegateCommand LoadModelCommand { get; }
    public DelegateCommand UnloadModelCommand { get; }
    public DelegateCommand AutoDetectCurrentCommand { get; }
    public DelegateCommand AutoDetectAllCommand { get; }

    // 导出命令
    public DelegateCommand AddFolderCommand { get; }
    public DelegateCommand ExportCocoCommand { get; }
    public DelegateCommand ExportVocCommand { get; }

    // 撤销/重做
    public DelegateCommand UndoCommand { get; }
    public DelegateCommand RedoCommand { get; }

    // ── 命令实现 ──────────────────────────────────────────────────────────────

    private async void ExecuteNewProject()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "选择标注项目目录"
        };

        if (dialog.ShowDialog() != true)
            return;

        var projectName = Path.GetFileName(dialog.SelectedPath);
        if (string.IsNullOrWhiteSpace(projectName))
            projectName = "AnnotationProject";

        // 默认类别
        var defaultClasses = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "object", Color = "#FF0000" }
        };

        try
        {
            Project = await _annotationService.CreateProjectAsync(projectName, dialog.SelectedPath, defaultClasses);
            UpdateClassesList();
            StatusMessage = $"项目已创建: {projectName}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "创建项目失败");
            StatusMessage = $"创建项目失败: {ex.Message}";
        }
    }

    private async void ExecuteOpenProject()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ScaffoldX 标注项目|*.scaffoldx-annotation.json|JSON 文件|*.json",
            Title = "打开标注项目"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            Project = await _annotationService.LoadProjectAsync(dialog.FileName);
            UpdateClassesList();
            UpdateStatistics();

            if (Project.Annotations.Count > 0)
            {
                await LoadImageAsync(0);
            }

            StatusMessage = $"项目已加载: {Project.ProjectName}, 共 {Project.Annotations.Count} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载项目失败");
            StatusMessage = $"加载项目失败: {ex.Message}";
        }
    }

    private async void ExecuteSaveProject()
    {
        if (Project == null) return;

        try
        {
            await _annotationService.SaveProjectAsync(Project);
            StatusMessage = "项目已保存";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存项目失败");
            StatusMessage = $"保存项目失败: {ex.Message}";
        }
    }

    private async void ExecuteAddImages()
    {
        if (Project == null)
        {
            StatusMessage = "请先创建或打开项目";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|所有文件|*.*",
            Title = "选择图像文件",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await _annotationService.AddImagesAsync(Project, dialog.FileNames);
            UpdateStatistics();

            // 如果是第一次添加图像，加载第一张
            if (CurrentImageIndex < 0 && Project.Annotations.Count > 0)
            {
                await LoadImageAsync(0);
            }

            StatusMessage = $"已添加 {dialog.FileNames.Length} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加图像失败");
            StatusMessage = $"添加图像失败: {ex.Message}";
        }
    }

    private async void ExecuteExportYolo()
    {
        if (Project == null || Project.Annotations.Count == 0)
        {
            StatusMessage = "没有可导出的标注数据";
            return;
        }

        var dialog = new VistaFolderBrowserDialog
        {
            Description = "选择 YOLO 数据集输出目录"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            StatusMessage = "正在导出 YOLO 数据集...";
            await _annotationService.ExportYoloDatasetAsync(Project, dialog.SelectedPath);
            StatusMessage = $"YOLO 数据集已导出到: {dialog.SelectedPath}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 YOLO 数据集失败");
            StatusMessage = $"导出失败: {ex.Message}";
        }
    }

    private bool CanNavigateImage() => Project != null && Project.Annotations.Count > 0;

    private async void ExecutePreviousImage()
    {
        if (CurrentImageIndex > 0)
        {
            await SaveCurrentAnnotationAsync();
            await LoadImageAsync(CurrentImageIndex - 1);
        }
    }

    private async void ExecuteNextImage()
    {
        if (Project != null && CurrentImageIndex < Project.Annotations.Count - 1)
        {
            await SaveCurrentAnnotationAsync();
            await LoadImageAsync(CurrentImageIndex + 1);
        }
    }

    private void ExecuteAddClass()
    {
        if (Project == null) return;

        var newIndex = Project.Classes.Count;
        var newClass = new AnnotationClass
        {
            Index = newIndex,
            Name = $"class_{newIndex}",
            Color = GetColorForIndex(newIndex)
        };

        Project.Classes.Add(newClass);
        UpdateClassesList();
        StatusMessage = $"已添加类别: {newClass.Name}";
    }

    private bool CanRemoveClass() => Project != null && Project.Classes.Count > 1;

    private void ExecuteRemoveClass()
    {
        if (Project == null || Project.Classes.Count <= 1) return;

        var lastClass = Project.Classes.Last();
        Project.Classes.Remove(lastClass);
        UpdateClassesList();
        StatusMessage = $"已移除类别: {lastClass.Name}";
    }

    private bool CanDeleteSelectedBox() => SelectedBox != null;

    private void ExecuteDeleteSelectedBox()
    {
        if (SelectedBox == null || CurrentAnnotation == null) return;

        PushUndoSnapshot();
        CurrentAnnotation.Boxes.Remove(SelectedBox);
        SelectedBox = null;
        UpdateBoxesList();
        StatusMessage = "已删除选中的边界框";
    }

    private void ExecuteClearAllBoxes()
    {
        if (CurrentAnnotation == null) return;

        PushUndoSnapshot();
        CurrentAnnotation.Boxes.Clear();
        UpdateBoxesList();
        StatusMessage = "已清空所有边界框";
    }

    private void ExecuteImageMouseDown(Point point)
    {
        if (Project == null || CurrentAnnotation == null) return;

        IsDrawing = true;
        DrawStartPoint = point;
        DrawEndPoint = point;
        PushUndoSnapshot();
    }

    private void ExecuteImageMouseMove(Point point)
    {
        if (!IsDrawing) return;
        DrawEndPoint = point;
    }

    private async void ExecuteImageMouseUp(Point point)
    {
        if (!IsDrawing || Project == null || CurrentAnnotation == null) return;

        IsDrawing = false;
        DrawEndPoint = point;

        // 计算归一化的边界框
        if (CurrentImage == null) return;

        var x1 = Math.Min(DrawStartPoint.X, DrawEndPoint.X);
        var y1 = Math.Min(DrawStartPoint.Y, DrawEndPoint.Y);
        var x2 = Math.Max(DrawStartPoint.X, DrawEndPoint.X);
        var y2 = Math.Max(DrawStartPoint.Y, DrawEndPoint.Y);

        // 忽略太小的框（可能是误点击）
        if (x2 - x1 < 5 || y2 - y1 < 5)
            return;

        // 转换为归一化坐标
        var centerX = (x1 + x2) / 2 / CurrentImage.PixelWidth;
        var centerY = (y1 + y2) / 2 / CurrentImage.PixelHeight;
        var width = (x2 - x1) / CurrentImage.PixelWidth;
        var height = (y2 - y1) / CurrentImage.PixelHeight;

        // 获取当前选中的类别
        var selectedClass = SelectedClassIndex < Project.Classes.Count
            ? Project.Classes[SelectedClassIndex]
            : Project.Classes.FirstOrDefault();

        if (selectedClass == null) return;

        var box = new BoundingBoxAnnotation
        {
            ClassIndex = selectedClass.Index,
            ClassName = selectedClass.Name,
            CenterX = centerX,
            CenterY = centerY,
            Width = width,
            Height = height
        };

        CurrentAnnotation.Boxes.Add(box);
        UpdateBoxesList();

        StatusMessage = $"已添加边界框: {selectedClass.Name}";
    }

    // ── 自动标注命令实现 ──────────────────────────────────────────────────────

    private async void ExecuteLoadModel()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ONNX 模型|*.onnx|所有文件|*.*",
            Title = "选择 ONNX 模型文件"
        };

        if (dialog.ShowDialog() != true) return;

        // 尝试查找同目录下的 classes.txt
        var modelDir = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
        var classesFile = Path.Combine(modelDir, "classes.txt");
        var classesPath = File.Exists(classesFile) ? classesFile : null;

        try
        {
            StatusMessage = "正在加载模型...";
            await _autoLabelingService.LoadModelAsync(dialog.FileName, classesPath);
            IsModelLoaded = true;
            StatusMessage = $"模型已加载: {Path.GetFileName(dialog.FileName)}";
            _logger.Information("自动标注模型已加载: {Path}", dialog.FileName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载模型失败");
            StatusMessage = $"加载模型失败: {ex.Message}";
        }
    }

    private void ExecuteUnloadModel()
    {
        _autoLabelingService.UnloadModel();
        IsModelLoaded = false;
        StatusMessage = "模型已卸载";
    }

    private bool CanAutoDetectCurrent()
        => IsModelLoaded && CurrentAnnotation != null && CurrentImage != null && !IsAutoDetecting;

    private async void ExecuteAutoDetectCurrent()
    {
        if (CurrentAnnotation == null) return;

        try
        {
            IsAutoDetecting = true;
            StatusMessage = "正在自动标注当前图像...";

            // 保存当前框的快照用于撤销
            PushUndoSnapshot();

            var detections = await _autoLabelingService.DetectAsync(
                CurrentAnnotation.ImagePath, ConfidenceThreshold);

            // 将检测结果添加到当前标注
            foreach (var box in detections)
            {
                CurrentAnnotation.Boxes.Add(box);
            }

            UpdateBoxesList();
            StatusMessage = $"自动标注完成: 检测到 {detections.Count} 个目标";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "自动标注失败");
            StatusMessage = $"自动标注失败: {ex.Message}";
        }
        finally
        {
            IsAutoDetecting = false;
        }
    }

    private bool CanAutoDetectAll()
        => IsModelLoaded && Project != null && Project.Annotations.Count > 0 && !IsAutoDetecting;

    private async void ExecuteAutoDetectAll()
    {
        if (Project == null) return;

        // 过滤出未标注的图像
        var unannotated = Project.Annotations
            .Where(a => a.Boxes.Count == 0)
            .Select(a => a.ImagePath)
            .ToList();

        if (unannotated.Count == 0)
        {
            StatusMessage = "所有图像均已标注，无需自动标注";
            return;
        }

        try
        {
            IsAutoDetecting = true;
            AutoDetectTotal = unannotated.Count;
            AutoDetectProgress = 0;

            var progress = new Progress<(int current, int total)>(p =>
            {
                AutoDetectProgress = p.current;
                RaisePropertyChanged(nameof(AutoDetectProgressText));
            });

            StatusMessage = $"正在批量自动标注 {unannotated.Count} 张图像...";

            var results = await _autoLabelingService.DetectBatchAsync(
                unannotated, ConfidenceThreshold, progress);

            // 将检测结果写入对应的标注数据
            int totalDetections = 0;
            foreach (var annotation in Project.Annotations)
            {
                if (results.TryGetValue(annotation.ImagePath, out var detections))
                {
                    foreach (var box in detections)
                    {
                        annotation.Boxes.Add(box);
                    }
                    totalDetections += detections.Count;
                }
            }

            UpdateBoxesList();
            UpdateStatistics();
            StatusMessage = $"批量自动标注完成: {unannotated.Count} 张图像, 共 {totalDetections} 个目标";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "批量自动标注失败");
            StatusMessage = $"批量自动标注失败: {ex.Message}";
        }
        finally
        {
            IsAutoDetecting = false;
        }
    }

    // ── 文件夹导入 ──────────────────────────────────────────────────────────

    private async void ExecuteAddFolder()
    {
        if (Project == null)
        {
            StatusMessage = "请先创建或打开项目";
            return;
        }

        var dialog = new VistaFolderBrowserDialog
        {
            Description = "选择包含图像的文件夹"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif" };
            var imageFiles = Directory.EnumerateFiles(dialog.SelectedPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (imageFiles.Count == 0)
            {
                StatusMessage = "所选文件夹中没有找到图像文件";
                return;
            }

            await _annotationService.AddImagesAsync(Project, imageFiles);
            UpdateStatistics();

            if (CurrentImageIndex < 0 && Project.Annotations.Count > 0)
            {
                await LoadImageAsync(0);
            }

            StatusMessage = $"已从文件夹导入 {imageFiles.Count} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "文件夹导入失败");
            StatusMessage = $"文件夹导入失败: {ex.Message}";
        }
    }

    // ── 多格式导出 ──────────────────────────────────────────────────────────

    private async void ExecuteExportCoco()
    {
        if (Project == null || Project.Annotations.Count == 0)
        {
            StatusMessage = "没有可导出的标注数据";
            return;
        }

        var dialog = new VistaFolderBrowserDialog { Description = "选择 COCO 数据集输出目录" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            StatusMessage = "正在导出 COCO 数据集...";
            await _annotationService.ExportCocoDatasetAsync(Project, dialog.SelectedPath);
            StatusMessage = $"COCO 数据集已导出到: {dialog.SelectedPath}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 COCO 数据集失败");
            StatusMessage = $"COCO 导出失败: {ex.Message}";
        }
    }

    private async void ExecuteExportVoc()
    {
        if (Project == null || Project.Annotations.Count == 0)
        {
            StatusMessage = "没有可导出的标注数据";
            return;
        }

        var dialog = new VistaFolderBrowserDialog { Description = "选择 VOC 数据集输出目录" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            StatusMessage = "正在导出 Pascal VOC 数据集...";
            await _annotationService.ExportVocDatasetAsync(Project, dialog.SelectedPath);
            StatusMessage = $"Pascal VOC 数据集已导出到: {dialog.SelectedPath}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 VOC 数据集失败");
            StatusMessage = $"VOC 导出失败: {ex.Message}";
        }
    }

    // ── 撤销/重做 ──────────────────────────────────────────────────────────

    private void PushUndoSnapshot()
    {
        if (CurrentAnnotation == null) return;
        _undoStack.Push(CurrentAnnotation.Boxes.Select(b => new BoundingBoxAnnotation
        {
            ClassIndex = b.ClassIndex,
            ClassName = b.ClassName,
            CenterX = b.CenterX,
            CenterY = b.CenterY,
            Width = b.Width,
            Height = b.Height
        }).ToList());
        _redoStack.Clear();
    }

    private bool CanUndo() => _undoStack.Count > 0 && CurrentAnnotation != null;

    private void ExecuteUndo()
    {
        if (CurrentAnnotation == null || _undoStack.Count == 0) return;

        // 保存当前状态到 redo 栈
        _redoStack.Push(CurrentAnnotation.Boxes.Select(b => new BoundingBoxAnnotation
        {
            ClassIndex = b.ClassIndex,
            ClassName = b.ClassName,
            CenterX = b.CenterX,
            CenterY = b.CenterY,
            Width = b.Width,
            Height = b.Height
        }).ToList());

        // 恢复上一个状态
        var snapshot = _undoStack.Pop();
        CurrentAnnotation.Boxes.Clear();
        foreach (var box in snapshot) CurrentAnnotation.Boxes.Add(box);
        UpdateBoxesList();
        StatusMessage = "已撤销";
    }

    private bool CanRedo() => _redoStack.Count > 0 && CurrentAnnotation != null;

    private void ExecuteRedo()
    {
        if (CurrentAnnotation == null || _redoStack.Count == 0) return;

        // 保存当前状态到 undo 栈
        _undoStack.Push(CurrentAnnotation.Boxes.Select(b => new BoundingBoxAnnotation
        {
            ClassIndex = b.ClassIndex,
            ClassName = b.ClassName,
            CenterX = b.CenterX,
            CenterY = b.CenterY,
            Width = b.Width,
            Height = b.Height
        }).ToList());

        // 恢复 redo 状态
        var snapshot = _redoStack.Pop();
        CurrentAnnotation.Boxes.Clear();
        foreach (var box in snapshot) CurrentAnnotation.Boxes.Add(box);
        UpdateBoxesList();
        StatusMessage = "已重做";
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

    private async Task LoadImageAsync(int index)
    {
        if (Project == null || index < 0 || index >= Project.Annotations.Count)
            return;

        CurrentImageIndex = index;
        CurrentAnnotation = Project.Annotations[index];

        // 加载图像
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(CurrentAnnotation.ImagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            CurrentImage = bitmap;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载图像失败: {ImagePath}", CurrentAnnotation.ImagePath);
            CurrentImage = null;
            StatusMessage = $"加载图像失败: {ex.Message}";
            return;
        }

        // 如果图像尺寸为 0，更新尺寸
        if (CurrentAnnotation.ImageWidth == 0 || CurrentAnnotation.ImageHeight == 0)
        {
            CurrentAnnotation.ImageWidth = CurrentImage.PixelWidth;
            CurrentAnnotation.ImageHeight = CurrentImage.PixelHeight;
        }

        UpdateBoxesList();
        StatusMessage = $"图像 {index + 1}/{Project.Annotations.Count}: {Path.GetFileName(CurrentAnnotation.ImagePath)}";
    }

    private async Task SaveCurrentAnnotationAsync()
    {
        if (Project == null || CurrentAnnotation == null) return;

        await _annotationService.UpdateAnnotationAsync(Project, CurrentAnnotation);
        UpdateStatistics();
    }

    private void UpdateClassesList()
    {
        Classes.Clear();
        if (Project == null) return;

        foreach (var cls in Project.Classes)
        {
            Classes.Add(cls);
        }
    }

    private void UpdateBoxesList()
    {
        CurrentBoxes.Clear();
        if (CurrentAnnotation == null) return;

        foreach (var box in CurrentAnnotation.Boxes)
        {
            CurrentBoxes.Add(box);
        }

        CurrentBoxCount = CurrentBoxes.Count;
        RaisePropertyChanged(nameof(HasBoxes));
    }

    private void UpdateStatistics()
    {
        if (Project == null) return;

        TotalImages = Project.Annotations.Count;
        AnnotatedImages = Project.Annotations.Count(a => a.Boxes.Count > 0);
        ProjectName = Project.ProjectName;

        RaisePropertyChanged(nameof(AnnotationProgressText));
    }

    private static string GetColorForIndex(int index)
    {
        var colors = new[]
        {
            "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF",
            "#00FFFF", "#FF8000", "#8000FF", "#0080FF", "#FF0080"
        };
        return colors[index % colors.Length];
    }
}
