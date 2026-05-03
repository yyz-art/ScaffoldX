using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using ScaffoldX.App.Models;
using Serilog;

namespace ScaffoldX.App.Services;

/// <summary>
/// YOLO 训练服务实现，通过 Python 子进程调用 Ultralytics 进行训练。
/// </summary>
public class YoloTrainingService : IYoloTrainingService
{
    private readonly ILogger _logger = Log.ForContext<YoloTrainingService>();

    // 预训练模型列表
    private static readonly List<PretrainedModel> AvailableModels = new()
    {
        new() { Name = "yolov8n.pt", Description = "YOLOv8 Nano - 最快，适合边缘设备", SizeMB = 6.2, Map50_95 = 37.3, GpuSpeed = 0.99 },
        new() { Name = "yolov8s.pt", Description = "YOLOv8 Small - 平衡速度和精度", SizeMB = 21.5, Map50_95 = 44.9, GpuSpeed = 1.20 },
        new() { Name = "yolov8m.pt", Description = "YOLOv8 Medium - 中等精度", SizeMB = 49.7, Map50_95 = 50.2, GpuSpeed = 1.83 },
        new() { Name = "yolov8l.pt", Description = "YOLOv8 Large - 高精度", SizeMB = 83.7, Map50_95 = 52.9, GpuSpeed = 2.39 },
        new() { Name = "yolov8x.pt", Description = "YOLOv8 Extra Large - 最高精度", SizeMB = 130.5, Map50_95 = 53.9, GpuSpeed = 3.53 },
    };

