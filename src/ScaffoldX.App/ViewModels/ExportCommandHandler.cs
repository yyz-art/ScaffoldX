using System.IO;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Serilog;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// Export/import handler, managing YOLO/COCO/VOC dataset export, annotation import, and video frame extraction.
/// </summary>
public class ExportCommandHandler : BindableBase
{
    private readonly IAnnotationService _annotationService;
    private readonly IVideoFrameService _videoFrameService;
    private readonly AnnotationExportService _exportService;
    private readonly AnnotationContext _ctx;
    private readonly ILogger _logger = Log.ForContext<ExportCommandHandler>();

    private string _videoImportProgress = string.Empty;

    /// <summary>
    /// Initializes the export/import handler.
    /// </summary>
    public ExportCommandHandler(
        IAnnotationService annotationService,
        IVideoFrameService videoFrameService,
        AnnotationContext ctx)
    {
        _annotationService = annotationService;
        _videoFrameService = videoFrameService;
        _ctx = ctx;
        _exportService = new AnnotationExportService(annotationService);

        ExportYoloCommand = new DelegateCommand(ExecuteExportYolo);
        ExportCocoCommand = new DelegateCommand(ExecuteExportCoco);
        ExportVocCommand = new DelegateCommand(ExecuteExportVoc);
        ExportDotCommand = new DelegateCommand(ExecuteExportDot);
        ExportMotCommand = new DelegateCommand(ExecuteExportMot);
        ImportAnnotationsCommand = new DelegateCommand(ExecuteImportAnnotations, () => _ctx.GetProject() != null);
        ImportVideoCommand = new DelegateCommand(ExecuteImportVideo, () => _ctx.GetProject() != null);
    }

    /// <summary>Export YOLO dataset command.</summary>
    public DelegateCommand ExportYoloCommand { get; }

    /// <summary>Export COCO dataset command.</summary>
    public DelegateCommand ExportCocoCommand { get; }

    /// <summary>Export Pascal VOC dataset command.</summary>
    public DelegateCommand ExportVocCommand { get; }

    /// <summary>Export DOTA dataset command.</summary>
    public DelegateCommand ExportDotCommand { get; }

    /// <summary>Export MOT dataset command.</summary>
    public DelegateCommand ExportMotCommand { get; }

    /// <summary>Import annotations from YOLO .txt files command.</summary>
    public DelegateCommand ImportAnnotationsCommand { get; }

    /// <summary>Import video frames command.</summary>
    public DelegateCommand ImportVideoCommand { get; }

    /// <summary>Video import progress text.</summary>
    public string VideoImportProgress
    {
        get => _videoImportProgress;
        private set => SetProperty(ref _videoImportProgress, value);
    }

    /// <summary>
    /// Exports the project in YOLO format, prompting the user for an output directory.
    /// </summary>
    private async void ExecuteExportYolo()
    {
        if (!AnnotationExportService.CanExport(_ctx.GetProject()))
        {
            _ctx.SetStatusMessage("没有可导出的标注数据");
            return;
        }

        var dialog = new VistaFolderBrowserDialog
        {
            Description = "选择 YOLO 数据集输出目录"
        };

        if (dialog.ShowDialog() != true)
            return;

        _ctx.SetStatusMessage("正在导出 YOLO 数据集...");
        var success = await _exportService.ExportYoloAsync(_ctx.GetProject()!, dialog.SelectedPath);
        _ctx.SetStatusMessage(success
            ? $"YOLO 数据集已导出到: {dialog.SelectedPath}"
            : "导出失败，请查看日志");
    }

    /// <summary>
    /// Exports the project in COCO JSON format, prompting the user for an output directory.
    /// </summary>
    private async void ExecuteExportCoco()
    {
        if (!AnnotationExportService.CanExport(_ctx.GetProject()))
        {
            _ctx.SetStatusMessage("没有可导出的标注数据");
            return;
        }

        var dialog = new VistaFolderBrowserDialog { Description = "选择 COCO 数据集输出目录" };
        if (dialog.ShowDialog() != true) return;

        _ctx.SetStatusMessage("正在导出 COCO 数据集...");
        var success = await _exportService.ExportCocoAsync(_ctx.GetProject()!, dialog.SelectedPath);
        _ctx.SetStatusMessage(success
            ? $"COCO 数据集已导出到: {dialog.SelectedPath}"
            : "COCO 导出失败，请查看日志");
    }

