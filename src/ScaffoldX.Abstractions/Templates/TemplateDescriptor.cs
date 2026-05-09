namespace ScaffoldX.Abstractions.Templates;

public sealed class TemplateDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string OutputPathTemplate { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public IReadOnlyList<string> RequiredFeatures { get; init; } = [];
    public IReadOnlyList<ExcludeCondition> ExcludeWhen { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool ShouldBeIncluded(IReadOnlyDictionary<string, object> configContext)
    {
        if (IsRequired) return true;

        foreach (var feature in RequiredFeatures)
        {
            if (!configContext.TryGetValue(feature, out var value)) return false;
            if (value is not bool b || !b) return false;
        }

        foreach (var condition in ExcludeWhen)
        {
            if (configContext.TryGetValue(condition.Key, out var value)
                && value?.ToString() == condition.Value)
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class ExcludeCondition
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;

    public ExcludeCondition() { }

    public ExcludeCondition(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
