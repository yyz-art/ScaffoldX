using FluentAssertions;
using Moq;
using ScaffoldX.App.Services;
using Xunit;

#nullable disable

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// Tests verifying the <see cref="IDialogService"/> interface contract and mockability.
/// </summary>
public class IDialogServiceTests
{
    [Fact]
    public void Interface_Exists_And_Is_Public()
    {
        // Arrange & Act
        var type = typeof(IDialogService);

        // Assert
        type.IsInterface.Should().BeTrue();
        type.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Interface_Declares_ShowOpenFolderDialog()
    {
        var method = typeof(IDialogService).GetMethod(nameof(IDialogService.ShowOpenFolderDialog));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Interface_Declares_ShowOpenFileDialog()
    {
        var method = typeof(IDialogService).GetMethod(nameof(IDialogService.ShowOpenFileDialog));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Interface_Declares_ShowOpenFilesDialog()
    {
        var method = typeof(IDialogService).GetMethod(nameof(IDialogService.ShowOpenFilesDialog));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IReadOnlyList<string>));
    }

    [Fact]
    public void Interface_Declares_ShowSaveFileDialog()
    {
        var method = typeof(IDialogService).GetMethod(nameof(IDialogService.ShowSaveFileDialog));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(string));
    }

    [Fact]
    public void Mock_Can_Setup_ShowOpenFolderDialog()
    {
        // Arrange
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.ShowOpenFolderDialog(It.IsAny<string?>()))
            .Returns(@"C:\Projects\MyAnnotation");

        // Act
        var result = mock.Object.ShowOpenFolderDialog("选择目录");

        // Assert
        result.Should().Be(@"C:\Projects\MyAnnotation");
    }

    [Fact]
    public void Mock_Can_Setup_ShowOpenFolderDialog_Returning_Null()
    {
        // Arrange — user cancels dialog
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.ShowOpenFolderDialog(It.IsAny<string?>()))
            .Returns((string?)null);

        // Act
        var result = mock.Object.ShowOpenFolderDialog();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Mock_Can_Setup_ShowOpenFileDialog()
    {
        // Arrange
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.ShowOpenFileDialog(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(@"C:\Data\project.json");

        // Act
        var result = mock.Object.ShowOpenFileDialog("JSON|*.json", "打开项目");

        // Assert
        result.Should().Be(@"C:\Data\project.json");
    }

    [Fact]
    public void Mock_Can_Setup_ShowOpenFilesDialog()
    {
        // Arrange
        var files = new List<string> { @"C:\img1.jpg", @"C:\img2.png", @"C:\img3.bmp" };
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.ShowOpenFilesDialog(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(files);

        // Act
        var result = mock.Object.ShowOpenFilesDialog("Images|*.jpg;*.png", "选择图像");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(@"C:\img1.jpg");
    }

    [Fact]
    public void Mock_Can_Setup_ShowSaveFileDialog()
    {
        // Arrange
        var mock = new Mock<IDialogService>();
        mock.Setup(d => d.ShowSaveFileDialog(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(@"C:\output\model.pt");

        // Act
        var result = mock.Object.ShowSaveFileDialog("Model|*.pt", "保存模型");

        // Assert
        result.Should().Be(@"C:\output\model.pt");
    }

    [Fact]
    public void WpfDialogService_Implements_IDialogService()
    {
        // Assert — type check only (cannot instantiate without WPF dispatcher)
        typeof(WpfDialogService).Should().Implement<IDialogService>();
    }
}
