using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandDrawController : MonoBehaviour
{
    [Header("Pencil")]
    [SerializeField] private Transform pencilTip;
    [SerializeField] private PencilContactDetector contactDetector;

    [Header("Rendering")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float strokeWidth = 0.01f;
    [SerializeField] private int tubeSides = 8;

    [Header("Stroke Settings")]
    [SerializeField] private float minPointDistance = 0.008f;

    [Header("Evaluation")]
    [SerializeField] private KanaEvaluator evaluator;
    [SerializeField] private int minPointsToAcceptStroke = 8;

    [Header("Kana Template Recorder")]
    [SerializeField] private KanaTemplateRecorder kanaRecorder;

    [Header("Ghost Overlay")]
    [SerializeField] private KanaGhostOverlay ghost;

    [Header("Wrist Menu Blocking")]
    [SerializeField] private WristMenuBlocker wristMenuBlocker;

    [Header("Evaluation Colors")]
    [SerializeField] private Color correctColor = new Color(0.1f, 0.9f, 0.2f);
    [SerializeField] private Color almostColor = new Color(1f, 0.8f, 0.1f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.2f, 0.2f);

    [Header("Cleanup")]
    [SerializeField] private float strokeLifetime = 2.5f;
    [SerializeField] private bool useFadeAndDestroy = false;

    private readonly List<TubeRenderer> tubes = new();
    private readonly List<List<Vector3>> currentKanaStrokes = new();
    private readonly List<TubeRenderer> currentKanaTubes = new();

    private TubeRenderer currentTube;
    private StrokeRecorder recorder;

    private bool wasTouching;

    void Awake()
    {
        recorder = new StrokeRecorder(minPointDistance);
        NewTube();

        if (evaluator != null)
            evaluator.OnEvaluated += HandleEvaluation;
    }

    void OnDestroy()
    {
        if (evaluator != null)
            evaluator.OnEvaluated -= HandleEvaluation;
    }

    void Update()
    {
        if (pencilTip == null || contactDetector == null) return;

        if (!contactDetector.IsTouching)
        {
            EndStrokeIfNeeded();
            return;
        }

        if (!wasTouching)
        {
            recorder.BeginStroke();

            if (ghost != null)
                ghost.gameObject.SetActive(false);

            wristMenuBlocker?.SetDrawingBlocked(true);
        }

        if (recorder.TryAddPoint(contactDetector.ContactPoint))
        {
            if (recorder.CurrentStroke != null && recorder.CurrentStroke.Count >= 2)
                currentTube.SetPositions(recorder.CurrentStroke.ToArray());
        }

        wasTouching = true;
    }

    private void EndStrokeIfNeeded()
    {
        if (!wasTouching) return;

        List<Vector3> finishedStroke = null;
        if (recorder.CurrentStroke != null && recorder.CurrentStroke.Count > 0)
            finishedStroke = new List<Vector3>(recorder.CurrentStroke);

        bool strokeAccepted = recorder.EndStroke(minPointsToAcceptStroke);

        if (strokeAccepted && finishedStroke != null)
        {
            currentKanaStrokes.Add(finishedStroke);
            kanaRecorder?.SetLastFinishedKanaStrokes(currentKanaStrokes);

            if (evaluator != null && evaluator.IsReadyToEvaluate(currentKanaStrokes.Count))
            {
                evaluator.EvaluateWorldStrokes(currentKanaStrokes);
                currentKanaStrokes.Clear();
            }
        }

        NewTube();
        wasTouching = false;

        if (ghost != null)
            ghost.gameObject.SetActive(true);

        wristMenuBlocker?.SetDrawingBlocked(false);
    }

    private void NewTube()
    {
        var go = new GameObject($"StrokeTube_{tubes.Count}");
        go.transform.SetParent(transform, true);

        var tr = go.AddComponent<TubeRenderer>();
        tr.InitIfNeeded();

        tubes.Add(tr);
        currentKanaTubes.Add(tr);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && lineMaterial != null)
            mr.material = lineMaterial;

        tr._radiusOne = strokeWidth;
        tr._radiusTwo = strokeWidth;
        tr._sides = tubeSides;

        tr.SetPositions(System.Array.Empty<Vector3>());
        currentTube = tr;
    }

    private void HandleEvaluation(float score, bool correct)
    {
        if (currentKanaTubes == null || currentKanaTubes.Count == 0) return;

        Color targetColor =
            correct ? correctColor :
            score > 0.60f ? almostColor :
            wrongColor;

        foreach (var tube in currentKanaTubes)
        {
            if (tube == null) continue;

            var renderer = tube.GetComponent<MeshRenderer>();
            if (renderer == null) continue;

            renderer.material.color = targetColor;

            if (useFadeAndDestroy)
                StartCoroutine(FadeAndDestroy(tube, strokeLifetime));
            else
                Destroy(tube.gameObject, strokeLifetime);
        }
    }

    private IEnumerator FadeAndDestroy(TubeRenderer tube, float duration)
    {
        if (tube == null) yield break;

        var renderer = tube.GetComponent<MeshRenderer>();
        if (renderer == null) yield break;

        var material = renderer.material;
        if (material == null) yield break;

        float t = 0f;
        Color startColor = material.color;

        while (t < duration)
        {
            if (tube == null) yield break;

            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / duration);

            Color c = startColor;
            c.a = alpha;
            material.color = c;

            yield return null;
        }

        if (tube != null)
            Destroy(tube.gameObject);
    }

    public void ClearCurrentKanaData()
    {
        currentKanaStrokes.Clear();
        currentKanaTubes.Clear();
    }

    public void ClearAllDrawnTubes()
    {
        foreach (var tube in tubes)
        {
            if (tube != null)
                Destroy(tube.gameObject);
        }

        tubes.Clear();
        currentKanaTubes.Clear();
        currentTube = null;

        NewTube();
    }
}