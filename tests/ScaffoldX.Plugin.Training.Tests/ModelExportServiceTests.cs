using ScaffoldX.Plugin.Training.Services;
using Xunit;

namespace ScaffoldX.Plugin.Training.Tests;

public class ModelExportServiceTests
{
    private const string ModelPath = @"C:\models\best.pt";

    [Fact]
    public async Task ExportOnnxAsync_生成包含onnx导出的脚本()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportOnnxAsync(ModelPath);
        Assert.Contains("from ultralytics import YOLO", script);
        Assert.Contains("onnx", script);
    }

    [Fact]
    public async Task ExportOnnxAsync_包含模型路径()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportOnnxAsync(ModelPath);
        Assert.Contains(ModelPath, script);
    }

    [Fact]
    public async Task ExportTorchScriptAsync_生成包含torchscript导出的脚本()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportTorchScriptAsync(ModelPath);
        Assert.Contains("from ultralytics import YOLO", script);
        Assert.Contains("torchscript", script);
    }

    [Fact]
    public async Task ExportTorchScriptAsync_包含模型路径()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportTorchScriptAsync(ModelPath);
        Assert.Contains(ModelPath, script);
    }

    [Fact]
    public async Task ExportTensorRtAsync_生成包含engine导出的脚本()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportTensorRtAsync(ModelPath);
        Assert.Contains("from ultralytics import YOLO", script);
        Assert.Contains("engine", script);
    }

    [Fact]
    public async Task ExportTensorRtAsync_包含模型路径()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportTensorRtAsync(ModelPath);
        Assert.Contains(ModelPath, script);
    }

    [Fact]
    public async Task ExportOnnxAsync_包含export调用()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportOnnxAsync(ModelPath);
        Assert.Contains(".export(", script);
    }

    [Fact]
    public async Task ExportTensorRtAsync_包含half参数()
    {
        var svc = new ModelExportService();
        var script = await svc.ExportTensorRtAsync(ModelPath);
        Assert.Contains("half=", script);
    }
}
