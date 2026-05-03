using FluentAssertions;
using ScaffoldX.Core.FileGeneration;
using ScaffoldX.Core.Models;
using Xunit;

namespace ScaffoldX.Core.Tests.FileGeneration;

/// <summary>
/// FileTreeBuilder 单元测试，验证文件树构建逻辑。
/// </summary>
public class FileTreeBuilderTests
{
    private readonly FileTreeBuilder _sut = new();

    [Fact]
    public void BuildTree_ShouldCreateRootNode_WithProjectName()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "TestProject",
            UIFramework = "WPF"
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        tree.Should().NotBeNull();
        tree.Name.Should().Be("TestProject");
        tree.NodeType.Should().Be(NodeType.Folder);
    }

    [Fact]
    public void BuildTree_ShouldContainSolutionFile()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "TestProject",
            UIFramework = "WPF"
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var slnFile = tree.Children.FirstOrDefault(c => c.Name == "TestProject.sln");
        slnFile.Should().NotBeNull();
        slnFile!.NodeType.Should().Be(NodeType.OtherFile);
    }

    [Fact]
    public void BuildTree_ShouldContainSrcDirectory()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "TestProject",
            UIFramework = "WPF"
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var srcDir = tree.Children.FirstOrDefault(c => c.Name == "src");
        srcDir.Should().NotBeNull();
        srcDir!.NodeType.Should().Be(NodeType.Folder);
    }

    [Fact]
    public void BuildTree_ShouldContainVisionModules_WhenVisionEnabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "VisionProject",
            UIFramework = "WPF",
            EnableVision = true,
            CameraBrand = "Hikvision"
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var srcDir = tree.Children.FirstOrDefault(c => c.Name == "src");
        srcDir.Should().NotBeNull();

        // 检查是否包含视觉模块
        var visionNodes = GetAllNodes(tree)
            .Where(n => n.Name.Contains("Vision") || n.Name.Contains("Camera"))
            .ToList();

        visionNodes.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildTree_ShouldContainInferenceEngine_WhenVisionEnabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "VisionProject",
            UIFramework = "WPF",
            EnableVision = true
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var inferenceNodes = GetAllNodes(tree)
            .Where(n => n.Name.Contains("Inference"))
            .ToList();

        inferenceNodes.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildTree_ShouldNotContainVisionModules_WhenVisionDisabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "TestProject",
            UIFramework = "WPF",
            EnableVision = false
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var visionNodes = GetAllNodes(tree)
            .Where(n => n.Name.Contains("Vision"))
            .ToList();

        visionNodes.Should().BeEmpty();
    }

    [Fact]
    public void BuildTree_ShouldContainTestsDirectory()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "TestProject",
            UIFramework = "WPF"
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var testsDir = tree.Children.FirstOrDefault(c => c.Name == "tests");
        testsDir.Should().NotBeNull();
        testsDir!.NodeType.Should().Be(NodeType.Folder);
    }

    [Fact]
    public void BuildTree_ShouldContainGitignore_WhenGitEnabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "TestProject",
            UIFramework = "WPF",
            InitGitRepository = true
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var gitignore = GetAllNodes(tree).FirstOrDefault(n => n.Name == ".gitignore");
        gitignore.Should().NotBeNull();
    }

    [Fact]
    public void BuildTree_ShouldNotContainGitignore_WhenGitDisabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "TestProject",
            UIFramework = "WPF",
            InitGitRepository = false
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var gitignore = GetAllNodes(tree).FirstOrDefault(n => n.Name == ".gitignore");
        gitignore.Should().BeNull();
    }

    [Fact]
    public void BuildTree_ShouldContainSystemModules_WhenEnabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "SystemProject",
            UIFramework = "WPF",
            EnableUserManagement = true,
            EnableRolePermission = true,
            EnableSystemLog = true,
            EnableThemeSwitcher = true
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var allNodes = GetAllNodes(tree);
        allNodes.Should().Contain(n => n.Name == "UserManagement");
        allNodes.Should().Contain(n => n.Name == "RolePermission");
        allNodes.Should().Contain(n => n.Name == "SystemLog");
        allNodes.Should().Contain(n => n.Name == "ThemeSwitcher");
    }

    [Fact]
    public void BuildTree_ShouldNotContainSystemModules_WhenDisabled()
    {
        // Arrange
        var config = new ProjectConfig
        {
            ProjectName = "NoSystemProject",
            UIFramework = "WPF",
            EnableUserManagement = false,
            EnableRolePermission = false,
            EnableSystemLog = false,
            EnableThemeSwitcher = false
        };

        // Act
        var tree = _sut.BuildTree(config);

        // Assert
        var allNodes = GetAllNodes(tree);
        allNodes.Should().NotContain(n => n.Name == "UserManagement");
        allNodes.Should().NotContain(n => n.Name == "RolePermission");
        allNodes.Should().NotContain(n => n.Name == "SystemLog");
        allNodes.Should().NotContain(n => n.Name == "ThemeSwitcher");
    }

    private static List<FileTreeNode> GetAllNodes(FileTreeNode root)
    {
        var nodes = new List<FileTreeNode> { root };
        foreach (var child in root.Children)
        {
            nodes.AddRange(GetAllNodes(child));
        }
        return nodes;
    }
}
