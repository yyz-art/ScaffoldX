using Xunit;
using ScaffoldX.Abstractions.Plugins;

namespace ScaffoldX.Abstractions.Tests.Plugins;

public class PluginMetadataTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var meta = new PluginMetadata
        {
            Id = "scaffold",
            DisplayName = "项目脚手架",
        };

        Assert.Equal("scaffold", meta.Id);
        Assert.Equal("项目脚手架", meta.DisplayName);
        Assert.Equal(string.Empty, meta.Description);
        Assert.Equal("1.0.0", meta.Version);
        Assert.Equal(string.Empty, meta.IconKey);
        Assert.Equal(0, meta.Order);
        Assert.Empty(meta.Dependencies);
        Assert.Empty(meta.FeatureToggles);
    }

    [Fact]
    public void Dependencies_ContainsIds()
    {
        var meta = new PluginMetadata
        {
            Id = "vision",
            DisplayName = "视觉推理",
            Dependencies = ["annotation"],
        };

        Assert.Single(meta.Dependencies);
        Assert.Contains("annotation", meta.Dependencies);
    }

    [Fact]
    public void FeatureToggles_ContainsToggles()
    {
        var meta = new PluginMetadata
        {
            Id = "scaffold",
            DisplayName = "脚手架",
            FeatureToggles =
            [
                new FeatureToggle
                {
                    Key = "EnableSiemensS7",
                    DisplayName = "西门子 S7 驱动",
                    DefaultValue = false,
                    Group = "Drivers",
                },
            ],
        };

        Assert.Single(meta.FeatureToggles);
        Assert.Equal("EnableSiemensS7", meta.FeatureToggles[0].Key);
        Assert.False(meta.FeatureToggles[0].DefaultValue);
    }
}

public class PluginStateTests
{
    [Fact]
    public void EnumValues_AreExpected()
    {
        Assert.True(Enum.IsDefined(typeof(PluginState), PluginState.NotLoaded));
        Assert.True(Enum.IsDefined(typeof(PluginState), PluginState.Loading));
        Assert.True(Enum.IsDefined(typeof(PluginState), PluginState.Loaded));
        Assert.True(Enum.IsDefined(typeof(PluginState), PluginState.Unloading));
        Assert.True(Enum.IsDefined(typeof(PluginState), PluginState.Error));
    }
}

public class FeatureToggleTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var toggle = new FeatureToggle();

        Assert.Equal(string.Empty, toggle.Key);
        Assert.Equal(string.Empty, toggle.DisplayName);
        Assert.Equal(string.Empty, toggle.Description);
        Assert.False(toggle.DefaultValue);
        Assert.Equal(string.Empty, toggle.Group);
    }
}
