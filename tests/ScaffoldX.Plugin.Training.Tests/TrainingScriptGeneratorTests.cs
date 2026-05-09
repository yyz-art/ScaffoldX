using ScaffoldX.Plugin.Training.Models;
using ScaffoldX.Plugin.Training.Services;
using Xunit;

namespace ScaffoldX.Plugin.Training.Tests;

public class TrainingScriptGeneratorTests
{
    private static TrainingConfig CreateValidConfig() => new()
    {
        ModelType = ModelType.YoloV8,
        DatasetPath = @"C:\data\coco128",
        Epochs = 50,
        BatchSize = 8,
        ImageSize = 640,
        LearningRate = 0.001,
        ProjectName = "TestProject",
        OutputDirectory = @"C:\output"
    };

    [Fact]
    public void GenerateYoloTrainingScript_包含from_ultralytics导入()
    {
        var gen = new TrainingScriptGenerator();
        var script = gen.GenerateYoloTrainingScript(CreateValidConfig());
        Assert.Contains("from ultralytics import YOLO", script);
    }

    [Fact]
    public void GenerateYoloTrainingScript_包含YOLO模型加载()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var script = gen.GenerateYoloTrainingScript(config);
        Assert.Contains("YOLO(", script);
    }

    [Fact]
    public void GenerateYoloTrainingScript_YoloV5使用yolov5模型()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig() with { ModelType = ModelType.YoloV5 };
        var script = gen.GenerateYoloTrainingScript(config);
        Assert.Contains("yolov5", script);
    }

    [Fact]
    public void GenerateYoloTrainingScript_YoloV11使用yolo11模型()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig() with { ModelType = ModelType.YoloV11 };
        var script = gen.GenerateYoloTrainingScript(config);
        Assert.Contains("yolo11", script);
    }

    [Fact]
    public void GenerateYoloTrainingScript_包含train调用()
    {
        var gen = new TrainingScriptGenerator();
        var script = gen.GenerateYoloTrainingScript(CreateValidConfig());
        Assert.Contains(".train(", script);
    }

    [Fact]
    public void GenerateYoloTrainingScript_包含epochs参数()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var script = gen.GenerateYoloTrainingScript(config);
        Assert.Contains($"epochs={config.Epochs}", script);
    }

    [Fact]
    public void GenerateYoloTrainingScript_包含batch参数()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var script = gen.GenerateYoloTrainingScript(config);
        Assert.Contains($"batch={config.BatchSize}", script);
    }

    [Fact]
    public void GenerateYoloTrainingScript_包含imgsz参数()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var script = gen.GenerateYoloTrainingScript(config);
        Assert.Contains($"imgsz={config.ImageSize}", script);
    }

    [Fact]
    public void GenerateShellScript_Windows生成bat内容()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var script = gen.GenerateShellScript(config, isWindows: true);
        Assert.Contains("@echo off", script);
        Assert.Contains("python", script);
    }

    [Fact]
    public void GenerateShellScript_Linux生成sh内容()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var script = gen.GenerateShellScript(config, isWindows: false);
        Assert.Contains("#!/bin/bash", script);
        Assert.Contains("python", script);
    }

    [Fact]
    public void GenerateTrainingConfig_生成YAML格式()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var yaml = gen.GenerateTrainingConfig(config);
        Assert.Contains("model_type:", yaml);
        Assert.Contains("dataset_path:", yaml);
        Assert.Contains("epochs:", yaml);
        Assert.Contains("batch_size:", yaml);
        Assert.Contains("image_size:", yaml);
        Assert.Contains("learning_rate:", yaml);
    }

    [Fact]
    public void GenerateTrainingConfig_包含项目名()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var yaml = gen.GenerateTrainingConfig(config);
        Assert.Contains("project_name:", yaml);
        Assert.Contains(config.ProjectName, yaml);
    }

    [Fact]
    public void GenerateYoloTrainingScript_有预训练模型路径时使用该路径()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig() with { PretrainedModelPath = @"C:\models\best.pt" };
        var script = gen.GenerateYoloTrainingScript(config);
        Assert.Contains(config.PretrainedModelPath, script);
    }

    [Fact]
    public void GenerateShellScript_Windows包含训练脚本调用()
    {
        var gen = new TrainingScriptGenerator();
        var config = CreateValidConfig();
        var script = gen.GenerateShellScript(config, isWindows: true);
        Assert.Contains("train_", script);
    }
}