    /// <inheritdoc/>
    public async Task<EnvironmentCheckResult> CheckEnvironmentAsync()
    {
        var result = new EnvironmentCheckResult();

        try
        {
            // 检查 Python
            var pythonOutput = await RunProcessAsync("python", "--version");
            if (pythonOutput.Success)
            {
                result.PythonInstalled = true;
                result.PythonVersion = pythonOutput.Output.Trim();
            }
        }
        catch
        {
            result.PythonInstalled = false;
        }

        if (!result.PythonInstalled)
        {
            result.ErrorMessage = "Python 未安装。请安装 Python 3.8 或更高版本。";
            return result;
        }

        try
        {
            // 检查 Ultralytics
            var ultralyticsOutput = await RunProcessAsync("python", "-c \"import ultralytics; print(ultralytics.__version__)\"");
            if (ultralyticsOutput.Success)
            {
                result.UltralyticsInstalled = true;
                result.UltralyticsVersion = ultralyticsOutput.Output.Trim();
            }
        }
        catch
        {
            result.UltralyticsInstalled = false;
        }

        try
        {
            // 检查 PyTorch
            var pytorchOutput = await RunProcessAsync("python", "-c \"import torch; print(torch.__version__)\"");
            if (pytorchOutput.Success)
            {
                result.PyTorchInstalled = true;
            }

            // 检查 CUDA
            var cudaOutput = await RunProcessAsync("python", "-c \"import torch; print(torch.cuda.is_available()); print(torch.version.cuda)\"");
            if (cudaOutput.Success)
            {
                var lines = cudaOutput.Output.Split('\n');
                if (lines.Length >= 2)
                {
                    result.CudaAvailable = lines[0].Trim().ToLower() == "true";
                    result.CudaVersion = lines[1].Trim();
                }
            }
        }
        catch
        {
            result.PyTorchInstalled = false;
        }

        if (!result.IsReady)
        {
            result.ErrorMessage = "训练环境未就绪。请运行 'Install Dependencies' 安装所需依赖。";
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> InstallDependenciesAsync(IProgress<string> progress)
    {
        progress.Report("正在安装 ultralytics...");

        var result = await RunProcessAsync("pip", "install ultralytics", progress);

        if (!result.Success)
        {
            _logger.Error("安装 ultralytics 失败: {Error}", result.Error);
            return false;
        }

        progress.Report("ultralytics 安装完成");
        return true;
    }

    /// <inheritdoc/>
    public async Task<TrainingResult> TrainAsync(
        YoloTrainingConfig config,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 创建训练脚本
            var scriptPath = Path.Combine(config.OutputPath, "train_script.py");
            var scriptContent = GenerateTrainingScript(config);
            await File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);

            // 创建输出目录
            Directory.CreateDirectory(config.OutputPath);

            progress.Report(new TrainingProgress
            {
                StatusMessage = "正在启动训练...",
                TotalEpochs = config.Epochs
            });

            // 运行训练脚本
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = scriptPath,
                    WorkingDirectory = config.OutputPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // 读取输出
            var outputTask = ReadProcessOutputAsync(process, progress, config.Epochs, cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            stopwatch.Stop();

            if (process.ExitCode != 0)
            {
                _logger.Error("训练失败: {Error}", error);
                return new TrainingResult
                {
                    Success = false,
                    ErrorMessage = error,
                    TotalTime = stopwatch.Elapsed
                };
            }

            // 解析最终结果
            var result = ParseTrainingOutput(output);
            result.TotalTime = stopwatch.Elapsed;
            result.ModelPath = Path.Combine(config.OutputPath, "weights", "best.pt");

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new TrainingResult
            {
                Success = false,
                ErrorMessage = "训练已取消",
                TotalTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "训练过程中发生错误");
            return new TrainingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                TotalTime = stopwatch.Elapsed
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ModelValidationResult> ValidateAsync(string modelPath, string datasetPath)
    {
        var script = $$$"""
            from ultralytics import YOLO
            import json

            model = YOLO('{{{modelPath.Replace("\\", "\\\\")}}}')
            results = model.val(data='{{{Path.Combine(datasetPath, "data.yaml").Replace("\\", "\\\\")}}}')

            output = {{
                "map50": results.box.map50,
                "map50_95": results.box.map,
                "precision": results.box.mp,
                "recall": results.box.mr,
                "inference_speed": results.speed['inference']
            }}

            print(json.dumps(output))
            """;

        var scriptPath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(scriptPath, script);

        try
        {
            var result = await RunProcessAsync("python", scriptPath);
            if (result.Success)
            {
                return JsonSerializer.Deserialize<ModelValidationResult>(result.Output.Trim()) ?? new ModelValidationResult();
            }
        }
        finally
        {
            File.Delete(scriptPath);
        }

        return new ModelValidationResult();
    }

    /// <inheritdoc/>
    public async Task<bool> ExportToOnnxAsync(string modelPath, string outputPath, int imageSize = 640)
    {
        var script = $"""
            from ultralytics import YOLO

            model = YOLO('{modelPath.Replace("\\", "\\\\")}')
            model.export(format='onnx', imgsz={imageSize}, simplify=True)
            """;

        var scriptPath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(scriptPath, script);

        try
        {
            var result = await RunProcessAsync("python", scriptPath);
            return result.Success;
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<PretrainedModel> GetAvailableModels()
    {
        // 检查哪些模型已下载
        var modelsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "ultralytics");

        foreach (var model in AvailableModels)
        {
            var modelPath = Path.Combine(modelsDir, model.Name);
            model.IsDownloaded = File.Exists(modelPath);
        }

        return AvailableModels.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<bool> DownloadModelAsync(string modelName, IProgress<string> progress)
    {
        progress.Report($"正在下载 {modelName}...");

        var script = $"""
            from ultralytics import YOLO

            model = YOLO('{modelName}')
            print('Download complete')
            """;

        var scriptPath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(scriptPath, script);

        try
        {
            var result = await RunProcessAsync("python", scriptPath, progress);
            return result.Success;
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    /// <inheritdoc/>
    public async Task<TrainingResult> ResumeTrainAsync(
        YoloTrainingConfig config,
        string resumeFromPath,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var scriptPath = Path.Combine(config.OutputPath, "resume_train_script.py");
            var scriptContent = GenerateTrainingScript(config, resumeFromPath);
            await File.WriteAllTextAsync(scriptPath, scriptContent, cancellationToken);

            Directory.CreateDirectory(config.OutputPath);

            progress.Report(new TrainingProgress
            {
                StatusMessage = "正在从检查点恢复训练...",
                TotalEpochs = config.Epochs
            });

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = scriptPath,
                    WorkingDirectory = config.OutputPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = ReadProcessOutputAsync(process, progress, config.Epochs, cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            stopwatch.Stop();

            if (process.ExitCode != 0)
            {
                _logger.Error("恢复训练失败: {Error}", error);
                return new TrainingResult { Success = false, ErrorMessage = error, TotalTime = stopwatch.Elapsed };
            }

            var result = ParseTrainingOutput(output);
            result.TotalTime = stopwatch.Elapsed;
            result.ModelPath = Path.Combine(config.OutputPath, "weights", "best.pt");
            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new TrainingResult { Success = false, ErrorMessage = "训练已取消", TotalTime = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "恢复训练异常");
            return new TrainingResult { Success = false, ErrorMessage = ex.Message, TotalTime = stopwatch.Elapsed };
        }
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

    private string GenerateTrainingScript(YoloTrainingConfig config, string? resumeFromPath = null)
    {
        var classNamesJson = JsonSerializer.Serialize(config.ClassNames);
        var device = config.UseGpu ? "0" : "cpu";
        var modelPath = resumeFromPath ?? config.PretrainedModel;
        var isResume = resumeFromPath != null;

        return $$$"""
            from ultralytics import YOLO
            import json

            # 加载模型{{{{(isResume ? "（从检查点恢复）" : "（预训练）")}}}}
            model = YOLO('{{{modelPath.Replace("\\", "\\\\")}}}')

            # 训练配置
            results = model.train(
                data='{{{Path.Combine(config.DatasetPath, "data.yaml").Replace("\\", "\\\\")}}}',
                epochs={{{config.Epochs}}},
                batch={{{config.BatchSize}}},
                imgsz={{{config.ImageSize}}},
                lr0={{{config.LearningRate}}},
                workers={{{config.Workers}}},
                device='{{{device}}}',
                project='{{{config.OutputPath.Replace("\\", "\\\\")}}}',
                name='train',
                exist_ok=True,
                pretrained=True,
                optimizer='auto',
                verbose=True,
                seed=0,
                deterministic=True,
                single_cls=False,
                rect=False,
                cos_lr=False,
                close_mosaic=10,
                resume={{{(isResume ? "True" : "False")}}},
                amp=True,
                overlap_mask=True,
                mask_ratio=4,
                dropout=0.0,
                val=True,
                save=True,
                save_json=False,
                save_hybrid=False,
                conf=None,
                iou=0.7,
                max_det=300,
                half=False,
                dnn=False,
                plots=True,
                source=None,
                vid_stride=1,
                stream_buffer=False,
                visualize=False,
                augment=False,
                agnostic_nms=False,
                classes=None,
                retina_masks=False,
                embed=None,
                show=False,
                save_frames=False,
                save_txt=False,
                save_conf=False,
                save_crop=False,
                show_labels=True,
                show_conf=True,
                show_boxes=True,
                line_width=None,
                format='torchscript',
                keras=False,
                optimize=False,
                int8=False,
                dynamic=False,
                simplify=False,
                opset=None,
                workspace=4,
                nms=False,
                lr0={{{config.LearningRate}}},
                lrf=0.01,
                momentum=0.937,
                weight_decay=0.0005,
                warmup_epochs=3.0,
                warmup_momentum=0.8,
                warmup_bias_lr=0.1,
                box=7.5,
                cls=0.5,
                dfl=1.5,
                pose=12.0,
                kobj=1.0,
                label_smoothing=0.0,
                nbs=64,
                hsv_h=0.015,
                hsv_s=0.7,
                hsv_v=0.4,
                degrees=0.0,
                translate=0.1,
                scale=0.5,
                shear=0.0,
                perspective=0.0,
                flipud=0.0,
                fliplr=0.5,
                bgr=0.0,
                mosaic=1.0,
                mixup=0.0,
                copy_paste=0.0,
                auto_augment='randaugment',
                erasing=0.4,
                crop_fraction=1.0,
                cfg=None,
                tracker='botsort.yaml',
            )

            # 输出训练结果
            output = {{
                "success": True,
                "map50": float(results.results_dict.get('metrics/mAP50(B)', 0)),
                "map50_95": float(results.results_dict.get('metrics/mAP50-95(B)', 0)),
            }}

            print(json.dumps(output))
            """;
    }

    private async Task<string> ReadProcessOutputAsync(
        Process process,
        IProgress<TrainingProgress> progress,
        int totalEpochs,
        CancellationToken cancellationToken)
    {
        var output = new System.Text.StringBuilder();

        while (!process.StandardOutput.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line == null) break;

            output.AppendLine(line);

            // 解析训练进度
            var epochMatch = Regex.Match(line, @"Epoch\s+(\d+)/(\d+)");
            if (epochMatch.Success)
            {
                var currentEpoch = int.Parse(epochMatch.Groups[1].Value);
                progress.Report(new TrainingProgress
                {
                    CurrentEpoch = currentEpoch,
                    TotalEpochs = totalEpochs,
                    StatusMessage = $"训练中... Epoch {currentEpoch}/{totalEpochs}"
                });
            }

            // 解析损失值
            var lossMatch = Regex.Match(line, @"loss:\s+([\d.]+)");
            if (lossMatch.Success)
            {
                var loss = double.Parse(lossMatch.Groups[1].Value);
                progress.Report(new TrainingProgress
                {
                    Loss = loss,
                    StatusMessage = $"训练中... Loss: {loss:F4}"
                });
            }
        }

        return output.ToString();
    }

    private TrainingResult ParseTrainingOutput(string output)
    {
        var result = new TrainingResult { Success = true };

        try
        {
            // 尝试解析 JSON 输出
            var jsonMatch = Regex.Match(output, @"\{[^}]+\}");
            if (jsonMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
                if (json.TryGetProperty("map50", out var map50))
                    result.FinalMap50 = map50.GetDouble();
                if (json.TryGetProperty("map50_95", out var map50_95))
                    result.FinalMap50_95 = map50_95.GetDouble();
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "解析训练输出失败");
        }

        return result;
    }

    private async Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        IProgress<string>? progress = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                progress?.Report(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new ProcessResult
        {
            Success = process.ExitCode == 0,
            Output = output.ToString(),
            Error = error.ToString()
        };
    }

    private class ProcessResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
