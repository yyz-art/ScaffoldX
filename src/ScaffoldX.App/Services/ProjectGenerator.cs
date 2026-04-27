using System.Diagnostics;
using System.IO;
using System.Text.Json;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// <see cref="IProjectGenerator"/> 的默认实现。
/// 按照"验证 → 构建上下文 → 选择模板 → 渲染 → 后处理 → 记录历史"六步流程生成项目骨架。
/// </summary>
public class ProjectGenerator : IProjectGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IHistoryService _historyService;
    private readonly IValidationService _validationService;

    /// <summary>
    /// 初始化 <see cref="ProjectGenerator"/>，通过构造函数注入所有依赖。
    /// </summary>
    /// <param name="templateEngine">Scriban 模板引擎。</param>
    /// <param name="historyService">历史记录服务。</param>
    /// <param name="validationService">输入验证服务。</param>
    public ProjectGenerator(
        ITemplateEngine templateEngine,
        IHistoryService historyService,
        IValidationService validationService)
    {
        _templateEngine = templateEngine;
        _historyService = historyService;
        _validationService = validationService;
    }

    /// <summary>
    /// 异步执行项目生成流程：验证配置 → 构建变量上下文 → 选择模板 → 渲染模板 → 后处理 → 记录历史。
    /// </summary>
    /// <param name="config">项目生成配置。</param>
    /// <param name="progress">进度回调，用于向 UI 层报告当前步骤和百分比。</param>
    /// <returns>生成结果，包含成功/失败状态、输出路径、文件数量及耗时。</returns>
    public async Task<GenerationResult> GenerateAsync(
        ProjectConfig config,
        IProgress<GenerationProgress> progress)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 步骤 1：验证配置
            progress.Report(new GenerationProgress("正在验证配置…", 5));
            ValidationResult nameResult = _validationService.ValidateProjectName(config.ProjectName);
            if (!nameResult.IsValid)
            {
                return GenerationResult.Fail($"项目名称验证失败：{nameResult.ErrorMessage}");
            }

            ValidationResult pathResult = _validationService.ValidateOutputPath(
                config.OutputPath, config.ProjectName);
            if (!pathResult.IsValid)
            {
                return GenerationResult.Fail($"输出路径验证失败：{pathResult.ErrorMessage}");
            }

            // 步骤 2：构建变量上下文
            progress.Report(new GenerationProgress("正在构建变量上下文…", 15));
            string namespacePrefix = string.IsNullOrWhiteSpace(config.NamespacePrefix)
                ? _validationService.ToPascalCase(config.ProjectName)
                : config.NamespacePrefix;

            string targetFramework = config.DotNetVersion switch
            {
                ".NET 6" => config.UIFramework == "WPF" ? "net6.0-windows" : "net6.0",
                _        => config.UIFramework == "WPF" ? "net8.0-windows" : "net8.0"
            };

            var variables = new Dictionary<string, object>
            {
                ["project_name"]       = config.ProjectName,
                ["namespace_prefix"]   = namespacePrefix,
                ["output_path"]        = config.OutputPath,
                ["ui_framework"]       = config.UIFramework,
                ["dot_net_version"]    = config.DotNetVersion,
                ["target_framework"]   = targetFramework,
                ["project_type"]       = config.ProjectType,
                ["project_description"]= config.ProjectDescription,
                ["selected_drivers"]   = config.SelectedDrivers,
                ["enable_simulation"]  = config.EnableSimulationDriver,
                ["default_plc_ip"]     = config.DefaultPLCIp,
                ["default_plc_port"]   = config.DefaultPLCPort,
                ["s7_rack"]            = config.S7Rack,
                ["s7_slot"]            = config.S7Slot,
                ["opcua_endpoint"]     = config.OpcUaEndpoint,
                ["camera_brand"]       = config.CameraBrand,
                ["model_type"]         = config.ModelType,
                ["model_path"]         = config.ModelPath,
                ["enable_pipeline"]    = config.EnablePipeline,
                ["selected_modules"]   = config.SelectedModules,
                ["enable_login_window"]= config.EnableLoginWindow,
                ["enable_cross_platform"] = config.EnableCrossPlatform,
                ["force_password_change"] = config.ForcePasswordChange
            };

            // 步骤 3：选择模板集合
            progress.Report(new GenerationProgress("正在选择模板…", 30));
            IReadOnlyList<(string RelativePath, string TemplateContent)> templates =
                SelectTemplates(config);

            // 步骤 4：渲染模板并写入文件
            progress.Report(new GenerationProgress("正在渲染并写入文件…", 45));
            string projectRoot = Path.Combine(config.OutputPath, config.ProjectName);
            Directory.CreateDirectory(projectRoot);

            int fileCount = 0;
            int total = templates.Count;

            for (int i = 0; i < total; i++)
            {
                (string relativePath, string templateContent) = templates[i];
                string renderedPath = _templateEngine.Render(relativePath, variables);
                string renderedContent = _templateEngine.Render(templateContent, variables);

                string fullPath = Path.Combine(projectRoot, renderedPath);
                string? dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await File.WriteAllTextAsync(fullPath, renderedContent).ConfigureAwait(false);
                fileCount++;

                int percent = 45 + (int)((i + 1) / (double)total * 35);
                progress.Report(new GenerationProgress($"已写入：{renderedPath}", percent));
            }

            // 步骤 5：后处理（占位，可扩展为格式化、dotnet restore 等）
            progress.Report(new GenerationProgress("正在执行后处理…", 85));
            await PostProcessAsync(projectRoot, config).ConfigureAwait(false);

            // 步骤 6：记录历史
            progress.Report(new GenerationProgress("正在记录历史…", 95));
            string configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await _historyService.SaveAsync(new ProjectHistory
            {
                ProjectName     = config.ProjectName,
                ProjectType     = config.ProjectType,
                OutputPath      = projectRoot,
                TargetFramework = targetFramework,
                UIFramework     = config.UIFramework,
                CreatedAt       = DateTime.UtcNow,
                ConfigJson      = configJson
            }).ConfigureAwait(false);

            stopwatch.Stop();
            progress.Report(new GenerationProgress("生成完成！", 100));

            return GenerationResult.Ok(projectRoot, fileCount, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return GenerationResult.Fail($"生成过程中发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据项目类型返回对应的模板集合（相对路径 + 模板内容）。
    /// 当前为最小骨架实现，后续可替换为从嵌入资源或磁盘加载。
    /// </summary>
    /// <param name="config">项目配置，用于区分项目类型。</param>
    /// <returns>模板列表，每项包含相对路径模板和文件内容模板。</returns>
    private static IReadOnlyList<(string RelativePath, string TemplateContent)> SelectTemplates(
        ProjectConfig config)
    {
        return config.ProjectType switch
        {
            "Collection" => CollectionTemplates(),
            "Vision"     => VisionTemplates(),
            "System"     => SystemTemplates(),
            _            => CollectionTemplates()
        };
    }

    /// <summary>
    /// 采集类项目的最小模板集合。
    /// </summary>
    private static List<(string, string)> CollectionTemplates()
    {
        return new List<(string, string)>
        {
            (
                "{{project_name}}.sln",
                "# {{project_name}} Solution\n# Generated by ScaffoldX\n"
            ),
            (
                "src/{{project_name}}/{{project_name}}.csproj",
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <TargetFramework>{{target_framework}}</TargetFramework>\n" +
                "    <RootNamespace>{{namespace_prefix}}</RootNamespace>\n" +
                "    <Nullable>enable</Nullable>\n" +
                "    <ImplicitUsings>enable</ImplicitUsings>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n"
            ),
            (
                "src/{{project_name}}/Program.cs",
                "// {{project_name}} - {{project_description}}\n" +
                "// Generated by ScaffoldX\n\n" +
                "namespace {{namespace_prefix}};\n\n" +
                "internal static class Program\n{\n" +
                "    [STAThread]\n" +
                "    private static void Main()\n    {\n" +
                "        // Entry point\n    }\n}\n"
            )
        };
    }

    /// <summary>
    /// 视觉类项目的最小模板集合。
    /// </summary>
    private static List<(string, string)> VisionTemplates()
    {
        return new List<(string, string)>
        {
            (
                "{{project_name}}.sln",
                "# {{project_name}} Vision Solution\n# Generated by ScaffoldX\n"
            ),
            (
                "src/{{project_name}}/{{project_name}}.csproj",
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <TargetFramework>{{target_framework}}</TargetFramework>\n" +
                "    <RootNamespace>{{namespace_prefix}}</RootNamespace>\n" +
                "    <Nullable>enable</Nullable>\n" +
                "    <ImplicitUsings>enable</ImplicitUsings>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n"
            )
        };
    }

    /// <summary>
    /// 系统类项目的最小模板集合。
    /// </summary>
    private static List<(string, string)> SystemTemplates()
    {
        return new List<(string, string)>
        {
            (
                "{{project_name}}.sln",
                "# {{project_name}} System Solution\n# Generated by ScaffoldX\n"
            ),
            (
                "src/{{project_name}}/{{project_name}}.csproj",
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <TargetFramework>{{target_framework}}</TargetFramework>\n" +
                "    <RootNamespace>{{namespace_prefix}}</RootNamespace>\n" +
                "    <Nullable>enable</Nullable>\n" +
                "    <ImplicitUsings>enable</ImplicitUsings>\n" +
                "  </PropertyGroup>\n" +
                "</Project>\n"
            )
        };
    }

    /// <summary>
    /// 生成后处理步骤，当前为占位实现（可扩展为 dotnet restore、格式化等）。
    /// </summary>
    /// <param name="projectRoot">生成的项目根目录。</param>
    /// <param name="config">项目配置。</param>
    private static Task PostProcessAsync(string projectRoot, ProjectConfig config)
    {
        _ = projectRoot;
        _ = config;
        return Task.CompletedTask;
    }
}
