using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [Header("Bloques (en orden)")]
    [SerializeField] private List<BlockContentSO> blocks = new();

    [Header("Activity Managers (una sola instancia de cada uno)")]
    [SerializeField] private KanaLessonManager         drawingManager;
    [SerializeField] private BasketActivityManager     basketManager;
    [SerializeField] private KanaOrderingActivityManager orderingManager;
    [SerializeField] private ReadingActivityManager    readingManager;

    [Header("Activity Roots (para mostrar u ocultar)")]
    [SerializeField] private GameObject drawingRoot;
    [SerializeField] private GameObject basketRoot;
    [SerializeField] private GameObject orderingRoot;
    [SerializeField] private GameObject readingRoot;

    // Eventos
    public event Action<string, int> OnBlockStarsChanged;
    public event Action<string>      OnBlockUnlocked;

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
        if (avg >= 0.5f)  return 2;
        if (anyStarted)   return 1;
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
    // Navegacion e inyeccion de datos
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

        currentBlockIndex = blockIndex;

        // Mostrar u ocultar cada actividad segun el bloque
        SetActivityActive(drawingRoot,  drawingManager  != null && content.hasDrawing);
        SetActivityActive(basketRoot,   basketManager   != null && content.hasBasket);
        SetActivityActive(orderingRoot, orderingManager != null && content.hasOrdering);
        SetActivityActive(readingRoot,  readingManager  != null && content.hasReading);

        // Inyectar datos
        if (content.hasDrawing  && content.kanaTemplateSet != null)
            drawingManager?.SetData(content.kanaTemplateSet);

        if (content.hasBasket   && content.basketSequence != null)
            basketManager?.SetData(content.basketSequence);

        if (content.hasOrdering && content.orderingSequence != null)
            orderingManager?.SetData(content.orderingSequence);

        if (content.hasReading  && content.readingSequence != null)
            readingManager?.SetData(content.readingSequence);

        Debug.Log($"[BlockManager] Mostrando bloque: {content.blockName}");
    }

    public void ShowBlock(string blockId)
    {
        int idx = blocks.FindIndex(b => b.blockId == blockId);
        if (idx >= 0) ShowBlock(idx);
    }

    public void HideAllActivities()
    {
        SetActivityActive(drawingRoot,  false);
        SetActivityActive(basketRoot,   false);
        SetActivityActive(orderingRoot, false);
        SetActivityActive(readingRoot,  false);
        currentBlockIndex = -1;
    }

    private void SetActivityActive(GameObject root, bool active)
    {
        if (root != null) root.SetActive(active);
    }

    // -----------------------------------------------------------------------
    // API publica
    // -----------------------------------------------------------------------

    public List<BlockContentSO> GetBlocks() => blocks;
    public BlockContentSO GetCurrentBlock() =>
        currentBlockIndex >= 0 ? blocks[currentBlockIndex] : null;

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
        Debug.Log("[BlockManager] Progreso reiniciado.");
    }
}
