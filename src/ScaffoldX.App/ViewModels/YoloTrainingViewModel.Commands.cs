using System.IO;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.ViewModels;

/// <summary>
/// YoloTrainingViewModel 的命令实现部分（partial class）。
/// </summary>
public partial class YoloTrainingViewModel
{
    private async Task ExecuteCheckEnvironmentAsync()
    {
        EnvironmentStatus = "检查中...";
        StatusMessage = "正在检查训练环境...";

        try
        {
            _environmentCheck = await _trainingService.CheckEnvironmentAsync();

            if (_environmentCheck.IsReady)
            {
                EnvironmentStatus = $"就绪 - Python {_environmentCheck.PythonVersion}, " +
                                   $"Ultralytics {_environmentCheck.UltralyticsVersion}";
                if (_environmentCheck.CudaAvailable)
                {
                    EnvironmentStatus += $", CUDA {_environmentCheck.CudaVersion}";
                }
                StatusMessage = "训练环境就绪";
            }
            else
            {
                EnvironmentStatus = $"未就绪 - {_environmentCheck.ErrorMessage}";
                StatusMessage = "训练环境未就绪，请安装依赖";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "检查环境失败");
            EnvironmentStatus = "检查失败";
            StatusMessage = $"检查环境失败: {ex.Message}";
        }
    }

    private async Task ExecuteInstallDependenciesAsync()
    {
        StatusMessage = "正在安装依赖...";
        IsTraining = true;

        try
        {
            var progress = new Progress<string>(message => StatusMessage = message);
            var success = await _trainingService.InstallDependenciesAsync(progress);

            if (success)
            {
                StatusMessage = "依赖安装完成";
                await ExecuteCheckEnvironmentAsync();
            }
            else
            {
                StatusMessage = "依赖安装失败";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "安装依赖失败");
            StatusMessage = $"安装依赖失败: {ex.Message}";
            IsError = true;
        }
        finally
        {
            IsTraining = false;
        }
    }

    private void ExecuteBrowseDataset()
    {
        var dialog = new VistaFolderBrowserDialog { Description = "选择 YOLO 数据集目录" };

        if (dialog.ShowDialog() == true)
        {
            Config.DatasetPath = dialog.SelectedPath;

            var dataYamlPath = Path.Combine(dialog.SelectedPath, "data.yaml");
            if (File.Exists(dataYamlPath))
            {
                try
                {
                    var content = File.ReadAllText(dataYamlPath);
                    var ncMatch = System.Text.RegularExpressions.Regex.Match(content, @"nc:\s*(\d+)");
                    if (ncMatch.Success)
                    {
                        Config.NumClasses = int.Parse(ncMatch.Groups[1].Value);
                    }

                    var namesMatch = System.Text.RegularExpressions.Regex.Match(content, @"names:\s*\[(.+?)\]");
                    if (namesMatch.Success)
                    {
                        Config.ClassNamesText = namesMatch.Groups[1].Value
                            .Replace("'", "")
                            .Replace("\"", "")
                            .Trim();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "解析 data.yaml 失败");
                }
            }
        }
    }

    private void ExecuteBrowseOutput()
    {
        var dialog = new VistaFolderBrowserDialog { Description = "选择模型输出目录" };

        if (dialog.ShowDialog() == true)
        {
            Config.OutputPath = dialog.SelectedPath;
        }
    }

