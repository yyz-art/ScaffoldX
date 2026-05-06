using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// ONNX YOLO 目标检测器，支持 YOLOv5/v8 格式的 ONNX 模型。
/// </summary>
public class OnnxDetector : InferenceEngineBase
{
    private InferenceSession? _session;
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
            InputWidth = 640;
            InputHeight = 640;
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

            var input = new float[1 * 3 * InputHeight * InputWidth];
            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    var pixelOffset = y * data.Stride + x * 3;
                    var ch0Offset = y * InputWidth + x;
                    var ch1Offset = InputHeight * InputWidth + ch0Offset;
                    var ch2Offset = 2 * InputHeight * InputWidth + ch0Offset;

                    input[ch0Offset] = buffer[pixelOffset + 2] / 255f; // R (BGR in bitmap)
                    input[ch1Offset] = buffer[pixelOffset + 1] / 255f; // G
                    input[ch2Offset] = buffer[pixelOffset] / 255f;     // B
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

        var output = outputs[0];
        var numClasses = _classNames.Count > 0 ? _classNames.Count : 80;

        // Detect YOLOv8-seg format: [1, 4+numClasses+32, 8400] + [1, 32, 160, 160]
        // vs YOLOv8 det format: [1, 4+numClasses, 8400]
        // vs YOLOv5 format: [1, numDetections, 5+numClasses]
        var numMaskCoeffs = 32;
        var yolov8SegFeatures = 4 + numClasses + numMaskCoeffs; // 116 for 80 classes
        var yolov8Features = 4 + numClasses;
        var yolov5Features = 5 + numClasses;

        // Check for YOLOv8-seg: two outputs with [1, 116, N] + [1, 32, H, W]
        if (outputs.Length >= 2 && output.Length % yolov8SegFeatures == 0)
        {
            var numDetections = output.Length / yolov8SegFeatures;
            if (numDetections > yolov8SegFeatures)
            {
                return ParseYolov8SegTransposed(output, outputs[1], numClasses, numMaskCoeffs,
                    numDetections, originalWidth, originalHeight);
            }
        }

        if (output.Length % yolov8Features == 0)
        {
            var numDetections = output.Length / yolov8Features;

            // YOLOv8 transposed: numDetections >> numFeatures (e.g. 8400 >> 84)
            if (numDetections > yolov8Features)
            {
                return ParseYolov8Transposed(output, numClasses, numDetections, originalWidth, originalHeight);
            }
            else if (numDetections > 0)
            {
                return ParseYolov8RowMajor(output, numClasses, yolov8Features, numDetections, originalWidth, originalHeight);
            }
        }

        if (output.Length % yolov5Features == 0)
        {
            return ParseYolov5(output, numClasses, yolov5Features, originalWidth, originalHeight);
        }

        // Fallback: try YOLOv8 with different class count heuristics
        if (output.Length % yolov8Features == 0)
        {
            var numDetections = output.Length / yolov8Features;
            return ParseYolov8Transposed(output, numClasses, numDetections, originalWidth, originalHeight);
        }

