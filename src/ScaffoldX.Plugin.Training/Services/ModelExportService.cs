using System.Text;

namespace ScaffoldX.Plugin.Training.Services;

public sealed class ModelExportService : IModelExportService
{
    public Task<string> ExportOnnxAsync(string modelPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("from ultralytics import YOLO");
        sb.AppendLine();
        sb.AppendLine($"model = YOLO(\"{modelPath}\")");
        sb.AppendLine();
        sb.AppendLine("model.export(format=\"onnx\")");
        sb.AppendLine("print(\"ONNX 导出完成\")");
        return Task.FromResult(sb.ToString());
    }

    public Task<string> ExportTorchScriptAsync(string modelPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("from ultralytics import YOLO");
        sb.AppendLine();
        sb.AppendLine($"model = YOLO(\"{modelPath}\")");
        sb.AppendLine();
        sb.AppendLine("model.export(format=\"torchscript\")");
        sb.AppendLine("print(\"TorchScript 导出完成\")");
        return Task.FromResult(sb.ToString());
    }

    public Task<string> ExportTensorRtAsync(string modelPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("from ultralytics import YOLO");
        sb.AppendLine();
        sb.AppendLine($"model = YOLO(\"{modelPath}\")");
        sb.AppendLine();
        sb.AppendLine("model.export(format=\"engine\", half=True)");
        sb.AppendLine("print(\"TensorRT 导出完成\")");
        return Task.FromResult(sb.ToString());
    }
}
