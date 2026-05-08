using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// YOLO 标注工具 ViewModel，作为轻量 facade 协调子 ViewModel 和 handler。
/// 状态由 <see cref="AnnotationStateVM"/>、<see cref="ImageStateVM"/>、<see cref="ClassStateVM"/> 管理，
/// 命令实现由 <see cref="ProjectCommandHandler"/> 和 9 个 handler 承担。
/// </summary>
public class AnnotationViewModel : BindableBase
{
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
    private readonly ProjectCommandHandler _projectCommandHandler;

    /// <summary>
    /// 初始化标注 ViewModel，注册命令。
    /// </summary>
    public AnnotationViewModel(IAnnotationService annotationService, IAutoLabelingService autoLabelingService, IVideoFrameService videoFrameService, IDialogService dialogService)
    {
        // ── 创建子 ViewModel ──────────────────────────────────────────────
        ImageState = new ImageStateVM();
        AnnotationState = new AnnotationStateVM(_drawingState);
        ClassState = new ClassStateVM();

        // ── 创建共享上下文 ────────────────────────────────────────────────
        var ctx = new AnnotationContext
        {
            GetProject = () => AnnotationState.Project,
            GetCurrentAnnotation = () => AnnotationState.CurrentAnnotation,
            SetCurrentAnnotation = value => AnnotationState.CurrentAnnotation = value,
            GetCurrentImage = () => _imageNavigationHandler.CurrentImage,
            GetCurrentImageIndex = () => _imageNavigationHandler.CurrentImageIndex,
            GetTotalImages = () => AnnotationState.TotalImages,
            GetSelectedClassIndex = () => _classManagementHandler.SelectedClassIndex,
            GetPolylineCount = () => AnnotationState.TotalPolylineCount,
            GetCircleCount = () => AnnotationState.TotalCircleCount,
            SetStatusMessage = value => AnnotationState.StatusMessage = value,
            UpdateBoxesList = () => AnnotationState.UpdateBoxesList(),
            UpdateStatistics = () => AnnotationState.UpdateStatistics(),
            UpdateClassDistribution = () => AnnotationState.UpdateClassDistribution(),
            UpdateClassesList = () => ClassState.UpdateClassesList(AnnotationState.Project),
            PushUndoSnapshot = () => _undoRedoHandler.PushUndoSnapshot(),
            LoadFirstImage = () => _imageNavigationHandler.LoadImageAsync(0),
            LoadImageAsync = index => _imageNavigationHandler.LoadImageAsync(index),
            DrawingState = _drawingState,
            GetIsObbMode = () => _obbDrawingHandler.IsObbMode,
            GetIsPolygonMode = () => _polygonDrawingHandler.IsPolygonMode,
            DisableObbMode = () => _obbDrawingHandler.IsObbMode = false,
            DisablePolygonMode = () => _polygonDrawingHandler.IsPolygonMode = false,
        };

        // ── 创建 handler ──────────────────────────────────────────────────
        _imageNavigationHandler = new ImageNavigationHandler(annotationService, ctx);
        _autoLabelingHandler = new AutoLabelingCommandHandler(autoLabelingService, ctx);
        _classManagementHandler = new ClassManagementHandler(ctx);
        _undoRedoHandler = new UndoRedoHandler(ctx);
        _polygonDrawingHandler = new PolygonDrawingHandler(ctx);
        _obbDrawingHandler = new ObbDrawingHandler(ctx);
        _exportHandler = new ExportCommandHandler(annotationService, videoFrameService, ctx);
        _reviewHandler = new ReviewCommandHandler(ctx);
        _sam3Handler = new Sam3LabelingCommandHandler(autoLabelingService, ctx);

        // ── 创建项目命令处理器 ─────────────────────────────────────────────
        _projectCommandHandler = new ProjectCommandHandler(
            annotationService, autoLabelingService, dialogService,
            AnnotationState, ClassState, ImageState,
            _imageNavigationHandler, _classManagementHandler,
            _polygonDrawingHandler, _obbDrawingHandler,
            _undoRedoHandler, _drawingState);

        // ── 转发 PropertyChanged ──────────────────────────────────────────
        _autoLabelingHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _imageNavigationHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _classManagementHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _polygonDrawingHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _obbDrawingHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _undoRedoHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _exportHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _reviewHandler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _sam3Handler.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        AnnotationState.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        ClassState.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        ImageState.PropertyChanged += (_, e) => RaisePropertyChanged(e.PropertyName);
        _projectCommandHandler.RaisePropertyChange += name => RaisePropertyChanged(name);
    }

    // ── 子 ViewModel（公开供测试和高级绑定） ──────────────────────────────────

    /// <summary>图像状态子 ViewModel（缩放）。</summary>
    public ImageStateVM ImageState { get; }

