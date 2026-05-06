using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// ONNX 图像分类器，支持标准的图像分类 ONNX 模型。
/// </summary>
public class OnnxClassifier : InferenceEngineBase
{
    private InferenceSession? _session;
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
            _session?.Dispose();
            _session = new InferenceSession(modelPath);

            foreach (var key in _session.InputMetadata.Keys)
            {
                InputName = key;
                break;
            }

            OutputNames = _session.OutputMetadata.Keys.ToArray();

            // 尝试从模型输入元数据读取维度 [batch, channels, height, width]
            if (_session.InputMetadata.Count > 0)
            {
                var firstInput = _session.InputMetadata.Values.First();
                var dims = firstInput.Dimensions;
                if (dims is { Length: 4 })
                {
                    var height = dims[2];
                    var width = dims[3];
                    if (height > 0 && width > 0)
                    {
                        InputHeight = height;
                        InputWidth = width;
                        return;
                    }
                }
            }

            // 回退到默认值
            InputWidth = 224;
            InputHeight = 224;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"加载 ONNX 模型失败: {ex.Message}", ex);
        }
    }

    protected override float[] Preprocess(Bitmap image)
    {
        using var resized = ResizeImage(image, InputWidth, InputHeight);
        var data = resized.LockBits(
            new Rectangle(0, 0, resized.Width, resized.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            var buffer = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            var mean = new float[] { 0.485f, 0.456f, 0.406f };
            var std = new float[] { 0.229f, 0.224f, 0.225f };

            var input = new float[1 * 3 * InputHeight * InputWidth];
            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var pixelOffset = y * data.Stride + x * 3;
                    var ch0Offset = y * InputWidth + x;
                    var ch1Offset = InputHeight * InputWidth + ch0Offset;
                    var ch2Offset = 2 * InputHeight * InputWidth + ch0Offset;

                    input[ch0Offset] = (buffer[pixelOffset + 2] / 255f - mean[0]) / std[0]; // R
                    input[ch1Offset] = (buffer[pixelOffset + 1] / 255f - mean[1]) / std[1]; // G
                    input[ch2Offset] = (buffer[pixelOffset] / 255f - mean[2]) / std[2];     // B
                }
            }
            return input;
        }
        finally
        {
            resized.UnlockBits(data);
        }
    }

    protected override float[][] RunInference(float[] input)
    {
        if (_session == null)
            throw new InvalidOperationException("模型未加载");

        var tensor = new DenseTensor<float>(input, new[] { 1, 3, InputHeight, InputWidth });
        var inputValue = NamedOnnxValue.CreateFromTensor(InputName, tensor);

        using var outputs = _session.Run(new List<NamedOnnxValue> { inputValue });

        var results = new List<float[]>();
        foreach (var output in outputs)
        {
            var tensorValue = output.AsTensor<float>();
            results.Add(tensorValue.ToArray());
        }

        return results.ToArray();
    }

    protected override List<InferenceResult> Postprocess(float[][] outputs, int originalWidth, int originalHeight)
    {
        var results = new List<InferenceResult>();

        if (outputs.Length == 0)
            return results;

        var logits = outputs[0];

        var probabilities = Softmax(logits);

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
                BoundingBox = new RectangleF(0, 0, originalWidth, originalHeight)
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session?.Dispose();
            _session = null;
        }

        base.Dispose(disposing);
    }
}
