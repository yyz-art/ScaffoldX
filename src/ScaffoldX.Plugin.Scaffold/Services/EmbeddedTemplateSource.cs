using System.IO;
using System.Reflection;
using ScaffoldX.Abstractions.Templates;

namespace ScaffoldX.Plugin.Scaffold.Services;

public interface ITemplateSource
{
    Task<IReadOnlyList<TemplateEntry>> LoadAllAsync();
}

public sealed class TemplateEntry
{
    public string Name { get; init; } = string.Empty;
    public TemplateMetadata Metadata { get; init; } = new();
    public string Content { get; init; } = string.Empty;
}

public sealed class EmbeddedTemplateSource : ITemplateSource
{
    private readonly Assembly _assembly;
    private readonly string _resourcePrefix;

    public EmbeddedTemplateSource(Assembly assembly, string resourcePrefix)
    {
        _assembly = assembly;
        _resourcePrefix = resourcePrefix;
    }

    public static EmbeddedTemplateSource ForTemplatesAssembly()
    {
        var assembly = typeof(ScaffoldX.Templates.Marker).Assembly;
        return new EmbeddedTemplateSource(assembly, "ScaffoldX.Templates");
    }

    public async Task<IReadOnlyList<TemplateEntry>> LoadAllAsync()
    {
        var entries = new List<TemplateEntry>();
        var resourceNames = _assembly.GetManifestResourceNames();

        var tmetaResources = resourceNames
            .Where(r => r.EndsWith(".tmeta.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var tmetaResource in tmetaResources)
        {
            var meta = await LoadMetadataAsync(tmetaResource);
            if (meta == null) continue;

            var stplResource = GetStplResourceName(tmetaResource);
            var content = await LoadContentAsync(stplResource);
            if (content == null) continue;

            entries.Add(new TemplateEntry
            {
                Name = meta.Name,
                Metadata = meta,
                Content = content
            });
        }

        return entries;
    }

    private async Task<TemplateMetadata?> LoadMetadataAsync(string resourceName)
    {
        try
        {
            await using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return TemplateMetadataLoader.LoadFromJson(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> LoadContentAsync(string resourceName)
    {
        try
        {
            await using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch
        {
            return null;
        }
    }

    private string GetStplResourceName(string tmetaResourceName)
    {
        return tmetaResourceName.Replace(".tmeta.json", ".stpl");
    }
}
