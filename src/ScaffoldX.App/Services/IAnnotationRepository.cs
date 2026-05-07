using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// 标注项目的数据访问接口，负责项目的 CRUD 和图像管理。
/// </summary>
public interface IAnnotationRepository
{
    /// <summary>创建新的标注项目。</summary>
    Task<AnnotationProject> CreateProjectAsync(string projectName, string projectDirectory, List<AnnotationClass> classes);

    /// <summary>加载已有的标注项目。</summary>
    Task<AnnotationProject> LoadProjectAsync(string projectFilePath);

    /// <summary>保存标注项目到文件。</summary>
    Task SaveProjectAsync(AnnotationProject project);

    /// <summary>批量添加图像到项目。</summary>
    Task AddImagesAsync(AnnotationProject project, IEnumerable<string> imagePaths);

    /// <summary>更新图像的标注数据。</summary>
    Task UpdateAnnotationAsync(AnnotationProject project, AnnotationData annotation);
}
