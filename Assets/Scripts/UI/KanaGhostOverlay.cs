using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KanaGhostOverlay : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private BoardProjector projector;
    [SerializeField] private KanaTemplateSet templateSet;
    [SerializeField] private string label = "し";

    [Header("Visual")]
    [SerializeField] private float surfaceOffset = 0.002f;
    [SerializeField] private float width = 0.004f;
    [SerializeField] private Material lineMaterial;

    [Header("Animation")]
    [SerializeField] private bool animateOnBuild = true;
    [SerializeField] private float strokeDrawDuration = 0.8f;
    [SerializeField] private float delayBetweenStrokes = 0.25f;
    [SerializeField] private bool loopAnimation = true;
    [SerializeField] private float delayBeforeLoopRestart = 0.8f;

    private readonly List<LineRenderer> lines = new();
    private Coroutine animationRoutine;

    void OnEnable()
    {
        BuildGhost();
    }

    void OnDisable()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
    }

    public void SetLabel(string newLabel)
    {
        label = newLabel;
        BuildGhost();
    }

    public void BuildGhost()
    {
        Clear();

        var tpl = FindTemplate(label);
        if (tpl == null || tpl.strokes == null || tpl.strokes.Count == 0) return;
        if (projector == null || projector.BoardTransform == null) return;

        Vector2 kanaCenter = CalculateGlobalBoundsCenter(tpl.strokes);

        foreach (var stroke in tpl.strokes)
        {
            if (stroke == null || stroke.points == null || stroke.points.Count < 2)
                continue;

            var go = new GameObject("GhostStroke");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.widthMultiplier = width;
            lr.useWorldSpace = true;
            lr.positionCount = 0;

            Vector3[] worldPts = new Vector3[stroke.points.Count];

            for (int i = 0; i < stroke.points.Count; i++)
            {
                Vector2 centered = stroke.points[i] - kanaCenter;
                Vector3 w = projector.BoardTransform.TransformPoint(new Vector3(centered.x, centered.y, 0f));
                w += projector.BoardTransform.forward * surfaceOffset;
                worldPts[i] = w;
            }

            lines.Add(lr);
            lr.gameObject.SetActive(true);
            lr.positionCount = animateOnBuild ? 0 : worldPts.Length;

            if (!animateOnBuild)
                lr.SetPositions(worldPts);

            var cache = go.AddComponent<GhostStrokeCache>();
            cache.worldPoints = worldPts;
        }

        if (animateOnBuild)
        {
            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            animationRoutine = StartCoroutine(AnimateStrokeOrder());
        }
    }

    private IEnumerator AnimateStrokeOrder()
    {
        do
        {
            foreach (var lr in lines)
            {
                if (lr == null) continue;
                lr.positionCount = 0;
            }

            foreach (var lr in lines)
            {
                if (lr == null) continue;

                var cache = lr.GetComponent<GhostStrokeCache>();
                if (cache == null || cache.worldPoints == null || cache.worldPoints.Length < 2)
                    continue;

                yield return StartCoroutine(AnimateSingleStroke(lr, cache.worldPoints, strokeDrawDuration));

                if (lr == null) yield break;

                yield return new WaitForSeconds(delayBetweenStrokes);
            }

            if (loopAnimation)
                yield return new WaitForSeconds(delayBeforeLoopRestart);

        } while (loopAnimation);
    }

    // Los objetos Unity solo pueden ser destruidos entre frames, nunca a mitad de uno.
    // Por eso un único null-check después del yield return null es suficiente.
    private IEnumerator AnimateSingleStroke(LineRenderer lr, Vector3[] points, float duration)
    {
        if (lr == null || points == null || points.Length < 2)
            yield break;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            int count = Mathf.Clamp(Mathf.RoundToInt(u * points.Length), 1, points.Length);

            lr.positionCount = count;
            for (int i = 0; i < count; i++)
                lr.SetPosition(i, points[i]);

            yield return null;

            if (lr == null) yield break;
        }

        lr.positionCount = points.Length;
        lr.SetPositions(points);
    }

    public void RestartAnimation()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateStrokeOrder());
    }

    private Vector2 CalculateGlobalBoundsCenter(List<KanaTemplateSet.Stroke2D> strokes)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var stroke in strokes)
        {
            if (stroke == null || stroke.points == null) continue;

            foreach (var p in stroke.points)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
        }

        return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    private KanaTemplateSet.KanaTemplate FindTemplate(string lbl)
    {
        if (templateSet == null) return null;

        foreach (var t in templateSet.templates)
        {
            if (t != null && t.label == lbl)
                return t;
        }

        return null;
    }

    private void Clear()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        foreach (var l in lines)
        {
            if (l != null)
                Destroy(l.gameObject);
        }

        lines.Clear();
    }
}