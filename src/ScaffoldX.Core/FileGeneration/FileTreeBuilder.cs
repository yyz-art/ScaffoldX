using ScaffoldX.Core.Models;
using ScaffoldX.Core.TemplateProcessing;

namespace ScaffoldX.Core.FileGeneration;

/// <summary>
/// 文件树节点类型枚举。
/// </summary>
public enum NodeType
{
    /// <summary>目录节点。</summary>
    Folder,
    /// <summary>C# 源文件（.cs）。</summary>
    CsFile,
    /// <summary>XAML / AXAML 文件。</summary>
    XamlFile,
    /// <summary>项目文件（.csproj）。</summary>
    CsprojFile,
    /// <summary>其他类型文件（.json、.props、.sln 等）。</summary>
    OtherFile,
}

/// <summary>
/// 文件树节点，用于向导步骤四的预览展示。
/// </summary>
public class FileTreeNode
{
    /// <summary>节点显示名称（文件名或目录名）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>节点类型。</summary>
    public NodeType NodeType { get; set; }

    /// <summary>子节点列表；叶子节点为空列表。</summary>
    public List<FileTreeNode> Children { get; set; } = new();

    /// <summary>节点相对于输出根目录的完整相对路径（使用正斜杠）。</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>是否为必须生成的节点（对应 TemplateFile.IsRequired）。</summary>
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// 根据 <see cref="ProjectConfig"/> 构建将要生成的文件树，供向导预览使用。
/// </summary>
public class FileTreeBuilder
{
    // ── 公共 API ──────────────────────────────────────────────────────────

