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
    [SerializeField] private float surfaceOffset = 0.01f;
    [SerializeField] private float width = 0.004f;
    [SerializeField] private Material lineMaterial;

    [Header("Tamanio")]
    [Tooltip("Tamanio uniforme al que se escala cada kana en coordenadas locales del pizarron.")]
    [SerializeField] private float targetSize = 0.08f;

    [Tooltip("Trazos mas chicos que este tamanio (en world space) usan loop=true como workaround del bug del LineRenderer. Default: 0.05 (5cm).")]
    [SerializeField] private float shortStrokeLoopThreshold = 0.05f;

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
        if (gameObject.activeInHierarchy)
            BuildGhost();
    }

    /// <summary>
    /// Cambia el template set — usado cuando se cambia de bloque.
    /// </summary>
    public void SetTemplateSet(KanaTemplateSet newTemplateSet)
    {
        templateSet = newTemplateSet;
    }

    public void BuildGhost()
    {
        Clear();

        var tpl = FindTemplate(label);
        if (tpl == null || tpl.strokes == null || tpl.strokes.Count == 0) return;
        if (projector == null || projector.BoardTransform == null) return;

        Vector2 kanaCenter = CalculateGlobalBoundsCenter(tpl.strokes);
        float maxExtent = CalculateMaxExtent(tpl.strokes, kanaCenter);
        float scale = (maxExtent > 0f) ? (targetSize / maxExtent) : 1f;

        foreach (var stroke in tpl.strokes)
        {
            if (stroke == null || stroke.points == null || stroke.points.Count < 2)
                continue;

            // Calcular tamanio del trazo (diagonal del AABB despues de escalar)
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in stroke.points)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            float strokeSize = Mathf.Sqrt(
                (maxX - minX) * (maxX - minX) +
                (maxY - minY) * (maxY - minY)
            ) * scale;

            var go = new GameObject("GhostStroke");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.useWorldSpace = true;
            lr.positionCount = 0;
            lr.widthMultiplier = 1f;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;

            // Workaround: trazos cortos (dakuten/handakuten) tienen bug en
            // la malla del LineRenderer. loop=true cierra la malla, dibujando
            // una linea de regreso por el mismo camino que se superpone
            // visualmente. Solo aplicamos para trazos pequenios donde el
            // cierre no es visible.
            lr.loop = strokeSize < shortStrokeLoopThreshold;

            Vector3[] worldPts = new Vector3[stroke.points.Count];

            for (int i = 0; i < stroke.points.Count; i++)
            {
                Vector2 normalized = (stroke.points[i] - kanaCenter) * scale;
                Vector3 w = projector.BoardTransform.TransformPoint(new Vector3(normalized.x, normalized.y, 0f));
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
            // reset visual
            foreach (var lr in lines)
            {
                if (lr == null) continue;
                lr.positionCount = 0;
            }

            // animar stroke por stroke
            foreach (var lr in lines)
            {
                if (lr == null) continue;

                var cache = lr.GetComponent<GhostStrokeCache>();
                if (cache == null || cache.worldPoints == null || cache.worldPoints.Length < 2)
                    continue;

                yield return StartCoroutine(AnimateSingleStroke(lr, cache.worldPoints, strokeDrawDuration));

                if (lr == null)
                    yield break;

                yield return new WaitForSeconds(delayBetweenStrokes);
            }

            if (loopAnimation)
                yield return new WaitForSeconds(delayBeforeLoopRestart);

        } while (loopAnimation);
    }

    private IEnumerator AnimateSingleStroke(LineRenderer lr, Vector3[] points, float duration)
    {
        if (lr == null || points == null || points.Length < 2)
            yield break;

        float t = 0f;

        while (t < duration)
        {
            // Si el LineRenderer ya fue destruido, salir limpio
            if (lr == null)
                yield break;

            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            int count = Mathf.Clamp(
                Mathf.RoundToInt(u * points.Length),
                1,
                points.Length
            );

            if (lr == null)
                yield break;

            if (lr.positionCount != count)
                lr.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                if (lr == null)
                    yield break;

                lr.SetPosition(i, points[i]);
            }

            yield return null;
        }

        if (lr == null)
            yield break;

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

    /// <summary>
    /// Devuelve la mayor dimension (mitad de ancho o alto) del bounding box.
    /// Se usa para escalar el kana a targetSize de forma uniforme.
    /// </summary>
    private float CalculateMaxExtent(List<KanaTemplateSet.Stroke2D> strokes, Vector2 center)
    {
        float maxExtent = 0f;

        foreach (var stroke in strokes)
        {
            if (stroke == null || stroke.points == null) continue;
            foreach (var p in stroke.points)
            {
                float dx = Mathf.Abs(p.x - center.x);
                float dy = Mathf.Abs(p.y - center.y);
                if (dx > maxExtent) maxExtent = dx;
                if (dy > maxExtent) maxExtent = dy;
            }
        }

        return maxExtent;
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