namespace ScaffoldX.Plugin.Training.Services;

public interface IModelExportService
{
    Task<string> ExportOnnxAsync(string modelPath);
    Task<string> ExportTorchScriptAsync(string modelPath);
    Task<string> ExportTensorRtAsync(string modelPath);
}
