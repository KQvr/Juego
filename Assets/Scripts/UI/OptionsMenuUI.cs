using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu de opciones basico.
/// Agrega aqui los sliders/toggles que necesites.
///
/// Estructura del Canvas sugerida:
///   OptionsMenuCanvas (World Space)
///   ├── TitleText        (TMP_Text) → "Opciones"
///   ├── VolumeSlider     (Slider)   → volumen general
///   ├── VolumeLabel      (TMP_Text) → "Volumen: 80%"
///   └── BackButton       (Button)   → regresa al menu principal
/// </summary>
public class OptionsMenuUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject optionsCanvas;

    [Header("Opciones")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeLabel;

    [Header("Navegacion")]
    [SerializeField] private Button backButton;
    [SerializeField] private MainMenuUI mainMenuUI;

    private const string VOLUME_KEY = "MasterVolume";

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            UpdateVolumeLabel(volumeSlider.value);
        }

        HideOptions();
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(GoBack);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    public void ShowOptions()
    {
        Debug.Log($"[OptionsMenuUI] ShowOptions - optionsCanvas null: {optionsCanvas == null}");
        if (optionsCanvas != null) optionsCanvas.SetActive(true);
        else Debug.LogWarning("[OptionsMenuUI] optionsCanvas es null!");
    }

    public void HideOptions()
    {
         optionsCanvas.SetActive(false);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
        PlayerPrefs.Save();
        UpdateVolumeLabel(value);
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel != null)
            volumeLabel.text = $"Volumen: {Mathf.RoundToInt(value * 100)}%";
    }

    private void GoBack()
    {
        HideOptions();
        mainMenuUI?.ShowMainMenu();
    }
}
