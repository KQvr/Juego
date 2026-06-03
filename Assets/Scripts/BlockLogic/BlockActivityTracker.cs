using System;
using UnityEngine;

public enum BlockActivityType { Drawing, Basket, Ordering, Reading }

/// <summary>
/// Coloquelo en el mismo GameObject que el activity manager.
/// Reporta progreso y completado al BlockManager.
///
/// El blockId es DINAMICO — lo asigna BlockManager cuando el usuario
/// entra a un bloque (via SetBlockId). El tipo de actividad si es fijo.
///
/// Persiste tres datos por bloque + tipo de actividad:
///   - progress  (float 0..1)
///   - completed (bool)
///   - index     (int — item actual dentro de la secuencia, para reanudar)
/// </summary>
public class BlockActivityTracker : MonoBehaviour
{
    [Header("Identidad")]
    [Tooltip("Tipo de actividad — fijo. El bloque al que pertenece es dinamico.")]
    [SerializeField] private BlockActivityType activityType;

    public BlockActivityType ActivityType => activityType;
    public string ActivityId => activityType.ToString().ToLower();
    public string CurrentBlockId { get; private set; }
    public float Progress { get; private set; }
    public bool IsCompleted { get; private set; }
    public int CurrentItemIndex { get; private set; }

    public event Action<BlockActivityTracker> OnTrackerCompleted;

    void Start()
    {
        BlockManager.Instance?.RegisterActivity(this);
    }

    void OnDestroy()
    {
        BlockManager.Instance?.UnregisterActivity(this);
    }

    /// <summary>
    /// Llamado por BlockManager cuando el usuario entra a un bloque.
    /// El tracker recarga su estado para el bloque dado.
    /// </summary>
    public void SetBlockId(string newBlockId)
    {
        CurrentBlockId = newBlockId;

        if (string.IsNullOrEmpty(newBlockId))
        {
            Progress = 0f;
            IsCompleted = false;
            CurrentItemIndex = 0;
            return;
        }

        Progress = PlayerPrefs.GetFloat(SaveKey("progress"), 0f);
        IsCompleted = PlayerPrefs.GetInt(SaveKey("completed"), 0) == 1;
        CurrentItemIndex = PlayerPrefs.GetInt(SaveKey("index"), 0);
    }

    public void SetProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
        Save();
        BlockManager.Instance?.OnActivityProgressChanged(this);
    }

    /// <summary>
    /// Guarda el indice del item actual dentro de la actividad,
    /// para poder reanudar en la misma posicion al cambiar de escena.
    /// </summary>
    public void SetCurrentItemIndex(int index)
    {
        CurrentItemIndex = Mathf.Max(0, index);
        Save();
    }

    public void MarkAsCompleted()
    {
        if (IsCompleted) return;

        IsCompleted = true;
        Progress = 1f;
        Save();

        OnTrackerCompleted?.Invoke(this);
        BlockManager.Instance?.OnActivityCompleted(this);
    }

    public void ResetProgress()
    {
        IsCompleted = false;
        Progress = 0f;
        CurrentItemIndex = 0;
        Save();
        BlockManager.Instance?.OnActivityProgressChanged(this);
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(CurrentBlockId)) return;
        PlayerPrefs.SetFloat(SaveKey("progress"), Progress);
        PlayerPrefs.SetInt(SaveKey("completed"), IsCompleted ? 1 : 0);
        PlayerPrefs.SetInt(SaveKey("index"), CurrentItemIndex);
        PlayerPrefs.Save();
    }

    private string SaveKey(string suffix) =>
        ProfileManager.Key($"block_{CurrentBlockId}_activity_{ActivityId}_{suffix}");

    // -----------------------------------------------------------------------
    // Helpers estaticos para leer estado de cualquier bloque sin tracker
    // -----------------------------------------------------------------------

    public static float GetSavedProgress(string blockId, BlockActivityType type)
    {
        string id = type.ToString().ToLower();
        return PlayerPrefs.GetFloat(ProfileManager.Key($"block_{blockId}_activity_{id}_progress"), 0f);
    }

    public static bool GetSavedCompleted(string blockId, BlockActivityType type)
    {
        string id = type.ToString().ToLower();
        return PlayerPrefs.GetInt(ProfileManager.Key($"block_{blockId}_activity_{id}_completed"), 0) == 1;
    }

    public static int GetSavedItemIndex(string blockId, BlockActivityType type)
    {
        string id = type.ToString().ToLower();
        return PlayerPrefs.GetInt(ProfileManager.Key($"block_{blockId}_activity_{id}_index"), 0);
    }

    public static void ClearSaved(string blockId, BlockActivityType type)
    {
        string id = type.ToString().ToLower();
        PlayerPrefs.DeleteKey(ProfileManager.Key($"block_{blockId}_activity_{id}_progress"));
        PlayerPrefs.DeleteKey(ProfileManager.Key($"block_{blockId}_activity_{id}_completed"));
        PlayerPrefs.DeleteKey(ProfileManager.Key($"block_{blockId}_activity_{id}_index"));
    }
}