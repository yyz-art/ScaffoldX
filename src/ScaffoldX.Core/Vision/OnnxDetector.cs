using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// ONNX YOLO 目标检测器，支持 YOLOv5/v8 格式的 ONNX 模型。
/// </summary>
public class OnnxDetector : InferenceEngineBase
{
    private object? _session; // InferenceSession
    private readonly List<string> _classNames = new();
    private float _confidenceThreshold = 0.5f;
    private float _nmsThreshold = 0.45f;

    /// <summary>置信度阈值。</summary>
    public float ConfidenceThreshold
    {
        get => _confidenceThreshold;
        set => _confidenceThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>NMS 阈值。</summary>
    public float NmsThreshold
    {
        get => _nmsThreshold;
        set => _nmsThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>类别名称列表。</summary>
    public IReadOnlyList<string> ClassNames => _classNames.AsReadOnly();

    /// <summary>
    /// 设置类别名称。
    /// </summary>
    /// <param name="classNames">类别名称列表。</param>
    public void SetClassNames(IEnumerable<string> classNames)
    {
        _classNames.Clear();
        _classNames.AddRange(classNames);
    }

    /// <summary>
    /// 从文件加载类别名称。
    /// </summary>
    /// <param name="classesFilePath">classes.txt 文件路径。</param>
    public void LoadClassNames(string classesFilePath)
    {
        if (!File.Exists(classesFilePath))
            throw new FileNotFoundException("类别文件不存在", classesFilePath);

        var names = File.ReadAllLines(classesFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim());

        SetClassNames(names);
    }

    // ── 基类方法实现 ──────────────────────────────────────────────────────────

    protected override void LoadModelInternal(string modelPath)
    {
        // 尝试使用 ONNX Runtime
        try
        {
            var sessionType = Type.GetType("Microsoft.ML.OnnxRuntime.InferenceSession, Microsoft.ML.OnnxRuntime");
            if (sessionType != null)
            {
                _session = Activator.CreateInstance(sessionType, modelPath);

                // 获取输入信息
                var inputMetaProperty = sessionType.GetProperty("InputMetadata");
                if (inputMetaProperty?.GetValue(_session) is IDictionary<string, object> inputMeta)
                {
                    foreach (var key in inputMeta.Keys)
                    {
                        InputName = key;
                        break;
                    }
                }

                // 获取输出信息
                var outputMetaProperty = sessionType.GetProperty("OutputMetadata");
                if (outputMetaProperty?.GetValue(_session) is IDictionary<string, object> outputMeta)
                {
                    OutputNames = outputMeta.Keys.ToArray();
                }

                // 设置默认输入尺寸
                InputWidth = 640;
                InputHeight = 640;
            }
            else
            {
                throw new InvalidOperationException(
                    "ONNX Runtime 未安装。请安装 Microsoft.ML.OnnxRuntime NuGet 包。");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"加载 ONNX 模型失败: {ex.Message}", ex);
        }
    }

    protected override float[] Preprocess(Bitmap image)
    {
        // 调整图像大小到模型输入尺寸
        using var resized = ResizeImage(image, InputWidth, InputHeight);

        // 转换为 RGB float 数组，归一化到 [0, 1]
        var inputData = new float[1 * 3 * InputHeight * InputWidth];
        var index = 0;

        // 先填充 R 通道
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                inputData[index++] = pixel.R / 255.0f;
            }
        }

        // 填充 G 通道
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                inputData[index++] = pixel.G / 255.0f;
            }
        }

        // 填充 B 通道
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                inputData[index++] = pixel.B / 255.0f;
            }
        }

        return inputData;
    }

    protected override float[][] RunInference(float[] input)
    {
        if (_session == null)
            throw new InvalidOperationException("模型未加载");

        var sessionType = _session.GetType();

        // 创建输入张量
        var tensorType = Type.GetType("Microsoft.ML.OnnxRuntime.Tensors.DenseTensor`1, Microsoft.ML.OnnxRuntime");
        if (tensorType == null)
            throw new InvalidOperationException("无法创建 Tensor 类型");

        var floatTensorType = tensorType.MakeGenericType(typeof(float));
        var tensor = Activator.CreateInstance(floatTensorType, input, new[] { 1, 3, InputHeight, InputWidth });

        // 创建输入集合
        var namedValueType = Type.GetType("Microsoft.ML.OnnxRuntime.NamedOnnxValue, Microsoft.ML.OnnxRuntime");
        if (namedValueType == null)
            throw new InvalidOperationException("无法创建 NamedOnnxValue 类型");

        var createMethod = namedValueType.GetMethod("CreateFromTensor");
        if (createMethod == null)
            throw new InvalidOperationException("无法找到 CreateFromTensor 方法");

        var inputValue = createMethod.Invoke(null, new object[] { InputName, tensor! });

        // 创建输入列表
        var inputsList = (System.Collections.IList)Activator.CreateInstance(
            typeof(List<>).MakeGenericType(namedValueType))!;
        inputsList.Add(inputValue);

        // 执行推理
        var runMethod = sessionType.GetMethod("Run", new[] { inputsList.GetType() });
        if (runMethod == null)
            throw new InvalidOperationException("无法找到 Run 方法");

        var outputs = runMethod.Invoke(_session, new object[] { inputsList }) as IDisposable;
        if (outputs == null)
            throw new InvalidOperationException("推理返回空结果");

        // 解析输出
        var results = new List<float[]>();

        foreach (var output in (System.Collections.IEnumerable)outputs)
        {
            var valueProperty = output.GetType().GetProperty("Value");
            var value = valueProperty?.GetValue(output);

            if (value != null)
            {
                var asTensorMethod = value.GetType().GetMethod("AsTensor");
                var tensorValue = asTensorMethod?.Invoke(value, null);

                if (tensorValue != null)
                {
                    var toArrayMethod = tensorValue.GetType().GetMethod("ToArray");
                    var array = toArrayMethod?.Invoke(tensorValue, null) as float[];
                    if (array != null)
                    {
                        results.Add(array);
                    }
                }
            }

            (output as IDisposable)?.Dispose();
        }

        return results.ToArray();
    }

    protected override List<InferenceResult> Postprocess(float[][] outputs, int originalWidth, int originalHeight)
    {
        var results = new List<InferenceResult>();

        if (outputs.Length == 0)
            return results;

        var output = outputs[0];

        // YOLOv8 输出格式: [1, 84, 8400] 或 [1, 8400, 84]
        // 84 = 4 (bbox) + 80 (classes) 对于 COCO 数据集
        // 需要根据实际模型调整

        // 假设输出格式为 [1, numDetections, 5 + numClasses]
        var numClasses = _classNames.Count > 0 ? _classNames.Count : 80;
        var numValuesPerDetection = 5 + numClasses;

        if (output.Length % numValuesPerDetection != 0)
        {
            // 尝试其他格式
            return TryParseAlternativeFormat(output, originalWidth, originalHeight);
        }

        var numDetections = output.Length / numValuesPerDetection;

        for (int i = 0; i < numDetections; i++)
        {
            var offset = i * numValuesPerDetection;

            // 解析边界框 (cx, cy, w, h)
            var cx = output[offset];
            var cy = output[offset + 1];
            var w = output[offset + 2];
            var h = output[offset + 3];

            // 找到最大类别分数
            var maxScore = 0f;
            var maxClassIndex = 0;

            for (int j = 0; j < numClasses; j++)
            {
                var score = output[offset + 5 + j];
                if (score > maxScore)
                {
                    maxScore = score;
                    maxClassIndex = j;
                }
            }

            // 过滤低置信度
            if (maxScore < _confidenceThreshold)
                continue;

            // 转换为像素坐标
            var scaleX = (float)originalWidth / InputWidth;
            var scaleY = (float)originalHeight / InputHeight;

            var x = (cx - w / 2) * scaleX;
            var y = (cy - h / 2) * scaleY;
            var width = w * scaleX;
            var height = h * scaleY;

            // 裁剪到图像范围
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            width = Math.Min(width, originalWidth - x);
            height = Math.Min(height, originalHeight - y);

            var className = maxClassIndex < _classNames.Count
                ? _classNames[maxClassIndex]
                : $"class_{maxClassIndex}";

            results.Add(new InferenceResult
            {
                ClassIndex = maxClassIndex,
                ClassName = className,
                Confidence = maxScore,
                BoundingBox = new RectangleF(x, y, width, height)
            });
        }

        // 应用 NMS
        return ApplyNms(results, _nmsThreshold);
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

    private List<InferenceResult> TryParseAlternativeFormat(float[] output, int originalWidth, int originalHeight)
    {
        var results = new List<InferenceResult>();

        // 尝试 YOLOv5 格式: [1, 25200, 85]
        var numClasses = _classNames.Count > 0 ? _classNames.Count : 80;
        var numValuesPerDetection = 5 + numClasses;

        if (output.Length % numValuesPerDetection == 0)
        {
            var numDetections = output.Length / numValuesPerDetection;

            for (int i = 0; i < numDetections; i++)
            {
                var offset = i * numValuesPerDetection;

                var cx = output[offset];
                var cy = output[offset + 1];
                var w = output[offset + 2];
                var h = output[offset + 3];
                var objectness = output[offset + 4];

                var maxScore = 0f;
                var maxClassIndex = 0;

                for (int j = 0; j < numClasses; j++)
                {
                    var score = output[offset + 5 + j] * objectness;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxClassIndex = j;
                    }
                }

                if (maxScore < _confidenceThreshold)
                    continue;

                var scaleX = (float)originalWidth / InputWidth;
                var scaleY = (float)originalHeight / InputHeight;

                var x = (cx - w / 2) * scaleX;
                var y = (cy - h / 2) * scaleY;
                var width = w * scaleX;
                var height = h * scaleY;

                x = Math.Max(0, x);
                y = Math.Max(0, y);
                width = Math.Min(width, originalWidth - x);
                height = Math.Min(height, originalHeight - y);

                var className = maxClassIndex < _classNames.Count
                    ? _classNames[maxClassIndex]
                    : $"class_{maxClassIndex}";

                results.Add(new InferenceResult
                {
                    ClassIndex = maxClassIndex,
                    ClassName = className,
                    Confidence = maxScore,
                    BoundingBox = new RectangleF(x, y, width, height)
                });
            }
        }

        return ApplyNms(results, _nmsThreshold);
    }

    private static List<InferenceResult> ApplyNms(List<InferenceResult> detections, float nmsThreshold)
    {
        var result = new List<InferenceResult>();

        // 按类别分组
        var grouped = detections.GroupBy(d => d.ClassIndex);

        foreach (var group in grouped)
        {
            var sorted = group.OrderByDescending(d => d.Confidence).ToList();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                result.Add(best);
                sorted.RemoveAt(0);

                // 移除与 best 重叠度高的检测
                sorted.RemoveAll(d => CalculateIoU(best.BoundingBox, d.BoundingBox) > nmsThreshold);
            }
        }

        return result;
    }

    private static float CalculateIoU(RectangleF a, RectangleF b)
    {
        var intersection = RectangleF.Intersect(a, b);
        if (intersection.IsEmpty)
            return 0;

        var intersectionArea = intersection.Width * intersection.Height;
        var unionArea = a.Width * a.Height + b.Width * b.Height - intersectionArea;

        return intersectionArea / unionArea;
    }

    private static Bitmap ResizeImage(Bitmap image, int width, int height)
    {
        var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        using (var graphics = Graphics.FromImage(result))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, new Rectangle(0, 0, width, height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        return result;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            (_session as IDisposable)?.Dispose();
            _session = null;
        }

        base.Dispose(disposing);
    }
}
