using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Templates;
using ScaffoldX.Plugin.Scaffold.Services;
using Xunit;

namespace ScaffoldX.Plugin.Scaffold.Tests.Services;

public class EnhancedProjectGeneratorTests
{
    private readonly ITemplateEngine _templateEngine = new ScribanTemplateEngine();
    private readonly IValidationService _validationService = new ValidationService();

    private class FakeTemplateSource : ITemplateSource
    {
        private readonly List<TemplateEntry> _entries;

        public FakeTemplateSource(List<TemplateEntry> entries)
        {
            _entries = entries;
        }

        public Task<IReadOnlyList<TemplateEntry>> LoadAllAsync()
        {
            return Task.FromResult<IReadOnlyList<TemplateEntry>>(_entries);
        }
    }

    private EnhancedProjectGenerator CreateGenerator(ITemplateSource templateSource)
    {
        return new EnhancedProjectGenerator(_templateEngine, _validationService, templateSource);
    }

    private static ConfigRegistry CreateConfigRegistry(string projectName = "TestProject", string? outputPath = null)
    {
        outputPath ??= Path.GetTempPath();
        var registry = new ConfigRegistry();
        registry.Register(new ScaffoldConfigSection
        {
            ProjectName = projectName,
            OutputDirectory = outputPath,
            ProjectType = "WPF",
            TargetFramework = "net10.0-windows",
        });
        registry.Register(new CollectionConfigSection());
        registry.Register(new VisionConfigSection());
        registry.Register(new SystemConfigSection());
        registry.Register(new UIConfigSection());
        return registry;
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ScaffoldX_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task GenerateAsync_无模板_返回失败()
    {
        var templateSource = new FakeTemplateSource([]);
        var generator = CreateGenerator(templateSource);
        var registry = CreateConfigRegistry();
        var result = await generator.GenerateAsync(registry);
        Assert.False(result.Success);
        Assert.Contains("未找到", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_无效项目名_返回失败()
    {
        var templateSource = new FakeTemplateSource([
            new TemplateEntry
            {
                Name = "Test",
                Metadata = new TemplateMetadata { Name = "Test", IsRequired = true },
                Content = "##OUTPUT: test.txt\ncontent"
            }
        ]);
        var generator = CreateGenerator(templateSource);
        var registry = CreateConfigRegistry("1Invalid");
        var result = await generator.GenerateAsync(registry);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task GenerateAsync_有模板_生成文件()
    {
        var outputDir = CreateTempDir();

        try
        {
            var templateSource = new FakeTemplateSource([
                new TemplateEntry
                {
                    Name = "TestFile",
                    Metadata = new TemplateMetadata
                    {
                        Name = "TestFile",
                        IsRequired = true,
                        OutputPathTemplate = "src/{{ProjectName}}.App/Program.cs"
                    },
                    Content = "// Generated for {{ ProjectName }}\nnamespace {{ NamespacePrefix }}.App;"
                }
            ]);

            var generator = CreateGenerator(templateSource);
            var registry = CreateConfigRegistry("TestProj", outputDir);
            var result = await generator.GenerateAsync(registry);

            Assert.True(result.Success);
            Assert.Equal(1, result.FileCount);

            var generatedFile = Path.Combine(outputDir, "TestProj", "src", "TestProj.App", "Program.cs");
            Assert.True(File.Exists(generatedFile));
            var content = await File.ReadAllTextAsync(generatedFile);
            Assert.Contains("TestProj", content);
        }
        finally
        {
            try { Directory.Delete(outputDir, true); } catch { }
        }
    }

    [Fact]
    public async Task GenerateAsync_根据配置过滤模板()
    {
        var outputDir = CreateTempDir();

        try
        {
            var templateSource = new FakeTemplateSource([
                new TemplateEntry
                {
                    Name = "Required",
                    Metadata = new TemplateMetadata
                    {
                        Name = "Required",
                        IsRequired = true,
                        OutputPathTemplate = "src/Required.txt"
                    },
                    Content = "Required content"
                },
                new TemplateEntry
                {
                    Name = "S7Driver",
                    Metadata = new TemplateMetadata
                    {
                        Name = "S7Driver",
                        IsRequired = false,
                        RequiredFeatures = ["EnableSiemensS7"],
                        OutputPathTemplate = "src/S7Driver.txt"
                    },
                    Content = "S7 Driver content"
                }
            ]);

            var generator = CreateGenerator(templateSource);

            var registry1 = CreateConfigRegistry("FilterTest1", outputDir);
            var result1 = await generator.GenerateAsync(registry1);
            Assert.True(result1.Success);
            Assert.Equal(1, result1.FileCount);

            var registry2 = CreateConfigRegistry("FilterTest2", outputDir);
            registry2.Register(new CollectionConfigSection { EnableSiemensS7 = true });
            var result2 = await generator.GenerateAsync(registry2);
            Assert.True(result2.Success);
            Assert.Equal(2, result2.FileCount);
        }
        finally
        {
            try { Directory.Delete(outputDir, true); } catch { }
        }
    }

    [Fact]
    public async Task GenerateAsync_渲染模板变量()
    {
        var outputDir = CreateTempDir();

        try
        {
            var templateSource = new FakeTemplateSource([
                new TemplateEntry
                {
                    Name = "RenderTest",
                    Metadata = new TemplateMetadata
                    {
                        Name = "RenderTest",
                        IsRequired = true,
                        OutputPathTemplate = "src/{{ProjectName}}.txt"
                    },
                    Content = "Hello {{ Author }} from {{ ProjectName }}!"
                }
            ]);

            var generator = CreateGenerator(templateSource);
            var registry = CreateConfigRegistry("RenderProj", outputDir);
            var scaffold = registry.GetSection("Scaffold") as ScaffoldConfigSection;
            if (scaffold != null) scaffold.Author = "TestAuthor";
            var result = await generator.GenerateAsync(registry);

            Assert.True(result.Success);
            var generatedFile = Path.Combine(outputDir, "RenderProj", "src", "RenderProj.txt");
            Assert.True(File.Exists(generatedFile));
            var content = await File.ReadAllTextAsync(generatedFile);
            Assert.Contains("Hello TestAuthor from RenderProj!", content);
        }
        finally
        {
            try { Directory.Delete(outputDir, true); } catch { }
        }
    }

    [Fact]
    public async Task GenerateAsync_报告进度()
    {
        var templateSource = new FakeTemplateSource([
            new TemplateEntry
            {
                Name = "Test",
                Metadata = new TemplateMetadata { Name = "Test", IsRequired = true, OutputPathTemplate = "test.txt" },
                Content = "content"
            }
        ]);
        var generator = CreateGenerator(templateSource);
        var registry = CreateConfigRegistry();
        var messages = new List<string>();

        var progress = new DirectProgress<GenerationProgress>(p => messages.Add(p.Message));
        await generator.GenerateAsync(registry, progress);

        Assert.NotEmpty(messages);
    }

    private sealed class DirectProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
