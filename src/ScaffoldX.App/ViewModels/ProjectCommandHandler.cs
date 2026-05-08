using System.IO;
using System.Windows;
using Prism.Commands;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// 项目级命令处理器，从 AnnotationViewModel 提取的命令实现。
/// 负责项目管理、图像导入、边界框绘制和模式切换等命令。
/// </summary>
public class ProjectCommandHandler
{
    private readonly IAnnotationService _annotationService;
    private readonly IAutoLabelingService _autoLabelingService;
    private readonly IDialogService _dialogService;
    private readonly ILogger _logger = Log.ForContext<ProjectCommandHandler>();

    private readonly AnnotationStateVM _annotationState;
    private readonly ClassStateVM _classState;
    private readonly ImageStateVM _imageState;
    private readonly ImageNavigationHandler _imageNavigationHandler;
    private readonly ClassManagementHandler _classManagementHandler;
    private readonly PolygonDrawingHandler _polygonDrawingHandler;
    private readonly ObbDrawingHandler _obbDrawingHandler;
    private readonly UndoRedoHandler _undoRedoHandler;
    private readonly DrawingStateManager _drawingState;

    /// <summary>
    /// 当命令执行后需要通知 UI 属性变化时触发。
    /// </summary>
    public event Action<string>? RaisePropertyChange;

