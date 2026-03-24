using System.Collections.Generic;
using UnityEngine;

public class KanaRecognizer : MonoBehaviour
{
    [SerializeField] private KanaTemplateSet templateSet;

    private readonly DollarOneRecognizer recognizer = new();

    public class Result
    {
        public string name;
        public float shapeScore;
        public KanaTemplateSet.KanaTemplate matchedTemplate;
        public bool matchedStrokeCount;
    }

    public Result Recognize(List<List<Vector2>> userStrokes, string targetLabel)
    {
        if (templateSet == null || userStrokes == null || userStrokes.Count == 0)
        {
            return new Result
            {
                name = "Unknown",
                shapeScore = 0f,
                matchedTemplate = null,
                matchedStrokeCount = false
            };
        }

        KanaTemplateSet.KanaTemplate bestTemplate = null;
        float bestScore = 0f;

        foreach (var tpl in templateSet.templates)
        {
            if (tpl == null) continue;
            if (tpl.label != targetLabel) continue;
            if (tpl.strokes == null) continue;
            if (tpl.strokes.Count != userStrokes.Count) continue;

            float totalScore = 0f;
            bool valid = true;

            for (int i = 0; i < userStrokes.Count; i++)
            {
                var tplStroke = tpl.strokes[i].points;
                var userStroke = userStrokes[i];

                if (tplStroke == null || tplStroke.Count < 8 || userStroke == null || userStroke.Count < 8)
                {
                    valid = false;
                    break;
                }

                recognizer.Clear();
                recognizer.AddTemplate(tpl.label, tplStroke);

                var r = recognizer.Recognize(userStroke);
                totalScore += r.score;
            }

            if (!valid) continue;

            float avgScore = totalScore / userStrokes.Count;

            if (avgScore > bestScore)
            {
                bestScore = avgScore;
                bestTemplate = tpl;
            }
        }

        return new Result
        {
            name = bestTemplate != null ? bestTemplate.label : "Unknown",
            shapeScore = bestScore,
            matchedTemplate = bestTemplate,
            matchedStrokeCount = bestTemplate != null
        };
    }
}