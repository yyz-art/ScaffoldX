using Xunit;
using ScaffoldX.Abstractions.Templates;

namespace ScaffoldX.Abstractions.Tests.Templates;

public class TemplateDescriptorTests
{
    [Fact]
    public void RequiredFeatures_Empty_AlwaysIncluded()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "CommonSolution",
            Category = "Common",
            IsRequired = false,
            RequiredFeatures = [],
            ExcludeWhen = [],
        };

        var ctx = new Dictionary<string, object> { ["EnableSiemensS7"] = false };
        Assert.True(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void RequiredFeatures_AllTrue_Included()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "S7Driver",
            Category = "Collection",
            RequiredFeatures = ["EnableSiemensS7"],
            ExcludeWhen = [],
        };

        var ctx = new Dictionary<string, object> { ["EnableSiemensS7"] = true };
        Assert.True(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void RequiredFeatures_OneFalse_Excluded()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "S7Driver",
            Category = "Collection",
            RequiredFeatures = ["EnableSiemensS7"],
            ExcludeWhen = [],
        };

        var ctx = new Dictionary<string, object> { ["EnableSiemensS7"] = false };
        Assert.False(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void RequiredFeatures_KeyMissing_Excluded()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "S7Driver",
            Category = "Collection",
            RequiredFeatures = ["EnableSiemensS7"],
            ExcludeWhen = [],
        };

        var ctx = new Dictionary<string, object>();
        Assert.False(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void IsRequired_AlwaysIncluded()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "CommonSolution",
            Category = "Common",
            IsRequired = true,
            RequiredFeatures = ["NonExistentFeature"],
            ExcludeWhen = [],
        };

        var ctx = new Dictionary<string, object>();
        Assert.True(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void ExcludeWhen_ConditionMet_Excluded()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "SidebarView",
            Category = "Common",
            IsRequired = false,
            RequiredFeatures = [],
            ExcludeWhen = [new ExcludeCondition("NavigationStyle", "TopNav")],
        };

        var ctx = new Dictionary<string, object> { ["NavigationStyle"] = "TopNav" };
        Assert.False(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void ExcludeWhen_ConditionNotMet_Included()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "SidebarView",
            Category = "Common",
            IsRequired = false,
            RequiredFeatures = [],
            ExcludeWhen = [new ExcludeCondition("NavigationStyle", "TopNav")],
        };

        var ctx = new Dictionary<string, object> { ["NavigationStyle"] = "LeftSidebar" };
        Assert.True(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void ExcludeWhen_KeyMissing_Included()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "SidebarView",
            Category = "Common",
            IsRequired = false,
            RequiredFeatures = [],
            ExcludeWhen = [new ExcludeCondition("NavigationStyle", "TopNav")],
        };

        var ctx = new Dictionary<string, object>();
        Assert.True(descriptor.ShouldBeIncluded(ctx));
    }

    [Fact]
    public void RequiredFeatures_And_ExcludeWhen_BothApply()
    {
        var descriptor = new TemplateDescriptor
        {
            Name = "S7View",
            Category = "Collection",
            IsRequired = false,
            RequiredFeatures = ["EnableSiemensS7"],
            ExcludeWhen = [new ExcludeCondition("NavigationStyle", "TopNav")],
        };

        var ctx = new Dictionary<string, object>
        {
            ["EnableSiemensS7"] = true,
            ["NavigationStyle"] = "TopNav",
        };
        Assert.False(descriptor.ShouldBeIncluded(ctx));
    }
}

public class TemplateMetadataTests
{
    [Fact]
    public void ToDescriptor_MapsAllFields()
    {
        var meta = new TemplateMetadata
        {
            Name = "S7Driver",
            OutputPathTemplate = "src/{{project_name}}.Core/Drivers/S7Driver.cs",
            Category = "Collection",
            IsRequired = false,
            RequiredFeatures = ["EnableSiemensS7"],
            ExcludeWhen = [new ExcludeCondition("NavigationStyle", "TopNav")],
            Tags = ["driver", "siemens"],
        };

        var descriptor = meta.ToDescriptor();

        Assert.Equal("S7Driver", descriptor.Name);
        Assert.Equal("src/{{project_name}}.Core/Drivers/S7Driver.cs", descriptor.OutputPathTemplate);
        Assert.Equal("Collection", descriptor.Category);
        Assert.False(descriptor.IsRequired);
        Assert.Single(descriptor.RequiredFeatures);
        Assert.Single(descriptor.ExcludeWhen);
        Assert.Equal(2, descriptor.Tags.Count);
    }
}
