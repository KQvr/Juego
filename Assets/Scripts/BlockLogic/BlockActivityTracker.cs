using System;
using UnityEngine;

/// <summary>
/// Colócalo en el mismo GameObject que el activity manager.
/// Reporta progreso y completado al BlockManager.
///
/// Conexión en Inspector:
///   - Asignar blockId y activityId
///   - El activity manager llama a SetProgress(float) y MarkAsCompleted()
///     via sus propios eventos, o directamente desde su script.
/// </summary>
public class BlockActivityTracker : MonoBehaviour
{
    [Header("Identidad")]
    [SerializeField] private string blockId;
    [SerializeField] private string activityId;

    public string BlockId   => blockId;
    public string ActivityId => activityId;
    public float  Progress  { get; private set; }
    public bool   IsCompleted { get; private set; }

    public event Action<BlockActivityTracker> OnTrackerCompleted;

    void Start()
    {
        // Cargar progreso guardado
        float saved = PlayerPrefs.GetFloat(SaveKey("progress"), 0f);
        bool  comp  = PlayerPrefs.GetInt(SaveKey("completed"), 0) == 1;

        Progress    = saved;
        IsCompleted = comp;

        BlockManager.Instance?.RegisterActivity(this);
    }

    void OnDestroy()
    {
        BlockManager.Instance?.UnregisterActivity(this);
    }

    /// <summary>
    /// Llamado por el activity manager con un valor entre 0 y 1.
    /// </summary>
    public void SetProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
        Save();
        BlockManager.Instance?.OnActivityProgressChanged(this);
    }

    /// <summary>
    /// Llamado por el activity manager cuando su secuencia termina al 100%.
    /// </summary>
    public void MarkAsCompleted()
    {
        if (IsCompleted) return;

        IsCompleted = true;
        Progress    = 1f;
        Save();

        OnTrackerCompleted?.Invoke(this);
        BlockManager.Instance?.OnActivityCompleted(this);
    }

    public void ResetProgress()
    {
        IsCompleted = false;
        Progress    = 0f;
        Save();
        BlockManager.Instance?.OnActivityProgressChanged(this);
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(SaveKey("progress"),   Progress);
        PlayerPrefs.SetInt(SaveKey("completed"),    IsCompleted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private string SaveKey(string suffix) =>
        $"block_{blockId}_activity_{activityId}_{suffix}";
}
