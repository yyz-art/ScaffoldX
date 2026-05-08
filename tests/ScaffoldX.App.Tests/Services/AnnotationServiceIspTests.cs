using FluentAssertions;
using Moq;
using ScaffoldX.App.Models;
using ScaffoldX.App.Services;
using Xunit;

namespace ScaffoldX.App.Tests.Services;

/// <summary>
/// 验证 AnnotationService ISP 拆分的正确性。
/// 确保 IAnnotationRepository 和 IAnnotationExporter 可独立使用，
/// IAnnotationService 作为组合接口向后兼容。
/// </summary>
public class AnnotationServiceIspTests
{
    // ── 接口继承验证 ────────────────────────────────────────────────────────

    [Fact]
    public void IAnnotationService_ShouldInheritIAnnotationRepository()
    {
        typeof(IAnnotationService).IsAssignableTo(typeof(IAnnotationRepository)).Should().BeTrue();
    }

    [Fact]
    public void IAnnotationService_ShouldInheritIAnnotationExporter()
    {
        typeof(IAnnotationService).IsAssignableTo(typeof(IAnnotationExporter)).Should().BeTrue();
    }

    // ── 实现验证 ───────────────────────────────────────────────────────────

    [Fact]
    public void AnnotationRepository_ShouldImplementIAnnotationRepository()
    {
        typeof(AnnotationRepository).IsAssignableTo(typeof(IAnnotationRepository)).Should().BeTrue();
    }

    [Fact]
    public void AnnotationExporter_ShouldImplementIAnnotationExporter()
    {
        typeof(AnnotationExporter).IsAssignableTo(typeof(IAnnotationExporter)).Should().BeTrue();
    }

    [Fact]
    public void AnnotationService_ShouldImplementIAnnotationService()
    {
        typeof(AnnotationService).IsAssignableTo(typeof(IAnnotationService)).Should().BeTrue();
    }

    // ── ISP 隔离验证：仓储不包含导出方法 ──────────────────────────────────

    [Fact]
    public void AnnotationRepository_ShouldNotImplementIAnnotationExporter()
    {
        typeof(AnnotationRepository).IsAssignableTo(typeof(IAnnotationExporter)).Should().BeFalse(
            "AnnotationRepository 不应包含导出职责，否则违反 ISP");
    }

    [Fact]
    public void AnnotationExporter_ShouldNotImplementIAnnotationRepository()
    {
        typeof(AnnotationExporter).IsAssignableTo(typeof(IAnnotationRepository)).Should().BeFalse(
            "AnnotationExporter 不应包含仓储职责，否则违反 ISP");
    }

    // ── 独立实例化验证 ────────────────────────────────────────────────────

    [Fact]
    public void AnnotationRepository_CanBeInstantiatedIndependently()
    {
        var act = () => new AnnotationRepository();
        act.Should().NotThrow();
    }

    [Fact]
    public void AnnotationExporter_CanBeInstantiatedIndependently()
    {
        var act = () => new AnnotationExporter();
        act.Should().NotThrow();
    }

    [Fact]
    public void AnnotationService_DefaultConstructor_CreatesWithDefaults()
    {
        var act = () => new AnnotationService();
        act.Should().NotThrow();
    }

