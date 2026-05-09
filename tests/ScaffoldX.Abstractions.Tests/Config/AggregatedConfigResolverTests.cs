using Xunit;
using ScaffoldX.Abstractions.Config;

namespace ScaffoldX.Abstractions.Tests.Config;

public class AggregatedConfigResolverTests
{
    [Fact]
    public void BuildVariableContext_SingleSection_ReturnsAllDefaults()
    {
        var registry = new ConfigRegistry();
        var section = new TestConfigSection("Scaffold", "Scaffold", new Dictionary<string, object>
        {
            ["ProjectName"] = "MyApp",
            ["TargetFramework"] = "net10.0-windows",
        });
        registry.Register(section);

        var resolver = new AggregatedConfigResolver(registry);
        var ctx = resolver.BuildVariableContext();

        Assert.Equal("MyApp", ctx["ProjectName"]);
        Assert.Equal("net10.0-windows", ctx["TargetFramework"]);
    }

    [Fact]
    public void BuildVariableContext_MultipleSections_MergesAll()
    {
        var registry = new ConfigRegistry();
        registry.Register(new TestConfigSection("Scaffold", "Scaffold", new Dictionary<string, object>
        {
            ["ProjectName"] = "MyApp",
        }));
        registry.Register(new TestConfigSection("Scaffold.Collection", "Collection", new Dictionary<string, object>
        {
            ["EnableSiemensS7"] = true,
            ["EnableModbusTcp"] = false,
        }));

        var resolver = new AggregatedConfigResolver(registry);
        var ctx = resolver.BuildVariableContext();

        Assert.Equal("MyApp", ctx["ProjectName"]);
        Assert.Equal(true, ctx["EnableSiemensS7"]);
        Assert.Equal(false, ctx["EnableModbusTcp"]);
    }

    [Fact]
    public void BuildVariableContext_PropertyConflict_LaterSectionOverwrites()
    {
        var registry = new ConfigRegistry();
        registry.Register(new TestConfigSection("A", "A", new Dictionary<string, object>
        {
            ["SharedKey"] = "from-A",
        }));
        registry.Register(new TestConfigSection("B", "B", new Dictionary<string, object>
        {
            ["SharedKey"] = "from-B",
        }));

        var resolver = new AggregatedConfigResolver(registry);
        var ctx = resolver.BuildVariableContext();

        Assert.Equal("from-B", ctx["SharedKey"]);
    }

    [Fact]
    public void BuildVariableContext_EmptyRegistry_ReturnsEmptyDictionary()
    {
        var registry = new ConfigRegistry();
        var resolver = new AggregatedConfigResolver(registry);

        var ctx = resolver.BuildVariableContext();

        Assert.NotNull(ctx);
        Assert.Empty(ctx);
    }

    [Fact]
    public void BuildVariableContext_CaseInsensitiveKeys()
    {
        var registry = new ConfigRegistry();
        registry.Register(new TestConfigSection("A", "A", new Dictionary<string, object>
        {
            ["ProjectName"] = "First",
        }));
        registry.Register(new TestConfigSection("B", "B", new Dictionary<string, object>
        {
            ["projectname"] = "Second",
        }));

        var resolver = new AggregatedConfigResolver(registry);
        var ctx = resolver.BuildVariableContext();

        Assert.Equal("Second", ctx["ProjectName"]);
    }

    private sealed class TestConfigSection(
        string sectionId,
        string displayName,
        Dictionary<string, object>? defaults = null
    ) : IConfigSection
    {
        public string SectionId => sectionId;
        public string DisplayName => displayName;
        public Dictionary<string, object> GetDefaults() => defaults ?? new();
        public IReadOnlyList<ValidationError> Validate() => [];
    }
}
