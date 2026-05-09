namespace ScaffoldX.Abstractions.Config;

public sealed class ConfigRegistry
{
    private readonly Dictionary<string, IConfigSection> _sections = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IConfigSection section)
    {
        _sections[section.SectionId] = section;
    }

    public IConfigSection? GetSection(string sectionId)
    {
        return _sections.TryGetValue(sectionId, out var section) ? section : null;
    }

    public IReadOnlyList<IConfigSection> GetAllSections()
    {
        return _sections.Values.ToList().AsReadOnly();
    }
}
