using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu principal — escena propia.
/// El boton Play solo se habilita si hay un perfil activo.
///
/// Configurar en Build Settings:
///   - MainMenu  (escena 0)
///   - Game      (escena 1)
///   - Options   (escena 2)
///   - Profiles  (escena 3)
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button profilesButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;

    [Header("Etiqueta de perfil")]
    [SerializeField] private TMP_Text profileLabel;

    [Header("Nombres de escenas")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string optionsSceneName = "Options";
    [SerializeField] private string profilesSceneName = "Profiles";

    void Start()
    {
        if (playButton != null) playButton.onClick.AddListener(LoadGame);
        if (profilesButton != null) profilesButton.onClick.AddListener(LoadProfiles);
        if (optionsButton != null) optionsButton.onClick.AddListener(LoadOptions);
        if (exitButton != null) exitButton.onClick.AddListener(ExitApp);

        RefreshProfileUI();
    }

    void OnEnable()
    {
        RefreshProfileUI();
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(LoadGame);
        if (profilesButton != null) profilesButton.onClick.RemoveListener(LoadProfiles);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(LoadOptions);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitApp);
    }

    private void RefreshProfileUI()
    {
        bool hasProfile = ProfileManager.HasActiveProfile;

        if (playButton != null)
            playButton.interactable = hasProfile;

        if (profileLabel != null)
        {
            profileLabel.text = hasProfile
                ? $"Perfil: {ProfileManager.CurrentName}"
                : "Selecciona un perfil para jugar";
        }
    }

    private void LoadGame()
    {
        if (!ProfileManager.HasActiveProfile) return;
        SceneManager.LoadScene(gameSceneName);
    }

    private void LoadProfiles() => SceneManager.LoadScene(profilesSceneName);
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