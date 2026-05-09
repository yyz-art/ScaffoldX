using System.Text;
using ScaffoldX.Plugin.Training.Models;

namespace ScaffoldX.Plugin.Training.Services;

public sealed class TrainingScriptGenerator : ITrainingScriptGenerator
{
    public string GenerateYoloTrainingScript(TrainingConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("from ultralytics import YOLO");
        sb.AppendLine();

        var modelPath = GetModelLoadPath(config);
        sb.AppendLine($"model = YOLO(\"{modelPath}\")");
        sb.AppendLine();

        sb.AppendLine($"results = model.train(");
        sb.AppendLine($"    data=\"{config.DatasetPath}\",");
        sb.AppendLine($"    epochs={config.Epochs},");
        sb.AppendLine($"    batch={config.BatchSize},");
        sb.AppendLine($"    imgsz={config.ImageSize},");
        sb.AppendLine($"    lr0={config.LearningRate},");
        if (!string.IsNullOrWhiteSpace(config.ProjectName))
            sb.AppendLine($"    project=\"{config.ProjectName}\",");
        if (!string.IsNullOrWhiteSpace(config.OutputDirectory))
            sb.AppendLine($"    name=\"{config.OutputDirectory}\",");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("print(\"训练完成\")");

        return sb.ToString();
    }

    public string GenerateShellScript(TrainingConfig config, bool isWindows)
    {
        var scriptFileName = $"train_{config.ModelType.ToString().ToLower()}.py";
        if (isWindows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine($"python {scriptFileName}");
            sb.AppendLine("pause");
            return sb.ToString();
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine("#!/bin/bash");
            sb.AppendLine($"python3 {scriptFileName}");
            return sb.ToString();
        }
    }

    public string GenerateTrainingConfig(TrainingConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"model_type: {config.ModelType}");
        sb.AppendLine($"dataset_path: \"{config.DatasetPath}\"");
        sb.AppendLine($"epochs: {config.Epochs}");
        sb.AppendLine($"batch_size: {config.BatchSize}");
        sb.AppendLine($"image_size: {config.ImageSize}");
        sb.AppendLine($"learning_rate: {config.LearningRate}");
        if (!string.IsNullOrWhiteSpace(config.ProjectName))
            sb.AppendLine($"project_name: \"{config.ProjectName}\"");
        if (!string.IsNullOrWhiteSpace(config.OutputDirectory))
            sb.AppendLine($"output_directory: \"{config.OutputDirectory}\"");
        if (!string.IsNullOrWhiteSpace(config.PretrainedModelPath))
            sb.AppendLine($"pretrained_model_path: \"{config.PretrainedModelPath}\"");
        return sb.ToString();
    }

    private static string GetModelLoadPath(TrainingConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.PretrainedModelPath))
            return config.PretrainedModelPath;

        return config.ModelType switch
        {
            ModelType.YoloV5 => "yolov5s.pt",
            ModelType.YoloV8 => "yolov8n.pt",
            ModelType.YoloV11 => "yolo11n.pt",
            _ => "yolov8n.pt"
        };
    }
}
