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

    private readonly List<BlockActivityTracker> allTrackers = new();
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

        // Buscar TODOS los trackers (incluyendo los que estan en GameObjects
        // inactivos) y registrarlos manualmente. Esto evita el bug donde
        // las actividades se desactivan en Start del ActivityMenuManager y
        // los trackers nunca corren su Start, por lo que no se registran.
        var trackers = FindObjectsByType<BlockActivityTracker>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        foreach (var t in trackers)
            if (!allTrackers.Contains(t))
                allTrackers.Add(t);

        ValidateTrackerSetup();
    }

    /// <summary>
    /// Verifica que cada activity manager tenga un tracker del tipo correcto
    /// y que no haya trackers compartidos o duplicados.
    /// </summary>
    private void ValidateTrackerSetup()
    {
        // Contar trackers por tipo
        var byType = new Dictionary<BlockActivityType, List<BlockActivityTracker>>();
        foreach (BlockActivityType t in System.Enum.GetValues(typeof(BlockActivityType)))
            byType[t] = new List<BlockActivityTracker>();

        foreach (var t in allTrackers)
            if (t != null) byType[t.ActivityType].Add(t);

        foreach (var pair in byType)
        {
            if (pair.Value.Count == 0)
                Debug.LogWarning($"[BlockManager] No hay tracker de tipo {pair.Key} en la escena.");
            else if (pair.Value.Count > 1)
                Debug.LogWarning($"[BlockManager] Hay {pair.Value.Count} trackers de tipo {pair.Key}. " +
                                 $"Deberia haber solo uno. GameObjects: " +
                                 $"{string.Join(", ", pair.Value.ConvertAll(x => x.gameObject.name))}");
        }

        // Verificar que cada manager tenga un tracker del tipo correcto
        CheckManagerTracker(drawingManager?.ActivityTracker, BlockActivityType.Drawing, "drawingManager");
        CheckManagerTracker(basketManager?.ActivityTracker, BlockActivityType.Basket, "basketManager");
        CheckManagerTracker(orderingManager?.ActivityTracker, BlockActivityType.Ordering, "orderingManager");
        CheckManagerTracker(readingManager?.ActivityTracker, BlockActivityType.Reading, "readingManager");

        // Verificar que no haya managers compartiendo el mismo tracker
        var refs = new[]
        {
            (drawingManager?.ActivityTracker,  "drawingManager"),
            (basketManager?.ActivityTracker,   "basketManager"),
            (orderingManager?.ActivityTracker, "orderingManager"),
            (readingManager?.ActivityTracker,  "readingManager"),
        };
        for (int i = 0; i < refs.Length; i++)
        {
            for (int j = i + 1; j < refs.Length; j++)
            {
                if (refs[i].Item1 != null && refs[i].Item1 == refs[j].Item1)
                    Debug.LogError($"[BlockManager] {refs[i].Item2} y {refs[j].Item2} comparten el MISMO tracker " +
                                   $"({refs[i].Item1.gameObject.name}). Cada manager debe tener su propio tracker.");
            }
        }
    }

    private static void CheckManagerTracker(BlockActivityTracker tracker, BlockActivityType expected, string managerName)
    {
        if (tracker == null)
        {
            Debug.LogWarning($"[BlockManager] {managerName} no tiene Activity Tracker asignado.");
            return;
        }
        if (tracker.ActivityType != expected)
            Debug.LogError($"[BlockManager] {managerName} tiene un tracker de tipo {tracker.ActivityType} " +
                           $"pero deberia ser {expected}. Cambia el Activity Type en el tracker, o asigna otro.");
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
        if (!allTrackers.Contains(tracker))
            allTrackers.Add(tracker);

        // Si ya hay un bloque activo, asignarselo al tracker recien registrado
        var current = GetCurrentBlock();
        if (current != null)
            tracker.SetBlockId(current.blockId);
    }

    public void UnregisterActivity(BlockActivityTracker tracker)
    {
        allTrackers.Remove(tracker);
    }

    public void OnActivityProgressChanged(BlockActivityTracker tracker)
    {
        if (string.IsNullOrEmpty(tracker.CurrentBlockId)) return;
        OnBlockStarsChanged?.Invoke(tracker.CurrentBlockId, CalculateStars(tracker.CurrentBlockId));
    }

    public void OnActivityCompleted(BlockActivityTracker tracker)
    {
        if (string.IsNullOrEmpty(tracker.CurrentBlockId)) return;
        int stars = CalculateStars(tracker.CurrentBlockId);
        OnBlockStarsChanged?.Invoke(tracker.CurrentBlockId, stars);
        if (stars == 3) TryUnlockNext(tracker.CurrentBlockId);
    }

    // -----------------------------------------------------------------------
    // Estrellas — lee de PlayerPrefs, asi funciona para cualquier bloque
    // -----------------------------------------------------------------------

    public int CalculateStars(string blockId)
    {
        var block = blocks.Find(b => b.blockId == blockId);
        if (block == null) return 0;

        int totalActivities = 0;
        int completedCount = 0;
        float totalProgress = 0f;
        bool anyStarted = false;

        void Consider(BlockActivityType type)
        {
            totalActivities++;
            float p = BlockActivityTracker.GetSavedProgress(blockId, type);
            bool c = BlockActivityTracker.GetSavedCompleted(blockId, type);
            totalProgress += p;
            if (c) completedCount++;
            if (p > 0f) anyStarted = true;
        }

        if (block.hasDrawing) Consider(BlockActivityType.Drawing);
        if (block.hasBasket) Consider(BlockActivityType.Basket);
        if (block.hasOrdering) Consider(BlockActivityType.Ordering);
        if (block.hasReading) Consider(BlockActivityType.Reading);

        if (totalActivities == 0) return 0;
        if (completedCount == totalActivities) return 3;
        if (totalProgress / totalActivities >= 0.5f) return 2;
        if (anyStarted) return 1;
        return 0;
    }

    public int GetStars(string blockId) => CalculateStars(blockId);

    // -----------------------------------------------------------------------
    // Desbloqueo
    // -----------------------------------------------------------------------

    public bool IsUnlocked(string blockId) =>
        PlayerPrefs.GetInt(ProfileManager.Key($"block_{blockId}_unlocked"), 0) == 1;

    private void EnsureUnlocked(string blockId)
    {
        if (!IsUnlocked(blockId))
        {
            PlayerPrefs.SetInt(ProfileManager.Key($"block_{blockId}_unlocked"), 1);
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

        // Asignar el blockId actual a todos los trackers para que reporten
        // bajo el bloque correcto
        foreach (var t in allTrackers)
            t.SetBlockId(content.blockId);

        // Mostrar la primera actividad disponible
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

        // Limpiar blockId en los trackers para que no guarden bajo un bloque
        foreach (var t in allTrackers)
            t.SetBlockId(null);
    }

    // -----------------------------------------------------------------------
    // API publica
    // -----------------------------------------------------------------------

    public List<BlockContentSO> GetBlocks() => blocks;

    public BlockContentSO GetCurrentBlock() =>
        currentBlockIndex >= 0 && currentBlockIndex < blocks.Count
            ? blocks[currentBlockIndex]
            : null;

    public bool HasActiveBlock => currentBlockIndex >= 0;

    public void ResetAllProgress()
    {
        foreach (var block in blocks)
        {
            PlayerPrefs.DeleteKey(ProfileManager.Key($"block_{block.blockId}_unlocked"));

            // Borrar progreso de cada tipo de actividad bajo cada bloque
            BlockActivityTracker.ClearSaved(block.blockId, BlockActivityType.Drawing);
            BlockActivityTracker.ClearSaved(block.blockId, BlockActivityType.Basket);
            BlockActivityTracker.ClearSaved(block.blockId, BlockActivityType.Ordering);
            BlockActivityTracker.ClearSaved(block.blockId, BlockActivityType.Reading);
        }

        // Resetear trackers en memoria
        foreach (var t in allTrackers)
            t.ResetProgress();

        if (blocks.Count > 0) EnsureUnlocked(blocks[0].blockId);
        PlayerPrefs.Save();
        currentBlockIndex = -1;
        Debug.Log("[BlockManager] Progreso reiniciado.");
    }

    /// <summary>
    /// Reinicia el progreso unicamente del bloque actual.
    /// </summary>
    public void ResetCurrentBlockProgress()
    {
        var current = GetCurrentBlock();
        if (current == null)
        {
            Debug.LogWarning("[BlockManager] No hay un bloque activo para reiniciar.");
            return;
        }

        // Borrar PlayerPrefs del bloque actual
        BlockActivityTracker.ClearSaved(current.blockId, BlockActivityType.Drawing);
        BlockActivityTracker.ClearSaved(current.blockId, BlockActivityType.Basket);
        BlockActivityTracker.ClearSaved(current.blockId, BlockActivityType.Ordering);
        BlockActivityTracker.ClearSaved(current.blockId, BlockActivityType.Reading);

        // Resetear trackers en memoria
        foreach (var t in allTrackers)
            t.ResetProgress();

        // Re-inyectar datos para reiniciar las actividades
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