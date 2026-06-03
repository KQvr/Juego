using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI de seleccion de perfiles. Escena separada con 3 slots.
///
/// Estructura del Canvas:
///   ├── Title ("Selecciona un perfil")
///   ├── SlotsContainer
///   │     ├── Slot1
///   │     │     ├── SlotButton (Button)
///   │     │     ├── SlotLabel (TMP_Text — muestra nombre o "Crear perfil")
///   │     │     └── DeleteButton (Button — solo visible si hay perfil)
///   │     ├── Slot2 (igual)
///   │     └── Slot3 (igual)
///   ├── KeyboardPanel (oculto por defecto, gestionado por VRKeyboard)
///   │     ├── CreateTitle (TMP_Text — "Crear perfil 1")
///   │     ├── DisplayText (TMP_Text — texto escrito)
///   │     ├── KeysGrid (botones A-Z, ñ, numeros, etc.)
///   │     └── SpaceButton, BackspaceButton, ConfirmButton, CancelButton
///   └── BackButton (Button — vuelve al MainMenu)
/// </summary>
public class ProfileSelectionUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private GameObject slotsPanel;
    [SerializeField] private Button[] slotButtons = new Button[3];
    [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[3];
    [SerializeField] private Button[] slotDeleteButtons = new Button[3];

    [Header("Teclado")]
    [SerializeField] private VRKeyboard keyboard;
    [SerializeField] private TMP_Text createTitle;

    [Header("Navegacion")]
    [SerializeField] private Button backButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int pendingSlotIndex = -1;

    void Start()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i;
            if (slotButtons[i] != null)
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(idx));
            if (slotDeleteButtons[i] != null)
                slotDeleteButtons[i].onClick.AddListener(() => OnDeleteClicked(idx));
        }

        if (keyboard != null)
        {
            keyboard.OnConfirm += OnKeyboardConfirm;
            keyboard.OnCancel += OnKeyboardCancel;
            keyboard.Close(); // empieza cerrado
        }

        if (backButton != null) backButton.onClick.AddListener(GoBack);

        RefreshSlots();
    }

    void OnDestroy()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null) slotButtons[i].onClick.RemoveAllListeners();
            if (slotDeleteButtons[i] != null) slotDeleteButtons[i].onClick.RemoveAllListeners();
        }

        if (keyboard != null)
        {
            keyboard.OnConfirm -= OnKeyboardConfirm;
            keyboard.OnCancel -= OnKeyboardCancel;
        }

        if (backButton != null) backButton.onClick.RemoveListener(GoBack);
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < ProfileManager.MaxProfiles; i++)
        {
            if (i < slotLabels.Length && slotLabels[i] != null)
            {
                if (ProfileManager.ProfileExists(i))
                {
                    string name = ProfileManager.GetName(i);
                    bool isActive = ProfileManager.CurrentIndex == i;
                    slotLabels[i].text = isActive ? $"★ {name}" : name;
                }
                else
                {
                    slotLabels[i].text = "+ Crear perfil";
                }
            }

            if (i < slotDeleteButtons.Length && slotDeleteButtons[i] != null)
                slotDeleteButtons[i].gameObject.SetActive(ProfileManager.ProfileExists(i));
        }
    }

    private void OnSlotClicked(int index)
    {
        if (ProfileManager.ProfileExists(index))
        {
            // Seleccionar perfil existente
            ProfileManager.CurrentIndex = index;
            RefreshSlots();
        }
        else
        {
            // Crear nuevo: ocultar slots y abrir teclado
            pendingSlotIndex = index;
            if (createTitle != null)
                createTitle.text = $"Crear perfil {index + 1}";
            if (slotsPanel != null) slotsPanel.SetActive(false);
            keyboard?.Open("");
        }
    }

    private void OnDeleteClicked(int index)
    {
        ProfileManager.DeleteProfile(index);
        RefreshSlots();
    }

    private void OnKeyboardConfirm(string text)
    {
        if (pendingSlotIndex < 0) return;
        if (string.IsNullOrWhiteSpace(text))
        {
            pendingSlotIndex = -1;
            if (slotsPanel != null) slotsPanel.SetActive(true);
            return;
        }

        ProfileManager.CreateOrUpdateName(pendingSlotIndex, text.Trim());
        ProfileManager.CurrentIndex = pendingSlotIndex;
        pendingSlotIndex = -1;
        if (slotsPanel != null) slotsPanel.SetActive(true);
        RefreshSlots();
    }

    private void OnKeyboardCancel()
    {
        pendingSlotIndex = -1;
        if (slotsPanel != null) slotsPanel.SetActive(true);
    }

    private void GoBack() => SceneManager.LoadScene(mainMenuSceneName);
}