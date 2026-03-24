using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class KanaTemplateRecorder : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private BoardProjector projector;
    [SerializeField] private KanaTemplateSet templateSet;

    [Header("Template")]
    [SerializeField] private string label = "し";
    [SerializeField] private int expectedStrokeCount = 1;
    [SerializeField] private int minPointsPerStroke = 8;

    [Header("Runtime Export (APK)")]
    [SerializeField] private bool exportJsonInBuild = true;

    private List<List<Vector3>> lastFinishedKanaStrokes;

    public void SetLastFinishedKanaStrokes(List<List<Vector3>> kanaStrokes)
    {
        if (kanaStrokes == null)
        {
            lastFinishedKanaStrokes = null;
            return;
        }

        lastFinishedKanaStrokes = new List<List<Vector3>>();
        foreach (var stroke in kanaStrokes)
        {
            if (stroke == null) continue;
            lastFinishedKanaStrokes.Add(new List<Vector3>(stroke));
        }
    }

    [ContextMenu("Capture Kana Template")]
    public void TryCaptureTemplate()
    {
        if (projector == null || projector.BoardTransform == null)
        {
            Debug.LogWarning("[KanaTemplateRecorder] Falta BoardProjector.");
            return;
        }

        if (templateSet == null)
        {
            Debug.LogWarning("[KanaTemplateRecorder] Falta KanaTemplateSet.");
            return;
        }

        if (lastFinishedKanaStrokes == null || lastFinishedKanaStrokes.Count == 0)
        {
            Debug.LogWarning("[KanaTemplateRecorder] No hay strokes capturados.");
            return;
        }

        if (lastFinishedKanaStrokes.Count != expectedStrokeCount)
        {
            Debug.LogWarning($"[KanaTemplateRecorder] Se esperaban {expectedStrokeCount} strokes, pero llegaron {lastFinishedKanaStrokes.Count}.");
            return;
        }

        var kanaTemplate = new KanaTemplateSet.KanaTemplate
        {
            label = label,
            strokes = new List<KanaTemplateSet.Stroke2D>()
        };

        for (int i = 0; i < lastFinishedKanaStrokes.Count; i++)
        {
            var worldStroke = lastFinishedKanaStrokes[i];

            if (worldStroke == null || worldStroke.Count < minPointsPerStroke)
            {
                Debug.LogWarning($"[KanaTemplateRecorder] El stroke {i + 1} no tiene suficientes puntos.");
                return;
            }

            var stroke2D = new List<Vector2>(worldStroke.Count);
            foreach (var p in worldStroke)
                stroke2D.Add(projector.WorldToBoard2D(p));

            kanaTemplate.strokes.Add(new KanaTemplateSet.Stroke2D
            {
                points = stroke2D
            });
        }

        templateSet.templates.Add(kanaTemplate);

#if UNITY_EDITOR
        EditorUtility.SetDirty(templateSet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($" Template guardado: '{label}' con {kanaTemplate.strokes.Count} stroke(s).");
#else
        Debug.Log($" Template capturado en runtime: '{label}' con {kanaTemplate.strokes.Count} stroke(s).");
#endif

        if (!Application.isEditor && exportJsonInBuild)
            ExportLastTemplateToJson(kanaTemplate);
    }

    [Serializable]
    private class JsonStroke
    {
        public List<Vector2> points = new();
    }

    [Serializable]
    private class JsonKanaTemplate
    {
        public string label;
        public List<JsonStroke> strokes = new();
    }

    private void ExportLastTemplateToJson(KanaTemplateSet.KanaTemplate tpl)
    {
        try
        {
            var jsonTpl = new JsonKanaTemplate
            {
                label = tpl.label,
                strokes = new List<JsonStroke>()
            };

            foreach (var s in tpl.strokes)
            {
                jsonTpl.strokes.Add(new JsonStroke
                {
                    points = new List<Vector2>(s.points)
                });
            }

            string json = JsonUtility.ToJson(jsonTpl, true);

            string safeLabel = tpl.label.Replace("/", "_");
            string fileName = $"kana_{safeLabel}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string path = Path.Combine(Application.persistentDataPath, fileName);

            File.WriteAllText(path, json);
            Debug.Log($" JSON exportado en: {path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[KanaTemplateRecorder] No se pudo exportar JSON: {e.Message}");
        }
    }
}