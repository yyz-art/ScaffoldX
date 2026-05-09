using Xunit;
using ScaffoldX.Abstractions.Config;

namespace ScaffoldX.Abstractions.Tests.Config;

public class ConfigRegistryTests
{
    [Fact]
    public void Register_And_GetSection_ReturnsRegisteredSection()
    {
        var registry = new ConfigRegistry();
        var section = new TestConfigSection("test-section", "Test Section");

        registry.Register(section);
        var result = registry.GetSection("test-section");

        Assert.NotNull(result);
        Assert.Same(section, result);
    }

    [Fact]
    public void GetSection_NotRegistered_ReturnsNull()
    {
        var registry = new ConfigRegistry();

        var result = registry.GetSection("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void Register_DuplicateSectionId_OverwritesPrevious()
    {
        var registry = new ConfigRegistry();
        var section1 = new TestConfigSection("id", "First");
        var section2 = new TestConfigSection("id", "Second");

        registry.Register(section1);
        registry.Register(section2);

        var result = registry.GetSection("id");
        Assert.Same(section2, result);
    }

    [Fact]
    public void GetAllSections_ReturnsAllRegistered()
    {
        var registry = new ConfigRegistry();
        var s1 = new TestConfigSection("a", "A");
        var s2 = new TestConfigSection("b", "B");

        registry.Register(s1);
        registry.Register(s2);

        var all = registry.GetAllSections();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetAllSections_Empty_ReturnsEmptyList()
    {
        var registry = new ConfigRegistry();

        var all = registry.GetAllSections();

        Assert.NotNull(all);
        Assert.Empty(all);
    }

    private sealed class TestConfigSection(string sectionId, string displayName) : IConfigSection
    {
        public string SectionId => sectionId;
        public string DisplayName => displayName;
        public Dictionary<string, object> GetDefaults() => new();
        public IReadOnlyList<ValidationError> Validate() => [];
    }
}