    /// <summary>
    /// 初始化项目命令处理器。
    /// </summary>
    public ProjectCommandHandler(
        IAnnotationService annotationService,
        IAutoLabelingService autoLabelingService,
        IDialogService dialogService,
        AnnotationStateVM annotationState,
        ClassStateVM classState,
        ImageStateVM imageState,
        ImageNavigationHandler imageNavigationHandler,
        ClassManagementHandler classManagementHandler,
        PolygonDrawingHandler polygonDrawingHandler,
        ObbDrawingHandler obbDrawingHandler,
        UndoRedoHandler undoRedoHandler,
        DrawingStateManager drawingState)
    {
        _annotationService = annotationService;
        _autoLabelingService = autoLabelingService;
        _dialogService = dialogService;
        _annotationState = annotationState;
        _classState = classState;
        _imageState = imageState;
        _imageNavigationHandler = imageNavigationHandler;
        _classManagementHandler = classManagementHandler;
        _polygonDrawingHandler = polygonDrawingHandler;
        _obbDrawingHandler = obbDrawingHandler;
        _undoRedoHandler = undoRedoHandler;
        _drawingState = drawingState;

        NewProjectCommand = new DelegateCommand(ExecuteNewProject);
        OpenProjectCommand = new DelegateCommand(ExecuteOpenProject);
        SaveProjectCommand = new DelegateCommand(ExecuteSaveProject);
        AddImagesCommand = new DelegateCommand(ExecuteAddImages);
        AddFolderCommand = new DelegateCommand(ExecuteAddFolder);

        DeleteSelectedBoxCommand = new DelegateCommand(ExecuteDeleteSelectedBox, CanDeleteSelectedBox);
        ClearAllBoxesCommand = new DelegateCommand(ExecuteClearAllBoxes, () => _annotationState.HasBoxes);

        ImageMouseDownCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteImageMouseDown(p.Value); });
        ImageMouseMoveCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteImageMouseMove(p.Value); });
        ImageMouseUpCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteImageMouseUp(p.Value); });

        SwitchToBboxModeCommand = new DelegateCommand(ExecuteSwitchToBboxMode);
        SwitchToPolygonModeCommand = new DelegateCommand(ExecuteSwitchToPolygonMode);
        SwitchToObbModeCommand = new DelegateCommand(ExecuteSwitchToObbMode);
        CancelDrawingCommand = new DelegateCommand(ExecuteCancelDrawing);

        LoadSam3ModelCommand = new DelegateCommand(async () => await ExecuteLoadSam3ModelAsync());
        ResetZoomCommand = new DelegateCommand(ExecuteResetZoom);
    }

    // ── 命令属性 ──────────────────────────────────────────────────────────────

    public DelegateCommand NewProjectCommand { get; }
    public DelegateCommand OpenProjectCommand { get; }
    public DelegateCommand SaveProjectCommand { get; }
    public DelegateCommand AddImagesCommand { get; }
    public DelegateCommand AddFolderCommand { get; }

    public DelegateCommand DeleteSelectedBoxCommand { get; }
    public DelegateCommand ClearAllBoxesCommand { get; }

    public DelegateCommand<Point?> ImageMouseDownCommand { get; }
    public DelegateCommand<Point?> ImageMouseMoveCommand { get; }
    public DelegateCommand<Point?> ImageMouseUpCommand { get; }

    public DelegateCommand SwitchToBboxModeCommand { get; }
    public DelegateCommand SwitchToPolygonModeCommand { get; }
    public DelegateCommand SwitchToObbModeCommand { get; }
    public DelegateCommand CancelDrawingCommand { get; }

    public DelegateCommand LoadSam3ModelCommand { get; }
    public DelegateCommand ResetZoomCommand { get; }

    // ── 项目管理 ──────────────────────────────────────────────────────────────

    private async void ExecuteNewProject()
    {
        var selectedPath = _dialogService.ShowOpenFolderDialog("选择标注项目目录");
        if (selectedPath == null)
            return;

        var projectName = Path.GetFileName(selectedPath);
        if (string.IsNullOrWhiteSpace(projectName))
            projectName = "AnnotationProject";

        var defaultClasses = new List<AnnotationClass>
        {
            new() { Index = 0, Name = "object", Color = "#FF0000" }
        };

        try
        {
            _annotationState.Project = await _annotationService.CreateProjectAsync(projectName, selectedPath, defaultClasses);
            _classState.UpdateClassesList(_annotationState.Project);
            _annotationState.StatusMessage = $"项目已创建: {projectName}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "创建项目失败");
            _annotationState.StatusMessage = $"创建项目失败: {ex.Message}";
        }
    }

    private async void ExecuteOpenProject()
    {
        var filePath = _dialogService.ShowOpenFileDialog(
            "ScaffoldX 标注项目|*.scaffoldx-annotation.json|JSON 文件|*.json",
            "打开标注项目");

        if (filePath == null)
            return;

        try
        {
            _annotationState.Project = await _annotationService.LoadProjectAsync(filePath);
            _classState.UpdateClassesList(_annotationState.Project);
            _annotationState.UpdateStatistics();

            if (_annotationState.Project.Annotations.Count > 0)
            {
                await _imageNavigationHandler.LoadImageAsync(0);
            }

            _annotationState.StatusMessage = $"项目已加载: {_annotationState.Project.ProjectName}, 共 {_annotationState.Project.Annotations.Count} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载项目失败");
            _annotationState.StatusMessage = $"加载项目失败: {ex.Message}";
        }
    }

    private async void ExecuteSaveProject()
    {
        if (_annotationState.Project == null) return;

        try
        {
            await _annotationService.SaveProjectAsync(_annotationState.Project);
            _annotationState.StatusMessage = "项目已保存";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存项目失败");
            _annotationState.StatusMessage = $"保存项目失败: {ex.Message}";
        }
    }

    // ── 图像导入 ──────────────────────────────────────────────────────────────

    private async void ExecuteAddImages()
    {
        if (_annotationState.Project == null)
        {
            _annotationState.StatusMessage = "请先创建或打开项目";
            return;
        }

        var filePaths = _dialogService.ShowOpenFilesDialog(
            "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|所有文件|*.*",
            "选择图像文件");

        if (filePaths == null || filePaths.Count == 0)
            return;

        try
        {
            await _annotationService.AddImagesAsync(_annotationState.Project, filePaths);
            _annotationState.UpdateStatistics();
            _imageNavigationHandler.RaiseCanNavigateChanged();

            if (_imageNavigationHandler.CurrentImageIndex < 0 && _annotationState.Project.Annotations.Count > 0)
            {
                await _imageNavigationHandler.LoadImageAsync(0);
            }

            _annotationState.StatusMessage = $"已添加 {filePaths.Count} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加图像失败");
            _annotationState.StatusMessage = $"添加图像失败: {ex.Message}";
        }
    }

    private async void ExecuteAddFolder()
    {
        if (_annotationState.Project == null)
        {
            _annotationState.StatusMessage = "请先创建或打开项目";
            return;
        }

        var selectedPath = _dialogService.ShowOpenFolderDialog("选择包含图像的文件夹");
        if (selectedPath == null) return;

        try
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif" };
            var imageFiles = Directory.EnumerateFiles(selectedPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (imageFiles.Count == 0)
            {
                _annotationState.StatusMessage = "所选文件夹中没有找到图像文件";
                return;
            }

            await _annotationService.AddImagesAsync(_annotationState.Project, imageFiles);
            _annotationState.UpdateStatistics();
            _imageNavigationHandler.RaiseCanNavigateChanged();

            if (_imageNavigationHandler.CurrentImageIndex < 0 && _annotationState.Project.Annotations.Count > 0)
                await _imageNavigationHandler.LoadImageAsync(0);

            _annotationState.StatusMessage = $"已从文件夹导入 {imageFiles.Count} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "文件夹导入失败");
            _annotationState.StatusMessage = $"文件夹导入失败: {ex.Message}";
        }
    }

    // ── 边界框操作 ────────────────────────────────────────────────────────────

    private bool CanDeleteSelectedBox() => _annotationState.SelectedBox != null;

    private void ExecuteDeleteSelectedBox()
    {
        if (_annotationState.SelectedBox == null || _annotationState.CurrentAnnotation == null) return;

        _undoRedoHandler.PushUndoSnapshot();
        _annotationState.CurrentAnnotation.Boxes.Remove(_annotationState.SelectedBox);
        _annotationState.SelectedBox = null;
        _annotationState.UpdateBoxesList();
        _annotationState.UpdateClassDistribution();
        _annotationState.StatusMessage = "已删除选中的边界框";
    }

    private void ExecuteClearAllBoxes()
    {
        if (_annotationState.CurrentAnnotation == null) return;

        _undoRedoHandler.PushUndoSnapshot();
        _annotationState.CurrentAnnotation.Boxes.Clear();
        _annotationState.UpdateBoxesList();
        _annotationState.UpdateClassDistribution();
        _annotationState.StatusMessage = "已清空所有边界框";
    }

    // ── 边界框绘制 ────────────────────────────────────────────────────────────

    private void ExecuteImageMouseDown(Point point)
    {
        if (_annotationState.Project == null || _annotationState.CurrentAnnotation == null) return;

        _annotationState.IsDrawing = true;
        _annotationState.DrawStartPoint = point;
        _annotationState.DrawEndPoint = point;
        _undoRedoHandler.PushUndoSnapshot();
    }

    private void ExecuteImageMouseMove(Point point)
    {
        if (!_annotationState.IsDrawing) return;
        _annotationState.DrawEndPoint = point;
    }

    private void ExecuteImageMouseUp(Point point)
    {
        if (!_annotationState.IsDrawing || _annotationState.Project == null || _annotationState.CurrentAnnotation == null) return;

        _annotationState.IsDrawing = false;
        _annotationState.DrawEndPoint = point;

        var currentImage = _imageNavigationHandler.CurrentImage;
        if (currentImage == null) return;

        var x1 = Math.Min(_annotationState.DrawStartPoint.X, _annotationState.DrawEndPoint.X);
        var y1 = Math.Min(_annotationState.DrawStartPoint.Y, _annotationState.DrawEndPoint.Y);
        var x2 = Math.Max(_annotationState.DrawStartPoint.X, _annotationState.DrawEndPoint.X);
        var y2 = Math.Max(_annotationState.DrawStartPoint.Y, _annotationState.DrawEndPoint.Y);

        if (x2 - x1 < 5 || y2 - y1 < 5)
            return;

        var centerX = (x1 + x2) / 2 / currentImage.PixelWidth;
        var centerY = (y1 + y2) / 2 / currentImage.PixelHeight;
        var width = (x2 - x1) / currentImage.PixelWidth;
        var height = (y2 - y1) / currentImage.PixelHeight;

        var selectedClass = _classManagementHandler.SelectedClassIndex < _annotationState.Project.Classes.Count
            ? _annotationState.Project.Classes[_classManagementHandler.SelectedClassIndex]
            : _annotationState.Project.Classes.FirstOrDefault();

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

        _annotationState.CurrentAnnotation.Boxes.Add(box);
        _annotationState.UpdateBoxesList();
        _annotationState.UpdateClassDistribution();

        _annotationState.StatusMessage = $"已添加边界框: {selectedClass.Name}";
    }

    // ── 模式切换 ──────────────────────────────────────────────────────────────

    private void ExecuteSwitchToBboxMode()
    {
        if (_polygonDrawingHandler.IsPolygonMode)
            _polygonDrawingHandler.IsPolygonMode = false;
        if (_obbDrawingHandler.IsObbMode)
            _obbDrawingHandler.IsObbMode = false;
        _annotationState.StatusMessage = "边界框模式";
    }

    private void ExecuteSwitchToPolygonMode()
    {
        if (!_polygonDrawingHandler.IsPolygonMode)
        {
            _polygonDrawingHandler.IsPolygonMode = true;
            _obbDrawingHandler.IsObbMode = false;
            _annotationState.StatusMessage = "多边形模式：单击添加顶点，双击完成";
        }
    }

    private void ExecuteSwitchToObbMode()
    {
        if (!_obbDrawingHandler.IsObbMode)
        {
            _obbDrawingHandler.IsObbMode = true;
            _polygonDrawingHandler.IsPolygonMode = false;
            _annotationState.StatusMessage = "OBB 模式：拖拽定义中心和大小，松开后移动鼠标设置角度，单击确认";
        }
    }

    private void ExecuteCancelDrawing()
    {
        if (_drawingState.IsDrawingPolygon)
            _polygonDrawingHandler.ExecuteCancelPolygon();
        else if (_drawingState.IsDrawingObb || _drawingState.IsRotatingObb)
            _obbDrawingHandler.ExecuteCancelObb();
        else if (_annotationState.IsDrawing)
        {
            _annotationState.IsDrawing = false;
            _annotationState.StatusMessage = "已取消绘制";
        }
    }

    // ── SAM 3 模型加载 ──────────────────────────────────────────────────────

    private async Task ExecuteLoadSam3ModelAsync()
    {
        var selectedPath = _dialogService.ShowOpenFolderDialog("选择 SAM 3 模型目录（需包含 encoder.pt、text_encoder.pt、decoder.pt）");
        if (selectedPath == null) return;

        try
        {
            _annotationState.StatusMessage = "正在加载 SAM 3 模型...";
            await _autoLabelingService.LoadSam3ModelAsync(selectedPath);
            _annotationState.StatusMessage = "SAM 3 模型加载完成";
            RaisePropertyChange?.Invoke("IsSam3ModelLoaded");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SAM 3 模型加载失败");
            _annotationState.StatusMessage = $"SAM 3 模型加载失败: {ex.Message}";
        }
    }

    // ── 缩放 ──────────────────────────────────────────────────────────────────

    private void ExecuteResetZoom()
    {
        _imageState.ResetZoom();
        _annotationState.StatusMessage = "缩放已重置为 100%";
    }
}
