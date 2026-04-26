using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Actividad de lectura: muestra un texto con una palabra clave subrayada
/// y hasta 5 objetos 3D. El jugador pincha el objeto correcto para avanzar.
///
/// Reutiliza BasketCollectible y ResettableObject del pool de objetos.
/// La detección de selección se hace por pinch (IndexPinchGate_OVR).
/// </summary>
public class ReadingActivityManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ReadingActivitySequenceSO sequence;

    [Header("UI")]
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Objetos del Pool")]
    [SerializeField] private List<ResettableObject> allObjects = new();

    [Header("Respawn Points (máximo 5)")]
    [SerializeField] private List<Transform> respawnPoints = new();

    [Header("Interacción")]
    [SerializeField] private IndexPinchGate_OVR pinchGate;
    [SerializeField] private IndexTipProvider_OVR indexTipProvider;

    [Header("Flow")]
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private int maxVisibleObjects = 5;

    private int currentIndex = 0;
    private bool locked = false;
    private readonly List<ResettableObject> activeObjects = new();

    void Start()
    {
        ShowCurrentItem();
    }

    void Update()
    {
        if (locked) return;
        if (pinchGate == null || !pinchGate.IsPinchingStrong) return;

        // Buscar qué objeto está siendo pinchado (el más cercano a la punta del dedo)
        CheckPinchSelection();
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
            StartCoroutine(HandleResult(correct, collectible, obj));
            return;
        }
    }

    private IEnumerator HandleResult(bool correct, BasketCollectible selected, ResettableObject selectedObj)
    {
        locked = true;

        if (feedbackText != null)
        {
            feedbackText.text = correct
                ? $"¡Correcto! {selected.DisplayName}"
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

    private void Advance()
    {
        currentIndex++;

        if (currentIndex >= sequence.items.Count)
        {
            if (loopSequence)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex = sequence.items.Count - 1;

                if (feedbackText != null)
                    feedbackText.text = "¡Actividad completada!";

                Debug.Log("[ReadingActivityManager] Secuencia completada.");
                return;
            }
        }

        ShowCurrentItem();
    }

    private void ShowCurrentItem()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0)
        {
            if (bodyText != null) bodyText.text = "Sin actividad";
            return;
        }

        var item = sequence.items[currentIndex];

        if (bodyText != null)
            bodyText.text = item.bodyText;

        if (feedbackText != null)
            feedbackText.text = "";

        SetupObjects(item);
    }

    private void SetupObjects(ReadingActivityItemData item)
    {
        // Desactivar todos los objetos del pool
        foreach (var obj in allObjects)
            if (obj != null) obj.gameObject.SetActive(false);

        activeObjects.Clear();

        if (respawnPoints == null || respawnPoints.Count == 0)
        {
            Debug.LogWarning("[ReadingActivityManager] No hay respawn points asignados.");
            return;
        }

        // Separar objeto correcto y distractores
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
            Debug.LogWarning($"[ReadingActivityManager] No se encontró objeto con itemId={item.correctItemId}");
            return;
        }

        Shuffle(distractors);

        activeObjects.Add(correctObject);
        for (int i = 0; i < distractors.Count && activeObjects.Count < maxVisibleObjects; i++)
            activeObjects.Add(distractors[i]);

        // Mezclar posiciones
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

    public void RestartActivity()
    {
        currentIndex = 0;
        locked = false;
        ShowCurrentItem();
    }
}
