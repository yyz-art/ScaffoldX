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
    private readonly IVideoFrameService _videoFrameService;
    private readonly ILogger _logger = Log.ForContext<AnnotationViewModel>();
    private readonly DrawingStateManager _drawingState = new();

    private readonly AutoLabelingCommandHandler _autoLabelingHandler;
    private readonly ImageNavigationHandler _imageNavigationHandler;
    private readonly ClassManagementHandler _classManagementHandler;
    private readonly PolygonDrawingHandler _polygonDrawingHandler;
    private readonly ObbDrawingHandler _obbDrawingHandler;
    private readonly UndoRedoHandler _undoRedoHandler;
    private readonly ExportCommandHandler _exportHandler;
    private readonly ReviewCommandHandler _reviewHandler;
    private readonly Sam3LabelingCommandHandler _sam3Handler;
    private readonly IAutoLabelingService _autoLabelingService;

    /// <summary>SAM 3 标注处理器（供 View 层调用点提示方法）。</summary>
    public Sam3LabelingCommandHandler Sam3Handler => _sam3Handler;

    private AnnotationProject? _project;
    private AnnotationData? _currentAnnotation;
    private bool _isDrawing;
    private BoundingBoxAnnotation? _selectedBox;
    private string _statusMessage = "就绪";
    private string _projectName = string.Empty;
    private int _totalImages;
    private int _annotatedImages;
    private int _currentBoxCount;

    private double _zoomLevel = 1.0;

    private int _totalBoxCount;
    private int _totalPolygonCount;
    private int _totalObbCount;
    private int _totalPolylineCount;
    private int _totalCircleCount;
    private int _totalAnnotationCount;
    private string _classDistributionText = string.Empty;
    private int _annotatedImageCount;

    /// <summary>
    /// 初始化标注 ViewModel，注册命令。
    /// </summary>
    /// <param name="annotationService">标注服务。</param>
    /// <param name="autoLabelingService">自动标注服务。</param>
    /// <param name="videoFrameService">视频帧提取服务。</param>
    public AnnotationViewModel(IAnnotationService annotationService, IAutoLabelingService autoLabelingService, IVideoFrameService videoFrameService)
    {
        _annotationService = annotationService;
        _videoFrameService = videoFrameService;
        _autoLabelingService = autoLabelingService;

        _imageNavigationHandler = new ImageNavigationHandler(
            annotationService,
            getProject: () => Project,
            getCurrentAnnotation: () => CurrentAnnotation,
            setCurrentAnnotation: value => CurrentAnnotation = value,
            getTotalImages: () => TotalImages,
            setStatusMessage: value => StatusMessage = value,
            updateBoxesList: UpdateBoxesList,
            updateStatistics: UpdateStatistics);

        _autoLabelingHandler = new AutoLabelingCommandHandler(
            autoLabelingService,
            getCurrentAnnotation: () => CurrentAnnotation,
            getProject: () => Project,
            getCurrentImage: () => _imageNavigationHandler.CurrentImage,
            setStatusMessage: value => StatusMessage = value,
            pushUndoSnapshot: PushUndoSnapshot,
            updateBoxesList: UpdateBoxesList,
            updateClassDistribution: UpdateClassDistribution,
            updateStatistics: UpdateStatistics);

        _classManagementHandler = new ClassManagementHandler(
            getProject: () => Project,
            updateClassesList: UpdateClassesList,
            setStatusMessage: value => StatusMessage = value);

        _undoRedoHandler = new UndoRedoHandler(
            getCurrentAnnotation: () => CurrentAnnotation,
            updateBoxesList: UpdateBoxesList,
            updateClassDistribution: UpdateClassDistribution,
            setStatusMessage: value => StatusMessage = value);

        _polygonDrawingHandler = new PolygonDrawingHandler(
            _drawingState,
            getProject: () => Project,
            getCurrentAnnotation: () => CurrentAnnotation,
            getCurrentImage: () => _imageNavigationHandler.CurrentImage,
            getSelectedClassIndex: () => SelectedClassIndex,
            getIsObbMode: () => _obbDrawingHandler.IsObbMode,
            disableObbMode: () => _obbDrawingHandler.IsObbMode = false,
            setStatusMessage: value => StatusMessage = value,
            pushUndoSnapshot: () => _undoRedoHandler.PushUndoSnapshot(),
            updateBoxesList: UpdateBoxesList,
            updateClassDistribution: UpdateClassDistribution);

        _obbDrawingHandler = new ObbDrawingHandler(
            _drawingState,
            getProject: () => Project,
            getCurrentAnnotation: () => CurrentAnnotation,
            getCurrentImage: () => _imageNavigationHandler.CurrentImage,
            getSelectedClassIndex: () => SelectedClassIndex,
            getIsPolygonMode: () => _polygonDrawingHandler.IsPolygonMode,
            disablePolygonMode: () => _polygonDrawingHandler.IsPolygonMode = false,
            setStatusMessage: value => StatusMessage = value,
            pushUndoSnapshot: () => _undoRedoHandler.PushUndoSnapshot(),
            updateBoxesList: UpdateBoxesList,
            updateClassDistribution: UpdateClassDistribution);

        _exportHandler = new ExportCommandHandler(
            annotationService,
            videoFrameService,
            getProject: () => Project,
            getCurrentAnnotation: () => CurrentAnnotation,
            getCurrentImageIndex: () => CurrentImageIndex,
            loadFirstImage: () => _imageNavigationHandler.LoadImageAsync(0),
            setStatusMessage: value => StatusMessage = value,
            updateBoxesList: UpdateBoxesList,
            updateStatistics: UpdateStatistics);

        _reviewHandler = new ReviewCommandHandler(
            getProject: () => Project,
            getCurrentImageIndex: () => CurrentImageIndex,
            loadImageAsync: index => _imageNavigationHandler.LoadImageAsync(index),
            setStatusMessage: value => StatusMessage = value,
            updateStatistics: UpdateStatistics,
            getPolylineCount: () => TotalPolylineCount,
            getCircleCount: () => TotalCircleCount);

        _sam3Handler = new Sam3LabelingCommandHandler(
            autoLabelingService,
            getCurrentAnnotation: () => CurrentAnnotation,
            getProject: () => Project,
            getCurrentImage: () => _imageNavigationHandler.CurrentImage,
            setStatusMessage: value => StatusMessage = value,
            pushUndoSnapshot: () => _undoRedoHandler.PushUndoSnapshot(),
            updateBoxesList: UpdateBoxesList,
            updateClassDistribution: UpdateClassDistribution,
            updateStatistics: UpdateStatistics);

        _autoLabelingHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _imageNavigationHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _classManagementHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _polygonDrawingHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _obbDrawingHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _undoRedoHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _exportHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _reviewHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _sam3Handler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);

        NewProjectCommand = new DelegateCommand(ExecuteNewProject);
        OpenProjectCommand = new DelegateCommand(ExecuteOpenProject);
        SaveProjectCommand = new DelegateCommand(ExecuteSaveProject);
        AddImagesCommand = new DelegateCommand(ExecuteAddImages);

        DeleteSelectedBoxCommand = new DelegateCommand(ExecuteDeleteSelectedBox, CanDeleteSelectedBox);
        ClearAllBoxesCommand = new DelegateCommand(ExecuteClearAllBoxes, () => HasBoxes);

        ImageMouseDownCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteImageMouseDown(p.Value); });
        ImageMouseMoveCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteImageMouseMove(p.Value); });
        ImageMouseUpCommand = new DelegateCommand<Point?>(p => { if (p.HasValue) ExecuteImageMouseUp(p.Value); });

        AddFolderCommand = new DelegateCommand(ExecuteAddFolder);

        SwitchToBboxModeCommand = new DelegateCommand(ExecuteSwitchToBboxMode);
        SwitchToPolygonModeCommand = new DelegateCommand(ExecuteSwitchToPolygonMode);
        SwitchToObbModeCommand = new DelegateCommand(ExecuteSwitchToObbMode);
        CancelDrawingCommand = new DelegateCommand(ExecuteCancelDrawing);

        LoadSam3ModelCommand = new DelegateCommand(async () => await ExecuteLoadSam3ModelAsync());
        ResetZoomCommand = new DelegateCommand(ExecuteResetZoom);
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

    /// <summary>当前显示的图像（转发至 ImageNavigationHandler）。</summary>
    public BitmapImage? CurrentImage => _imageNavigationHandler.CurrentImage;

    /// <summary>当前图像在列表中的索引（转发至 ImageNavigationHandler）。</summary>
    public int CurrentImageIndex => _imageNavigationHandler.CurrentImageIndex;

    /// <summary>当前选中的类别索引（转发至 ClassManagementHandler）。</summary>
    public int SelectedClassIndex
    {
        get => _classManagementHandler.SelectedClassIndex;
        set => _classManagementHandler.SelectedClassIndex = value;
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
        get => _drawingState.DrawStartPoint;
        private set
        {
            _drawingState.DrawStartPoint = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>绘制终点。</summary>
    public Point DrawEndPoint
    {
        get => _drawingState.DrawEndPoint;
        private set
        {
            _drawingState.DrawEndPoint = value;
            RaisePropertyChanged();
        }
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

    /// <summary>当前图像的所有标注（边界框 + 多边形 + OBB）统一集合，用于 ListBox 绑定。</summary>
    public ObservableCollection<object> AllAnnotations { get; } = new();

    /// <summary>是否有边界框。</summary>
    public bool HasBoxes => CurrentBoxes.Count > 0;

    // ── 标注统计属性 ────────────────────────────────────────────────────────

    /// <summary>当前图像的边界框数量。</summary>
    public int TotalBoxCount
    {
        get => _totalBoxCount;
        private set => SetProperty(ref _totalBoxCount, value);
    }

    /// <summary>当前图像的多边形数量。</summary>
    public int TotalPolygonCount
    {
        get => _totalPolygonCount;
        private set => SetProperty(ref _totalPolygonCount, value);
    }

    /// <summary>当前图像的 OBB 数量。</summary>
    public int TotalObbCount
    {
        get => _totalObbCount;
        private set => SetProperty(ref _totalObbCount, value);
    }

    /// <summary>当前图像的折线数量。</summary>
    public int TotalPolylineCount
    {
        get => _totalPolylineCount;
        private set => SetProperty(ref _totalPolylineCount, value);
    }

    /// <summary>当前图像的圆形数量。</summary>
    public int TotalCircleCount
    {
        get => _totalCircleCount;
        private set => SetProperty(ref _totalCircleCount, value);
    }

    /// <summary>当前图像的标注总数。</summary>
    public int TotalAnnotationCount
    {
        get => _totalAnnotationCount;
        private set => SetProperty(ref _totalAnnotationCount, value);
    }

    /// <summary>项目中各类别的标注数量汇总文本。</summary>
    public string ClassDistributionText
    {
        get => _classDistributionText;
        private set => SetProperty(ref _classDistributionText, value);
    }

    /// <summary>项目中已标注图像数量。</summary>
    public int AnnotatedImageCount
    {
        get => _annotatedImageCount;
        private set => SetProperty(ref _annotatedImageCount, value);
    }

    /// <summary>类别列表。</summary>
    public ObservableCollection<AnnotationClass> Classes { get; } = new();

    /// <summary>图像导航文字（转发至 ImageNavigationHandler）。</summary>
    public string ImageNavigationText => _imageNavigationHandler.ImageNavigationText;

    /// <summary>标注进度文字。</summary>
    public string AnnotationProgressText => Project == null
        ? string.Empty
        : $"已标注: {AnnotatedImages} / {TotalImages}";

    // ── 自动标注属性（转发至 AutoLabelingCommandHandler） ────────────────────

    /// <summary>模型是否已加载。</summary>
    public bool IsModelLoaded => _autoLabelingHandler.IsModelLoaded;

    /// <summary>已加载模型名称。</summary>
    public string LoadedModelName => _autoLabelingHandler.LoadedModelName;

    /// <summary>是否正在自动检测。</summary>
    public bool IsAutoDetecting => _autoLabelingHandler.IsAutoDetecting;

    /// <summary>置信度阈值。</summary>
    public float ConfidenceThreshold
    {
        get => _autoLabelingHandler.ConfidenceThreshold;
        set => _autoLabelingHandler.ConfidenceThreshold = value;
    }

    /// <summary>自动检测进度当前值。</summary>
    public int AutoDetectProgress => _autoLabelingHandler.AutoDetectProgress;

    /// <summary>自动检测进度总数。</summary>
    public int AutoDetectTotal => _autoLabelingHandler.AutoDetectTotal;

    /// <summary>自动检测进度文字。</summary>
    public string AutoDetectProgressText => _autoLabelingHandler.AutoDetectProgressText;

    // ── 多边形模式属性（转发至 PolygonDrawingHandler） ──────────────────────

    /// <summary>是否处于多边形绘制模式。</summary>
    public bool IsPolygonMode
    {
        get => _polygonDrawingHandler.IsPolygonMode;
        set => _polygonDrawingHandler.IsPolygonMode = value;
    }

    /// <summary>多边形模式切换按钮文字。</summary>
    public string PolygonModeButtonText => _polygonDrawingHandler.PolygonModeButtonText;

    /// <summary>当前正在绘制的多边形顶点集合（屏幕坐标）。</summary>
    public ObservableCollection<System.Windows.Point> CurrentPolygonPoints => _polygonDrawingHandler.CurrentPolygonPoints;

    // ── OBB 模式属性（转发至 ObbDrawingHandler） ────────────────────────────

    /// <summary>是否处于 OBB（旋转边界框）绘制模式。</summary>
    public bool IsObbMode
    {
        get => _obbDrawingHandler.IsObbMode;
        set => _obbDrawingHandler.IsObbMode = value;
    }

    /// <summary>OBB 模式切换按钮文字。</summary>
    public string ObbModeButtonText => _obbDrawingHandler.ObbModeButtonText;

    /// <summary>是否正在绘制 OBB（定义尺寸阶段）。</summary>
    public bool IsDrawingObb => _obbDrawingHandler.IsDrawingObb;

    /// <summary>是否正在旋转 OBB（设置角度阶段）。</summary>
    public bool IsRotatingObb => _obbDrawingHandler.IsRotatingObb;

    /// <summary>OBB 中心点（屏幕坐标）。</summary>
    public System.Windows.Point ObbCenter => _obbDrawingHandler.ObbCenter;

    /// <summary>OBB 尺寸（屏幕坐标）。</summary>
    public System.Windows.Size ObbSize => _obbDrawingHandler.ObbSize;

    /// <summary>OBB 旋转角度（弧度）。</summary>
    public double ObbAngle => _obbDrawingHandler.ObbAngle;

    /// <summary>当前缩放级别（1.0 = 100%）。</summary>
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (SetProperty(ref _zoomLevel, Math.Clamp(value, 0.1, 10.0)))
            {
                RaisePropertyChanged(nameof(ZoomLevelText));
            }
        }
    }

    /// <summary>缩放级别显示文本。</summary>
    public string ZoomLevelText => $"{ZoomLevel * 100:F0}%";

    /// <summary>视频导入进度文字（转发至 ExportCommandHandler）。</summary>
    public string VideoImportProgress => _exportHandler.VideoImportProgress;

    // ── 审查属性（转发至 ReviewCommandHandler） ──────────────────────────────

    /// <summary>审查摘要文本。</summary>
    public string ReviewSummaryText => _reviewHandler.ReviewSummaryText;

    /// <summary>未标注图像数量。</summary>
    public int UnannotatedImageCount => _reviewHandler.UnannotatedImageCount;

    /// <summary>是否有未标注图像。</summary>
    public bool HasUnannotatedImages => _reviewHandler.HasUnannotatedImages;

    // ── 命令（转发至处理器） ────────────────────────────────────────────────

    /// <summary>上一张图像命令。</summary>
    public DelegateCommand PreviousImageCommand => _imageNavigationHandler.PreviousImageCommand;

    /// <summary>下一张图像命令。</summary>
    public DelegateCommand NextImageCommand => _imageNavigationHandler.NextImageCommand;

    /// <summary>添加类别命令。</summary>
    public DelegateCommand AddClassCommand => _classManagementHandler.AddClassCommand;

    /// <summary>移除类别命令。</summary>
    public DelegateCommand RemoveClassCommand => _classManagementHandler.RemoveClassCommand;

    /// <summary>加载模型命令。</summary>
    public DelegateCommand LoadModelCommand => _autoLabelingHandler.LoadModelCommand;

    /// <summary>加载 SAM 3 模型命令。</summary>
    public DelegateCommand LoadSam3ModelCommand { get; }

    /// <summary>卸载模型命令。</summary>
    public DelegateCommand UnloadModelCommand => _autoLabelingHandler.UnloadModelCommand;

    /// <summary>自动检测当前图像命令。</summary>
    public DelegateCommand AutoDetectCurrentCommand => _autoLabelingHandler.AutoDetectCurrentCommand;

    /// <summary>自动检测所有图像命令。</summary>
    public DelegateCommand AutoDetectAllCommand => _autoLabelingHandler.AutoDetectAllCommand;

    /// <summary>按索引选择类别命令。</summary>
    public DelegateCommand<int?> SelectClassCommand => _classManagementHandler.SelectClassCommand;

    /// <summary>撤销命令。</summary>
    public DelegateCommand UndoCommand => _undoRedoHandler.UndoCommand;

    /// <summary>重做命令。</summary>
    public DelegateCommand RedoCommand => _undoRedoHandler.RedoCommand;

    /// <summary>切换多边形模式命令。</summary>
    public DelegateCommand TogglePolygonModeCommand => _polygonDrawingHandler.TogglePolygonModeCommand;

    /// <summary>完成多边形绘制命令。</summary>
    public DelegateCommand FinishPolygonCommand => _polygonDrawingHandler.FinishPolygonCommand;

    /// <summary>取消多边形绘制命令。</summary>
    public DelegateCommand CancelPolygonCommand => _polygonDrawingHandler.CancelPolygonCommand;

    /// <summary>多边形模式鼠标按下命令。</summary>
    public DelegateCommand<Point?> PolygonMouseDownCommand => _polygonDrawingHandler.PolygonMouseDownCommand;

    /// <summary>多边形模式双击命令。</summary>
    public DelegateCommand<Point?> PolygonDoubleClickCommand => _polygonDrawingHandler.PolygonDoubleClickCommand;

    /// <summary>切换 OBB 模式命令。</summary>
    public DelegateCommand ToggleObbModeCommand => _obbDrawingHandler.ToggleObbModeCommand;

    /// <summary>完成 OBB 绘制命令。</summary>
    public DelegateCommand FinishObbCommand => _obbDrawingHandler.FinishObbCommand;

    /// <summary>取消 OBB 绘制命令。</summary>
    public DelegateCommand CancelObbCommand => _obbDrawingHandler.CancelObbCommand;

    /// <summary>OBB 模式鼠标按下命令。</summary>
    public DelegateCommand<Point?> ObbMouseDownCommand => _obbDrawingHandler.ObbMouseDownCommand;

    /// <summary>OBB 模式鼠标移动命令。</summary>
    public DelegateCommand<Point?> ObbMouseMoveCommand => _obbDrawingHandler.ObbMouseMoveCommand;

    /// <summary>OBB 模式鼠标抬起命令。</summary>
    public DelegateCommand<Point?> ObbMouseUpCommand => _obbDrawingHandler.ObbMouseUpCommand;

    // ── SAM 3 分割属性（转发至 Sam3LabelingCommandHandler） ────────────────

    /// <summary>SAM 3 当前提示模式。</summary>
    public Sam3PromptMode Sam3PromptMode
    {
        get => _sam3Handler.CurrentPromptMode;
        set => _sam3Handler.CurrentPromptMode = value;
    }

    /// <summary>SAM 3 提示点集合。</summary>
    public ObservableCollection<Sam3Point> Sam3PromptPoints => _sam3Handler.PromptPoints;

    /// <summary>SAM 3 文本提示输入。</summary>
    public string Sam3TextPrompt
    {
        get => _sam3Handler.TextPromptInput;
        set => _sam3Handler.TextPromptInput = value;
    }

    /// <summary>SAM 3 是否正在处理。</summary>
    public bool IsSam3Processing => _sam3Handler.IsProcessing;

    /// <summary>SAM 3 模型是否已加载。</summary>
    public bool IsSam3ModelLoaded => _sam3Handler.IsSam3ModelLoaded;

    /// <summary>SAM 3 掩码预览。</summary>
    public byte[,]? Sam3MaskPreview => _sam3Handler.CurrentMaskPreview;

    /// <summary>是否有 SAM 3 掩码预览。</summary>
    public bool HasSam3MaskPreview => _sam3Handler.HasMaskPreview;

    // ── SAM 3 命令（转发至 Sam3LabelingCommandHandler） ────────────────────

    /// <summary>SAM 3 文本分割命令。</summary>
    public DelegateCommand Sam3SegmentByTextCommand => _sam3Handler.SegmentByTextCommand;

    /// <summary>SAM 3 批量文本分割命令。</summary>
    public DelegateCommand Sam3SegmentAllByTextCommand => _sam3Handler.SegmentAllByTextCommand;

    /// <summary>SAM 3 进入点模式命令。</summary>
    public DelegateCommand Sam3EnterPointModeCommand => _sam3Handler.EnterPointModeCommand;

    /// <summary>SAM 3 接受掩码命令。</summary>
    public DelegateCommand Sam3AcceptMaskCommand => _sam3Handler.AcceptMaskCommand;

    /// <summary>SAM 3 清除提示点命令。</summary>
    public DelegateCommand Sam3ClearPointsCommand => _sam3Handler.ClearPointsCommand;

    /// <summary>SAM 3 选择参考图命令。</summary>
    public DelegateCommand Sam3SelectReferenceCommand => _sam3Handler.SelectReferenceCommand;

    /// <summary>SAM 3 参考图分割命令。</summary>
    public DelegateCommand Sam3SegmentByReferenceCommand => _sam3Handler.SegmentByReferenceCommand;

    /// <summary>导出 YOLO 数据集命令（转发至 ExportCommandHandler）。</summary>
    public DelegateCommand ExportYoloCommand => _exportHandler.ExportYoloCommand;

    /// <summary>导出 COCO 数据集命令（转发至 ExportCommandHandler）。</summary>
    public DelegateCommand ExportCocoCommand => _exportHandler.ExportCocoCommand;

    /// <summary>导出 VOC 数据集命令（转发至 ExportCommandHandler）。</summary>
    public DelegateCommand ExportVocCommand => _exportHandler.ExportVocCommand;

    /// <summary>导出 DOTA 数据集命令（转发至 ExportCommandHandler）。</summary>
    public DelegateCommand ExportDotCommand => _exportHandler.ExportDotCommand;

    /// <summary>导出 MOT 数据集命令（转发至 ExportCommandHandler）。</summary>
    public DelegateCommand ExportMotCommand => _exportHandler.ExportMotCommand;

    /// <summary>导入标注命令（转发至 ExportCommandHandler）。</summary>
    public DelegateCommand ImportAnnotationsCommand => _exportHandler.ImportAnnotationsCommand;

    /// <summary>导入视频命令（转发至 ExportCommandHandler）。</summary>
    public DelegateCommand ImportVideoCommand => _exportHandler.ImportVideoCommand;

    /// <summary>跳转到下一个未标注图像命令（转发至 ReviewCommandHandler）。</summary>
    public DelegateCommand GotoNextUnannotatedCommand => _reviewHandler.GotoNextUnannotatedCommand;

    /// <summary>刷新审查摘要命令（转发至 ReviewCommandHandler）。</summary>
    public DelegateCommand RefreshReviewSummaryCommand => _reviewHandler.RefreshReviewSummaryCommand;

    // ── 命令（ViewModel 持有） ───────────────────────────────────────────────

    public DelegateCommand NewProjectCommand { get; }
    public DelegateCommand OpenProjectCommand { get; }
    public DelegateCommand SaveProjectCommand { get; }
    public DelegateCommand AddImagesCommand { get; }

    public DelegateCommand DeleteSelectedBoxCommand { get; }
    public DelegateCommand ClearAllBoxesCommand { get; }

    public DelegateCommand<Point?> ImageMouseDownCommand { get; }
    public DelegateCommand<Point?> ImageMouseMoveCommand { get; }
    public DelegateCommand<Point?> ImageMouseUpCommand { get; }

    public DelegateCommand AddFolderCommand { get; }

    public DelegateCommand SwitchToBboxModeCommand { get; }
    public DelegateCommand SwitchToPolygonModeCommand { get; }
    public DelegateCommand SwitchToObbModeCommand { get; }
    public DelegateCommand CancelDrawingCommand { get; }
    public DelegateCommand ResetZoomCommand { get; }

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
                await _imageNavigationHandler.LoadImageAsync(0);
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

            if (CurrentImageIndex < 0 && Project.Annotations.Count > 0)
            {
                await _imageNavigationHandler.LoadImageAsync(0);
            }

            StatusMessage = $"已添加 {dialog.FileNames.Length} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加图像失败");
            StatusMessage = $"添加图像失败: {ex.Message}";
        }
    }

    private bool CanDeleteSelectedBox() => SelectedBox != null;

    private void ExecuteDeleteSelectedBox()
    {
        if (SelectedBox == null || CurrentAnnotation == null) return;

        PushUndoSnapshot();
        CurrentAnnotation.Boxes.Remove(SelectedBox);
        SelectedBox = null;
        UpdateBoxesList();
        UpdateClassDistribution();
        StatusMessage = "已删除选中的边界框";
    }

    private void ExecuteClearAllBoxes()
    {
        if (CurrentAnnotation == null) return;

        PushUndoSnapshot();
        CurrentAnnotation.Boxes.Clear();
        UpdateBoxesList();
        UpdateClassDistribution();
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

    private void ExecuteImageMouseUp(Point point)
    {
        if (!IsDrawing || Project == null || CurrentAnnotation == null) return;

        IsDrawing = false;
        DrawEndPoint = point;

        if (CurrentImage == null) return;

        var x1 = Math.Min(DrawStartPoint.X, DrawEndPoint.X);
        var y1 = Math.Min(DrawStartPoint.Y, DrawEndPoint.Y);
        var x2 = Math.Max(DrawStartPoint.X, DrawEndPoint.X);
        var y2 = Math.Max(DrawStartPoint.Y, DrawEndPoint.Y);

        if (x2 - x1 < 5 || y2 - y1 < 5)
            return;

        var centerX = (x1 + x2) / 2 / CurrentImage.PixelWidth;
        var centerY = (y1 + y2) / 2 / CurrentImage.PixelHeight;
        var width = (x2 - x1) / CurrentImage.PixelWidth;
        var height = (y2 - y1) / CurrentImage.PixelHeight;

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
        UpdateClassDistribution();

        StatusMessage = $"已添加边界框: {selectedClass.Name}";
    }

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
                await _imageNavigationHandler.LoadImageAsync(0);

            StatusMessage = $"已从文件夹导入 {imageFiles.Count} 张图像";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "文件夹导入失败");
            StatusMessage = $"文件夹导入失败: {ex.Message}";
        }
    }

    // ── SAM 3 模型加载 ──────────────────────────────────────────────────────

    private async Task ExecuteLoadSam3ModelAsync()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "选择 SAM 3 模型目录（需包含 encoder.pt、text_encoder.pt、decoder.pt）"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            StatusMessage = "正在加载 SAM 3 模型...";
            await _autoLabelingService.LoadSam3ModelAsync(dialog.SelectedPath);
            StatusMessage = "SAM 3 模型加载完成";
            RaisePropertyChanged(nameof(IsSam3ModelLoaded));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SAM 3 模型加载失败");
            StatusMessage = $"SAM 3 模型加载失败: {ex.Message}";
        }
    }

    // ── 撤销/重做（委托至 UndoRedoHandler） ────────────────────────────────

    /// <summary>
    /// 推送当前标注数据的快照到撤销栈。
    /// </summary>
    private void PushUndoSnapshot() => _undoRedoHandler.PushUndoSnapshot();

    // ── 快捷键命令实现 ────────────────────────────────────────────────────────

    private void ExecuteSwitchToBboxMode()
    {
        if (_polygonDrawingHandler.IsPolygonMode)
            _polygonDrawingHandler.IsPolygonMode = false;
        if (_obbDrawingHandler.IsObbMode)
            _obbDrawingHandler.IsObbMode = false;
        StatusMessage = "边界框模式";
    }

    private void ExecuteSwitchToPolygonMode()
    {
        if (!_polygonDrawingHandler.IsPolygonMode)
        {
            _polygonDrawingHandler.IsPolygonMode = true;
            _obbDrawingHandler.IsObbMode = false;
            StatusMessage = "多边形模式：单击添加顶点，双击完成";
        }
    }

    private void ExecuteSwitchToObbMode()
    {
        if (!_obbDrawingHandler.IsObbMode)
        {
            _obbDrawingHandler.IsObbMode = true;
            _polygonDrawingHandler.IsPolygonMode = false;
            StatusMessage = "OBB 模式：拖拽定义中心和大小，松开后移动鼠标设置角度，单击确认";
        }
    }

    private void ExecuteCancelDrawing()
    {
        if (_drawingState.IsDrawingPolygon)
            _polygonDrawingHandler.ExecuteCancelPolygon();
        else if (_drawingState.IsDrawingObb || _drawingState.IsRotatingObb)
            _obbDrawingHandler.ExecuteCancelObb();
        else if (IsDrawing)
        {
            IsDrawing = false;
            StatusMessage = "已取消绘制";
        }
    }

    private void ExecuteResetZoom()
    {
        ZoomLevel = 1.0;
        StatusMessage = "缩放已重置为 100%";
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

    private void UpdateClassesList()
    {
        Classes.Clear();
        if (Project == null) return;

        foreach (var cls in Project.Classes)
            Classes.Add(cls);
    }

    private void UpdateBoxesList()
    {
        CurrentBoxes.Clear();
        AllAnnotations.Clear();

        if (CurrentAnnotation == null)
        {
            CurrentBoxCount = 0;
            UpdateAnnotationStatistics();
            RaisePropertyChanged(nameof(HasBoxes));
            return;
        }

        foreach (var box in CurrentAnnotation.Boxes)
        {
            CurrentBoxes.Add(box);
            AllAnnotations.Add(box);
        }

        foreach (var polygon in CurrentAnnotation.Polygons)
            AllAnnotations.Add(polygon);

        foreach (var obb in CurrentAnnotation.OrientedBoxes)
            AllAnnotations.Add(obb);

        foreach (var seg in CurrentAnnotation.Segmentations)
            AllAnnotations.Add(seg);

        CurrentBoxCount = CurrentBoxes.Count;
        RaisePropertyChanged(nameof(HasBoxes));
        UpdateAnnotationStatistics();
    }

    private void UpdateStatistics()
    {
        if (Project == null) return;

        TotalImages = Project.Annotations.Count;
        AnnotatedImages = Project.Annotations.Count(a =>
            a.Boxes.Count > 0 || a.Polygons.Count > 0 || a.OrientedBoxes.Count > 0 || a.Polylines.Count > 0 || a.Circles.Count > 0 || a.Segmentations.Count > 0);
        AnnotatedImageCount = AnnotatedImages;
        ProjectName = Project.ProjectName;

        RaisePropertyChanged(nameof(AnnotationProgressText));
        UpdateClassDistribution();
    }

    private void UpdateAnnotationStatistics()
    {
        if (CurrentAnnotation == null)
        {
            TotalBoxCount = 0;
            TotalPolygonCount = 0;
            TotalObbCount = 0;
            TotalPolylineCount = 0;
            TotalCircleCount = 0;
            TotalAnnotationCount = 0;
            return;
        }

        TotalBoxCount = CurrentAnnotation.Boxes.Count;
        TotalPolygonCount = CurrentAnnotation.Polygons.Count;
        TotalObbCount = CurrentAnnotation.OrientedBoxes.Count;
        TotalPolylineCount = CurrentAnnotation.Polylines.Count;
        TotalCircleCount = CurrentAnnotation.Circles.Count;
        TotalAnnotationCount = TotalBoxCount + TotalPolygonCount + TotalObbCount + TotalPolylineCount + TotalCircleCount + CurrentAnnotation.Segmentations.Count;
    }

    private void UpdateClassDistribution()
    {
        if (Project == null)
        {
            ClassDistributionText = string.Empty;
            return;
        }

        var distribution = new Dictionary<string, int>();

        foreach (var annotation in Project.Annotations)
        {
            foreach (var box in annotation.Boxes)
                distribution[box.ClassName] = distribution.GetValueOrDefault(box.ClassName) + 1;

            foreach (var polygon in annotation.Polygons)
                distribution[polygon.ClassName] = distribution.GetValueOrDefault(polygon.ClassName) + 1;

            foreach (var obb in annotation.OrientedBoxes)
                distribution[obb.ClassName] = distribution.GetValueOrDefault(obb.ClassName) + 1;

            foreach (var seg in annotation.Segmentations)
                distribution[seg.ClassName] = distribution.GetValueOrDefault(seg.ClassName) + 1;
        }

        ClassDistributionText = distribution.Count == 0
            ? "暂无标注数据"
            : string.Join(", ", distribution.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}: {kv.Value}"));
    }
}
