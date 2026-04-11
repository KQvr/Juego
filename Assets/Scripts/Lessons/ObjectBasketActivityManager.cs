using System.Collections;
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

    [Header("Flow")]
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private float feedbackDuration = 1.5f;

    private int currentIndex = 0;
    private bool locked = false;

    void Start()
    {
        if (basketReceiver != null)
            basketReceiver.OnItemDropped += HandleItemDropped;

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

        if (correct)
        {
            droppedItem.gameObject.SetActive(false);
            yield return new WaitForSeconds(feedbackDuration);
            Advance();
        }
        else
        {
            var resettable = droppedItem.GetComponent<ResettableObject>();
            yield return new WaitForSeconds(feedbackDuration);

            if (resettable != null)
                resettable.ResetToStart();

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

        ShowCurrentPrompt();
    }

    private void ShowCurrentPrompt()
    {
        if (sequence == null || sequence.items == null || sequence.items.Count == 0)
        {
            if (promptText != null)
                promptText.text = "Sin actividad";
            return;
        }

        var current = sequence.items[currentIndex];

        if (promptText != null)
            promptText.text = current.promptText;
    }
}