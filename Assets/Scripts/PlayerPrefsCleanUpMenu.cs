#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidades de editor para limpiar PlayerPrefs del proyecto.
/// Este archivo debe estar dentro de una carpeta llamada "Editor"
/// (por ejemplo: Assets/Editor/PlayerPrefsCleanupMenu.cs).
/// </summary>
public static class PlayerPrefsCleanupMenu
{
    [MenuItem("VR Japanese/Purgar todos los datos guardados", priority = 100)]
    public static void PurgeAll()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Purgar PlayerPrefs",
            "Esto va a borrar TODOS los datos guardados:\n\n" +
            "• Perfiles (nombres)\n" +
            "• Progreso de bloques\n" +
            "• Estrellas y desbloqueos\n" +
            "• Mano dominante\n" +
            "• Tutoriales vistos\n" +
            "• Volumen y otras preferencias\n\n" +
            "¿Estas seguro?",
            "Si, borrar todo",
            "Cancelar"
        );

        if (!confirm) return;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[PlayerPrefsCleanupMenu] Todos los PlayerPrefs purgados.");
    }

    [MenuItem("VR Japanese/Purgar solo progreso (mantener perfiles)", priority = 101)]
    public static void PurgeProgressOnly()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Purgar solo progreso",
            "Esto borra el progreso de bloques pero mantiene:\n\n" +
            "• Los perfiles y sus nombres\n" +
            "• La mano dominante\n" +
            "• El volumen\n\n" +
            "¿Continuar?",
            "Si, borrar progreso",
            "Cancelar"
        );

        if (!confirm) return;

        // Recorre los 3 perfiles posibles + sin perfil
        for (int p = -1; p < ProfileManager.MaxProfiles; p++)
        {
            string prefix = p >= 0 ? $"p{p}_" : "";

            // Borrar todas las keys de bloques (asumiendo bloque 1..8)
            for (int b = 1; b <= 8; b++)
            {
                string blockId = $"block_{b}";
                PlayerPrefs.DeleteKey($"{prefix}block_{blockId}_unlocked");

                foreach (string activity in new[] { "drawing", "basket", "ordering", "reading" })
                {
                    PlayerPrefs.DeleteKey($"{prefix}block_{blockId}_activity_{activity}_progress");
                    PlayerPrefs.DeleteKey($"{prefix}block_{blockId}_activity_{activity}_completed");
                    PlayerPrefs.DeleteKey($"{prefix}block_{blockId}_activity_{activity}_index");
                }
            }

            // Tutoriales
            foreach (string activity in new[] { "drawing", "basket", "ordering", "reading" })
                PlayerPrefs.DeleteKey($"{prefix}tutorial_{activity}_seen");
        }

        PlayerPrefs.Save();
        Debug.Log("[PlayerPrefsCleanupMenu] Progreso purgado. Perfiles preservados.");
    }

    [MenuItem("VR Japanese/Mostrar todos los PlayerPrefs (debug)", priority = 200)]
    public static void DumpAll()
    {
        Debug.Log("[PlayerPrefsCleanupMenu] No hay forma directa de listar todas las keys de PlayerPrefs " +
                  "en runtime en Unity. Si quieres ver el contenido completo: " +
                  "Windows → Regedit → HKEY_CURRENT_USER\\SOFTWARE\\Unity\\UnityEditor\\<Company>\\<Product>. " +
                  "Mac → ~/Library/Preferences/unity.<Company>.<Product>.plist. " +
                  "Quest → adb shell run-as <package>.");
    }
}
#endif