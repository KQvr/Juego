using System.Collections.Generic;
using UnityEngine;

public static class StrokeDirectionChecks
{
    public static Vector2 GetOverallDirection(List<Vector2> points)
    {
        if (points == null || points.Count < 2) return Vector2.zero;
        return (points[points.Count - 1] - points[0]).normalized;
    }

    public static Vector2 GetInitialDirection(List<Vector2> points, int step = 4)
    {
        if (points == null || points.Count < step + 1) return Vector2.zero;
        return (points[step] - points[0]).normalized;
    }

    public static Vector2 GetFinalDirection(List<Vector2> points, int step = 4)
    {
        if (points == null || points.Count < step + 1) return Vector2.zero;

        int last = points.Count - 1;
        int prev = last - step;

        return (points[last] - points[prev]).normalized;
    }

    public static float DirectionSimilarity(Vector2 a, Vector2 b)
    {
        if (a == Vector2.zero || b == Vector2.zero) return 0f;
        return Vector2.Dot(a.normalized, b.normalized);
    }
}