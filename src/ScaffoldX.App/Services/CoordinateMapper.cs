namespace ScaffoldX.App.Services;

/// <summary>
/// 坐标变换工具类，提供归一化坐标与绝对像素坐标之间的转换。
/// 纯函数，无副作用。
/// </summary>
public static class CoordinateMapper
{
    /// <summary>
    /// 最小有效边界框尺寸（像素），低于此值视为无效标注。
    /// </summary>
    public const float MinBoxSize = 5f;

    /// <summary>
    /// 将归一化坐标 (0-1) 转换为绝对像素坐标。
    /// </summary>
    /// <param name="normX">归一化 X 坐标。</param>
    /// <param name="normY">归一化 Y 坐标。</param>
    /// <param name="imageWidth">图像宽度（像素）。</param>
    /// <param name="imageHeight">图像高度（像素）。</param>
    /// <returns>绝对像素坐标 (X, Y)。</returns>
    public static (double X, double Y) ToAbsolute(double normX, double normY, int imageWidth, int imageHeight)
        => (normX * imageWidth, normY * imageHeight);

    /// <summary>
    /// 将单个归一化值转换为绝对像素值。
    /// </summary>
    /// <param name="normalized">归一化值 (0-1)。</param>
    /// <param name="dimension">对应维度的像素尺寸。</param>
    /// <returns>绝对像素值。</returns>
    public static double ToAbsolute(double normalized, int dimension)
        => normalized * dimension;

    /// <summary>
    /// 将单个归一化值 (float) 转换为绝对像素值。
    /// </summary>
    public static float ToAbsolute(float normalized, int dimension)
        => normalized * dimension;

    /// <summary>
    /// 将绝对像素坐标转换为归一化坐标 (0-1)。
    /// </summary>
    /// <param name="absX">绝对 X 坐标（像素）。</param>
    /// <param name="absY">绝对 Y 坐标（像素）。</param>
    /// <param name="imageWidth">图像宽度（像素）。</param>
    /// <param name="imageHeight">图像高度（像素）。</param>
    /// <returns>归一化坐标 (X, Y)。</returns>
    public static (double X, double Y) ToNormalized(double absX, double absY, int imageWidth, int imageHeight)
        => (absX / imageWidth, absY / imageHeight);

    /// <summary>
    /// 将单个绝对像素值转换为归一化值 (0-1)。
    /// </summary>
    public static double ToNormalized(double absolute, int dimension)
        => absolute / dimension;

    /// <summary>
    /// 将归一化的中心坐标和尺寸转换为绝对像素的左上角坐标和宽高（YOLO → VOC/COCO bbox）。
    /// </summary>
    /// <param name="centerX">归一化中心 X。</param>
    /// <param name="centerY">归一化中心 Y。</param>
    /// <param name="width">归一化宽度。</param>
    /// <param name="height">归一化高度。</param>
    /// <param name="imageWidth">图像宽度（像素）。</param>
    /// <param name="imageHeight">图像高度（像素）。</param>
    /// <returns>绝对像素的左上角 (X, Y) 和宽高 (Width, Height)。</returns>
    public static (double X, double Y, double Width, double Height) ToAbsoluteBbox(
        double centerX, double centerY, double width, double height,
        int imageWidth, int imageHeight)
    {
        var absW = width * imageWidth;
        var absH = height * imageHeight;
        return (centerX * imageWidth - absW / 2, centerY * imageHeight - absH / 2, absW, absH);
    }

    /// <summary>
    /// 将归一化的中心坐标和尺寸 (float) 转换为绝对像素的左上角坐标和宽高。
    /// </summary>
    public static (float X, float Y, float Width, float Height) ToAbsoluteBbox(
        float centerX, float centerY, float width, float height,
        int imageWidth, int imageHeight)
    {
        var absW = width * imageWidth;
        var absH = height * imageHeight;
        return (centerX * imageWidth - absW / 2, centerY * imageHeight - absH / 2, absW, absH);
    }

    /// <summary>
    /// 将值限制在指定范围内。
    /// </summary>
    /// <param name="value">要限制的值。</param>
    /// <param name="min">最小值。</param>
    /// <param name="max">最大值。</param>
    /// <returns>限制后的值。</returns>
    public static double Clamp(double value, double min, double max)
        => Math.Max(min, Math.Min(max, value));

    /// <summary>
    /// 将整数值限制在指定范围内。
    /// </summary>
    public static int Clamp(int value, int min, int max)
        => Math.Max(min, Math.Min(max, value));
}
