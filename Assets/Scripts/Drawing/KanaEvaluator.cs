using System;
using System.Collections.Generic;
using UnityEngine;

public class KanaEvaluator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private BoardProjector projector;
    [SerializeField] private KanaRecognizer recognizer;

    [Header("Target")]
    [SerializeField] private string targetLabel = "し";
    [SerializeField] private int expectedStrokeCount = 1;

    [Header("Scoring")]
    [SerializeField] private float passScore = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float shapeWeight = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float directionWeight = 0.3f;

    [Header("Smoothing")]
    [SerializeField] private bool smoothStroke = true;
    [SerializeField] private int smoothingWindow = 5;
    [SerializeField] private int smoothingIterations = 2;

    public Action<float, bool> OnEvaluated;

    public void SetTarget(string newLabel, int strokeCount)
    {
        targetLabel = newLabel;
        expectedStrokeCount = strokeCount;
    }

    public bool IsReadyToEvaluate(int currentStrokeCount)
    {
        return currentStrokeCount >= expectedStrokeCount;
    }

    public void EvaluateWorldStrokes(List<List<Vector3>> worldStrokes)
    {
        if (projector == null || recognizer == null)
        {
            Debug.LogWarning("[KanaEvaluator] Missing projector or recognizer.");
            return;
        }

        if (worldStrokes == null || worldStrokes.Count != expectedStrokeCount)
        {
            Debug.LogWarning($"[KanaEvaluator] Expected {expectedStrokeCount} strokes, got {(worldStrokes == null ? 0 : worldStrokes.Count)}.");
            OnEvaluated?.Invoke(0f, false);
            return;
        }

        var strokes2D = new List<List<Vector2>>();

        foreach (var stroke in worldStrokes)
        {
            if (stroke == null || stroke.Count < 8)
            {
                Debug.LogWarning("[KanaEvaluator] One stroke is too short.");
                OnEvaluated?.Invoke(0f, false);
                return;
            }

            var s2d = new List<Vector2>(stroke.Count);
            foreach (var p in stroke)
                s2d.Add(projector.WorldToBoard2D(p));

            if (smoothStroke)
            {
                s2d = StrokeSmoothing2D.SmoothMovingAverage(
                    s2d,
                    smoothingWindow,
                    smoothingIterations
                );
            }

            strokes2D.Add(s2d);
        }

        var result = recognizer.Recognize(strokes2D, targetLabel);

        if (!result.matchedStrokeCount || result.matchedTemplate == null)
        {
            Debug.Log($"[KanaEvaluator] No matching template for '{targetLabel}' with {expectedStrokeCount} stroke(s).");
            OnEvaluated?.Invoke(0f, false);
            return;
        }

        float directionScore = ComputeDirectionScore(strokes2D, result.matchedTemplate);
        float finalScore = (result.shapeScore * shapeWeight) + (directionScore * directionWeight);

        bool correct = result.name == targetLabel && finalScore >= passScore;

        Debug.Log(
            $"[KanaEvaluator] Target={targetLabel} | " +
            $"Shape={result.shapeScore:0.00} | Direction={directionScore:0.00} | Final={finalScore:0.00} | " +
            $"Correct={correct}"
        );

        OnEvaluated?.Invoke(finalScore, correct);
    }

    private float ComputeDirectionScore(List<List<Vector2>> userStrokes, KanaTemplateSet.KanaTemplate template)
    {
        if (userStrokes == null || template == null || template.strokes == null) return 0f;
        if (userStrokes.Count != template.strokes.Count) return 0f;

        float total = 0f;
        int validCount = 0;

        for (int i = 0; i < userStrokes.Count; i++)
        {
            var userStroke = userStrokes[i];
            var templateStroke = template.strokes[i].points;

            if (userStroke == null || templateStroke == null) continue;
            if (userStroke.Count < 5 || templateStroke.Count < 5) continue;

            Vector2 userOverall = StrokeDirectionChecks.GetOverallDirection(userStroke);
            Vector2 tplOverall = StrokeDirectionChecks.GetOverallDirection(templateStroke);

            Vector2 userStart = StrokeDirectionChecks.GetInitialDirection(userStroke);
            Vector2 tplStart = StrokeDirectionChecks.GetInitialDirection(templateStroke);

            Vector2 userEnd = StrokeDirectionChecks.GetFinalDirection(userStroke);
            Vector2 tplEnd = StrokeDirectionChecks.GetFinalDirection(templateStroke);

            float overallSim = StrokeDirectionChecks.DirectionSimilarity(userOverall, tplOverall);
            float startSim = StrokeDirectionChecks.DirectionSimilarity(userStart, tplStart);
            float endSim = StrokeDirectionChecks.DirectionSimilarity(userEnd, tplEnd);

            // de [-1,1] a [0,1]
            overallSim = (overallSim + 1f) * 0.5f;
            startSim = (startSim + 1f) * 0.5f;
            endSim = (endSim + 1f) * 0.5f;

            float strokeDirectionScore =
                (overallSim * 0.4f) +
                (startSim * 0.3f) +
                (endSim * 0.3f);

            total += Mathf.Clamp01(strokeDirectionScore);
            validCount++;
        }

        if (validCount == 0) return 0f;
        return total / validCount;
    }
}