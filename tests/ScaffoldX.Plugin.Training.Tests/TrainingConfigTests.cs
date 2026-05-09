using ScaffoldX.Plugin.Training.Models;
using Xunit;

namespace ScaffoldX.Plugin.Training.Tests;

public class TrainingConfigTests
{
    [Fact]
    public void 默认值_Epochs为100()
    {
        var config = new TrainingConfig();
        Assert.Equal(100, config.Epochs);
    }

    [Fact]
    public void 默认值_BatchSize为16()
    {
        var config = new TrainingConfig();
        Assert.Equal(16, config.BatchSize);
    }

    [Fact]
    public void 默认值_ImageSize为640()
    {
        var config = new TrainingConfig();
        Assert.Equal(640, config.ImageSize);
    }

    [Fact]
    public void 默认值_LearningRate为001()
    {
        var config = new TrainingConfig();
        Assert.Equal(0.01, config.LearningRate);
    }

    [Fact]
    public void 默认值_ModelType为YoloV8()
    {
        var config = new TrainingConfig();
        Assert.Equal(ModelType.YoloV8, config.ModelType);
    }

    [Fact]
    public void Validate_Epochs为零_返回错误()
    {
        var config = new TrainingConfig { Epochs = 0 };
        var result = config.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Epochs"));
    }

    [Fact]
    public void Validate_BatchSize为负数_返回错误()
    {
        var config = new TrainingConfig { BatchSize = -1 };
        var result = config.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("BatchSize"));
    }

    [Fact]
    public void Validate_ImageSize不在允许范围_返回错误()
    {
        var config = new TrainingConfig { ImageSize = 999 };
        var result = config.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ImageSize"));
    }

    [Fact]
    public void Validate_LearningRate为零_返回错误()
    {
        var config = new TrainingConfig { LearningRate = 0 };
        var result = config.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("LearningRate"));
    }

    [Fact]
    public void Validate_LearningRate大于1_返回错误()
    {
        var config = new TrainingConfig { LearningRate = 1.5 };
        var result = config.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("LearningRate"));
    }

    [Fact]
    public void Validate_DatasetPath为空_返回错误()
    {
        var config = new TrainingConfig { DatasetPath = "" };
        var result = config.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("DatasetPath"));
    }

    [Fact]
    public void Validate_全部合法_返回有效()
    {
        var config = new TrainingConfig
        {
            ModelType = ModelType.YoloV11,
            DatasetPath = @"C:\data\coco128",
            Epochs = 50,
            BatchSize = 8,
            ImageSize = 640,
            LearningRate = 0.001,
            ProjectName = "MyProject",
            OutputDirectory = @"C:\output"
        };
        var result = config.Validate();
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ImageSize允许值320()
    {
        var config = new TrainingConfig { ImageSize = 320, DatasetPath = "x" };
        var result = config.Validate();
        Assert.DoesNotContain(result.Errors, e => e.Contains("ImageSize"));
    }

    [Fact]
    public void Validate_ImageSize允许值1280()
    {
        var config = new TrainingConfig { ImageSize = 1280, DatasetPath = "x" };
        var result = config.Validate();
        Assert.DoesNotContain(result.Errors, e => e.Contains("ImageSize"));
    }
}