    /// <summary>
    /// 根据项目配置构建完整的文件树。
    /// </summary>
    /// <param name="config">用户在向导中填写的项目配置。</param>
    /// <returns>代表解决方案根目录的根节点。</returns>
    public FileTreeNode BuildTree(ProjectConfig config)
    {
        var projectName = VariableResolver.ToPascalCase(config.ProjectName);
        var xamlExt = config.UIFramework.Equals("Avalonia", StringComparison.OrdinalIgnoreCase)
            ? "axaml"
            : "xaml";

        var root = new FileTreeNode
        {
            Name = projectName,
            NodeType = NodeType.Folder,
            RelativePath = string.Empty,
            IsRequired = true,
        };

        // 解决方案级文件
        AddChild(root, $"{projectName}.sln", NodeType.OtherFile, true);
        AddChild(root, "Directory.Build.props", NodeType.OtherFile, true);
        AddChild(root, "global.json", NodeType.OtherFile, true);
        AddChild(root, ".gitignore", NodeType.OtherFile, config.InitGitRepository);
        AddChild(root, "README.md", NodeType.OtherFile, true);

        if (config.GeneratePublishScripts)
        {
            AddChild(root, "publish.bat", NodeType.OtherFile, true);
            AddChild(root, "publish.sh", NodeType.OtherFile, true);
        }

        // src 目录
        var src = AddFolder(root, "src");

        // ── App 项目 ──────────────────────────────────────────────────────
        var appProject = AddFolder(src, $"{projectName}.App");
        AddChild(appProject, $"{projectName}.App.csproj", NodeType.CsprojFile, true);
        AddChild(appProject, "App.xaml", NodeType.XamlFile, true);
        AddChild(appProject, "App.xaml.cs", NodeType.CsFile, true);
        AddChild(appProject, $"MainWindow.{xamlExt}", NodeType.XamlFile, true);
        AddChild(appProject, "MainWindow.xaml.cs", NodeType.CsFile, true);

        var appViews = AddFolder(appProject, "Views");
        AddChild(appViews, $"ShellView.{xamlExt}", NodeType.XamlFile, true);
        AddChild(appViews, "ShellView.xaml.cs", NodeType.CsFile, true);

        var appVms = AddFolder(appProject, "ViewModels");
        AddChild(appVms, "ShellViewModel.cs", NodeType.CsFile, true);
        AddChild(appVms, "MainViewModel.cs", NodeType.CsFile, true);

        // ── Core 项目 ─────────────────────────────────────────────────────
        var coreProject = AddFolder(src, $"{projectName}.Core");
        AddChild(coreProject, $"{projectName}.Core.csproj", NodeType.CsprojFile, true);

        var coreModels = AddFolder(coreProject, "Models");
        AddChild(coreModels, "IPlugin.cs", NodeType.CsFile, true);
        AddChild(coreModels, "AppSettings.cs", NodeType.CsFile, true);

        var coreServices = AddFolder(coreProject, "Services");
        AddChild(coreServices, "IDataService.cs", NodeType.CsFile, true);

        // ── Infrastructure 项目 ───────────────────────────────────────────
        var infraProject = AddFolder(src, $"{projectName}.Infrastructure");
        AddChild(infraProject, $"{projectName}.Infrastructure.csproj", NodeType.CsprojFile, true);

        // 采集驱动
        if (config.EnableSiemensS7 || config.EnableModbusTcp || config.EnableOpcUa
            || config.EnableMitsubishiMc || config.EnableOmronFins)
        {
            var drivers = AddFolder(infraProject, "Drivers");

            if (config.EnableSiemensS7)
            {
                AddChild(drivers, "SiemensS7Driver.cs", NodeType.CsFile, false);
            }

            if (config.EnableModbusTcp)
            {
                AddChild(drivers, "ModbusTcpDriver.cs", NodeType.CsFile, false);
            }

            if (config.EnableOpcUa)
            {
                AddChild(drivers, "OpcUaDriver.cs", NodeType.CsFile, false);
            }

            if (config.EnableMitsubishiMc)
            {
                AddChild(drivers, "MitsubishiMcDriver.cs", NodeType.CsFile, false);
            }

            if (config.EnableOmronFins)
            {
                AddChild(drivers, "OmronFinsDriver.cs", NodeType.CsFile, false);
            }
        }

        // 视觉模块
        if (config.EnableVision)
        {
            var vision = AddFolder(infraProject, "Vision");
            AddChild(vision, $"{VariableResolver.ToPascalCase(config.CameraBrand)}Camera.cs", NodeType.CsFile, false);
            AddChild(vision, "VisionService.cs", NodeType.CsFile, false);
        }

        // 系统模块
        if (config.EnableUserManagement)
        {
            var users = AddFolder(infraProject, "UserManagement");
            AddChild(users, "UserService.cs", NodeType.CsFile, false);
            AddChild(users, "UserRepository.cs", NodeType.CsFile, false);
        }

        if (config.EnableAlarmManagement)
        {
            var alarms = AddFolder(infraProject, "Alarms");
            AddChild(alarms, "AlarmService.cs", NodeType.CsFile, false);
        }

        if (config.EnableDataLogging)
        {
            var logging = AddFolder(infraProject, "DataLogging");
            AddChild(logging, "DataLogService.cs", NodeType.CsFile, false);
        }

        if (config.EnableReporting)
        {
            var reports = AddFolder(infraProject, "Reporting");
            AddChild(reports, "ReportService.cs", NodeType.CsFile, false);
        }

        // tests 目录
        var tests = AddFolder(root, "tests");
        var coreTests = AddFolder(tests, $"{projectName}.Core.Tests");
        AddChild(coreTests, $"{projectName}.Core.Tests.csproj", NodeType.CsprojFile, true);
        AddChild(coreTests, "SampleTest.cs", NodeType.CsFile, true);

        return root;
    }

    // ── 私有辅助方法 ──────────────────────────────────────────────────────

    /// <summary>
    /// 在父节点下添加一个目录子节点并返回该节点。
    /// </summary>
    private static FileTreeNode AddFolder(FileTreeNode parent, string name)
    {
        var relativePath = string.IsNullOrEmpty(parent.RelativePath)
            ? name
            : $"{parent.RelativePath}/{name}";

        var node = new FileTreeNode
        {
            Name = name,
            NodeType = NodeType.Folder,
            RelativePath = relativePath,
            IsRequired = true,
        };

        parent.Children.Add(node);
        return node;
    }

    /// <summary>
    /// 在父节点下添加一个文件子节点。
    /// </summary>
    private static void AddChild(FileTreeNode parent, string name, NodeType nodeType, bool isRequired)
    {
        var relativePath = string.IsNullOrEmpty(parent.RelativePath)
            ? name
            : $"{parent.RelativePath}/{name}";

        parent.Children.Add(new FileTreeNode
        {
            Name = name,
            NodeType = nodeType,
            RelativePath = relativePath,
            IsRequired = isRequired,
        });
    }
}
