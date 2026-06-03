using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Actividad de lectura: muestra un texto con una palabra clave subrayada
/// y hasta 5 objetos 3D. El jugador pincha el objeto correcto para avanzar.
/// Narra el texto automaticamente al mostrarlo y tiene un boton para repetir.
/// </summary>
public class ReadingActivityManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ReadingActivitySequenceSO sequence;

    [Header("UI")]
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button repeatButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float narrationDelay = 0.4f;

    [Header("Objetos del Pool")]
    [SerializeField] private List<ResettableObject> allObjects = new();

    [Header("Respawn Points (maximo 5)")]
    [SerializeField] private List<Transform> respawnPoints = new();

    [Header("Interaccion")]
    [SerializeField] private IndexPinchGate_OVR pinchGate;
    [SerializeField] private IndexTipProvider_OVR indexTipProvider;

    [Header("Progreso")]
    [SerializeField] private BlockActivityTracker activityTracker;

    [Header("Flow")]
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private int maxVisibleObjects = 5;

    private int currentIndex = 0;
    private bool locked = false;
    private readonly List<ResettableObject> activeObjects = new();

    void Start()
    {
        if (repeatButton != null)
            repeatButton.onClick.AddListener(RepeatNarration);

        ShowCurrentItem();
    }

    void OnDestroy()
    {
        if (repeatButton != null)
            repeatButton.onClick.RemoveListener(RepeatNarration);
    }

    void OnEnable()
    {
        // Reconfigurar los objetos cada vez que la actividad se activa,
        // por si el pool compartido cambio de estado en otra actividad.
        if (sequence != null && sequence.items != null && sequence.items.Count > 0)
        {
            if (activityTracker != null && activityTracker.IsCompleted)
                ShowCompletedState();
            else
                ShowCurrentItem();
        }
    }

    void Update()
    {
        if (locked) return;
        if (pinchGate == null || !pinchGate.IsPinchingStrong) return;

        CheckPinchSelection();
    }

    // -------------------------------------------------------------------------
    // Narración
    // -------------------------------------------------------------------------

    private void PlayNarration(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void RepeatNarration()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0) return;
        var clip = sequence.items[currentIndex].narrationClip;
        PlayNarration(clip);
    }

    // -------------------------------------------------------------------------
    // Flujo principal
    // -------------------------------------------------------------------------

    private void ShowCurrentItem()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0)
        {
            if (bodyText != null) bodyText.text = "Sin actividad";
            return;
        }

        var item = sequence.items[currentIndex];

        if (bodyText != null) bodyText.text = item.bodyText;
        if (feedbackText != null) feedbackText.text = "";

        SetupObjects(item);
        StartCoroutine(NarrateAfterDelay(item.narrationClip));
    }

    private IEnumerator NarrateAfterDelay(AudioClip clip)
    {
        yield return new WaitForSeconds(narrationDelay);
        PlayNarration(clip);
    }

    private void CheckPinchSelection()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0) return;
        if (indexTipProvider == null || indexTipProvider.TipTransform == null) return;

        var current = sequence.items[currentIndex];
        Vector3 fingerPos = indexTipProvider.TipTransform.position;

        foreach (var obj in activeObjects)
        {
            if (obj == null) continue;

            var collectible = obj.GetComponent<BasketCollectible>();
            if (collectible == null) continue;

            var col = obj.GetComponent<Collider>();
            if (col == null) continue;

            Vector3 closest = col.ClosestPoint(fingerPos);
            float dist = Vector3.Distance(fingerPos, closest);

            if (dist > 0.04f) continue;

            bool correct = collectible.ItemId == current.correctItemId;
            StopAllCoroutines();
            StartCoroutine(HandleResult(correct, collectible));
            return;
        }
    }

    private IEnumerator HandleResult(bool correct, BasketCollectible selected)
    {
        locked = true;

        if (audioSource != null) audioSource.Stop();

        if (feedbackText != null)
        {
            feedbackText.text = correct
                ? $"!Correcto! {selected.DisplayName}"
                : $"Incorrecto: {selected.DisplayName}";
        }

        yield return new WaitForSeconds(feedbackDuration);

        if (correct)
        {
            Advance();
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = "";
        }

        locked = false;
    }

    public float GetProgress()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0) return 0f;
        return (float)currentIndex / sequence.items.Count;
    }

    private void Advance()
    {
        currentIndex++;

        if (currentIndex >= sequence.items.Count)
        {
            if (loopSequence)
            {
                currentIndex = 0;
                activityTracker?.SetCurrentItemIndex(currentIndex);
                activityTracker?.SetProgress(0f);
            }
            else
            {
                currentIndex = sequence.items.Count - 1;
                activityTracker?.SetCurrentItemIndex(currentIndex);
                activityTracker?.MarkAsCompleted();
                ShowCompletedState();
                Debug.Log("[ReadingActivityManager] Secuencia completada.");
                return;
            }
        }
        else
        {
            activityTracker?.SetCurrentItemIndex(currentIndex);
            activityTracker?.SetProgress(GetProgress());
        }

        ShowCurrentItem();
    }

    private void ShowCompletedState()
    {
        locked = true;

        if (feedbackText != null)
            feedbackText.text = "!Actividad completada!";

        if (bodyText != null)
            bodyText.text = "";

        // Ocultar todos los objetos
        foreach (var obj in allObjects)
            if (obj != null) obj.gameObject.SetActive(false);
        activeObjects.Clear();
    }

    // -------------------------------------------------------------------------
    // Setup de objetos
    // -------------------------------------------------------------------------

    private void SetupObjects(ReadingActivityItemData item)
    {
        foreach (var obj in allObjects)
            if (obj != null) obj.gameObject.SetActive(false);

        activeObjects.Clear();

        if (respawnPoints == null || respawnPoints.Count == 0)
        {
            Debug.LogWarning("[ReadingActivityManager] No hay respawn points asignados.");
            return;
        }

        ResettableObject correctObject = null;
        List<ResettableObject> distractors = new();

        foreach (var obj in allObjects)
        {
            if (obj == null) continue;

            var collectible = obj.GetComponent<BasketCollectible>();
            if (collectible == null) continue;

            if (collectible.ItemId == item.correctItemId && correctObject == null)
                correctObject = obj;
            else
                distractors.Add(obj);
        }

        if (correctObject == null)
        {
            Debug.LogWarning($"[ReadingActivityManager] No se encontro objeto con itemId={item.correctItemId}");
            return;
        }

        Shuffle(distractors);

        activeObjects.Add(correctObject);
        for (int i = 0; i < distractors.Count && activeObjects.Count < maxVisibleObjects; i++)
            activeObjects.Add(distractors[i]);

        List<int> pointIndices = new();
        for (int i = 0; i < Mathf.Min(respawnPoints.Count, activeObjects.Count); i++)
            pointIndices.Add(i);
        Shuffle(pointIndices);

        for (int i = 0; i < activeObjects.Count; i++)
        {
            var obj = activeObjects[i];
            if (obj == null) continue;

            var point = respawnPoints[pointIndices[i]];
            obj.SetRespawnPoint(point);
            obj.RespawnNow();
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    public void SetData(ReadingActivitySequenceSO newSequence)
    {
        sequence = newSequence;
        locked = false;

        // Cargar indice persistido (o 0 si no hay tracker)
        int savedIndex = activityTracker != null ? activityTracker.CurrentItemIndex : 0;
        currentIndex = (sequence != null && sequence.items != null && sequence.items.Count > 0)
            ? Mathf.Clamp(savedIndex, 0, sequence.items.Count - 1)
            : 0;

        if (!gameObject.activeInHierarchy) return;

        if (activityTracker != null && activityTracker.IsCompleted)
            ShowCompletedState();
        else
            ShowCurrentItem();
    }

    public void RestartActivity()
    {
        currentIndex = 0;
        locked = false;
        ShowCurrentItem();
    }
}