using System.Collections.Generic;
using UnityEngine;

public class StrokeRecorder
{
    public readonly List<List<Vector3>> Strokes = new();
    public List<Vector3> CurrentStroke { get; private set; }

    private Vector3 lastPoint;
    private bool hasLast;

    private readonly float minDistance;

    public StrokeRecorder(float minDistance)
    {
        this.minDistance = minDistance;
    }

    public void BeginStroke()
    {
        CurrentStroke = new List<Vector3>();
        hasLast = false;
    }

    public bool TryAddPoint(Vector3 p)
    {
        if (!hasLast)
        {
            lastPoint = p;
            hasLast = true;
            CurrentStroke.Add(p);
            return true;
        }

        if (Vector3.Distance(lastPoint, p) < minDistance) return false;

        lastPoint = p;
        CurrentStroke.Add(p);
        return true;
    }

    public bool EndStroke(int minPoints = 10)
    {
        if (CurrentStroke == null || CurrentStroke.Count < minPoints)
        {
            CurrentStroke = null;
            return false;
        }

        Strokes.Add(CurrentStroke);
        CurrentStroke = null;
        return true;
    }

    public void ClearAll()
    {
        Strokes.Clear();
        CurrentStroke = null;
        hasLast = false;
    }
}