    /// <summary>标注状态子 ViewModel（项目、标注数据、统计）。</summary>
    public AnnotationStateVM AnnotationState { get; }

    /// <summary>类别状态子 ViewModel（类别列表）。</summary>
    public ClassStateVM ClassState { get; }

    /// <summary>SAM 3 标注处理器（供 View 层调用点提示方法）。</summary>
    public Sam3LabelingCommandHandler Sam3Handler => _sam3Handler;

    // ── 向后兼容属性（委托至子 ViewModel） ────────────────────────────────────

    /// <summary>当前标注项目。</summary>
    public AnnotationProject? Project
    {
        get => AnnotationState.Project;
        private set
        {
            var changed = AnnotationState.Project != value;
            AnnotationState.Project = value;
            if (changed)
                _imageNavigationHandler.RaiseCanNavigateChanged();
        }
    }

    /// <summary>当前图像的标注数据。</summary>
    public AnnotationData? CurrentAnnotation
    {
        get => AnnotationState.CurrentAnnotation;
        private set => AnnotationState.CurrentAnnotation = value;
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
        get => AnnotationState.IsDrawing;
        private set => AnnotationState.IsDrawing = value;
    }

    /// <summary>绘制起点。</summary>
    public Point DrawStartPoint
    {
        get => AnnotationState.DrawStartPoint;
        private set => AnnotationState.DrawStartPoint = value;
    }

    /// <summary>绘制终点。</summary>
    public Point DrawEndPoint
    {
        get => AnnotationState.DrawEndPoint;
        private set => AnnotationState.DrawEndPoint = value;
    }

    /// <summary>当前选中的边界框。</summary>
    public BoundingBoxAnnotation? SelectedBox
    {
        get => AnnotationState.SelectedBox;
        set => AnnotationState.SelectedBox = value;
    }

    /// <summary>状态消息。</summary>
    public string StatusMessage
    {
        get => AnnotationState.StatusMessage;
        private set => AnnotationState.StatusMessage = value;
    }

    /// <summary>项目名称。</summary>
    public string ProjectName
    {
        get => AnnotationState.ProjectName;
        private set => AnnotationState.ProjectName = value;
    }

    /// <summary>总图像数。</summary>
    public int TotalImages
    {
        get => AnnotationState.TotalImages;
        private set => AnnotationState.TotalImages = value;
    }

    /// <summary>已标注图像数。</summary>
    public int AnnotatedImages
    {
        get => AnnotationState.AnnotatedImages;
        private set => AnnotationState.AnnotatedImages = value;
    }

    /// <summary>当前图像的边界框数量。</summary>
    public int CurrentBoxCount
    {
        get => AnnotationState.CurrentBoxCount;
        private set => AnnotationState.CurrentBoxCount = value;
    }

    /// <summary>当前图像的边界框集合（用于绑定）。</summary>
    public ObservableCollection<BoundingBoxAnnotation> CurrentBoxes => AnnotationState.CurrentBoxes;

    /// <summary>当前图像的所有标注统一集合。</summary>
    public ObservableCollection<object> AllAnnotations => AnnotationState.AllAnnotations;

    /// <summary>是否有边界框。</summary>
    public bool HasBoxes => AnnotationState.HasBoxes;

    // ── 标注统计属性（委托至 AnnotationState） ──────────────────────────────

    public int TotalBoxCount { get => AnnotationState.TotalBoxCount; private set => AnnotationState.TotalBoxCount = value; }
    public int TotalPolygonCount { get => AnnotationState.TotalPolygonCount; private set => AnnotationState.TotalPolygonCount = value; }
    public int TotalObbCount { get => AnnotationState.TotalObbCount; private set => AnnotationState.TotalObbCount = value; }
    public int TotalPolylineCount { get => AnnotationState.TotalPolylineCount; private set => AnnotationState.TotalPolylineCount = value; }
    public int TotalCircleCount { get => AnnotationState.TotalCircleCount; private set => AnnotationState.TotalCircleCount = value; }
    public int TotalAnnotationCount { get => AnnotationState.TotalAnnotationCount; private set => AnnotationState.TotalAnnotationCount = value; }
    public string ClassDistributionText { get => AnnotationState.ClassDistributionText; private set => AnnotationState.ClassDistributionText = value; }
    public int AnnotatedImageCount { get => AnnotationState.AnnotatedImageCount; private set => AnnotationState.AnnotatedImageCount = value; }

    /// <summary>类别列表。</summary>
    public ObservableCollection<AnnotationClass> Classes => ClassState.Classes;

    /// <summary>图像导航文字（转发至 ImageNavigationHandler）。</summary>
    public string ImageNavigationText => _imageNavigationHandler.ImageNavigationText;

    /// <summary>标注进度文字。</summary>
    public string AnnotationProgressText => AnnotationState.AnnotationProgressText;

