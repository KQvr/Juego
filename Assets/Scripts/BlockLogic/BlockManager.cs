using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [Header("Bloques (en orden)")]
    [SerializeField] private List<BlockContentSO> blocks = new();

    [Header("Activity Managers")]
    [SerializeField] private KanaLessonManager drawingManager;
    [SerializeField] private BasketActivityManager basketManager;
    [SerializeField] private KanaOrderingActivityManager orderingManager;
    [SerializeField] private ReadingActivityManager readingManager;
    [SerializeField] private ActivityMenuManager activityMenuManager;

    public event Action<string, int> OnBlockStarsChanged;
    public event Action<string> OnBlockUnlocked;

    private readonly Dictionary<string, List<BlockActivityTracker>> activitiesByBlock = new();
    private int currentBlockIndex = -1;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (blocks.Count > 0)
            EnsureUnlocked(blocks[0].blockId);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // -----------------------------------------------------------------------
    // Registro de actividades
    // -----------------------------------------------------------------------

    public void RegisterActivity(BlockActivityTracker tracker)
    {
        if (!activitiesByBlock.ContainsKey(tracker.BlockId))
            activitiesByBlock[tracker.BlockId] = new();

        if (!activitiesByBlock[tracker.BlockId].Contains(tracker))
            activitiesByBlock[tracker.BlockId].Add(tracker);
    }

    public void UnregisterActivity(BlockActivityTracker tracker)
    {
        if (activitiesByBlock.TryGetValue(tracker.BlockId, out var list))
            list.Remove(tracker);
    }

    public void OnActivityProgressChanged(BlockActivityTracker tracker)
    {
        OnBlockStarsChanged?.Invoke(tracker.BlockId, CalculateStars(tracker.BlockId));
    }

    public void OnActivityCompleted(BlockActivityTracker tracker)
    {
        int stars = CalculateStars(tracker.BlockId);
        OnBlockStarsChanged?.Invoke(tracker.BlockId, stars);
        if (stars == 3) TryUnlockNext(tracker.BlockId);
    }

    // -----------------------------------------------------------------------
    // Estrellas
    // -----------------------------------------------------------------------

    public int CalculateStars(string blockId)
    {
        if (!activitiesByBlock.TryGetValue(blockId, out var activities) ||
            activities == null || activities.Count == 0) return 0;

        bool allCompleted = true;
        float totalProgress = 0f;
        bool anyStarted = false;

        foreach (var a in activities)
        {
            if (!a.IsCompleted) allCompleted = false;
            if (a.Progress > 0f) anyStarted = true;
            totalProgress += a.Progress;
        }

        float avg = totalProgress / activities.Count;

        if (allCompleted) return 3;
        if (avg >= 0.5f) return 2;
        if (anyStarted) return 1;
        return 0;
    }

    public int GetStars(string blockId) => CalculateStars(blockId);

    // -----------------------------------------------------------------------
    // Desbloqueo
    // -----------------------------------------------------------------------

    public bool IsUnlocked(string blockId) =>
        PlayerPrefs.GetInt($"block_{blockId}_unlocked", 0) == 1;

    private void EnsureUnlocked(string blockId)
    {
        if (!IsUnlocked(blockId))
        {
            PlayerPrefs.SetInt($"block_{blockId}_unlocked", 1);
            PlayerPrefs.Save();
        }
    }

    private void TryUnlockNext(string completedBlockId)
    {
        int idx = blocks.FindIndex(b => b.blockId == completedBlockId);
        if (idx < 0 || idx + 1 >= blocks.Count) return;

        var next = blocks[idx + 1];
        if (IsUnlocked(next.blockId)) return;

        EnsureUnlocked(next.blockId);
        OnBlockUnlocked?.Invoke(next.blockId);
        Debug.Log($"[BlockManager] Bloque desbloqueado: {next.blockName}");
    }

    // -----------------------------------------------------------------------
    // Navegacion
    // -----------------------------------------------------------------------

    public void ShowBlock(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= blocks.Count) return;

        var content = blocks[blockIndex];

        if (!IsUnlocked(content.blockId))
        {
            Debug.LogWarning($"[BlockManager] Bloque bloqueado: {content.blockName}");
            return;
        }

        bool sameBlock = currentBlockIndex == blockIndex;
        currentBlockIndex = blockIndex;

        // FIX BUG 3: No activar todos los roots directamente.
        // Delegar al ActivityMenuManager para que muestre SOLO la primera actividad disponible.
        activityMenuManager?.ShowFirstAvailable(
            content.hasDrawing,
            content.hasBasket,
            content.hasOrdering,
            content.hasReading
        );

        // Inyectar datos solo si es un bloque diferente
        if (!sameBlock)
        {
            if (content.hasDrawing && content.kanaTemplateSet != null)
                drawingManager?.SetData(content.kanaTemplateSet);

            if (content.hasBasket && content.basketSequence != null)
                basketManager?.SetData(content.basketSequence);

            if (content.hasOrdering && content.orderingSequence != null)
                orderingManager?.SetData(content.orderingSequence);

            if (content.hasReading && content.readingSequence != null)
                readingManager?.SetData(content.readingSequence);
        }

        Debug.Log($"[BlockManager] Bloque activo: {content.blockName}");
    }

    public void ShowBlock(string blockId)
    {
        int idx = blocks.FindIndex(b => b.blockId == blockId);
        if (idx >= 0) ShowBlock(idx);
    }

    public void HideAllActivities()
    {
        activityMenuManager?.HideAllActivities();
        currentBlockIndex = -1;
    }

    // -----------------------------------------------------------------------
    // API publica
    // -----------------------------------------------------------------------

    public List<BlockContentSO> GetBlocks() => blocks;

    public BlockContentSO GetCurrentBlock() =>
        currentBlockIndex >= 0 && currentBlockIndex < blocks.Count
            ? blocks[currentBlockIndex]
            : null;

    /// <summary>
    /// True si hay un bloque seleccionado actualmente.
    /// Usado por WristMenuFollower para ocultar el menu si no hay bloque activo.
    /// </summary>
    public bool HasActiveBlock => currentBlockIndex >= 0;

    public void ResetAllProgress()
    {
        foreach (var block in blocks)
        {
            PlayerPrefs.DeleteKey($"block_{block.blockId}_unlocked");

            if (activitiesByBlock.TryGetValue(block.blockId, out var activities))
                foreach (var a in activities)
                    a.ResetProgress();
        }

        if (blocks.Count > 0) EnsureUnlocked(blocks[0].blockId);
        PlayerPrefs.Save();
        currentBlockIndex = -1;
        Debug.Log("[BlockManager] Progreso reiniciado.");
    }

    /// <summary>
    /// Reinicia el progreso unicamente del bloque actual.
    /// No afecta el desbloqueo del bloque ni el progreso de otros bloques.
    /// </summary>
    public void ResetCurrentBlockProgress()
    {
        var current = GetCurrentBlock();
        if (current == null)
        {
            Debug.LogWarning("[BlockManager] No hay un bloque activo para reiniciar.");
            return;
        }

        if (activitiesByBlock.TryGetValue(current.blockId, out var activities))
        {
            foreach (var a in activities)
                a.ResetProgress();
        }

        // Re-inyectar datos para reiniciar las actividades en memoria
        if (current.hasDrawing && current.kanaTemplateSet != null)
            drawingManager?.SetData(current.kanaTemplateSet);
        if (current.hasBasket && current.basketSequence != null)
            basketManager?.SetData(current.basketSequence);
        if (current.hasOrdering && current.orderingSequence != null)
            orderingManager?.SetData(current.orderingSequence);
        if (current.hasReading && current.readingSequence != null)
            readingManager?.SetData(current.readingSequence);

        OnBlockStarsChanged?.Invoke(current.blockId, 0);
        Debug.Log($"[BlockManager] Progreso del bloque '{current.blockName}' reiniciado.");
    }
}