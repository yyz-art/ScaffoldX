using System.IO;
using System.Text.Json;
using ScaffoldX.App.Models;

namespace ScaffoldX.App.Services;

/// <summary>
/// YOLO Python 训练脚本生成器，从 YoloTrainingService 中提取。
/// </summary>
internal static class YoloScriptGenerator
{
    /// <summary>
    /// 生成 YOLO 训练 Python 脚本。
    /// </summary>
    internal static string GenerateTrainingScript(YoloTrainingConfig config, string? resumeFromPath = null)
    {
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

    /// <summary>
    /// 生成模型验证 Python 脚本。
    /// </summary>
    internal static string GenerateValidationScript(string modelPath, string datasetPath)
    {
        return $$$"""
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
    }

    /// <summary>
    /// 生成 ONNX 导出 Python 脚本。
    /// </summary>
    internal static string GenerateExportOnnxScript(string modelPath, int imageSize)
    {
        return $"""
            from ultralytics import YOLO

            model = YOLO('{modelPath.Replace("\\", "\\\\")}')
            model.export(format='onnx', imgsz={imageSize}, simplify=True)
            """;
    }

    /// <summary>
    /// 生成模型下载 Python 脚本。
    /// </summary>
    internal static string GenerateDownloadScript(string modelName)
    {
        return $"""
            from ultralytics import YOLO

            model = YOLO('{modelName}')
            print('Download complete')
            """;
    }
}
