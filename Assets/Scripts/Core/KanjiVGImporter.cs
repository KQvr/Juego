#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool que importa los trazos de KanjiVG (SVG) a un KanaTemplateSet.
///
/// Uso:
///   1. Descarga KanjiVG desde github.com/KanjiVG/kanjivg/releases
///   2. Extrae el .zip en cualquier carpeta de tu PC
///   3. Abre Tools → KanjiVG Importer
///   4. Asigna el KanaTemplateSet y la carpeta SVG
///   5. Escribe los kana que quieres importar y presiona Import
/// </summary>
public class KanjiVGImporter : EditorWindow
{
    private KanaTemplateSet targetSet;
    private string svgFolderPath = "";
    private string kanaToImport = "あいうえおかきくけこさしすせそたちつてとなにぬねの";
    private int samplesPerCurve = 16;
    private bool skipExisting = true;
    private Vector2 scroll;

    // KanjiVG usa un canvas de 109x109.
    // OUTPUT_SCALE normaliza las coordenadas al espacio del pizarron (~±0.045 unidades).
    private const float SVG_SIZE = 109f;
    private const float OUTPUT_SCALE = 0.09f;

    [MenuItem("Tools/KanjiVG Importer")]
    static void ShowWindow() => GetWindow<KanjiVGImporter>("KanjiVG Importer");

