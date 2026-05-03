using System.Diagnostics;
using System.IO;
using System.Text.Json;
using ScaffoldX.App.Models;
using ScaffoldX.Core.TemplateProcessing;

namespace ScaffoldX.App.Services;

/// <summary>
/// <see cref="IProjectGenerator"/> 的实现。
/// 按照 PRD §10.4 定义的六步流程生成项目骨架：
/// 验证 → 构建上下文 → 选择模板 → 渲染 → 后处理 → 记录历史。
/// </summary>
public class ProjectGenerator : IProjectGenerator
{
    private readonly ITemplateEngine _templateEngine;
    private readonly IHistoryService _historyService;
    private readonly IValidationService _validationService;
    private readonly TemplateRegistry _templateRegistry;

    /// <summary>
    /// 初始化 <see cref="ProjectGenerator"/>，通过构造函数注入所有依赖。
    /// </summary>
    /// <param name="templateEngine">Scriban 模板引擎。</param>
    /// <param name="historyService">历史记录服务。</param>
    /// <param name="validationService">输入验证服务。</param>
    /// <param name="templateRegistry">模板注册表，管理所有 .stpl 模板的加载与查询。</param>
    public ProjectGenerator(
        ITemplateEngine templateEngine,
        IHistoryService historyService,
        IValidationService validationService,
        TemplateRegistry templateRegistry)
    {
        _templateEngine = templateEngine;
        _historyService = historyService;
        _validationService = validationService;
        _templateRegistry = templateRegistry;
    }

    /// <summary>
    /// 异步执行项目生成流程。
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

            // 步骤 2：构建变量上下文（通过 Core 层 VariableResolver）
            progress.Report(new GenerationProgress("正在构建变量上下文…", 15));
            var coreConfig = MapToCoreConfig(config);
            var variables = VariableResolver.BuildVariableContext(coreConfig);

            // 步骤 3：加载并选择模板集合
            progress.Report(new GenerationProgress("正在加载模板…", 25));
            await _templateRegistry.LoadFromAssemblyAsync().ConfigureAwait(false);
            var templates = _templateRegistry.GetTemplatesForConfig(coreConfig);

            if (templates.Count == 0)
            {
                return GenerationResult.Fail("没有匹配当前配置的模板，请检查项目类型和功能选项。");
            }

            // 步骤 4：渲染模板并写入文件
            progress.Report(new GenerationProgress("正在渲染并写入文件…", 40));
            string projectRoot = Path.Combine(config.OutputPath, config.ProjectName);
            Directory.CreateDirectory(projectRoot);

            int fileCount = 0;
            int total = templates.Count;

            for (int i = 0; i < total; i++)
            {
                var template = templates[i];

                // 渲染输出路径（路径中可能包含 Scriban 变量）
                string renderedPath = _templateEngine.Render(template.OutputPathTemplate, variables);

                // 渲染模板内容
                string renderedContent = _templateEngine.Render(template.Content, variables);

                // 后处理：行尾规范化、XML 实体还原、尾部空白清理
                renderedContent = PostProcessor.Process(renderedContent, renderedPath, coreConfig);

                // 写入文件
                string fullPath = Path.Combine(projectRoot, renderedPath);
                string? dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await File.WriteAllTextAsync(fullPath, renderedContent).ConfigureAwait(false);
                fileCount++;

                int percent = 40 + (int)((i + 1) / (double)total * 40);
                progress.Report(new GenerationProgress($"已写入：{renderedPath}", percent));
            }

            // 步骤 5：后处理（dotnet restore 等扩展步骤）
            progress.Report(new GenerationProgress("正在执行后处理…", 85));
            await PostProcessAsync(projectRoot, config).ConfigureAwait(false);

            // 步骤 6：记录历史
            progress.Report(new GenerationProgress("正在记录历史…", 95));
            string configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            string targetFramework = config.DotNetVersion switch
            {
                ".NET 6" => config.UIFramework == "WPF" ? "net6.0-windows" : "net6.0",
                _        => config.UIFramework == "WPF" ? "net8.0-windows" : "net8.0"
            };

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
    /// 将 App 层的 <see cref="Models.ProjectConfig"/> 映射为 Core 层的 <see cref="Core.Models.ProjectConfig"/>。
    /// 两层的配置模型字段命名和结构不同，此方法负责桥接。
    /// </summary>
    /// <param name="appConfig">App 层的项目配置。</param>
    /// <returns>Core 层的项目配置。</returns>
    private static Core.Models.ProjectConfig MapToCoreConfig(Models.ProjectConfig appConfig)
    {
        string targetFramework = appConfig.DotNetVersion switch
        {
            ".NET 6" => appConfig.UIFramework == "WPF" ? "net6.0-windows" : "net6.0",
            _        => appConfig.UIFramework == "WPF" ? "net8.0-windows" : "net8.0"
        };

        return new Core.Models.ProjectConfig
        {
            ProjectName     = appConfig.ProjectName,
            NamespacePrefix = appConfig.NamespacePrefix,
            TargetFramework = targetFramework,
            UIFramework     = appConfig.UIFramework,
            OutputDirectory = appConfig.OutputPath,
            Author          = string.Empty,
            Company         = string.Empty,
            Description     = appConfig.ProjectDescription,

            // 采集类：从 SelectedDrivers 列表映射到布尔标志
            EnableSiemensS7    = appConfig.SelectedDrivers.Contains("S7Net"),
            EnableModbusTcp    = appConfig.SelectedDrivers.Contains("ModbusTcp"),
            EnableOpcUa        = appConfig.SelectedDrivers.Contains("OpcUa"),
            EnableMitsubishiMc = appConfig.SelectedDrivers.Contains("MitsubishiMc"),
            EnableOmronFins    = appConfig.SelectedDrivers.Contains("OmronFins"),

            // 视觉类
            EnableVision = appConfig.ProjectType == "Vision",
            CameraBrand  = appConfig.CameraBrand,
            ModelType    = appConfig.ModelType,

            // 系统类
            EnableUserManagement  = appConfig.SelectedModules.Contains("UserManagement"),
            EnableRolePermission  = appConfig.SelectedModules.Contains("RolePermission"),
            EnableSystemLog       = appConfig.SelectedModules.Contains("SystemLog"),
            EnableThemeSwitcher   = appConfig.SelectedModules.Contains("ThemeSwitcher"),

            // 其他
            InitGitRepository     = true,
            GeneratePublishScripts = true
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
