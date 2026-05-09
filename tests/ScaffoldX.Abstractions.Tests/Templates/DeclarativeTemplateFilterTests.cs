using Xunit;
using ScaffoldX.Abstractions.Templates;

namespace ScaffoldX.Abstractions.Tests.Templates;

public class DeclarativeTemplateFilterTests
{
    private static TemplateDescriptor MakeDescriptor(
        string name,
        string category = "Common",
        bool isRequired = false,
        string[]? requiredFeatures = null,
        ExcludeCondition[]? excludeWhen = null)
    {
        return new TemplateDescriptor
        {
            Name = name,
            Category = category,
            IsRequired = isRequired,
            RequiredFeatures = requiredFeatures ?? [],
            ExcludeWhen = excludeWhen ?? [],
        };
    }

    [Fact]
    public void Filter_IsRequired_AlwaysIncludedEvenWithMissingFeatures()
    {
        var filter = new DeclarativeTemplateFilter();
        var descriptors = new List<TemplateDescriptor>
        {
            MakeDescriptor("A", isRequired: true, requiredFeatures: ["NonExistent"]),
            MakeDescriptor("B", isRequired: false, requiredFeatures: ["AlsoNonExistent"]),
        };
        var ctx = new Dictionary<string, object>();

        var result = filter.Apply(descriptors, ctx);

        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    [Fact]
    public void Filter_RequiredFeaturesMatch_Included()
    {
        var filter = new DeclarativeTemplateFilter();
        var descriptors = new List<TemplateDescriptor>
        {
            MakeDescriptor("S7", requiredFeatures: ["EnableSiemensS7"]),
        };
        var ctx = new Dictionary<string, object> { ["EnableSiemensS7"] = true };

        var result = filter.Apply(descriptors, ctx);

        Assert.Single(result);
    }

    [Fact]
    public void Filter_RequiredFeaturesMismatch_Excluded()
    {
        var filter = new DeclarativeTemplateFilter();
        var descriptors = new List<TemplateDescriptor>
        {
            MakeDescriptor("S7", requiredFeatures: ["EnableSiemensS7"]),
        };
        var ctx = new Dictionary<string, object> { ["EnableSiemensS7"] = false };

        var result = filter.Apply(descriptors, ctx);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ExcludeWhenMatch_Excluded()
    {
        var filter = new DeclarativeTemplateFilter();
        var descriptors = new List<TemplateDescriptor>
        {
            MakeDescriptor("Sidebar",
                excludeWhen: [new ExcludeCondition("NavigationStyle", "TopNav")]),
        };
        var ctx = new Dictionary<string, object> { ["NavigationStyle"] = "TopNav" };

        var result = filter.Apply(descriptors, ctx);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_MixedScenario()
    {
        var filter = new DeclarativeTemplateFilter();
        var descriptors = new List<TemplateDescriptor>
        {
            MakeDescriptor("Common", isRequired: true),
            MakeDescriptor("S7", requiredFeatures: ["EnableSiemensS7"]),
            MakeDescriptor("Modbus", requiredFeatures: ["EnableModbusTcp"]),
            MakeDescriptor("Sidebar", excludeWhen: [new ExcludeCondition("NavigationStyle", "TopNav")]),
            MakeDescriptor("TopNav", excludeWhen: [new ExcludeCondition("NavigationStyle", "LeftSidebar")]),
        };
        var ctx = new Dictionary<string, object>
        {
            ["EnableSiemensS7"] = true,
            ["EnableModbusTcp"] = false,
            ["NavigationStyle"] = "LeftSidebar",
        };

        var result = filter.Apply(descriptors, ctx);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Name == "Common");
        Assert.Contains(result, d => d.Name == "S7");
        Assert.Contains(result, d => d.Name == "Sidebar");
    }

    [Fact]
    public void Filter_EmptyDescriptors_ReturnsEmpty()
    {
        var filter = new DeclarativeTemplateFilter();
        var descriptors = new List<TemplateDescriptor>();
        var ctx = new Dictionary<string, object>();

        var result = filter.Apply(descriptors, ctx);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_EmptyContext_OnlyRequiredAndNoConditions()
    {
        var filter = new DeclarativeTemplateFilter();
        var descriptors = new List<TemplateDescriptor>
        {
            MakeDescriptor("Required", isRequired: true),
            MakeDescriptor("Optional", isRequired: false),
            MakeDescriptor("WithFeature", requiredFeatures: ["SomeFeature"]),
        };
        var ctx = new Dictionary<string, object>();

        var result = filter.Apply(descriptors, ctx);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Name == "Required");
        Assert.Contains(result, d => d.Name == "Optional");
    }
}
