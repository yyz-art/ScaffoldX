using Prism.Mvvm;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// YOLO 训练配置子 ViewModel，包含所有训练超参数和路径设置。
/// </summary>
public class TrainingConfigViewModel : BindableBase
{
    private string _datasetPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _pretrainedModel = "yolov8n.pt";
    private int _epochs = 100;
    private int _batchSize = 16;
    private int _imageSize = 640;
    private double _learningRate = 0.01;
    private int _numClasses = 1;
    private string _classNamesText = "object";
    private bool _useGpu = true;
    private int _workers = 8;
    private string _resumeModelPath = string.Empty;

    /// <summary>数据集路径。</summary>
    public string DatasetPath
    {
        get => _datasetPath;
        set => SetProperty(ref _datasetPath, value);
    }

    /// <summary>输出路径。</summary>
    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    /// <summary>预训练模型。</summary>
    public string PretrainedModel
    {
        get => _pretrainedModel;
        set => SetProperty(ref _pretrainedModel, value);
    }

    /// <summary>训练轮数。</summary>
    public int Epochs
    {
        get => _epochs;
        set => SetProperty(ref _epochs, value);
    }

    /// <summary>批次大小。</summary>
    public int BatchSize
    {
        get => _batchSize;
        set => SetProperty(ref _batchSize, value);
    }

    /// <summary>图像尺寸。</summary>
    public int ImageSize
    {
        get => _imageSize;
        set => SetProperty(ref _imageSize, value);
    }

    /// <summary>学习率。</summary>
    public double LearningRate
    {
        get => _learningRate;
        set => SetProperty(ref _learningRate, value);
    }

    /// <summary>类别数量。</summary>
    public int NumClasses
    {
        get => _numClasses;
        set => SetProperty(ref _numClasses, value);
    }

    /// <summary>类别名称（逗号分隔）。</summary>
    public string ClassNamesText
    {
        get => _classNamesText;
        set => SetProperty(ref _classNamesText, value);
    }

    /// <summary>是否使用 GPU。</summary>
    public bool UseGpu
    {
        get => _useGpu;
        set => SetProperty(ref _useGpu, value);
    }

    /// <summary>工作线程数。</summary>
    public int Workers
    {
        get => _workers;
        set => SetProperty(ref _workers, value);
    }

    /// <summary>恢复训练的模型路径。</summary>
    public string ResumeModelPath
    {
        get => _resumeModelPath;
        set => SetProperty(ref _resumeModelPath, value);
    }

    /// <summary>
    /// 根据当前配置构建训练配置对象。
    /// </summary>
    public YoloTrainingConfig BuildTrainingConfig()
    {
        return new YoloTrainingConfig
        {
            DatasetPath = DatasetPath,
            OutputPath = OutputPath,
            PretrainedModel = PretrainedModel,
            Epochs = Epochs,
            BatchSize = BatchSize,
            ImageSize = ImageSize,
            LearningRate = LearningRate,
            NumClasses = NumClasses,
            ClassNames = ClassNamesText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList(),
            UseGpu = UseGpu,
            Workers = Workers
        };
    }
}
