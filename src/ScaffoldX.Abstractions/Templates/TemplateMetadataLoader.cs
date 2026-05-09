using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScaffoldX.Abstractions.Templates;

public static class TemplateMetadataLoader
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static TemplateMetadata LoadFromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<TemplateMetadataDto>(json, _options)
            ?? throw new JsonException("Failed to deserialize template metadata");

        return new TemplateMetadata
        {
            Name = dto.Name ?? string.Empty,
            OutputPathTemplate = dto.OutputPathTemplate ?? string.Empty,
            Category = dto.Category ?? string.Empty,
            IsRequired = dto.IsRequired,
            RequiredFeatures = dto.RequiredFeatures ?? [],
            ExcludeWhen = dto.ExcludeWhen?.Select(e => new ExcludeCondition(e.Key ?? string.Empty, e.Value ?? string.Empty)).ToList() ?? [],
            Tags = dto.Tags ?? [],
        };
    }

    public static async Task<IReadOnlyList<TemplateMetadata>> LoadFromDirectoryAsync(string directory)
    {
        var result = new List<TemplateMetadata>();

        if (!Directory.Exists(directory)) return result;

        foreach (var file in Directory.GetFiles(directory, "*.tmeta.json", SearchOption.AllDirectories))
        {
            var json = await File.ReadAllTextAsync(file);
            var meta = LoadFromJson(json);
            result.Add(meta);
        }

        return result;
    }

    private sealed class TemplateMetadataDto
    {
        public string? Name { get; set; }
        public string? OutputPathTemplate { get; set; }
        public string? Category { get; set; }
        public bool IsRequired { get; set; }
        public List<string>? RequiredFeatures { get; set; }
        public List<ExcludeConditionDto>? ExcludeWhen { get; set; }
        public List<string>? Tags { get; set; }
    }

    private sealed class ExcludeConditionDto
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}
