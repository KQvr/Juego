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
    [Header("Volumen")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeLabel;

    [Header("Mano dominante")]
    [SerializeField] private Button handToggleButton;
    [SerializeField] private TMP_Text handLabel;

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

        if (handToggleButton != null)
            handToggleButton.onClick.AddListener(OnToggleHand);
        UpdateHandLabel();
    }

    void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(GoBack);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (handToggleButton != null) handToggleButton.onClick.RemoveListener(OnToggleHand);
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

    private void OnToggleHand()
    {
        HandPreference.Dominant = HandPreference.Dominant == Handedness.Right
            ? Handedness.Left
            : Handedness.Right;
        UpdateHandLabel();
    }

    private void UpdateHandLabel()
    {
        if (handLabel == null) return;
        string hand = HandPreference.Dominant == Handedness.Right ? "Derecha" : "Izquierda";
        handLabel.text = $"Mano dominante: {hand}";
    }

    private void GoBack() => SceneManager.LoadScene(mainMenuSceneName);
}