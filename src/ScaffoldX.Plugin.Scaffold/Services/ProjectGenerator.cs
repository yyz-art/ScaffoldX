using System.Diagnostics;
using System.IO;
using ScaffoldX.Abstractions.Config;
using ScaffoldX.Plugin.Scaffold.Services;

namespace ScaffoldX.Plugin.Scaffold.Services;

public interface IProjectGenerator
{
    Task<GenerationResult> GenerateAsync(ConfigRegistry configRegistry, IProgress<GenerationProgress>? progress = null);
}

public sealed class GenerationResult
{
    public bool Success { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public TimeSpan Elapsed { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;

    public static GenerationResult Ok(string outputPath, int fileCount, TimeSpan elapsed) =>
        new() { Success = true, OutputPath = outputPath, FileCount = fileCount, Elapsed = elapsed };

    public static GenerationResult Fail(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

public sealed class GenerationProgress
{
    public string Message { get; }
    public int Percent { get; }

    public GenerationProgress(string message, int percent)
    {
        Message = message;
        Percent = percent;
    }
}

public sealed class ProjectGenerator : IProjectGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IValidationService _validationService;

    public ProjectGenerator(ITemplateEngine templateEngine, IValidationService validationService)
    {
        _templateEngine = templateEngine;
        _validationService = validationService;
    }

    public async Task<GenerationResult> GenerateAsync(ConfigRegistry configRegistry, IProgress<GenerationProgress>? progress = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var resolver = new AggregatedConfigResolver(configRegistry);
            var variables = resolver.BuildVariableContext();

            progress?.Report(new GenerationProgress("正在验证配置…", 5));

            var projectName = variables.GetValueOrDefault("ProjectName", "")?.ToString() ?? "";
            var outputPath = variables.GetValueOrDefault("OutputDirectory", "")?.ToString() ?? "";

            var nameResult = _validationService.ValidateProjectName(projectName);
            if (!nameResult.IsValid)
                return GenerationResult.Fail($"项目名称验证失败：{nameResult.ErrorMessage}");

            progress?.Report(new GenerationProgress("正在构建变量上下文…", 15));
            progress?.Report(new GenerationProgress("生成完成（模板加载待集成）", 100));

            stopwatch.Stop();
            return GenerationResult.Ok(outputPath, 0, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return GenerationResult.Fail($"生成过程中发生异常：{ex.Message}");
        }
    }
}