    private async Task ExecuteStartTrainingAsync()
    {
        if (string.IsNullOrWhiteSpace(Config.DatasetPath))
        {
            StatusMessage = "请选择数据集路径";
            return;
        }

        if (string.IsNullOrWhiteSpace(Config.OutputPath))
        {
            Config.OutputPath = Path.Combine(Config.DatasetPath, "runs");
        }

        IsTraining = true;
        IsCompleted = false;
        IsError = false;
        CurrentEpoch = 0;
        Loss = 0;
        Map50 = 0;
        Map50_95 = 0;

        _cancellationTokenSource = new CancellationTokenSource();
        var trainingConfig = Config.BuildTrainingConfig();

        var progress = new Progress<TrainingProgress>(p =>
        {
            CurrentEpoch = p.CurrentEpoch;
            Loss = p.Loss;
            Map50 = p.Map50;
            Map50_95 = p.Map50_95;
            StatusMessage = p.StatusMessage;
        });

        try
        {
            StatusMessage = "正在启动训练...";
            var result = await _trainingService.TrainAsync(trainingConfig, progress, _cancellationTokenSource.Token);

            if (result.Success)
            {
                IsCompleted = true;
                Map50 = result.FinalMap50;
                Map50_95 = result.FinalMap50_95;
                ElapsedText = $"耗时 {result.TotalTime.TotalMinutes:F1} 分钟";
                StatusMessage = $"训练完成！mAP@0.5: {Map50:F3}, mAP@0.5:0.95: {Map50_95:F3}";
            }
            else
            {
                IsError = true;
                StatusMessage = $"训练失败: {result.ErrorMessage}";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "训练已取消";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "训练过程中发生错误");
            IsError = true;
            StatusMessage = $"训练失败: {ex.Message}";
        }
        finally
        {
            IsTraining = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void ExecuteCancelTraining()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "正在取消训练...";
    }

    private async Task ExecuteExportOnnxAsync()
    {
        var modelPath = Path.Combine(Config.OutputPath, "train", "weights", "best.pt");
        if (!File.Exists(modelPath))
        {
            StatusMessage = "找不到训练好的模型文件";
            return;
        }

        var outputPath = Path.Combine(Config.OutputPath, "export");
        Directory.CreateDirectory(outputPath);

        StatusMessage = "正在导出 ONNX 模型...";

        try
        {
            var success = await _trainingService.ExportToOnnxAsync(modelPath, outputPath, Config.ImageSize);

            if (success)
            {
                StatusMessage = $"ONNX 模型已导出到: {outputPath}";
            }
            else
            {
                StatusMessage = "ONNX 导出失败";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "导出 ONNX 失败");
            StatusMessage = $"导出 ONNX 失败: {ex.Message}";
            IsError = true;
        }
    }

    private void ExecuteBrowseResumeModel()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PyTorch 模型|*.pt|所有文件|*.*",
            Title = "选择恢复训练的检查点文件"
        };

        if (dialog.ShowDialog() == true)
        {
            Config.ResumeModelPath = dialog.FileName;
        }
    }

    private async Task ExecuteResumeTrainingAsync()
    {
        if (string.IsNullOrWhiteSpace(Config.ResumeModelPath) || !File.Exists(Config.ResumeModelPath))
        {
            StatusMessage = "请选择有效的检查点文件";
            return;
        }

        var trainingConfig = Config.BuildTrainingConfig();
        _cancellationTokenSource = new CancellationTokenSource();

        IsTraining = true;
        IsCompleted = false;
        IsError = false;
        StatusMessage = "正在从检查点恢复训练...";

        try
        {
            var progress = new Progress<TrainingProgress>(p =>
            {
                CurrentEpoch = p.CurrentEpoch;
                Loss = p.Loss;
                Map50 = p.Map50;
                Map50_95 = p.Map50_95;
                ElapsedText = p.Elapsed.ToString(@"hh\:mm\:ss");
                StatusMessage = p.StatusMessage;
            });

            var result = await _trainingService.ResumeTrainAsync(
                trainingConfig, Config.ResumeModelPath, progress, _cancellationTokenSource.Token);

            IsTraining = false;

            if (result.Success)
            {
                IsCompleted = true;
                StatusMessage = $"恢复训练完成！mAP@0.5: {result.FinalMap50:F3}, 耗时: {result.TotalTime:hh\\:mm\\:ss}";
                _logger.Information("恢复训练完成: {ModelPath}", result.ModelPath);
            }
            else
            {
                IsError = true;
                StatusMessage = $"恢复训练失败: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            IsTraining = false;
            IsError = true;
            _logger.Error(ex, "恢复训练异常");
            StatusMessage = $"恢复训练异常: {ex.Message}";
        }
    }

    private async Task ExecuteValidateModelAsync()
    {
        var modelPath = Path.Combine(Config.OutputPath, "train", "weights", "best.pt");
        if (!File.Exists(modelPath))
        {
            StatusMessage = "找不到训练好的模型文件";
            return;
        }

        ValidationStatus = "验证中...";
        StatusMessage = "正在验证模型性能...";

        try
        {
            var result = await _trainingService.ValidateAsync(modelPath, Config.DatasetPath);
            ValidationResult = result;
            ValidationStatus = $"mAP@0.5: {result.Map50:F3}, mAP@0.5:0.95: {result.Map50_95:F3}, " +
                              $"Precision: {result.Precision:F3}, Recall: {result.Recall:F3}, " +
                              $"推理速度: {result.InferenceSpeed:F1}ms/张";
            StatusMessage = "模型验证完成";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "模型验证失败");
            ValidationStatus = $"验证失败: {ex.Message}";
            StatusMessage = $"模型验证失败: {ex.Message}";
        }
    }
}
