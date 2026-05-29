using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de opciones — escena propia.
/// El boton de regreso carga la escena del MainMenu.
/// </summary>
public class OptionsMenuUI : MonoBehaviour
{
    [Header("Opciones")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeLabel;

    [Header("Navegacion")]
    [SerializeField] private Button backButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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

        // Aplicar volumen guardado
        AudioListener.volume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(GoBack);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
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

    private void GoBack() => SceneManager.LoadScene(mainMenuSceneName);
}