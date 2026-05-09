namespace ScaffoldX.Abstractions.Config;

public sealed class AggregatedConfigResolver
{
    private readonly ConfigRegistry _registry;

    public AggregatedConfigResolver(ConfigRegistry registry)
    {
        _registry = registry;
    }

    public Dictionary<string, object> BuildVariableContext()
    {
        var ctx = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in _registry.GetAllSections())
        {
            foreach (var (key, value) in section.GetDefaults())
            {
                ctx[key] = value;
            }
        }

        return ctx;
    }
}
