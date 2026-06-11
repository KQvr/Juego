using System;
using UnityEngine;

/// <summary>
/// Maneja hasta 3 perfiles de usuario. Cada perfil tiene su propio progreso
/// gracias a un prefijo en las keys de PlayerPrefs.
///
/// Uso desde otros scripts:
///   PlayerPrefs.GetFloat(ProfileManager.Key("MasterVolume"), 1f);
///   PlayerPrefs.SetInt(ProfileManager.Key("block_block_1_unlocked"), 1);
///
/// Si no hay perfil activo (CurrentIndex == -1), el prefijo es vacio y las
/// keys quedan iguales a antes (compatibilidad).
/// </summary>
public static class ProfileManager
{
    public const int MaxProfiles = 3;

    private const string CURRENT_KEY = "ActiveProfileIndex";
    private const string NAME_KEY_FORMAT = "Profile_{0}_Name";

    public static event Action OnProfileChanged;

    public static int CurrentIndex
    {
        get => PlayerPrefs.GetInt(CURRENT_KEY, -1);
        set
        {
            int clamped = Mathf.Clamp(value, -1, MaxProfiles - 1);
            if (CurrentIndex == clamped) return;
            PlayerPrefs.SetInt(CURRENT_KEY, clamped);
            PlayerPrefs.Save();
            OnProfileChanged?.Invoke();
        }
    }

    public static bool HasActiveProfile =>
        CurrentIndex >= 0 && CurrentIndex < MaxProfiles && ProfileExists(CurrentIndex);

    public static string CurrentName =>
        HasActiveProfile ? GetName(CurrentIndex) : "";

    public static string GetName(int index)
    {
        if (index < 0 || index >= MaxProfiles) return "";
        return PlayerPrefs.GetString(string.Format(NAME_KEY_FORMAT, index), "");
    }

    public static bool ProfileExists(int index) =>
        !string.IsNullOrEmpty(GetName(index));

    public static void CreateOrUpdateName(int index, string name)
    {
        if (index < 0 || index >= MaxProfiles) return;
        if (string.IsNullOrWhiteSpace(name)) return;
        PlayerPrefs.SetString(string.Format(NAME_KEY_FORMAT, index), name.Trim());
        PlayerPrefs.Save();
    }

    public static void DeleteProfile(int index)
    {
        if (index < 0 || index >= MaxProfiles) return;

        string prefix = $"p{index}_";

        // Borrar el nombre del perfil
        PlayerPrefs.DeleteKey(string.Format(NAME_KEY_FORMAT, index));

        // Borrar preferencias generales del perfil
        PlayerPrefs.DeleteKey(prefix + "DominantHand");
        PlayerPrefs.DeleteKey(prefix + "MasterVolume");

        // Borrar progreso de todos los bloques y actividades de este perfil.
        // Si se agregan bloques o actividades nuevas, actualizar estas listas.
        string[] blockIds = {
            "bk1", "bk2", "bk3", "bk4",
            "bk5", "bk6", "bk7", "bk8"
        };
        string[] activities = { "drawing", "basket", "ordering", "reading" };

        foreach (string blockId in blockIds)
        {
            PlayerPrefs.DeleteKey(prefix + $"block_{blockId}_unlocked");
            foreach (string activity in activities)
            {
                PlayerPrefs.DeleteKey(prefix + $"block_{blockId}_activity_{activity}_progress");
                PlayerPrefs.DeleteKey(prefix + $"block_{blockId}_activity_{activity}_completed");
                PlayerPrefs.DeleteKey(prefix + $"block_{blockId}_activity_{activity}_index");
            }
        }

        if (CurrentIndex == index)
            CurrentIndex = -1;
        PlayerPrefs.Save();

        Debug.Log($"[ProfileManager] Perfil {index} borrado junto con todo su progreso.");
    }

    // -----------------------------------------------------------------------
    // Prefijo para PlayerPrefs por perfil
    // -----------------------------------------------------------------------

    public static string Prefix =>
        HasActiveProfile ? $"p{CurrentIndex}_" : "";

    public static string Key(string baseKey) => Prefix + baseKey;
}