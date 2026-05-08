namespace ScaffoldX.App.Services.FormatExporters;

/// <summary>
/// Shared helper methods for format exporters.
/// </summary>
internal static class FormatExporterHelper
{
    /// <summary>
    /// Computes the 4 corner points of an oriented bounding box in absolute pixel coordinates.
    /// </summary>
    internal static (float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) ComputeObbCorners(
        float centerX, float centerY, float width, float height, float angle,
        int imageWidth, int imageHeight)
    {
        var absCx = CoordinateMapper.ToAbsolute(centerX, imageWidth);
        var absCy = CoordinateMapper.ToAbsolute(centerY, imageHeight);
        var absW = CoordinateMapper.ToAbsolute(width, imageWidth);
        var absH = CoordinateMapper.ToAbsolute(height, imageHeight);

        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);
        var hw = absW / 2;
        var hh = absH / 2;

        return (
            absCx + (-hw * cos - -hh * sin), absCy + (-hw * sin + -hh * cos),
            absCx + (hw * cos - -hh * sin),  absCy + (hw * sin + -hh * cos),
            absCx + (hw * cos - hh * sin),   absCy + (hw * sin + hh * cos),
            absCx + (-hw * cos - hh * sin),  absCy + (-hw * sin + hh * cos)
        );
    }
}
