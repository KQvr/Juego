using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu principal de la aplicacion.
/// Se muestra al iniciar y da acceso a los bloques, opciones y salida.
///
/// Estructura del Canvas sugerida:
///   MainMenuCanvas (World Space)
///   ├── TitleText       (TMP_Text) → nombre de la app
///   ├── PlayButton      (Button)   → ShowBlockMenu()
///   ├── OptionsButton   (Button)   → ShowOptionsMenu()
///   └── ExitButton      (Button)   → ExitApp()
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject mainMenuCanvas;

    [Header("Botones")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;

    [Header("Referencias")]
    [SerializeField] private BlockMenuUI blockMenuUI;
    [SerializeField] private OptionsMenuUI optionsMenuUI;

    void Start()
    {
        if (playButton != null) playButton.onClick.AddListener(ShowBlockMenu);
        if (optionsButton != null) optionsButton.onClick.AddListener(ShowOptionsMenu);
        if (exitButton != null) exitButton.onClick.AddListener(ExitApp);

        ShowMainMenu();
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(ShowBlockMenu);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(ShowOptionsMenu);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitApp);
    }

    public void ShowMainMenu()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
        blockMenuUI?.HideMenu();
        optionsMenuUI?.HideOptions();
    }

    private void ShowBlockMenu()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        blockMenuUI?.ShowMenu();
    }

    private void ShowOptionsMenu()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        optionsMenuUI?.ShowOptions();
    }

    private void ExitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}