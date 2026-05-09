namespace ScaffoldX.Abstractions.Config;

public interface IConfigSection
{
    string SectionId { get; }
    string DisplayName { get; }
    Dictionary<string, object> GetDefaults();
    IReadOnlyList<ValidationError> Validate();
}

public sealed class ValidationError
{
    public string PropertyName { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}
