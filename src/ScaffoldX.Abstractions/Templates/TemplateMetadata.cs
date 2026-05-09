namespace ScaffoldX.Abstractions.Templates;

public sealed class TemplateMetadata
{
    public string Name { get; init; } = string.Empty;
    public string OutputPathTemplate { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public IReadOnlyList<string> RequiredFeatures { get; init; } = [];
    public IReadOnlyList<ExcludeCondition> ExcludeWhen { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];

    public TemplateDescriptor ToDescriptor()
    {
        return new TemplateDescriptor
        {
            Name = Name,
            OutputPathTemplate = OutputPathTemplate,
            Category = Category,
            IsRequired = IsRequired,
            RequiredFeatures = RequiredFeatures,
            ExcludeWhen = ExcludeWhen,
            Tags = Tags,
        };
    }
}
