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
    [Header("Progreso")]
    [SerializeField] private BlockActivityTracker activityTracker;

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

    void OnEnable()
    {
        locked = false;
        ShowCurrentWord();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        locked = false;

        foreach (var tile in activeTiles)
            if (tile != null) Destroy(tile.gameObject);

        activeTiles.Clear();

        foreach (var slot in slots)
            if (slot != null) slot.ClearSlot();
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

        if (hintText != null) hintText.text = word.hintText;
        if (romajiText != null) romajiText.text = word.wordRomaji;
        if (feedbackText != null) feedbackText.text = "";

        StartCoroutine(SetupTilesDelayed(word));
    }

    // Espera un frame para que todos los componentes Meta XR terminen su
    // inicialización (Start/OnEnable) antes de posicionar las tiles.
    private IEnumerator SetupTilesDelayed(KanaWordItemData word)
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
            yield break;
        }

        // Mezclar spawn points
        List<int> spawnIndices = new();
        for (int i = 0; i < tileSpawnPoints.Count; i++)
            spawnIndices.Add(i);
        Shuffle(spawnIndices);

        // Instanciar tiles desactivadas
        List<(KanaTile tile, string character, Vector3 pos, Quaternion rot)> pending = new();

        for (int i = 0; i < word.wordKana.Length; i++)
        {
            if (tilePrefab == null)
            {
                Debug.LogWarning("[KanaOrderingActivityManager] Falta el tilePrefab.");
                yield break;
            }

            int spawnIdx = spawnIndices[i % spawnIndices.Count];
            Transform spawnPoint = tileSpawnPoints[spawnIdx];

            // Instanciar desactivado para que Start/OnEnable no corran todavía
            var tile = Instantiate(tilePrefab, spawnPoint.position, spawnPoint.rotation);
            tile.gameObject.SetActive(false);
            activeTiles.Add(tile);

            pending.Add((tile, word.wordKana[i].ToString(), spawnPoint.position, spawnPoint.rotation));
        }

        // Esperar un frame: en este punto Awake ya corrió pero Start/OnEnable no
        yield return null;

        // Ahora activar e inicializar — nuestro código corre después del SDK
        foreach (var (tile, character, pos, rot) in pending)
        {
            if (tile == null) continue;
            tile.gameObject.SetActive(true);

            // Esperar otro frame para que el OnEnable del SDK termine
            yield return null;

            tile.Initialize(character, pos, rot);
        }
    }

    private void CheckIfComplete()
    {
        if (locked) return;
        if (sequence == null || sequence.words == null || sequence.words.Count == 0) return;

        var word = sequence.words[currentIndex];
        int wordLength = word.wordKana.Length;

        for (int i = 0; i < wordLength; i++)
        {
            if (i >= slots.Count || slots[i] == null || slots[i].OccupiedBy == null)
                return;
        }

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
            feedbackText.text = correct ? "¡Correcto!" : "Intenta de nuevo";

        yield return new WaitForSeconds(feedbackDuration);

        if (correct)
        {
            Advance();
        }
        else
        {
            foreach (var slot in slots)
                if (slot != null) slot.ClearSlot();

            foreach (var tile in activeTiles)
                if (tile != null) tile.Respawn();

            if (feedbackText != null)
                feedbackText.text = "";
        }

        locked = false;
    }

    public float GetProgress()
    {
        if (sequence == null || sequence.words == null || sequence.words.Count == 0) return 0f;
        return (float)currentIndex / sequence.words.Count;
    }

    private void Advance()
    {
        currentIndex++;

        activityTracker?.SetProgress(GetProgress());

        if (currentIndex >= sequence.words.Count)
        {
            if (loopSequence)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex = sequence.words.Count - 1;
                activityTracker?.MarkAsCompleted();

                if (feedbackText != null)
                    feedbackText.text = "!Actividad completada!";

                Debug.Log("[KanaOrderingActivityManager] Secuencia completada.");
                return;
            }
        }

        ShowCurrentWord();
    }

    public void SetData(KanaWordSequenceSO newSequence)
    {
        sequence = newSequence;
        currentIndex = 0;
        locked = false;
        if (gameObject.activeInHierarchy)
            ShowCurrentWord();
        // else: OnEnable() lo llamara cuando el GameObject se active
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
