using ScaffoldX.Abstractions.Config;
using ScaffoldX.Plugin.Scaffold.Services;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.Services;

public class ProjectGeneratorTests
{
    private readonly ITemplateEngine _templateEngine = new ScribanTemplateEngine();
    private readonly IValidationService _validationService = new ValidationService();

    private ProjectGenerator CreateGenerator()
    {
        return new ProjectGenerator(_templateEngine, _validationService);
    }

    private static ConfigRegistry CreateConfigRegistry(string projectName = "TestProject", string outputPath = @"C:\Temp")
    {
        var registry = new ConfigRegistry();
        var section = new ScaffoldConfigSection
        {
            ProjectName = projectName,
            OutputDirectory = outputPath
        };
        registry.Register(section);
        return registry;
    }

    [Fact]
    public async Task GenerateAsync_InvalidProjectName_ReturnsFail()
    {
        var generator = CreateGenerator();
        var registry = CreateConfigRegistry("1Invalid");

        var result = await generator.GenerateAsync(registry);

        Assert.False(result.Success);
        Assert.Contains("验证失败", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_ValidConfig_ReturnsSuccess()
    {
        var generator = CreateGenerator();
        var registry = CreateConfigRegistry("ValidProject");

        var result = await generator.GenerateAsync(registry);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GenerateAsync_ReportsProgress()
    {
        var generator = CreateGenerator();
        var registry = CreateConfigRegistry("ProgressTest");
        var progressMessages = new List<string>();

        var progress = new Progress<GenerationProgress>(p => progressMessages.Add(p.Message));

        await generator.GenerateAsync(registry, progress);

        Assert.NotEmpty(progressMessages);
    }
}