    /// <summary>
    /// Exports the project in Pascal VOC XML format, prompting the user for an output directory.
    /// </summary>
    private async void ExecuteExportVoc()
    {
        if (!AnnotationExportService.CanExport(_ctx.GetProject()))
        {
            _ctx.SetStatusMessage("没有可导出的标注数据");
            return;
        }

        var dialog = new VistaFolderBrowserDialog { Description = "选择 VOC 数据集输出目录" };
        if (dialog.ShowDialog() != true) return;

        _ctx.SetStatusMessage("正在导出 Pascal VOC 数据集...");
        var success = await _exportService.ExportVocAsync(_ctx.GetProject()!, dialog.SelectedPath);
        _ctx.SetStatusMessage(success
            ? $"Pascal VOC 数据集已导出到: {dialog.SelectedPath}"
            : "VOC 导出失败，请查看日志");
    }

    /// <summary>
    /// Exports the project in DOTA format (oriented bounding boxes), prompting the user for an output directory.
    /// </summary>
    private async void ExecuteExportDot()
    {
        if (!AnnotationExportService.CanExport(_ctx.GetProject()))
        {
            _ctx.SetStatusMessage("没有可导出的标注数据");
            return;
        }

        var dialog = new VistaFolderBrowserDialog { Description = "选择 DOTA 数据集输出目录" };
        if (dialog.ShowDialog() != true) return;

        _ctx.SetStatusMessage("正在导出 DOTA 数据集...");
        var success = await _exportService.ExportDotAsync(_ctx.GetProject()!, dialog.SelectedPath);
        _ctx.SetStatusMessage(success
            ? $"DOTA 数据集已导出到: {dialog.SelectedPath}"
            : "DOTA 导出失败，请查看日志");
    }

    /// <summary>
    /// Exports the project in MOT Challenge format (multi-object tracking), prompting the user for an output directory.
    /// </summary>
    private async void ExecuteExportMot()
    {
        if (!AnnotationExportService.CanExport(_ctx.GetProject()))
        {
            _ctx.SetStatusMessage("没有可导出的标注数据");
            return;
        }

        var dialog = new VistaFolderBrowserDialog { Description = "选择 MOT 数据集输出目录" };
        if (dialog.ShowDialog() != true) return;

        _ctx.SetStatusMessage("正在导出 MOT 数据集...");
        var success = await _exportService.ExportMotAsync(_ctx.GetProject()!, dialog.SelectedPath);
        _ctx.SetStatusMessage(success
            ? $"MOT 数据集已导出到: {dialog.SelectedPath}"
            : "MOT 导出失败，请查看日志");
    }