    void OnGUI()
    {
        GUILayout.Label("KanjiVG → KanaTemplateSet", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetSet = (KanaTemplateSet)EditorGUILayout.ObjectField(
            "Target Template Set", targetSet, typeof(KanaTemplateSet), false);

        EditorGUILayout.BeginHorizontal();
        svgFolderPath = EditorGUILayout.TextField("Carpeta SVG", svgFolderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
            svgFolderPath = EditorUtility.OpenFolderPanel("Seleccionar carpeta KanjiVG", svgFolderPath, "");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Kana a importar (pega aqui los que necesitas):");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(70));
        kanaToImport = EditorGUILayout.TextArea(kanaToImport);
        EditorGUILayout.EndScrollView();

        samplesPerCurve = EditorGUILayout.IntSlider("Muestras por curva", samplesPerCurve, 8, 32);
        skipExisting = EditorGUILayout.Toggle("Omitir kana ya importados", skipExisting);

        EditorGUILayout.Space();

        GUI.enabled = targetSet != null &&
                      !string.IsNullOrEmpty(svgFolderPath) &&
                      !string.IsNullOrEmpty(kanaToImport);

        if (GUILayout.Button("Importar", GUILayout.Height(32)))
            Import();

        GUI.enabled = true;
    }

    // -----------------------------------------------------------------------
    // Import
    // -----------------------------------------------------------------------

    void Import()
    {
        int imported = 0, skipped = 0, failed = 0;

        var existingLabels = new HashSet<string>();
        if (skipExisting && targetSet.templates != null)
            foreach (var t in targetSet.templates)
                if (t != null) existingLabels.Add(t.label);

        foreach (char kana in kanaToImport)
        {
            string label = kana.ToString();

            if (skipExisting && existingLabels.Contains(label))
            {
                skipped++;
                continue;
            }

            string filename = $"{(int)kana:x5}.svg";
            string filepath = Path.Combine(svgFolderPath, filename);

            if (!File.Exists(filepath))
            {
                Debug.LogWarning($"[KanjiVGImporter] No encontrado: {filepath}");
                failed++;
                continue;
            }

            var strokes = ParseSVG(filepath);
            if (strokes == null || strokes.Count == 0)
            {
                Debug.LogWarning($"[KanjiVGImporter] Sin trazos validos: {label} ({filepath})");
                failed++;
                continue;
            }

            var template = new KanaTemplateSet.KanaTemplate
            {
                label = label,
                strokes = new List<KanaTemplateSet.Stroke2D>()
            };

            foreach (var pts in strokes)
                template.strokes.Add(new KanaTemplateSet.Stroke2D { points = pts });

            targetSet.templates.Add(template);
            existingLabels.Add(label);
            imported++;
        }

        EditorUtility.SetDirty(targetSet);
        AssetDatabase.SaveAssets();

        Debug.Log($"[KanjiVGImporter] Completado — Importados: {imported} | Omitidos: {skipped} | Fallidos: {failed}");
    }

    // -----------------------------------------------------------------------
    // SVG Parsing
    // -----------------------------------------------------------------------

    List<List<Vector2>> ParseSVG(string filepath)
    {
        var strokes = new SortedDictionary<int, List<Vector2>>();
        string svg = File.ReadAllText(filepath);

        // Buscamos lineas que contengan id="...-sN" y extraemos d="..."
        // Usamos busqueda manual para evitar problemas de encoding/escape.
        string[] lines = svg.Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (!line.StartsWith("<path")) continue;

            // Extraer numero de trazo del id (ej: kvg:03042-s1 -> 1)
            int idIdx = line.IndexOf("id=\"", StringComparison.Ordinal);
            if (idIdx < 0) continue;

            int idStart = idIdx + 4;
            int idEnd = line.IndexOf('"', idStart);
            if (idEnd < 0) continue;

            string idValue = line.Substring(idStart, idEnd - idStart);

            int sIdx = idValue.LastIndexOf("-s", StringComparison.Ordinal);
            if (sIdx < 0) continue;

            string numStr = idValue.Substring(sIdx + 2);
            if (!int.TryParse(numStr, out int strokeNum)) continue;
            if (strokes.ContainsKey(strokeNum)) continue;

            // Extraer atributo d="..."
            int dIdx = line.IndexOf(" d=\"", StringComparison.Ordinal);
            if (dIdx < 0) continue;

            int dStart = dIdx + 4;
            int dEnd = line.IndexOf('"', dStart);
            if (dEnd < 0) continue;

            string pathData = line.Substring(dStart, dEnd - dStart);
            if (string.IsNullOrEmpty(pathData)) continue;

            var pts = SamplePath(pathData);
            if (pts.Count >= 8)
                strokes[strokeNum] = pts;
        }

        Debug.Log($"[KanjiVGImporter] Trazos encontrados: {strokes.Count}");
        return new List<List<Vector2>>(strokes.Values);
    }

    List<Vector2> SamplePath(string d)
    {
        var points = new List<Vector2>();
        var tokens = Tokenize(d);
        var pos = Vector2.zero;
        var lastCtrl = Vector2.zero;
        int i = 0;

        while (i < tokens.Count)
        {
            if (!IsCmd(tokens[i])) { i++; continue; }

            char cmd = tokens[i][0];
            bool rel = char.IsLower(cmd);
            char abs = char.ToUpper(cmd);
            i++;

            switch (abs)
            {
                case 'M':
                    {
                        var p = ReadVec(tokens, ref i);
                        pos = rel ? pos + p : p;
                        points.Add(Normalize(pos));
                        lastCtrl = pos;
                        // Lineas implicitas despues de M
                        while (i < tokens.Count && !IsCmd(tokens[i]))
                        {
                            var p2 = ReadVec(tokens, ref i);
                            var end = rel ? pos + p2 : p2;
                            SampleLine(pos, end, points);
                            pos = end;
                            lastCtrl = pos;
                        }
                        break;
                    }
                case 'L':
                    {
                        while (i < tokens.Count && !IsCmd(tokens[i]))
                        {
                            var p = ReadVec(tokens, ref i);
                            var end = rel ? pos + p : p;
                            SampleLine(pos, end, points);
                            pos = end;
                            lastCtrl = pos;
                        }
                        break;
                    }
                case 'C':
                    {
                        while (i < tokens.Count && !IsCmd(tokens[i]))
                        {
                            var c1 = ReadVec(tokens, ref i); if (rel) c1 += pos;
                            var c2 = ReadVec(tokens, ref i); if (rel) c2 += pos;
                            var ep = ReadVec(tokens, ref i); if (rel) ep += pos;
                            SampleCubic(pos, c1, c2, ep, points);
                            lastCtrl = c2;
                            pos = ep;
                        }
                        break;
                    }
                case 'S': // Cubic bezier suavizado (c1 reflejado del anterior)
                    {
                        while (i < tokens.Count && !IsCmd(tokens[i]))
                        {
                            var c1 = 2 * pos - lastCtrl;
                            var c2 = ReadVec(tokens, ref i); if (rel) c2 += pos;
                            var ep = ReadVec(tokens, ref i); if (rel) ep += pos;
                            SampleCubic(pos, c1, c2, ep, points);
                            lastCtrl = c2;
                            pos = ep;
                        }
                        break;
                    }
                case 'Q':
                    {
                        while (i < tokens.Count && !IsCmd(tokens[i]))
                        {
                            var c1 = ReadVec(tokens, ref i); if (rel) c1 += pos;
                            var ep = ReadVec(tokens, ref i); if (rel) ep += pos;
                            SampleQuadratic(pos, c1, ep, points);
                            lastCtrl = c1;
                            pos = ep;
                        }
                        break;
                    }
                case 'T': // Quadratic bezier suavizado
                    {
                        while (i < tokens.Count && !IsCmd(tokens[i]))
                        {
                            var c1 = 2 * pos - lastCtrl;
                            var ep = ReadVec(tokens, ref i); if (rel) ep += pos;
                            SampleQuadratic(pos, c1, ep, points);
                            lastCtrl = c1;
                            pos = ep;
                        }
                        break;
                    }
                case 'Z':
                    break;
            }
        }

        return points;
    }

    // -----------------------------------------------------------------------
    // Muestreo de segmentos
    // -----------------------------------------------------------------------

    void SampleLine(Vector2 a, Vector2 b, List<Vector2> pts)
    {
        int steps = Mathf.Max(2, samplesPerCurve / 4);
        for (int s = 1; s <= steps; s++)
            pts.Add(Normalize(Vector2.Lerp(a, b, s / (float)steps)));
    }

    void SampleCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, List<Vector2> pts)
    {
        for (int s = 1; s <= samplesPerCurve; s++)
        {
            float t = s / (float)samplesPerCurve;
            float u = 1f - t;
            var p = u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
            pts.Add(Normalize(p));
        }
    }