    // ── 缩放属性（委托至 ImageState） ────────────────────────────────────────

    public double ZoomLevel { get => ImageState.ZoomLevel; set => ImageState.ZoomLevel = value; }
    public string ZoomLevelText => ImageState.ZoomLevelText;

    // ── 自动标注属性（转发至 AutoLabelingCommandHandler） ────────────────────

    public bool IsModelLoaded => _autoLabelingHandler.IsModelLoaded;
    public string LoadedModelName => _autoLabelingHandler.LoadedModelName;
    public bool IsAutoDetecting => _autoLabelingHandler.IsAutoDetecting;
    public float ConfidenceThreshold { get => _autoLabelingHandler.ConfidenceThreshold; set => _autoLabelingHandler.ConfidenceThreshold = value; }
    public int AutoDetectProgress => _autoLabelingHandler.AutoDetectProgress;
    public int AutoDetectTotal => _autoLabelingHandler.AutoDetectTotal;
    public string AutoDetectProgressText => _autoLabelingHandler.AutoDetectProgressText;

    // ── 多边形模式属性（转发至 PolygonDrawingHandler） ──────────────────────

    public bool IsPolygonMode { get => _polygonDrawingHandler.IsPolygonMode; set => _polygonDrawingHandler.IsPolygonMode = value; }
    public string PolygonModeButtonText => _polygonDrawingHandler.PolygonModeButtonText;
    public ObservableCollection<System.Windows.Point> CurrentPolygonPoints => _polygonDrawingHandler.CurrentPolygonPoints;

    // ── OBB 模式属性（转发至 ObbDrawingHandler） ────────────────────────────

    public bool IsObbMode { get => _obbDrawingHandler.IsObbMode; set => _obbDrawingHandler.IsObbMode = value; }
    public string ObbModeButtonText => _obbDrawingHandler.ObbModeButtonText;
    public bool IsDrawingObb => _obbDrawingHandler.IsDrawingObb;
    public bool IsRotatingObb => _obbDrawingHandler.IsRotatingObb;
    public System.Windows.Point ObbCenter => _obbDrawingHandler.ObbCenter;
    public System.Windows.Size ObbSize => _obbDrawingHandler.ObbSize;
    public double ObbAngle => _obbDrawingHandler.ObbAngle;

    // ── 视频导入属性（转发至 ExportCommandHandler） ──────────────────────────

    public string VideoImportProgress => _exportHandler.VideoImportProgress;

    // ── 审查属性（转发至 ReviewCommandHandler） ──────────────────────────────

    public string ReviewSummaryText => _reviewHandler.ReviewSummaryText;
    public int UnannotatedImageCount => _reviewHandler.UnannotatedImageCount;
    public bool HasUnannotatedImages => _reviewHandler.HasUnannotatedImages;

    // ── SAM 3 属性（转发至 Sam3LabelingCommandHandler） ────────────────────

    public Sam3PromptMode Sam3PromptMode { get => _sam3Handler.CurrentPromptMode; set => _sam3Handler.CurrentPromptMode = value; }
    public ObservableCollection<Sam3Point> Sam3PromptPoints => _sam3Handler.PromptPoints;
    public string Sam3TextPrompt { get => _sam3Handler.TextPromptInput; set => _sam3Handler.TextPromptInput = value; }
    public bool IsSam3Processing => _sam3Handler.IsProcessing;
    public bool IsSam3ModelLoaded => _sam3Handler.IsSam3ModelLoaded;
    public byte[,]? Sam3MaskPreview => _sam3Handler.CurrentMaskPreview;
    public bool HasSam3MaskPreview => _sam3Handler.HasMaskPreview;

    // ── 命令（转发至 ProjectCommandHandler） ────────────────────────────────

    public DelegateCommand NewProjectCommand => _projectCommandHandler.NewProjectCommand;
    public DelegateCommand OpenProjectCommand => _projectCommandHandler.OpenProjectCommand;
    public DelegateCommand SaveProjectCommand => _projectCommandHandler.SaveProjectCommand;
    public DelegateCommand AddImagesCommand => _projectCommandHandler.AddImagesCommand;
    public DelegateCommand DeleteSelectedBoxCommand => _projectCommandHandler.DeleteSelectedBoxCommand;
    public DelegateCommand ClearAllBoxesCommand => _projectCommandHandler.ClearAllBoxesCommand;
    public DelegateCommand<Point?> ImageMouseDownCommand => _projectCommandHandler.ImageMouseDownCommand;
    public DelegateCommand<Point?> ImageMouseMoveCommand => _projectCommandHandler.ImageMouseMoveCommand;
    public DelegateCommand<Point?> ImageMouseUpCommand => _projectCommandHandler.ImageMouseUpCommand;
    public DelegateCommand AddFolderCommand => _projectCommandHandler.AddFolderCommand;
    public DelegateCommand SwitchToBboxModeCommand => _projectCommandHandler.SwitchToBboxModeCommand;
    public DelegateCommand SwitchToPolygonModeCommand => _projectCommandHandler.SwitchToPolygonModeCommand;
    public DelegateCommand SwitchToObbModeCommand => _projectCommandHandler.SwitchToObbModeCommand;
    public DelegateCommand CancelDrawingCommand => _projectCommandHandler.CancelDrawingCommand;
    public DelegateCommand LoadSam3ModelCommand => _projectCommandHandler.LoadSam3ModelCommand;
    public DelegateCommand ResetZoomCommand => _projectCommandHandler.ResetZoomCommand;

