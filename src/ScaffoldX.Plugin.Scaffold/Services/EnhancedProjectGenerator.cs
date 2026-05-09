using System.Diagnostics;
using System.IO;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Abstractions.Templates;

namespace ScaffoldX.Plugin.Scaffold.Services;

public sealed class EnhancedProjectGenerator : IProjectGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IValidationService _validationService;
    private readonly ITemplateSource _templateSource;

    public EnhancedProjectGenerator(
        ITemplateEngine templateEngine,
        IValidationService validationService,
        ITemplateSource templateSource)
    {
        _templateEngine = templateEngine;
        _validationService = validationService;
        _templateSource = templateSource;
    }

    public async Task<GenerationResult> GenerateAsync(ConfigRegistry configRegistry, IProgress<GenerationProgress>? progress = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var resolver = new AggregatedConfigResolver(configRegistry);
            var variables = resolver.BuildVariableContext();

            var scaffoldSection = configRegistry.GetSection("Scaffold") as ScaffoldConfigSection;
            var projectName = scaffoldSection?.ProjectName ?? variables.GetValueOrDefault("ProjectName", "")?.ToString() ?? "";
            var outputPath = scaffoldSection?.OutputDirectory ?? variables.GetValueOrDefault("OutputDirectory", "")?.ToString() ?? "";

            if (!string.IsNullOrEmpty(outputPath) && !variables.ContainsKey("OutputDirectory"))
                variables["OutputDirectory"] = outputPath;

            progress?.Report(new GenerationProgress("正在验证配置…", 5));

            var nameResult = _validationService.ValidateProjectName(projectName);
            if (!nameResult.IsValid)
                return GenerationResult.Fail($"项目名称验证失败：{nameResult.ErrorMessage}");

            progress?.Report(new GenerationProgress("正在加载模板…", 15));

            var templates = await _templateSource.LoadAllAsync();
            if (templates.Count == 0)
                return GenerationResult.Fail("未找到任何模板文件");

            progress?.Report(new GenerationProgress($"已加载 {templates.Count} 个模板", 25));

            var descriptors = templates.Select(t => t.Metadata.ToDescriptor()).ToList();

            progress?.Report(new GenerationProgress("正在过滤模板…", 35));

            var filter = new DeclarativeTemplateFilter();
            var filtered = filter.Apply(descriptors, variables);

            progress?.Report(new GenerationProgress($"将生成 {filtered.Count} 个文件…", 45));

            var targetRoot = Path.Combine(outputPath, projectName);
            int total = filtered.Count;
            int current = 0;
            int fileCount = 0;

            foreach (var descriptor in filtered)
            {
                current++;
                var percent = 45 + (int)(current * 45.0 / Math.Max(total, 1));

                var match = templates.FirstOrDefault(t => t.Metadata.Name == descriptor.Name);
                if (match == null) continue;

                var (outputPathTemplate, cleanContent) = ParseTemplate(match.Content);

                var renderedOutputPath = string.IsNullOrEmpty(outputPathTemplate)
                    ? descriptor.OutputPathTemplate
                    : outputPathTemplate;

                if (string.IsNullOrEmpty(renderedOutputPath)) continue;

                var relativePath = _templateEngine.Render(renderedOutputPath, variables);
                var fullOutputPath = Path.Combine(targetRoot, relativePath);

                var rendered = _templateEngine.Render(cleanContent, variables);

                var dir = Path.GetDirectoryName(fullOutputPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                await File.WriteAllTextAsync(fullOutputPath, rendered);
                fileCount++;

                progress?.Report(new GenerationProgress($"已生成：{relativePath}", percent));
            }

            progress?.Report(new GenerationProgress("生成完成", 100));
            stopwatch.Stop();
            return GenerationResult.Ok(outputPath, fileCount, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return GenerationResult.Fail($"生成过程中发生异常：{ex.Message}");
        }
    }

    private static (string outputPath, string cleanContent) ParseTemplate(string content)
    {
        var lines = content.Split('\n');
        var outputPath = "";
        var cleanLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("##OUTPUT:"))
            {
                outputPath = trimmed["##OUTPUT:".Length..].Trim();
            }
            else if (trimmed.StartsWith("##REQUIRED:"))
            {
                // 跳过指令行
            }
            else
            {
                cleanLines.Add(line);
            }
        }

        return (outputPath, string.Join('\n', cleanLines));
    }
}
