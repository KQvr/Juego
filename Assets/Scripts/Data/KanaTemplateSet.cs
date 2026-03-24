using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Kana/Template Set", fileName = "KanaTemplateSet")]
public class KanaTemplateSet : ScriptableObject
{
    [Serializable]
    public class Stroke2D
    {
        public List<Vector2> points = new();
    }

    [Serializable]
    public class KanaTemplate
    {
        public string label;
        public List<Stroke2D> strokes = new();
    }

    public List<KanaTemplate> templates = new();
}