using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BasketActivityManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BasketActivitySequenceSO sequence;

    [Header("References")]
    [SerializeField] private BasketReceiver basketReceiver;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text japaneseText;

    [Header("Object Labels (slot-based)")]
    [SerializeField] private TMP_Text[] objectLabels = new TMP_Text[3];

    [Header("Objects Pool")]
    [SerializeField] private List<ResettableObject> allObjects = new();

    [Header("Respawn Points")]
    [SerializeField] private List<Transform> sharedRespawnPoints = new();

    [Header("Flow")]
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private int visibleObjectsPerRound = 3;

    private readonly List<ResettableObject> activeRoundObjects = new();

    private int currentIndex = 0;
    private bool locked = false;

    void Start()
    {
        if (basketReceiver != null)
            basketReceiver.OnItemDropped += HandleItemDropped;

        SetupRoundObjects();
        ShowCurrentPrompt();
    }

    void OnDestroy()
    {
        if (basketReceiver != null)
            basketReceiver.OnItemDropped -= HandleItemDropped;
    }

    private void HandleItemDropped(BasketCollectible droppedItem)
    {
        if (locked) return;
        if (sequence == null || sequence.items == null || sequence.items.Count == 0) return;
        if (droppedItem == null) return;

        var current = sequence.items[currentIndex];
        bool correct = droppedItem.ItemId == current.itemId;

        StopAllCoroutines();
        StartCoroutine(HandleResult(correct, droppedItem));
    }

    private IEnumerator HandleResult(bool correct, BasketCollectible droppedItem)
    {
        locked = true;

        if (feedbackText != null)
        {
            feedbackText.text = correct
                ? $" Correcto: {droppedItem.DisplayName}"
                : $" Incorrecto: {droppedItem.DisplayName}";
        }

        var resettable = droppedItem.GetComponent<ResettableObject>();

        if (correct)
        {
            droppedItem.gameObject.SetActive(false);
            yield return new WaitForSeconds(feedbackDuration);
            Advance();
        }
        else
        {
            yield return new WaitForSeconds(feedbackDuration);

            if (resettable != null)
                resettable.RespawnNow();

            if (feedbackText != null)
                feedbackText.text = "";
        }

        locked = false;
    }

    private void Advance()
    {
        currentIndex++;

        if (currentIndex >= sequence.items.Count)
        {
            if (loopSequence)
                currentIndex = 0;
            else
                currentIndex = sequence.items.Count - 1;
        }

        if (feedbackText != null)
            feedbackText.text = "";

        SetupRoundObjects();
        ShowCurrentPrompt();
    }

    private void ShowCurrentPrompt()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0)
        {
            if (promptText != null)
                promptText.text = "Sin actividad";

            if (japaneseText != null)
                japaneseText.text = "";

            return;
        }

        var current = sequence.items[currentIndex];

        if (promptText != null)
            promptText.text = current.promptText;

        if (japaneseText != null)
            japaneseText.text = current.japaneseText;
    }

    private void SetupRoundObjects()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0) return;
        if (allObjects == null || allObjects.Count == 0) return;
        if (sharedRespawnPoints == null || sharedRespawnPoints.Count < visibleObjectsPerRound)
        {
            Debug.LogWarning($"[BasketActivityManager] Se necesitan al menos {visibleObjectsPerRound} respawn points.");
            return;
        }

        if (objectLabels != null && objectLabels.Length < visibleObjectsPerRound)
        {
            Debug.LogWarning($"[BasketActivityManager] Hay menos labels ({objectLabels.Length}) que objetos visibles por ronda ({visibleObjectsPerRound}).");
        }

        activeRoundObjects.Clear();
        ClearAllObjectLabels();

        foreach (var obj in allObjects)
        {
            if (obj != null)
                obj.gameObject.SetActive(false);
        }

        var current = sequence.items[currentIndex];

        ResettableObject correctObject = null;
        List<ResettableObject> distractorsPool = new();

        foreach (var obj in allObjects)
        {
            if (obj == null) continue;

            var collectible = obj.GetComponent<BasketCollectible>();
            if (collectible == null) continue;

            if (collectible.ItemId == current.itemId && correctObject == null)
                correctObject = obj;
            else
                distractorsPool.Add(obj);
        }

        if (correctObject == null)
        {
            Debug.LogWarning($"[BasketActivityManager] No se encontró objeto correcto para itemId={current.itemId}");
            return;
        }

        Shuffle(distractorsPool);

        activeRoundObjects.Add(correctObject);

        for (int i = 0; i < distractorsPool.Count && activeRoundObjects.Count < visibleObjectsPerRound; i++)
            activeRoundObjects.Add(distractorsPool[i]);

        if (activeRoundObjects.Count < visibleObjectsPerRound)
        {
            Debug.LogWarning($"[BasketActivityManager] No hay suficientes objetos para completar {visibleObjectsPerRound} visibles.");
        }

        List<int> pointIndices = new List<int>();
        for (int i = 0; i < sharedRespawnPoints.Count; i++)
            pointIndices.Add(i);

        Shuffle(pointIndices);

        for (int i = 0; i < activeRoundObjects.Count; i++)
        {
            var obj = activeRoundObjects[i];
            if (obj == null) continue;

            int pointIndex = pointIndices[i];
            var point = sharedRespawnPoints[pointIndex];

            obj.SetRespawnPoint(point);
            obj.RespawnNow();

            UpdateObjectLabel(pointIndex, obj);
        }
    }

    private void UpdateObjectLabel(int slotIndex, ResettableObject obj)
    {
        if (objectLabels == null) return;
        if (slotIndex < 0 || slotIndex >= objectLabels.Length) return;
        if (objectLabels[slotIndex] == null) return;
        if (obj == null) return;

        var collectible = obj.GetComponent<BasketCollectible>();
        if (collectible == null)
        {
            objectLabels[slotIndex].text = "";
            return;
        }

        objectLabels[slotIndex].text = collectible.GetFullLabel();
    }

    private void ClearAllObjectLabels()
    {
        if (objectLabels == null) return;

        for (int i = 0; i < objectLabels.Length; i++)
        {
            if (objectLabels[i] != null)
                objectLabels[i].text = "";
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}