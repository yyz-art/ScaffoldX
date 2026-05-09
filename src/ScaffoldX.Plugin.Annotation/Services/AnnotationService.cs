using ScaffoldX.Plugin.Annotation.Models;

namespace ScaffoldX.Plugin.Annotation.Services;

public class AnnotationService : IAnnotationService
{
    private readonly AnnotationRepository _repository = new();
    private readonly YoloAnnotationExporter _yoloExporter = new();
    private readonly CocoAnnotationExporter _cocoExporter = new();

    public Task<AnnotationProject> CreateProjectAsync(string projectName, string projectDirectory, List<AnnotationClass> classes)
        => _repository.CreateProjectAsync(projectName, projectDirectory, classes);

    public Task<AnnotationProject> LoadProjectAsync(string projectFilePath)
        => _repository.LoadProjectAsync(projectFilePath);

    public Task SaveProjectAsync(AnnotationProject project)
        => _repository.SaveProjectAsync(project);

    public Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths)
        => _repository.AddImagesAsync(project, imagePaths);

    public Task UpdateAnnotationAsync(AnnotationProject project, AnnotationData annotation)
        => _repository.UpdateAnnotationAsync(project, annotation);

    public Task ExportYoloDatasetAsync(AnnotationProject project, string outputPath, double trainValSplit = 0.8)
        => _yoloExporter.ExportYoloDatasetAsync(project, outputPath, trainValSplit);

    public List<string> ToYoloFormat(AnnotationData annotation)
        => _yoloExporter.ToYoloFormat(annotation);

    public Task ExportCocoDatasetAsync(AnnotationProject project, string outputPath)
        => _cocoExporter.ExportCocoDatasetAsync(project, outputPath);

    public Task ExportVocDatasetAsync(AnnotationProject project, string outputPath)
        => throw new NotImplementedException();

    public Task ExportDotDatasetAsync(AnnotationProject project, string outputPath)
        => throw new NotImplementedException();

    public Task ExportMotDatasetAsync(AnnotationProject project, string outputPath)
        => throw new NotImplementedException();
}
