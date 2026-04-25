using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KanaOrderingActivityManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private KanaWordSequenceSO sequence;

    [Header("Tile Prefab y Spawn Points")]
    [SerializeField] private KanaTile tilePrefab;
    [SerializeField] private List<Transform> tileSpawnPoints = new();

    [Header("Slots (en orden de izquierda a derecha)")]
    [SerializeField] private List<KanaSlot> slots = new();

    [Header("UI")]
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text romajiText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Flow")]
    [SerializeField] private bool loopSequence = false;
    [SerializeField] private float feedbackDuration = 1.5f;

    private int currentIndex = 0;
    private bool locked = false;
    private readonly List<KanaTile> activeTiles = new();

    void Start()
    {
        foreach (var slot in slots)
            if (slot != null)
                slot.OnSlotChanged += CheckIfComplete;

        ShowCurrentWord();
    }

    void OnDestroy()
    {
        foreach (var slot in slots)
            if (slot != null)
                slot.OnSlotChanged -= CheckIfComplete;
    }

    private void ShowCurrentWord()
    {
        if (sequence == null || sequence.words == null || sequence.words.Count == 0)
        {
            if (hintText != null) hintText.text = "Sin palabras";
            return;
        }

        var word = sequence.words[currentIndex];

        if (hintText != null)    hintText.text    = word.hintText;
        if (romajiText != null)  romajiText.text  = word.wordRomaji;
        if (feedbackText != null) feedbackText.text = "";

        SetupTiles(word);
    }

    private void SetupTiles(KanaWordItemData word)
    {
        // Destruir tiles anteriores
        foreach (var tile in activeTiles)
            if (tile != null) Destroy(tile.gameObject);
        activeTiles.Clear();

        // Limpiar y mostrar solo los slots necesarios
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].ClearSlot();
            slots[i].gameObject.SetActive(i < word.wordKana.Length);
        }

        if (tileSpawnPoints == null || tileSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[KanaOrderingActivityManager] No hay spawn points asignados.");
            return;
        }

        // Mezclar spawn points para posiciones aleatorias
        List<int> spawnIndices = new();
        for (int i = 0; i < tileSpawnPoints.Count; i++)
            spawnIndices.Add(i);
        Shuffle(spawnIndices);

        // Instanciar una tile por cada kana de la palabra
        for (int i = 0; i < word.wordKana.Length; i++)
        {
            if (tilePrefab == null)
            {
                Debug.LogWarning("[KanaOrderingActivityManager] Falta el tilePrefab.");
                break;
            }

            int spawnIdx = spawnIndices[i % spawnIndices.Count];
            Transform spawnPoint = tileSpawnPoints[spawnIdx];

            var tile = Instantiate(tilePrefab);
            tile.Initialize(
                word.wordKana[i].ToString(),
                spawnPoint.position,
                spawnPoint.rotation
            );

            activeTiles.Add(tile);
        }
    }

    private void CheckIfComplete()
    {
        if (locked) return;
        if (sequence == null || sequence.words == null || sequence.words.Count == 0) return;

        var word = sequence.words[currentIndex];
        int wordLength = word.wordKana.Length;

        // Verificar que todos los slots necesarios estén llenos
        for (int i = 0; i < wordLength; i++)
        {
            if (i >= slots.Count || slots[i] == null || slots[i].OccupiedBy == null)
                return;
        }

        // Construir la respuesta
        string answer = "";
        for (int i = 0; i < wordLength; i++)
            answer += slots[i].OccupiedBy.KanaCharacter;

        bool correct = answer == word.wordKana;

        StopAllCoroutines();
        StartCoroutine(HandleResult(correct));
    }

    private IEnumerator HandleResult(bool correct)
    {
        locked = true;

        if (feedbackText != null)
            feedbackText.text = correct ? "◯ 正解！" : "✕ もう一度";

        yield return new WaitForSeconds(feedbackDuration);

        if (correct)
        {
            Advance();
        }
        else
        {
            // Limpiar slots y regresar tiles a su posición inicial
            foreach (var slot in slots)
                if (slot != null) slot.ClearSlot();

            foreach (var tile in activeTiles)
                if (tile != null) tile.Respawn();

            if (feedbackText != null)
                feedbackText.text = "";
        }

        locked = false;
    }

    private void Advance()
    {
        currentIndex++;

        if (currentIndex >= sequence.words.Count)
        {
            if (loopSequence)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex = sequence.words.Count - 1;

                if (feedbackText != null)
                    feedbackText.text = "完了！";

                Debug.Log("[KanaOrderingActivityManager] Secuencia completada.");
                return;
            }
        }

        ShowCurrentWord();
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
        ShowCurrentWord();
    }
}
