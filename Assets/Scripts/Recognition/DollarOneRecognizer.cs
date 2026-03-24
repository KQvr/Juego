using System.Collections.Generic;
using UnityEngine;

public class DollarOneRecognizer
{
    public struct Result
    {
        public string name;
        public float score;                 // 0..1 (mayor = mejor)
        public float distance;              // menor = mejor
        public List<Vector2> matchedTemplate;
    }

    private const int NumPoints = 64;
    private const float SquareSize = 1.0f;
    private static readonly Vector2 Origin = Vector2.zero;

    private class Template
    {
        public string Name;
        public List<Vector2> Points;
    }

    private readonly List<Template> _templates = new();

    public void Clear() => _templates.Clear();

    public void AddTemplate(string name, List<Vector2> rawPoints)
    {
        if (rawPoints == null || rawPoints.Count < 8) return;

        var pts = Normalize(rawPoints);
        _templates.Add(new Template
        {
            Name = name,
            Points = pts
        });
    }

    public Result Recognize(List<Vector2> rawPoints)
    {
        if (rawPoints == null || rawPoints.Count < 8 || _templates.Count == 0)
        {
            return new Result
            {
                name = "Unknown",
                score = 0f,
                distance = float.PositiveInfinity,
                matchedTemplate = null
            };
        }

        var candidate = Normalize(rawPoints);

        float bestDist = float.PositiveInfinity;
        string bestName = "Unknown";
        List<Vector2> bestTemplate = null;

        foreach (var t in _templates)
        {
            float d = PathDistance(candidate, t.Points);
            if (d < bestDist)
            {
                bestDist = d;
                bestName = t.Name;
                bestTemplate = t.Points;
            }
        }

        // Normalización de score
        // Diagonal completa del cuadrado 1x1 = sqrt(2)
        // Usamos media diagonal como referencia típica en $1
        float halfDiag = 0.5f * Mathf.Sqrt(SquareSize * SquareSize + SquareSize * SquareSize);

        float score = Mathf.Clamp01(1f - (bestDist / halfDiag));

        return new Result
        {
            name = bestName,
            score = score,
            distance = bestDist,
            matchedTemplate = bestTemplate
        };
    }

    // =========================================================
    // Normalization pipeline
    // =========================================================
    private static List<Vector2> Normalize(List<Vector2> points)
    {
        var pts = Resample(points, NumPoints);

        // OJO:
        // NO rotamos el trazo para conservar orientación real
        

        pts = ScaleTo(pts, SquareSize);
        pts = TranslateTo(pts, Origin);

        return pts;
    }

    private static List<Vector2> Resample(List<Vector2> points, int n)
    {
        float pathLength = PathLength(points);
        if (pathLength <= Mathf.Epsilon)
            return new List<Vector2>(points);

        float interval = pathLength / (n - 1);
        float distanceAccum = 0f;

        var newPoints = new List<Vector2>(n) { points[0] };

        for (int i = 1; i < points.Count; i++)
        {
            float d = Vector2.Distance(points[i - 1], points[i]);

            if ((distanceAccum + d) >= interval)
            {
                Vector2 first = points[i - 1];

                while ((distanceAccum + d) >= interval)
                {
                    float t = (interval - distanceAccum) / d;
                    Vector2 q = Vector2.Lerp(first, points[i], t);

                    newPoints.Add(q);

                    first = q;
                    d = Vector2.Distance(first, points[i]);
                    distanceAccum = 0f;

                    if (newPoints.Count == n)
                        break;
                }

                distanceAccum = d;
            }
            else
            {
                distanceAccum += d;
            }
        }

        // Si quedó corto por precisión numérica, rellena con el último punto
        while (newPoints.Count < n)
            newPoints.Add(points[^1]);

        return newPoints;
    }

    private static List<Vector2> ScaleTo(List<Vector2> points, float size)
    {
        Rect box = BoundingBox(points);

        float scale = Mathf.Max(box.width, box.height);
        if (scale < 1e-6f) scale = 1f;

        var newPoints = new List<Vector2>(points.Count);
        foreach (var p in points)
        {
            float x = (p.x - box.xMin) / scale * size;
            float y = (p.y - box.yMin) / scale * size;
            newPoints.Add(new Vector2(x, y));
        }

        return newPoints;
    }

    private static List<Vector2> TranslateTo(List<Vector2> points, Vector2 target)
    {
        Vector2 c = Centroid(points);

        var newPoints = new List<Vector2>(points.Count);
        foreach (var p in points)
            newPoints.Add(p + (target - c));

        return newPoints;
    }

    // =========================================================
    // Utility
    // =========================================================
    private static float PathDistance(List<Vector2> a, List<Vector2> b)
    {
        float d = 0f;
        int count = Mathf.Min(a.Count, b.Count);

        for (int i = 0; i < count; i++)
            d += Vector2.Distance(a[i], b[i]);

        return d / count;
    }

    private static float PathLength(List<Vector2> points)
    {
        float d = 0f;
        for (int i = 1; i < points.Count; i++)
            d += Vector2.Distance(points[i - 1], points[i]);

        return d;
    }

    private static Vector2 Centroid(List<Vector2> points)
    {
        float x = 0f;
        float y = 0f;

        foreach (var p in points)
        {
            x += p.x;
            y += p.y;
        }

        return new Vector2(x / points.Count, y / points.Count);
    }

    private static Rect BoundingBox(List<Vector2> points)
    {
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        foreach (var p in points)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }
}