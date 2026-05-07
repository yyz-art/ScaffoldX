using System.Drawing;
using TorchSharp;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// SAM 3 分割引擎接口，支持文本/点/框/参考图提示的开放词汇分割。
/// </summary>
public interface ISam3SegmentationEngine : IDisposable
{
    bool IsLoaded { get; }
    Task LoadModelAsync(string modelDir, CancellationToken ct = default);
    Task<ImageEmbedding> EncodeImageAsync(Bitmap image, CancellationToken ct = default);
    Task<List<SegmentationResult>> SegmentByTextAsync(ImageEmbedding embedding, IEnumerable<string> textPrompts, float threshold = 0.5f, CancellationToken ct = default);
    Task<SegmentationResult> SegmentByPointsAsync(ImageEmbedding embedding, IEnumerable<PointF> positivePoints, IEnumerable<PointF> negativePoints, CancellationToken ct = default);
    Task<SegmentationResult> SegmentByBoxAsync(ImageEmbedding embedding, RectangleF box, CancellationToken ct = default);
    Task<List<SegmentationResult>> SegmentByReferenceAsync(Bitmap image, Bitmap referenceImage, float threshold = 0.5f, CancellationToken ct = default);
}

/// <summary>
/// SAM 3 分割推理结果。
/// </summary>
public class SegmentationResult
{
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public byte[,] Mask { get; set; } = new byte[0, 0];
    public List<PointF> ContourPoints { get; set; } = new();
    public RectangleF BoundingBox { get; set; }
}

