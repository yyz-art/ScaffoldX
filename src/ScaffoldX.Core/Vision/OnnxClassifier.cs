using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// ONNX 图像分类器，支持标准的图像分类 ONNX 模型。
/// </summary>
public class OnnxClassifier : InferenceEngineBase
{
    private object? _session; // InferenceSession
    private readonly List<string> _classNames = new();
    private int _topK = 5;

    /// <summary>返回前 K 个预测结果。</summary>
    public int TopK
    {
        get => _topK;
        set => _topK = Math.Max(1, value);
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

                // 设置默认输入尺寸（分类模型通常是 224x224）
                InputWidth = 224;
                InputHeight = 224;
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

        // ImageNet 归一化参数
        var mean = new float[] { 0.485f, 0.456f, 0.406f };
        var std = new float[] { 0.229f, 0.224f, 0.225f };

        // 转换为 RGB float 数组，归一化
        var inputData = new float[1 * 3 * InputHeight * InputWidth];
        var index = 0;

        // 先填充 R 通道
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                inputData[index++] = (pixel.R / 255.0f - mean[0]) / std[0];
            }
        }

        // 填充 G 通道
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                inputData[index++] = (pixel.G / 255.0f - mean[1]) / std[1];
            }
        }

        // 填充 B 通道
        for (int y = 0; y < InputHeight; y++)
        {
            for (int x = 0; x < InputWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                inputData[index++] = (pixel.B / 255.0f - mean[2]) / std[2];
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

        var logits = outputs[0];

        // 应用 Softmax
        var probabilities = Softmax(logits);

        // 获取 Top K 结果
        var indexed = probabilities
            .Select((prob, index) => new { Index = index, Probability = prob })
            .OrderByDescending(x => x.Probability)
            .Take(_topK)
            .ToList();

        foreach (var item in indexed)
        {
            var className = item.Index < _classNames.Count
                ? _classNames[item.Index]
                : $"class_{item.Index}";

            results.Add(new InferenceResult
            {
                ClassIndex = item.Index,
                ClassName = className,
                Confidence = item.Probability,
                BoundingBox = new RectangleF(0, 0, originalWidth, originalHeight) // 分类模型返回整图
            });
        }

        return results;
    }

    // ── 辅助方法 ──────────────────────────────────────────────────────────────

    private static float[] Softmax(float[] logits)
    {
        var max = logits.Max();
        var exps = logits.Select(x => MathF.Exp(x - max)).ToArray();
        var sum = exps.Sum();
        return exps.Select(x => x / sum).ToArray();
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
