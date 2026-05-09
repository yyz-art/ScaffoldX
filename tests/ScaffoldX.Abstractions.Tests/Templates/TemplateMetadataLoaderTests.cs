using Xunit;
using ScaffoldX.Abstractions.Templates;

namespace ScaffoldX.Abstractions.Tests.Templates;

public class TemplateMetadataLoaderTests
{
    [Fact]
    public void LoadFromJson_ValidJson_ReturnsMetadata()
    {
        var json = """
        {
          "Name": "S7Driver.cs",
          "OutputPathTemplate": "",
          "Category": "Collection",
          "IsRequired": false,
          "RequiredFeatures": ["EnableSiemensS7"],
          "ExcludeWhen": [],
          "Tags": ["driver", "siemens"]
        }
        """;

        var meta = TemplateMetadataLoader.LoadFromJson(json);

        Assert.Equal("S7Driver.cs", meta.Name);
        Assert.Equal("Collection", meta.Category);
        Assert.False(meta.IsRequired);
        Assert.Single(meta.RequiredFeatures);
        Assert.Equal("EnableSiemensS7", meta.RequiredFeatures[0]);
    }

    [Fact]
    public void LoadFromJson_WithExcludeWhen_ReturnsMetadata()
    {
        var json = """
        {
          "Name": "SidebarView.xaml",
          "OutputPathTemplate": "",
          "Category": "Common",
          "IsRequired": false,
          "RequiredFeatures": [],
          "ExcludeWhen": [
            { "Key": "NavigationStyle", "Value": "TopNav" }
          ],
          "Tags": ["shell", "navigation"]
        }
        """;

        var meta = TemplateMetadataLoader.LoadFromJson(json);

        Assert.Equal("SidebarView.xaml", meta.Name);
        Assert.Single(meta.ExcludeWhen);
        Assert.Equal("NavigationStyle", meta.ExcludeWhen[0].Key);
        Assert.Equal("TopNav", meta.ExcludeWhen[0].Value);
    }

    [Fact]
    public void LoadFromJson_RequiredTemplate_NoFeatures()
    {
        var json = """
        {
          "Name": "Solution.sln",
          "OutputPathTemplate": "",
          "Category": "Common",
          "IsRequired": true,
          "RequiredFeatures": [],
          "ExcludeWhen": [],
          "Tags": []
        }
        """;

        var meta = TemplateMetadataLoader.LoadFromJson(json);

        Assert.True(meta.IsRequired);
        Assert.Empty(meta.RequiredFeatures);
        Assert.Empty(meta.ExcludeWhen);
    }

    [Fact]
    public void ToDescriptor_RoundTrip()
    {
        var json = """
        {
          "Name": "ModbusTcpDriver.cs",
          "OutputPathTemplate": "src/{{project_name}}.Drivers/ModbusTcp/",
          "Category": "Collection",
          "IsRequired": false,
          "RequiredFeatures": ["EnableModbusTcp"],
          "ExcludeWhen": [],
          "Tags": ["driver", "modbus"]
        }
        """;

        var meta = TemplateMetadataLoader.LoadFromJson(json);
        var descriptor = meta.ToDescriptor();

        Assert.Equal("ModbusTcpDriver.cs", descriptor.Name);
        Assert.Equal("src/{{project_name}}.Drivers/ModbusTcp/", descriptor.OutputPathTemplate);
        Assert.Equal("Collection", descriptor.Category);
        Assert.False(descriptor.IsRequired);
        Assert.Single(descriptor.RequiredFeatures);
    }
}
