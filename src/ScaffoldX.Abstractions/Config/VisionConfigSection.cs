namespace ScaffoldX.Abstractions.Config;

public sealed class VisionConfigSection : IConfigSection
{
    public bool EnableVision { get; set; }
    public string CameraBrand { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public bool EnablePipeline { get; set; }
    public string ModelPath { get; set; } = string.Empty;

    public string SectionId => "Scaffold.Vision";
    public string DisplayName => "视觉配置";

    public Dictionary<string, object> GetDefaults()
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["EnableVision"] = EnableVision,
            ["CameraBrand"] = CameraBrand,
            ["ModelType"] = ModelType,
            ["CameraBrandPascal"] = ToPascalCase(CameraBrand),
            ["ModelTypePascal"] = ToPascalCase(ModelType),
            ["EnablePipeline"] = EnablePipeline,
            ["ModelPath"] = ModelPath,
        };
    }

    public IReadOnlyList<ValidationError> Validate() => [];

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var segments = input.Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Text.StringBuilder(input.Length);
        foreach (var segment in segments)
        {
            if (segment.Length == 0) continue;
            result.Append(char.ToUpperInvariant(segment[0]));
            if (segment.Length > 1)
            {
                var remainder = segment[1..];
                result.Append(remainder == remainder.ToUpperInvariant()
                    ? remainder.ToLowerInvariant()
                    : remainder);
            }
        }
        return result.ToString();
    }
}
