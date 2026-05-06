using System.Drawing;

namespace ScaffoldX.Core.Vision;

/// <summary>
/// 将二值掩码转换为多边形轮廓点，使用 Marching Squares 算法提取轮廓，
/// Douglas-Peucker 算法简化点数。
/// </summary>
public static class MaskToPolygonConverter
{
    /// <summary>
    /// 从二值掩码提取多边形轮廓点（归一化坐标 0-1）。
    /// </summary>
    /// <param name="mask">二值掩码（0/1）。</param>
    /// <param name="tolerance">Douglas-Peucker 简化容差（像素），0 表示不简化。</param>
    /// <returns>归一化的轮廓点列表。</returns>
    public static List<PointF> Convert(byte[,] mask, float tolerance = 2.0f)
    {
        var height = mask.GetLength(0);
        var width = mask.GetLength(1);

        if (height == 0 || width == 0)
            return new List<PointF>();

        var contours = ExtractContours(mask, width, height);
        if (contours.Count == 0)
            return new List<PointF>();

        // 取最大轮廓
        var largest = contours.OrderByDescending(c => c.Count).First();

        if (tolerance > 0)
            largest = Simplify(largest, tolerance);

        // 归一化坐标
        return largest.Select(p => new PointF(p.X / width, p.Y / height)).ToList();
    }

    /// <summary>
    /// 使用 Marching Squares 提取所有轮廓。
    /// </summary>
    private static List<List<PointF>> ExtractContours(byte[,] mask, int width, int height)
    {
        var visited = new bool[height, width];
        var contours = new List<List<PointF>>();

        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                if (visited[y, x]) continue;
                if (mask[y, x] == 0) continue;

                var contour = TraceContour(mask, visited, x, y, width, height);
                if (contour.Count >= 3)
                    contours.Add(contour);
            }
        }

        return contours;
    }

    /// <summary>
    /// 从起点追踪轮廓边界。
    /// </summary>
    private static List<PointF> TraceContour(byte[,] mask, bool[,] visited, int startX, int startY, int width, int height)
    {
        var points = new List<PointF>();
        var directions = new (int dx, int dy)[] { (1, 0), (0, 1), (-1, 0), (0, -1) };
        int x = startX, y = startY;
        int dir = 0;
        int maxSteps = width * height;
        int steps = 0;

        do
        {
            if (visited[y, x]) break;
            visited[y, x] = true;
            points.Add(new PointF(x + 0.5f, y + 0.5f));

            bool found = false;
            for (int i = 0; i < 4; i++)
            {
                int newDir = (dir + 3 + i) % 4;
                int nx = x + directions[newDir].dx;
                int ny = y + directions[newDir].dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height && mask[ny, nx] != 0 && !visited[ny, nx])
                {
                    x = nx;
                    y = ny;
                    dir = newDir;
                    found = true;
                    break;
                }
            }

            if (!found) break;
            steps++;
        } while ((x != startX || y != startY) && steps < maxSteps);

        return points;
    }

    /// <summary>
    /// Douglas-Peucker 线简化算法（迭代实现，避免栈溢出）。
    /// </summary>
    public static List<PointF> Simplify(List<PointF> points, float tolerance)
    {
        if (points.Count <= 2)
            return points;

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;

        var stack = new Stack<(int start, int end)>();
        stack.Push((0, points.Count - 1));

        while (stack.Count > 0)
        {
            var (start, end) = stack.Pop();
            float maxDist = 0;
            int maxIndex = start;

            for (int i = start + 1; i < end; i++)
            {
                var dist = PerpendicularDistance(points[i], points[start], points[end]);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    maxIndex = i;
                }
            }

            if (maxDist > tolerance)
            {
                keep[maxIndex] = true;
                if (maxIndex - start > 1) stack.Push((start, maxIndex));
                if (end - maxIndex > 1) stack.Push((maxIndex, end));
            }
        }

        return points.Where((_, i) => keep[i]).ToList();
    }

    private static float PerpendicularDistance(PointF point, PointF lineStart, PointF lineEnd)
    {
        float dx = lineEnd.X - lineStart.X;
        float dy = lineEnd.Y - lineStart.Y;

        if (dx == 0 && dy == 0)
            return Distance(point, lineStart);

        float t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0, 1);

        var projX = lineStart.X + t * dx;
        var projY = lineStart.Y + t * dy;

        return Distance(point, new PointF(projX, projY));
    }

    private static float Distance(PointF a, PointF b)
        => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
