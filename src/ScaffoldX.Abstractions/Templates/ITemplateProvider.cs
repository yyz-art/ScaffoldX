namespace ScaffoldX.Abstractions.Templates;

public interface ITemplateProvider
{
    string Category { get; }
    Task<IReadOnlyList<TemplateDescriptor>> GetTemplatesAsync();
}

public interface ITemplateFilter
{
    IReadOnlyList<TemplateDescriptor> Apply(
        IReadOnlyList<TemplateDescriptor> descriptors,
        IReadOnlyDictionary<string, object> configContext);
}

public sealed class DeclarativeTemplateFilter : ITemplateFilter
{
    public IReadOnlyList<TemplateDescriptor> Apply(
        IReadOnlyList<TemplateDescriptor> descriptors,
        IReadOnlyDictionary<string, object> configContext)
    {
        var result = new List<TemplateDescriptor>();

        foreach (var descriptor in descriptors)
        {
            if (descriptor.ShouldBeIncluded(configContext))
            {
                result.Add(descriptor);
            }
        }

        return result;
    }
}
