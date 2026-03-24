using System.Collections.Generic;
using UnityEngine;

public static class StrokeSmoothing2D
{
    /// <summary>
    /// Simple moving-average smoothing. Keeps endpoints unchanged.
    /// windowSize must be odd: 3,5,7...
    /// iterations 1-3 recommended.
    /// </summary>
    public static List<Vector2> SmoothMovingAverage(List<Vector2> points, int windowSize = 5, int iterations = 2)
    {
        if (points == null) return null;
        if (points.Count < 5) return new List<Vector2>(points);

        windowSize = Mathf.Max(3, windowSize);
        if (windowSize % 2 == 0) windowSize += 1;

        iterations = Mathf.Clamp(iterations, 1, 6);

        var src = new List<Vector2>(points);
        var dst = new List<Vector2>(points.Count);

        int half = windowSize / 2;

        for (int it = 0; it < iterations; it++)
        {
            dst.Clear();

            for (int i = 0; i < src.Count; i++)
            {
                // Keep endpoints (important for recognizer stability)
                if (i == 0 || i == src.Count - 1)
                {
                    dst.Add(src[i]);
                    continue;
                }

                int start = Mathf.Max(0, i - half);
                int end = Mathf.Min(src.Count - 1, i + half);

                Vector2 sum = Vector2.zero;
                int count = 0;

                for (int k = start; k <= end; k++)
                {
                    sum += src[k];
                    count++;
                }

                dst.Add(sum / Mathf.Max(1, count));
            }

            // swap
            var tmp = src;
            src = dst;
            dst = tmp;
        }

        return src;
    }
}