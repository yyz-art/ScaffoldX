using ScaffoldX.Plugin.Annotation.Models;

namespace ScaffoldX.Plugin.Annotation.Services;

public interface IAnnotationRepository
{
    Task<AnnotationProject> CreateProjectAsync(string projectName, string projectDirectory, List<AnnotationClass> classes);
    Task<AnnotationProject> LoadProjectAsync(string projectFilePath);
    Task SaveProjectAsync(AnnotationProject project);
    Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths);
    Task UpdateAnnotationAsync(AnnotationProject project, AnnotationData annotation);
}

public interface IAnnotationExporter
{
    Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8);
    Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath);
    Task ExportVocDatasetAsync(AnnotationProject project, string outputPath);
    Task ExportDotDatasetAsync(AnnotationProject project, string outputPath);
    Task ExportMotDatasetAsync(AnnotationProject project, string outputPath);
    List<string> ToYoloFormat(AnnotationData annotation);
}

public interface IAnnotationService : IAnnotationRepository, IAnnotationExporter
{
}

public interface IVideoFrameService
{
    Task<VideoInfo> GetVideoInfoAsync(string videoPath, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ExtractFramesAsync(string videoPath, string outputDir, double fps = 1.0, CancellationToken ct = default);
}

public record VideoInfo(double Duration, int Width, int Height, double Fps, int TotalFrames);
