namespace ScaffoldX.App.Services;

/// <summary>
/// 标注服务门面接口，组合 <see cref="IAnnotationRepository"/> 和 <see cref="IAnnotationExporter"/>。
/// 保持向后兼容——现有代码可继续注入 IAnnotationService。
/// 新代码应优先注入更细粒度的接口。
/// </summary>
public interface IAnnotationService : IAnnotationRepository, IAnnotationExporter
{
}
