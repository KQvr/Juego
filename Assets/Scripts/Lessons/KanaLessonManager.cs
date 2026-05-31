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
    [SerializeField] private TMP_Text romajiText;
    [SerializeField] private HandDrawController handDrawController;

    [Header("Progreso")]
    [SerializeField] private BlockActivityTracker activityTracker;

    [Header("Flow")]
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private float advanceDelay = 1.0f;

    [Header("Text")]
    [SerializeField] private string textPrefix = "Escribe: ";
    [SerializeField] private string completedText = "Leccion completada";

    [SerializeField] private int _currentIndex = 0;
    private int currentIndex
    {
        get => _currentIndex;
        set
        {
            if (_currentIndex != value)
                Debug.Log($"[KanaLessonManager] currentIndex: {_currentIndex} → {value}\n{System.Environment.StackTrace}");
            _currentIndex = value;
        }
    }
    private bool advancing = false;

    // -----------------------------------------------------------------------
    // Romaji lookup — Hepburn
    // -----------------------------------------------------------------------
    private static readonly Dictionary<string, string> RomajiMap = new()
    {
        {"あ","a"},  {"い","i"},  {"う","u"},  {"え","e"},  {"お","o"},
        {"か","ka"}, {"き","ki"}, {"く","ku"}, {"け","ke"}, {"こ","ko"},
        {"さ","sa"}, {"し","shi"},{"す","su"}, {"せ","se"}, {"そ","so"},
        {"た","ta"}, {"ち","chi"},{"つ","tsu"},{"て","te"}, {"と","to"},
        {"な","na"}, {"に","ni"}, {"ぬ","nu"}, {"ね","ne"}, {"の","no"},
        {"は","ha"}, {"ひ","hi"}, {"ふ","fu"}, {"へ","he"}, {"ほ","ho"},
        {"ま","ma"}, {"み","mi"}, {"む","mu"}, {"め","me"}, {"も","mo"},
        {"や","ya"}, {"ゆ","yu"}, {"よ","yo"},
        {"ら","ra"}, {"り","ri"}, {"る","ru"}, {"れ","re"}, {"ろ","ro"},
        {"わ","wa"}, {"を","wo"}, {"ん","n"},
        {"が","ga"}, {"ぎ","gi"}, {"ぐ","gu"}, {"げ","ge"}, {"ご","go"},
        {"ざ","za"}, {"じ","ji"}, {"ず","zu"}, {"ぜ","ze"}, {"ぞ","zo"},
        {"だ","da"}, {"ぢ","ji"}, {"づ","zu"}, {"で","de"}, {"ど","do"},
        {"ば","ba"}, {"び","bi"}, {"ぶ","bu"}, {"べ","be"}, {"ぼ","bo"},
        {"ぱ","pa"}, {"ぴ","pi"}, {"ぷ","pu"}, {"ぺ","pe"}, {"ぽ","po"},
        {"ア","a"},  {"イ","i"},  {"ウ","u"},  {"エ","e"},  {"オ","o"},
        {"カ","ka"}, {"キ","ki"}, {"ク","ku"}, {"ケ","ke"}, {"コ","ko"},
        {"サ","sa"}, {"シ","shi"},{"ス","su"}, {"セ","se"}, {"ソ","so"},
        {"タ","ta"}, {"チ","chi"},{"ツ","tsu"},{"テ","te"}, {"ト","to"},
        {"ナ","na"}, {"ニ","ni"}, {"ヌ","nu"}, {"ネ","ne"}, {"ノ","no"},
        {"ハ","ha"}, {"ヒ","hi"}, {"フ","fu"}, {"ヘ","he"}, {"ホ","ho"},
        {"マ","ma"}, {"ミ","mi"}, {"ム","mu"}, {"メ","me"}, {"モ","mo"},
        {"ヤ","ya"}, {"ユ","yu"}, {"ヨ","yo"},
        {"ラ","ra"}, {"リ","ri"}, {"ル","ru"}, {"レ","re"}, {"ロ","ro"},
        {"ワ","wa"}, {"ヲ","wo"}, {"ン","n"},
        {"ガ","ga"}, {"ギ","gi"}, {"グ","gu"}, {"ゲ","ge"}, {"ゴ","go"},
        {"ザ","za"}, {"ジ","ji"}, {"ズ","zu"}, {"ゼ","ze"}, {"ゾ","zo"},
        {"ダ","da"}, {"ヂ","ji"}, {"ヅ","zu"}, {"デ","de"}, {"ド","do"},
        {"バ","ba"}, {"ビ","bi"}, {"ブ","bu"}, {"ベ","be"}, {"ボ","bo"},
        {"パ","pa"}, {"ピ","pi"}, {"プ","pu"}, {"ペ","pe"}, {"ポ","po"},
    };

    private static string GetRomaji(string label)
    {
        if (string.IsNullOrEmpty(label)) return "";
        return RomajiMap.TryGetValue(label, out string r) ? r : "";
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

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

    // -----------------------------------------------------------------------
    // Secuencia
    // -----------------------------------------------------------------------

    [ContextMenu("Rebuild Sequence From TemplateSet")]
    public void RebuildSequenceFromTemplateSet()
    {
        kanaSequence.Clear();

        if (templateSet == null || templateSet.templates == null || templateSet.templates.Count == 0)
        {
            Debug.LogWarning("[KanaLessonManager] No hay templates en KanaTemplateSet.");
            return;
        }

        var seen = new HashSet<string>();

        foreach (var tpl in templateSet.templates)
        {
            if (tpl == null || string.IsNullOrWhiteSpace(tpl.label)) continue;
            if (tpl.strokes == null || tpl.strokes.Count == 0) continue;
            if (seen.Contains(tpl.label)) continue;

            seen.Add(tpl.label);
            kanaSequence.Add(new KanaLessonItem
            {
                label = tpl.label,
                strokeCount = tpl.strokes.Count
            });
        }

        Debug.Log($"[KanaLessonManager] Secuencia reconstruida con {kanaSequence.Count} kana(s).");
    }

    // -----------------------------------------------------------------------
    // Evaluacion
    // -----------------------------------------------------------------------

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
                UpdateRomajiText("");
                Debug.Log("[KanaLessonManager] Leccion completada.");
                advancing = false;
                yield break;
            }
        }

        activityTracker?.SetProgress(kanaSequence.Count > 0 ? (float)currentIndex / kanaSequence.Count : 0f);
        ApplyCurrentKana();
        advancing = false;
    }

    // -----------------------------------------------------------------------
    // Aplicar kana actual
    // -----------------------------------------------------------------------

    private void ApplyCurrentKana()
    {
        if (kanaSequence == null || kanaSequence.Count == 0)
        {
            UpdateKanaText("Sin kanas");
            UpdateRomajiText("");
            Debug.LogWarning("[KanaLessonManager] No hay kanas en la secuencia.");
            return;
        }

        var item = kanaSequence[currentIndex];
        if (item == null) return;

        evaluator?.SetTarget(item.label, item.strokeCount);
        ghostOverlay?.SetLabel(item.label);

        if (handDrawController != null)
        {
            handDrawController.ClearCurrentKanaData();
            handDrawController.ClearAllDrawnTubes();
        }

        UpdateKanaText($"{textPrefix}{item.label}");
        UpdateRomajiText(GetRomaji(item.label));

        Debug.Log($"[KanaLessonManager] Ahora toca escribir: {item.label} ({item.strokeCount} trazo(s))");
    }

    private void UpdateKanaText(string text)
    {
        if (kanaText != null) kanaText.text = text;
    }

    private void UpdateRomajiText(string text)
    {
        if (romajiText != null) romajiText.text = text;
    }

    // -----------------------------------------------------------------------
    // API publica
    // -----------------------------------------------------------------------

    public void SetData(KanaTemplateSet newTemplateSet)
    {
        templateSet = newTemplateSet;
        currentIndex = 0;
        advancing = false;
        RebuildSequenceFromTemplateSet();
        if (gameObject.activeInHierarchy)
            ApplyCurrentKana();
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