    /// <summary>
    /// Imports YOLO-format annotation .txt files from a user-selected folder into the current project.
    /// </summary>
    private async void ExecuteImportAnnotations()
    {
        var project = _ctx.GetProject();
        if (project == null)
        {
            _ctx.SetStatusMessage("请先创建或打开项目");
            return;
        }

        var dialog = new VistaFolderBrowserDialog
        {
            Description = "选择包含 YOLO 格式标注文件（.txt）的文件夹"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var labelsDir = dialog.SelectedPath;
            var classNames = project.Classes.Select(c => c.Name).ToList();
            int importedCount = 0;

            foreach (var annotation in project.Annotations)
            {
                var imageName = Path.GetFileNameWithoutExtension(annotation.ImagePath);
                var labelPath = Path.Combine(labelsDir, $"{imageName}.txt");

                if (!File.Exists(labelPath))
                    continue;

                var lines = await File.ReadAllLinesAsync(labelPath);
                if (lines.Length == 0)
                    continue;

                if (annotation.ImageWidth == 0 || annotation.ImageHeight == 0)
                {
                    if (File.Exists(annotation.ImagePath))
                    {
                        try
                        {
                            using var image = System.Drawing.Image.FromFile(annotation.ImagePath);
                            annotation.ImageWidth = image.Width;
                            annotation.ImageHeight = image.Height;
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "跳过图像 {ImagePath}", annotation.ImagePath);
                            continue;
                        }
                    }
                }

                var (boxes, polygons, polylines, circles, orientedBoxes) = _annotationService.FromYoloFormat(
                    lines, annotation.ImageWidth, annotation.ImageHeight, classNames);

                foreach (var box in boxes)
                    annotation.Boxes.Add(box);

                foreach (var polygon in polygons)
                    annotation.Polygons.Add(polygon);

                foreach (var polyline in polylines)
                    annotation.Polylines.Add(polyline);

                foreach (var circle in circles)
                    annotation.Circles.Add(circle);

                foreach (var obb in orientedBoxes)
                    annotation.OrientedBoxes.Add(obb);

                if (boxes.Count > 0 || polygons.Count > 0 || polylines.Count > 0 || circles.Count > 0 || orientedBoxes.Count > 0)
                    importedCount++;
            }

            if (_ctx.GetCurrentAnnotation() != null)
                _ctx.UpdateBoxesList();

            _ctx.UpdateStatistics();
            _ctx.SetStatusMessage($"已导入 {importedCount} 张图像的标注数据");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导入标注失败");
            _ctx.SetStatusMessage($"导入标注失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports video frames by extracting frames from a user-selected video file and adding them to the project.
    /// </summary>
    private async void ExecuteImportVideo()
    {
        var project = _ctx.GetProject();
        if (project == null)
        {
            _ctx.SetStatusMessage("请先创建或打开项目");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv|所有文件|*.*",
            Title = "选择视频文件"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            VideoImportProgress = "正在获取视频信息...";
            _ctx.SetStatusMessage("正在分析视频文件...");

            var videoInfo = await _videoFrameService.GetVideoInfoAsync(dialog.FileName);
            _logger.Information("视频信息: {Duration:F1}s, {Width}x{Height}, {Fps:F1}fps, ~{Frames}帧",
                videoInfo.Duration, videoInfo.Width, videoInfo.Height, videoInfo.Fps, videoInfo.TotalFrames);

            var framesDir = Path.Combine(project.ProjectDirectory, "video_frames");
            Directory.CreateDirectory(framesDir);

            VideoImportProgress = $"正在提取帧 (共约 {videoInfo.TotalFrames} 帧)...";
            _ctx.SetStatusMessage("正在从视频中提取帧...");

            var frameFiles = await _videoFrameService.ExtractFramesAsync(
                dialog.FileName, framesDir, fps: 1.0);

            if (frameFiles.Count == 0)
            {
                _ctx.SetStatusMessage("未能从视频中提取到帧");
                VideoImportProgress = string.Empty;
                return;
            }

            VideoImportProgress = $"已提取 {frameFiles.Count} 帧，正在添加到项目...";

            await _annotationService.AddImagesAsync(project, frameFiles);
            _ctx.UpdateStatistics();

            if (_ctx.GetCurrentImageIndex() < 0 && project.Annotations.Count > 0)
                await _ctx.LoadFirstImage();

            VideoImportProgress = string.Empty;
            _ctx.SetStatusMessage($"已从视频导入 {frameFiles.Count} 帧图像");
            _logger.Information("视频帧导入完成: {Count} 帧", frameFiles.Count);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ffmpeg") || ex.Message.Contains("ffprobe"))
        {
            _logger.Error(ex, "视频处理工具不可用");
            _ctx.SetStatusMessage(ex.Message);
            VideoImportProgress = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "视频导入失败");
            _ctx.SetStatusMessage($"视频导入失败: {ex.Message}");
            VideoImportProgress = string.Empty;
        }
    }
}
