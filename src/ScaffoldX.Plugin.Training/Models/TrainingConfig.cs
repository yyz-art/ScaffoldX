namespace ScaffoldX.Plugin.Training.Models;

public enum ModelType
{
    YoloV5,
    YoloV8,
    YoloV11
}

public sealed record TrainingConfig
{
    public ModelType ModelType { get; set; } = ModelType.YoloV8;
    public string DatasetPath { get; set; } = string.Empty;
    public int Epochs { get; set; } = 100;
    public int BatchSize { get; set; } = 16;
    public int ImageSize { get; set; } = 640;
    public double LearningRate { get; set; } = 0.01;
    public string PretrainedModelPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    private static readonly int[] AllowedImageSizes = [320, 416, 512, 640, 1280];

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DatasetPath))
            errors.Add("DatasetPath 不能为空");

        if (Epochs <= 0)
            errors.Add("Epochs 必须大于 0");

        if (BatchSize <= 0)
            errors.Add("BatchSize 必须大于 0");

        if (!AllowedImageSizes.Contains(ImageSize))
            errors.Add("ImageSize 必须为 320/416/512/640/1280 之一");

        if (LearningRate <= 0 || LearningRate >= 1)
            errors.Add("LearningRate 必须在 (0, 1) 范围内");

        return new ValidationResult(errors.Count == 0, errors);
    }
}

public sealed class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    public ValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }
}