        return results;
    }

    // ── YOLOv8 transposed [1, 4+classes, N] parsing ─────────────────────────

    /// <summary>
    /// 解析 YOLOv8 转置格式输出 [1, 4+numClasses, numDetections]。
    /// 数据按列主序存储：特征沿行方向，检测沿列方向。
    /// </summary>
    private List<InferenceResult> ParseYolov8Transposed(
        float[] output, int numClasses, int numDetections, int originalWidth, int originalHeight)
    {
        var results = new List<InferenceResult>();

        for (int i = 0; i < numDetections; i++)
        {
            // Column-major access: feature j, detection i → output[j * numDetections + i]
            var cx = output[0 * numDetections + i];
            var cy = output[1 * numDetections + i];
            var w = output[2 * numDetections + i];
            var h = output[3 * numDetections + i];

            var maxScore = 0f;
            var maxClassIndex = 0;

            for (int j = 0; j < numClasses; j++)
            {
                var score = output[(4 + j) * numDetections + i];
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

            results.Add(CreateDetectionResult(
                cx, cy, w, h, maxClassIndex, maxScore,
                originalWidth, originalHeight, scaleX, scaleY, _classNames));
        }

        return ApplyNms(results, _nmsThreshold);
    }

    // ── YOLOv8-seg transposed [1, 4+classes+32, N] + prototypes [1, 32, 160, 160] ──

    /// <summary>
    /// 解析 YOLOv8-seg 转置格式输出。outputs[0] 形状 [1, 4+numClasses+numMaskCoeffs, numDetections]，
    /// outputs[1] 形状 [1, numMaskCoeffs, protoH, protoW]（掩码原型）。
    /// 通过掩码系数与原型矩阵相乘，为每个检测生成逐像素分割掩码。
    /// </summary>
    private List<InferenceResult> ParseYolov8SegTransposed(
        float[] detectionOutput, float[] prototypeOutput,
        int numClasses, int numMaskCoeffs, int numDetections,
        int originalWidth, int originalHeight)
    {
        var results = new List<InferenceResult>();

        // Prototype tensor shape: [1, numMaskCoeffs, protoH, protoW]
        // Infer protoH and protoW from total size
        var protoElements = prototypeOutput.Length / numMaskCoeffs;
        var protoH = (int)Math.Sqrt(protoElements);
        var protoW = protoElements / protoH;
        if (protoH * protoW != protoElements)
        {
            // Non-square prototypes: try common sizes
            protoH = 160;
            protoW = protoElements / protoH;
        }

        var segFeatureStart = 4 + numClasses;

        for (int i = 0; i < numDetections; i++)
        {
            // Column-major access: feature j, detection i → output[j * numDetections + i]
            var cx = detectionOutput[0 * numDetections + i];
            var cy = detectionOutput[1 * numDetections + i];
            var w = detectionOutput[2 * numDetections + i];
            var h = detectionOutput[3 * numDetections + i];

            var maxScore = 0f;
            var maxClassIndex = 0;

            for (int j = 0; j < numClasses; j++)
            {
                var score = detectionOutput[(4 + j) * numDetections + i];
                if (score > maxScore)
                {
                    maxScore = score;
                    maxClassIndex = j;
                }
            }

            if (maxScore < _confidenceThreshold)
                continue;

            // Extract mask coefficients for this detection
            var maskCoeffs = new float[numMaskCoeffs];
            for (int k = 0; k < numMaskCoeffs; k++)
            {
                maskCoeffs[k] = detectionOutput[(segFeatureStart + k) * numDetections + i];
            }

            // Generate mask by multiplying coefficients with prototypes
            // mask = sigmoid(maskCoeffs @ prototypes) → [protoH, protoW]
            var maskData = GenerateDetectionMask(maskCoeffs, prototypeOutput, numMaskCoeffs, protoH, protoW);

            var scaleX = (float)originalWidth / InputWidth;
            var scaleY = (float)originalHeight / InputHeight;

            var baseResult = CreateDetectionResult(
                cx, cy, w, h, maxClassIndex, maxScore,
                originalWidth, originalHeight, scaleX, scaleY, _classNames);

            var fullMask = ResizeMaskToOriginal(maskData, protoH, protoW, originalWidth, originalHeight);
            var binaryMask = ApplySigmoidAndThreshold(fullMask, originalWidth, originalHeight);

            results.Add(new SegmentationResult
            {
                ClassIndex = baseResult.ClassIndex,
                ClassName = baseResult.ClassName,
                Confidence = baseResult.Confidence,
                BoundingBox = baseResult.BoundingBox,
                SegmentationMask = binaryMask
            });
        }

        return ApplyNms(results, _nmsThreshold);
    }

    /// <summary>
    /// 通过掩码系数向量与原型矩阵相乘生成原型分辨率的掩码数据。
    /// 计算 mask = maskCoeffs @ prototypes（矩阵乘法），结果为 [protoH * protoW] 的一维数组。
    /// </summary>
    private static float[] GenerateDetectionMask(float[] maskCoeffs, float[] prototypes,
        int numCoeffs, int protoH, int protoW)
    {
        var maskSize = protoH * protoW;
        var mask = new float[maskSize];

        for (int hw = 0; hw < maskSize; hw++)
        {
            var sum = 0f;
            for (int c = 0; c < numCoeffs; c++)
            {
                // Prototype layout: [numCoeffs, protoH * protoW], row-major
                sum += maskCoeffs[c] * prototypes[c * maskSize + hw];
            }
            mask[hw] = sum;
        }

        return mask;
    }

    /// <summary>
    /// 将原型分辨率掩码缩放至原始图像尺寸（最近邻插值）。
    /// </summary>
    private static float[,] ResizeMaskToOriginal(float[] mask, int protoH, int protoW,
        int targetWidth, int targetHeight)
    {
        var result = new float[targetHeight, targetWidth];

        for (int y = 0; y < targetHeight; y++)
        {
            var srcY = Math.Min((int)((float)y / targetHeight * protoH), protoH - 1);
            for (int x = 0; x < targetWidth; x++)
            {
                var srcX = Math.Min((int)((float)x / targetWidth * protoW), protoW - 1);
                result[y, x] = mask[srcY * protoW + srcX];
            }
        }

        return result;
    }

    /// <summary>
    /// 对浮点掩码应用 sigmoid 激活并以 0.5 为阈值转换为二值字节掩码。
    /// </summary>
    private static byte[,] ApplySigmoidAndThreshold(float[,] mask, int width, int height,
        float threshold = 0.5f)
    {
        var result = new byte[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var sigmoid = 1.0f / (1.0f + MathF.Exp(-mask[y, x]));
                result[y, x] = sigmoid >= threshold ? (byte)1 : (byte)0;
            }
        }

        return result;
    }

    // ── YOLOv8 row-major [1, N, 4+classes] parsing ──────────────────────────

    /// <summary>
    /// 解析 YOLOv8 行主序格式输出 [1, numDetections, 4+numClasses]。
    /// 每行包含 [cx, cy, w, h, classScores...]（无 objectness）。
    /// </summary>
    private List<InferenceResult> ParseYolov8RowMajor(
        float[] output, int numClasses, int stride, int numDetections, int originalWidth, int originalHeight)
    {
        var results = new List<InferenceResult>();

        for (int i = 0; i < numDetections; i++)
        {
            var offset = i * stride;

            var cx = output[offset];
            var cy = output[offset + 1];
            var w = output[offset + 2];
            var h = output[offset + 3];

            var maxScore = 0f;
            var maxClassIndex = 0;

            for (int j = 0; j < numClasses; j++)
            {
                var score = output[offset + 4 + j];
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

            results.Add(CreateDetectionResult(
                cx, cy, w, h, maxClassIndex, maxScore,
                originalWidth, originalHeight, scaleX, scaleY, _classNames));
        }

        return ApplyNms(results, _nmsThreshold);
    }

    // ── YOLOv5 [1, N, 5+classes] parsing ────────────────────────────────────

    /// <summary>
    /// 解析 YOLOv5 格式输出 [1, numDetections, 5+numClasses]。
    /// 每行包含 [cx, cy, w, h, objectness, classScores...]。
    /// </summary>
    private List<InferenceResult> ParseYolov5(
        float[] output, int numClasses, int stride, int originalWidth, int originalHeight)
    {
        var results = new List<InferenceResult>();
        var numDetections = output.Length / stride;

        for (int i = 0; i < numDetections; i++)
        {
            var offset = i * stride;

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

            results.Add(CreateDetectionResult(
                cx, cy, w, h, maxClassIndex, maxScore,
                originalWidth, originalHeight, scaleX, scaleY, _classNames));
        }

        return ApplyNms(results, _nmsThreshold);
    }

    // ── NMS and helpers ──────────────────────────────────────────────────────

    private static List<InferenceResult> ApplyNms(List<InferenceResult> detections, float nmsThreshold)
    {
        var result = new List<InferenceResult>();

        var grouped = detections.GroupBy(d => d.ClassIndex);

        foreach (var group in grouped)
        {
            var sorted = group.OrderByDescending(d => d.Confidence).ToList();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                result.Add(best);
                sorted.RemoveAt(0);

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

        if (unionArea <= 0) return 0;
        return intersectionArea / unionArea;
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