    void SampleQuadratic(Vector2 p0, Vector2 p1, Vector2 p2, List<Vector2> pts)
    {
        for (int s = 1; s <= samplesPerCurve; s++)
        {
            float t = s / (float)samplesPerCurve;
            float u = 1f - t;
            var p = u * u * p0 + 2 * u * t * p1 + t * t * p2;
            pts.Add(Normalize(p));
        }
    }

    // KanjiVG usa Y-abajo; Unity usa Y-arriba → invertir Y al normalizar
    Vector2 Normalize(Vector2 p)
    {
        float x = (p.x - SVG_SIZE * 0.5f) / SVG_SIZE * OUTPUT_SCALE;
        float y = -(p.y - SVG_SIZE * 0.5f) / SVG_SIZE * OUTPUT_SCALE;
        return new Vector2(x, y);
    }

    // -----------------------------------------------------------------------
    // Tokenizer SVG
    // -----------------------------------------------------------------------

    static List<string> Tokenize(string d)
    {
        var tokens = new List<string>();
        var matches = Regex.Matches(d,
            @"[MmLlCcSsQqTtZz]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?");
        foreach (Match m in matches)
            tokens.Add(m.Value);
        return tokens;
    }

    static bool IsCmd(string s) =>
        s.Length == 1 && char.IsLetter(s[0]);

    static Vector2 ReadVec(List<string> tokens, ref int i)
    {
        float x = float.Parse(tokens[i++], System.Globalization.CultureInfo.InvariantCulture);
        float y = float.Parse(tokens[i++], System.Globalization.CultureInfo.InvariantCulture);
        return new Vector2(x, y);
    }
}
#endif