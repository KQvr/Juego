using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KanaLessonManager : MonoBehaviour
{
    [System.Serializable]
    public class KanaLessonItem
    {
        public string label = "あ";
        public int strokeCount = 1;
    }

    [Header("Lesson Source")]
    [SerializeField] private KanaTemplateSet templateSet;
    [SerializeField] private bool buildSequenceFromTemplateSet = true;

    [Header("Manual Sequence (used only if buildSequenceFromTemplateSet = false)")]
    [SerializeField] private List<KanaLessonItem> kanaSequence = new();

    [Header("References")]
    [SerializeField] private KanaEvaluator evaluator;
    [SerializeField] private KanaGhostOverlay ghostOverlay;
    [SerializeField] private TMP_Text kanaText;
    [SerializeField] private HandDrawController handDrawController;

    [Header("Flow")]
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private float advanceDelay = 1.0f;

    [Header("Text")]
    [SerializeField] private string textPrefix = "Escribe: ";
    [SerializeField] private string completedText = " Lección completada";

    private int currentIndex = 0;
    private bool advancing = false;

    void Start()
    {
        if (buildSequenceFromTemplateSet)
            RebuildSequenceFromTemplateSet();

        if (evaluator != null)
            evaluator.OnEvaluated += HandleEvaluation;

        ApplyCurrentKana();
    }

    void OnDestroy()
    {
        if (evaluator != null)
            evaluator.OnEvaluated -= HandleEvaluation;
    }

    [ContextMenu("Rebuild Sequence From TemplateSet")]
    public void RebuildSequenceFromTemplateSet()
    {
        kanaSequence.Clear();

        if (templateSet == null || templateSet.templates == null || templateSet.templates.Count == 0)
        {
            Debug.LogWarning("[KanaLessonManager] No hay templates en KanaTemplateSet.");
            return;
        }

        HashSet<string> seen = new();

        foreach (var tpl in templateSet.templates)
        {
            if (tpl == null) continue;
            if (string.IsNullOrWhiteSpace(tpl.label)) continue;
            if (tpl.strokes == null || tpl.strokes.Count == 0) continue;

            if (seen.Contains(tpl.label))
                continue;

            seen.Add(tpl.label);

            kanaSequence.Add(new KanaLessonItem
            {
                label = tpl.label,
                strokeCount = tpl.strokes.Count
            });
        }

        Debug.Log($"[KanaLessonManager] Secuencia reconstruida con {kanaSequence.Count} kana(s).");
    }

    private void HandleEvaluation(float score, bool correct)
    {
        if (!correct || advancing) return;
        StartCoroutine(AdvanceNextAfterDelay());
    }

    private IEnumerator AdvanceNextAfterDelay()
    {
        advancing = true;
        yield return new WaitForSeconds(advanceDelay);

        currentIndex++;

        if (currentIndex >= kanaSequence.Count)
        {
            if (loopSequence)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex = Mathf.Max(0, kanaSequence.Count - 1);
                UpdateKanaText(completedText);
                Debug.Log("Lección completada.");
                advancing = false;
                yield break;
            }
        }

        ApplyCurrentKana();
        advancing = false;
    }

    private void ApplyCurrentKana()
    {
        if (kanaSequence == null || kanaSequence.Count == 0)
        {
            UpdateKanaText("Sin kanas");
            Debug.LogWarning("[KanaLessonManager] No hay kanas en la secuencia.");
            return;
        }

        var item = kanaSequence[currentIndex];
        if (item == null)
        {
            Debug.LogWarning("[KanaLessonManager] KanaLessonItem nulo.");
            return;
        }

        evaluator?.SetTarget(item.label, item.strokeCount);
        ghostOverlay?.SetLabel(item.label);

        if (handDrawController != null)
        {
            handDrawController.ClearCurrentKanaData();
            handDrawController.ClearAllDrawnTubes();
        }

        UpdateKanaText($"{textPrefix}{item.label}");
        Debug.Log($" Ahora toca escribir: {item.label} ({item.strokeCount} stroke(s))");
    }

    private void UpdateKanaText(string text)
    {
        if (kanaText != null)
            kanaText.text = text;
    }

    public void RestartLesson()
    {
        currentIndex = 0;
        advancing = false;
        ApplyCurrentKana();
    }

    public void SetLessonIndex(int newIndex)
    {
        if (kanaSequence == null || kanaSequence.Count == 0) return;

        currentIndex = Mathf.Clamp(newIndex, 0, kanaSequence.Count - 1);
        advancing = false;
        ApplyCurrentKana();
    }

    public string GetCurrentKanaLabel()
    {
        if (kanaSequence == null || kanaSequence.Count == 0) return "";
        return kanaSequence[currentIndex].label;
    }

    public int GetCurrentKanaStrokeCount()
    {
        if (kanaSequence == null || kanaSequence.Count == 0) return 0;
        return kanaSequence[currentIndex].strokeCount;
    }
}