    [Fact]
    public void AnnotationService_DIConstructor_AcceptsInjectedDependencies()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        var act = () => new AnnotationService(repo.Object, exporter.Object);
        act.Should().NotThrow();
    }

    // ── 委托验证：AnnotationService 正确委托给子实现 ─────────────────────

    [Fact]
    public async Task AnnotationService_DelegatesCreateProject_ToRepository()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();
        var expectedProject = new AnnotationProject { ProjectName = "Test" };

        repo.Setup(r => r.CreateProjectAsync("Test", It.IsAny<string>(), It.IsAny<List<AnnotationClass>>()))
            .ReturnsAsync(expectedProject);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        var result = await sut.CreateProjectAsync("Test", @"C:\temp", new List<AnnotationClass> { new() { Name = "cls" } });

        result.Should().BeSameAs(expectedProject);
        repo.Verify(r => r.CreateProjectAsync("Test", It.IsAny<string>(), It.IsAny<List<AnnotationClass>>()), Times.Once);
    }

    [Fact]
    public async Task AnnotationService_DelegatesExportYolo_ToExporter()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        exporter.Setup(e => e.ExportYoloDatasetAsync(It.IsAny<AnnotationProject>(), It.IsAny<string>(), It.IsAny<double>()))
            .Returns(Task.CompletedTask);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        var project = new AnnotationProject { ProjectName = "Test" };
        await sut.ExportYoloDatasetAsync(project, @"C:\output");

        exporter.Verify(e => e.ExportYoloDatasetAsync(project, @"C:\output", 0.8), Times.Once);
    }

    [Fact]
    public void AnnotationService_DelegatesToYoloFormat_ToExporter()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();
        var annotation = new AnnotationData { ImagePath = "test.jpg" };
        var expected = new List<string> { "0 0.5 0.5 0.2 0.3" };

        exporter.Setup(e => e.ToYoloFormat(annotation)).Returns(expected);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        var result = sut.ToYoloFormat(annotation);

        result.Should().BeSameAs(expected);
        exporter.Verify(e => e.ToYoloFormat(annotation), Times.Once);
    }

    [Fact]
    public async Task AnnotationService_DelegatesLoadProject_ToRepository()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();
        var expectedProject = new AnnotationProject { ProjectName = "Loaded" };

        repo.Setup(r => r.LoadProjectAsync("path.json")).ReturnsAsync(expectedProject);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        var result = await sut.LoadProjectAsync("path.json");

        result.Should().BeSameAs(expectedProject);
        repo.Verify(r => r.LoadProjectAsync("path.json"), Times.Once);
    }

    [Fact]
    public async Task AnnotationService_DelegatesExportCoco_ToExporter()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        exporter.Setup(e => e.ExportCocoDatasetAsync(It.IsAny<AnnotationProject>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        await sut.ExportCocoDatasetAsync(new AnnotationProject(), @"C:\output");

        exporter.Verify(e => e.ExportCocoDatasetAsync(It.IsAny<AnnotationProject>(), @"C:\output"), Times.Once);
    }

    [Fact]
    public async Task AnnotationService_DelegatesExportVoc_ToExporter()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        exporter.Setup(e => e.ExportVocDatasetAsync(It.IsAny<AnnotationProject>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        await sut.ExportVocDatasetAsync(new AnnotationProject(), @"C:\output");

        exporter.Verify(e => e.ExportVocDatasetAsync(It.IsAny<AnnotationProject>(), @"C:\output"), Times.Once);
    }

    [Fact]
    public async Task AnnotationService_DelegatesExportDot_ToExporter()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        exporter.Setup(e => e.ExportDotDatasetAsync(It.IsAny<AnnotationProject>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        await sut.ExportDotDatasetAsync(new AnnotationProject(), @"C:\output");

        exporter.Verify(e => e.ExportDotDatasetAsync(It.IsAny<AnnotationProject>(), @"C:\output"), Times.Once);
    }

    [Fact]
    public async Task AnnotationService_DelegatesExportMot_ToExporter()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        exporter.Setup(e => e.ExportMotDatasetAsync(It.IsAny<AnnotationProject>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        await sut.ExportMotDatasetAsync(new AnnotationProject(), @"C:\output");

        exporter.Verify(e => e.ExportMotDatasetAsync(It.IsAny<AnnotationProject>(), @"C:\output"), Times.Once);
    }

    [Fact]
    public void AnnotationService_DelegatesFromYoloFormat_ToExporter()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();
        var expectedBoxes = new List<BoundingBoxAnnotation> { new() { ClassIndex = 0 } };

        exporter.Setup(e => e.FromYoloFormat(It.IsAny<IEnumerable<string>>(), 640, 480, It.IsAny<List<string>>()))
            .Returns((expectedBoxes, new List<PolygonAnnotation>(), new List<PolylineAnnotation>(),
                       new List<CircleAnnotation>(), new List<OrientedBoundingBoxAnnotation>()));

        var sut = new AnnotationService(repo.Object, exporter.Object);
        var (boxes, _, _, _, _) = sut.FromYoloFormat(new[] { "0 0.5 0.5 0.2 0.3" }, 640, 480, new List<string> { "cat" });

        boxes.Should().BeSameAs(expectedBoxes);
    }

    [Fact]
    public async Task AnnotationService_DelegatesSaveProject_ToRepository()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        repo.Setup(r => r.SaveProjectAsync(It.IsAny<AnnotationProject>())).Returns(Task.CompletedTask);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        await sut.SaveProjectAsync(new AnnotationProject { ProjectName = "Test", ProjectDirectory = @"C:\temp" });

        repo.Verify(r => r.SaveProjectAsync(It.IsAny<AnnotationProject>()), Times.Once);
    }

    [Fact]
    public async Task AnnotationService_DelegatesAddImages_ToRepository()
    {
        var repo = new Mock<IAnnotationRepository>();
        var exporter = new Mock<IAnnotationExporter>();

        repo.Setup(r => r.AddImagesAsync(It.IsAny<AnnotationProject>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Task.CompletedTask);

        var sut = new AnnotationService(repo.Object, exporter.Object);
        await sut.AddImagesAsync(new AnnotationProject(), new[] { "img.jpg" });

        repo.Verify(r => r.AddImagesAsync(It.IsAny<AnnotationProject>(), It.IsAny<IEnumerable<string>>()), Times.Once);
    }
}
