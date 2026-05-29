using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu principal — escena propia.
/// Cada boton carga su escena correspondiente.
///
/// Configurar en Build Settings:
///   - MainMenu (escena 0)
///   - Game     (escena 1)
///   - Options  (escena 2)
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;

    [Header("Nombres de escenas")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string optionsSceneName = "Options";

    void Start()
    {
        if (playButton != null) playButton.onClick.AddListener(LoadGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(LoadOptions);
        if (exitButton != null) exitButton.onClick.AddListener(ExitApp);
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(LoadGame);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(LoadOptions);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitApp);
    }

    private void LoadGame() => SceneManager.LoadScene(gameSceneName);
    private void LoadOptions() => SceneManager.LoadScene(optionsSceneName);

    private void ExitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}