    // ── 命令（转发至 handler） ──────────────────────────────────────────────

    public DelegateCommand PreviousImageCommand => _imageNavigationHandler.PreviousImageCommand;
    public DelegateCommand NextImageCommand => _imageNavigationHandler.NextImageCommand;
    public DelegateCommand AddClassCommand => _classManagementHandler.AddClassCommand;
    public DelegateCommand RemoveClassCommand => _classManagementHandler.RemoveClassCommand;
    public DelegateCommand<int?> SelectClassCommand => _classManagementHandler.SelectClassCommand;
    public DelegateCommand LoadModelCommand => _autoLabelingHandler.LoadModelCommand;
    public DelegateCommand UnloadModelCommand => _autoLabelingHandler.UnloadModelCommand;
    public DelegateCommand AutoDetectCurrentCommand => _autoLabelingHandler.AutoDetectCurrentCommand;
    public DelegateCommand AutoDetectAllCommand => _autoLabelingHandler.AutoDetectAllCommand;
    public DelegateCommand UndoCommand => _undoRedoHandler.UndoCommand;
    public DelegateCommand RedoCommand => _undoRedoHandler.RedoCommand;
    public DelegateCommand TogglePolygonModeCommand => _polygonDrawingHandler.TogglePolygonModeCommand;
    public DelegateCommand FinishPolygonCommand => _polygonDrawingHandler.FinishPolygonCommand;
    public DelegateCommand CancelPolygonCommand => _polygonDrawingHandler.CancelPolygonCommand;
    public DelegateCommand<Point?> PolygonMouseDownCommand => _polygonDrawingHandler.PolygonMouseDownCommand;
    public DelegateCommand<Point?> PolygonDoubleClickCommand => _polygonDrawingHandler.PolygonDoubleClickCommand;
    public DelegateCommand ToggleObbModeCommand => _obbDrawingHandler.ToggleObbModeCommand;
    public DelegateCommand FinishObbCommand => _obbDrawingHandler.FinishObbCommand;
    public DelegateCommand CancelObbCommand => _obbDrawingHandler.CancelObbCommand;
    public DelegateCommand<Point?> ObbMouseDownCommand => _obbDrawingHandler.ObbMouseDownCommand;
    public DelegateCommand<Point?> ObbMouseMoveCommand => _obbDrawingHandler.ObbMouseMoveCommand;
    public DelegateCommand<Point?> ObbMouseUpCommand => _obbDrawingHandler.ObbMouseUpCommand;
    public DelegateCommand Sam3SegmentByTextCommand => _sam3Handler.SegmentByTextCommand;
    public DelegateCommand Sam3SegmentAllByTextCommand => _sam3Handler.SegmentAllByTextCommand;
    public DelegateCommand Sam3EnterPointModeCommand => _sam3Handler.EnterPointModeCommand;
    public DelegateCommand Sam3AcceptMaskCommand => _sam3Handler.AcceptMaskCommand;
    public DelegateCommand Sam3ClearPointsCommand => _sam3Handler.ClearPointsCommand;
    public DelegateCommand Sam3SelectReferenceCommand => _sam3Handler.SelectReferenceCommand;
    public DelegateCommand Sam3SegmentByReferenceCommand => _sam3Handler.SegmentByReferenceCommand;
    public DelegateCommand ExportYoloCommand => _exportHandler.ExportYoloCommand;
    public DelegateCommand ExportCocoCommand => _exportHandler.ExportCocoCommand;
    public DelegateCommand ExportVocCommand => _exportHandler.ExportVocCommand;
    public DelegateCommand ExportDotCommand => _exportHandler.ExportDotCommand;
    public DelegateCommand ExportMotCommand => _exportHandler.ExportMotCommand;
    public DelegateCommand ImportAnnotationsCommand => _exportHandler.ImportAnnotationsCommand;
    public DelegateCommand ImportVideoCommand => _exportHandler.ImportVideoCommand;
    public DelegateCommand GotoNextUnannotatedCommand => _reviewHandler.GotoNextUnannotatedCommand;
    public DelegateCommand RefreshReviewSummaryCommand => _reviewHandler.RefreshReviewSummaryCommand;
}