/// <summary>
/// SAM 3 分割引擎。使用 TorchSharp 加载 TorchScript 模型。
/// 模型目录需包含：encoder.pt、text_encoder.pt、decoder.pt。
/// </summary>
public class Sam3Segmentor : ISam3SegmentationEngine
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private torch.jit.ScriptModule? _imageEncoder;
    private torch.jit.ScriptModule? _textEncoder;
    private torch.jit.ScriptModule? _decoder;
    private readonly Sam3Tokenizer _tokenizer = new();
    private volatile bool _isLoaded;

    public bool IsLoaded => _isLoaded;
    public int InputSize { get; set; } = 1024;

    public async Task LoadModelAsync(string modelDir, CancellationToken ct = default)
    {
        await _loadLock.WaitAsync(ct);
        try
        {
            DisposeModels();

            var encoderPath = Path.Combine(modelDir, "encoder.pt");
            var textEncoderPath = Path.Combine(modelDir, "text_encoder.pt");
            var decoderPath = Path.Combine(modelDir, "decoder.pt");

            // Support single combined model file as fallback
            var combinedPath = Path.Combine(modelDir, "sam3.pt");
            if (!File.Exists(encoderPath) && !File.Exists(textEncoderPath) && !File.Exists(decoderPath))
            {
                if (!Directory.Exists(modelDir))
                    throw new FileNotFoundException("模型目录不存在", modelDir);

                // Look for any .pt file in the directory
                var ptFiles = Directory.GetFiles(modelDir, "*.pt");
                if (ptFiles.Length == 1)
                {
                    combinedPath = ptFiles[0];
                }
                else if (ptFiles.Length == 0)
                {
                    throw new FileNotFoundException(
                        $"模型目录中未找到模型文件。需要 encoder.pt + text_encoder.pt + decoder.pt，或单个合并模型文件。目录: {modelDir}");
                }
                else
                {
                    throw new FileNotFoundException(
                        $"模型目录中有多个 .pt 文件但缺少 encoder.pt/text_encoder.pt/decoder.pt。请使用包含这 3 个文件的目录。目录: {modelDir}");
                }

                // Load single combined model — use it for all three components
                await Task.Run(() =>
                {
                    try
                    {
                        var model = torch.jit.load(combinedPath);
                        _imageEncoder = model;
                        _textEncoder = model;
                        _decoder = model;
                    }
                    catch (Exception ex)
                    {
                        DisposeModels();
                        throw new InvalidOperationException($"加载模型失败: {combinedPath}。请确认文件是有效的 TorchScript 模型。", ex);
                    }
                }, ct);
            }
            else
            {
                // Load 3 separate model files
                if (!File.Exists(encoderPath))
                    throw new FileNotFoundException("图像编码器模型不存在", encoderPath);
                if (!File.Exists(textEncoderPath))
                    throw new FileNotFoundException("文本编码器模型不存在", textEncoderPath);
                if (!File.Exists(decoderPath))
                    throw new FileNotFoundException("解码器模型不存在", decoderPath);

                await Task.Run(() =>
                {
                    try
                    {
                        _imageEncoder = torch.jit.load(encoderPath);
                        _textEncoder = torch.jit.load(textEncoderPath);
                        _decoder = torch.jit.load(decoderPath);
                    }
                    catch
                    {
                        DisposeModels();
                        throw;
                    }
                }, ct);
            }

            var vocabPath = Path.Combine(modelDir, "vocab.json");
            var mergesPath = Path.Combine(modelDir, "merges.txt");
            if (File.Exists(vocabPath) && File.Exists(mergesPath))
                _tokenizer.LoadVocab(vocabPath, mergesPath);

            _isLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<ImageEmbedding> EncodeImageAsync(Bitmap image, CancellationToken ct = default)
    {
        EnsureLoaded();
        return await Task.Run(() =>
        {
            using var tensor = BitmapToNormalizedTensor(image, InputSize, InputSize);
            using var rawEmbedding = (torch.Tensor)_imageEncoder!.forward(tensor);
            return new ImageEmbedding(rawEmbedding.detach().clone(), image.Width, image.Height, InputSize, InputSize);
        }, ct);
    }

    public async Task<List<SegmentationResult>> SegmentByTextAsync(
        ImageEmbedding embedding, IEnumerable<string> textPrompts, float threshold = 0.5f, CancellationToken ct = default)
    {
        EnsureLoaded();
        return await Task.Run(() =>
        {
            var results = new List<SegmentationResult>();
            foreach (var prompt in textPrompts)
            {
                var tokenIds = _tokenizer.Encode(prompt);
                using var textTensor = torch.tensor(tokenIds.Select(id => (long)id).ToArray(),
                    dtype: torch.ScalarType.Int64).reshape(new long[] { 1, tokenIds.Length });
                var textFeatures = (torch.Tensor)_textEncoder!.forward(textTensor);
                var maskLogits = (torch.Tensor)_decoder!.forward(embedding.Features, textFeatures);
                results.Add(MaskToResult(maskLogits, prompt, threshold, embedding.OriginalWidth, embedding.OriginalHeight));
                textFeatures.Dispose();
            }
            return results;
        }, ct);
    }

    public async Task<SegmentationResult> SegmentByPointsAsync(
        ImageEmbedding embedding, IEnumerable<PointF> positivePoints, IEnumerable<PointF> negativePoints, CancellationToken ct = default)
    {
        EnsureLoaded();
        return await Task.Run(() =>
        {
            var pointList = new List<(float x, float y, float label)>();
            foreach (var p in positivePoints) pointList.Add((p.X, p.Y, 1.0f));
            foreach (var p in negativePoints) pointList.Add((p.X, p.Y, 0.0f));
            if (pointList.Count == 0) throw new ArgumentException("至少需要一个提示点");

            var data = new float[pointList.Count * 3];
            for (int i = 0; i < pointList.Count; i++)
            {
                data[i * 3] = pointList[i].x;
                data[i * 3 + 1] = pointList[i].y;
                data[i * 3 + 2] = pointList[i].label;
            }

            using var pointTensor = torch.tensor(data, dtype: torch.ScalarType.Float32).reshape(new long[] { 1, pointList.Count, 3 });
            var maskLogits = (torch.Tensor)_decoder!.forward(embedding.Features, pointTensor);
            return MaskToResult(maskLogits, "point_segment", 0.5f, embedding.OriginalWidth, embedding.OriginalHeight);
        }, ct);
    }

    public async Task<SegmentationResult> SegmentByBoxAsync(
        ImageEmbedding embedding, RectangleF box, CancellationToken ct = default)
    {
        EnsureLoaded();
        return await Task.Run(() =>
        {
            var boxData = new float[] { box.X, box.Y, box.X + box.Width, box.Y + box.Height };
            using var boxTensor = torch.tensor(boxData, dtype: torch.ScalarType.Float32).reshape(new long[] { 1, 1, 4 });
            var maskLogits = (torch.Tensor)_decoder!.forward(embedding.Features, boxTensor);
            return MaskToResult(maskLogits, "box_segment", 0.5f, embedding.OriginalWidth, embedding.OriginalHeight);
        }, ct);
    }

    public async Task<List<SegmentationResult>> SegmentByReferenceAsync(
        Bitmap image, Bitmap referenceImage, float threshold = 0.5f, CancellationToken ct = default)
    {
        EnsureLoaded();
        using var targetEmb = await EncodeImageAsync(image, ct);
        return await Task.Run(() =>
        {
            using var refTensor = BitmapToNormalizedTensor(referenceImage, InputSize, InputSize);
            using var refEmb = (torch.Tensor)_imageEncoder!.forward(refTensor);
            var maskLogits = (torch.Tensor)_decoder!.forward(targetEmb.Features, refEmb);
            return new List<SegmentationResult> { MaskToResult(maskLogits, "reference_match", threshold, image.Width, image.Height) };
        }, ct);
    }

    private SegmentationResult MaskToResult(torch.Tensor maskLogits, string label, float threshold, int origW, int origH)
    {
        var shape = maskLogits.shape;
        var h = (int)shape[^2];
        var w = (int)shape[^1];

        using var sigmoid = torch.sigmoid(maskLogits);
        using var binary = sigmoid.ge(threshold);
        var maskData = new byte[h, w];
        var boolData = binary.data<bool>().ToArray();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                maskData[y, x] = boolData[y * w + x] ? (byte)1 : (byte)0;

        var contour = MaskToPolygonConverter.Convert(maskData, 2.0f);
        var bbox = ComputeBBox(maskData, origW, origH);

        maskLogits.Dispose();

        return new SegmentationResult { Label = label, Confidence = 1.0f, Mask = maskData, ContourPoints = contour, BoundingBox = bbox };
    }

    private static RectangleF ComputeBBox(byte[,] mask, int origW, int origH)
    {
        var h = mask.GetLength(0);
        var w = mask.GetLength(1);
        int minX = w, minY = h, maxX = 0, maxY = 0;
        bool found = false;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mask[y, x] != 0)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    found = true;
                }

        if (!found) return RectangleF.Empty;
        float sx = (float)origW / w, sy = (float)origH / h;
        return new RectangleF(minX * sx, minY * sy, (maxX - minX + 1) * sx, (maxY - minY + 1) * sy);
    }

    private static torch.Tensor BitmapToNormalizedTensor(Bitmap image, int tw, int th)
    {
        using var resized = new Bitmap(tw, th);
        using (var g = Graphics.FromImage(resized))
            g.DrawImage(image, 0, 0, tw, th);

        var data = resized.LockBits(new Rectangle(0, 0, tw, th), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        try
        {
            var buffer = new byte[data.Stride * data.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            var floatData = new float[3 * th * tw];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    var off = y * data.Stride + x * 3;
                    var c0 = y * tw + x;
                    floatData[c0] = buffer[off + 2] / 255f;
                    floatData[th * tw + c0] = buffer[off + 1] / 255f;
                    floatData[2 * th * tw + c0] = buffer[off] / 255f;
                }

            return torch.tensor(floatData, dtype: torch.ScalarType.Float32).reshape(new long[] { 1, 3, th, tw });
        }
        finally
        {
            resized.UnlockBits(data);
        }
    }

    private void EnsureLoaded()
    {
        if (!_isLoaded) throw new InvalidOperationException("SAM 3 模型未加载，请先调用 LoadModelAsync");
    }

    private void DisposeModels()
    {
        _imageEncoder?.Dispose();
        _textEncoder?.Dispose();
        _decoder?.Dispose();
        _imageEncoder = null;
        _textEncoder = null;
        _decoder = null;
        _isLoaded = false;
    }

    public void Dispose()
    {
        DisposeModels();
        _loadLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
