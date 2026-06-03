using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Teclado virtual hecho con botones de UI. Funciona en VR (con ray
/// interaction), Editor (con mouse) y cualquier plataforma sin depender
/// del SDK de Meta.
///
/// Setup en Unity:
///   - Un Canvas (World Space) con la estructura:
///       VRKeyboardRoot      (este GameObject, con VRKeyboard component)
///       ├── DisplayText     (TMP_Text — muestra el texto escrito)
///       ├── KeysGrid        (GridLayoutGroup con botones)
///       │     ├── KeyA      (Button con VRKeyboardKey, character = "A")
///       │     ├── KeyB      (...)
///       │     └── ...       (todas las letras que quieras)
///       ├── SpaceButton     (Button)
///       ├── BackspaceButton (Button)
///       ├── ConfirmButton   (Button)
///       └── CancelButton    (Button)
///
///   - Los VRKeyboardKey hijos se autoencuentran en Awake.
///   - Otros codigos se suscriben a OnConfirm/OnCancel.
///
/// Uso desde codigo:
///   keyboard.Open("texto inicial");
///   keyboard.OnConfirm += (text) => { ... };
///   keyboard.OnCancel  += () => { ... };
/// </summary>
public class VRKeyboard : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private string placeholder = "Escribe un nombre";

    [Header("Botones especiales")]
    [SerializeField] private Button spaceButton;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Limites")]
    [SerializeField] private int maxLength = 12;
    [SerializeField] private bool autoFindKeys = true;
    [SerializeField] private List<VRKeyboardKey> keys = new();

    // Eventos
    public event Action<string> OnConfirm;
    public event Action OnCancel;

    public string CurrentText => buffer;
    public bool IsOpen => panel != null ? panel.activeSelf : gameObject.activeSelf;

    private string buffer = "";

    void Awake()
    {
        if (autoFindKeys)
        {
            keys.Clear();
            keys.AddRange(GetComponentsInChildren<VRKeyboardKey>(includeInactive: true));
        }

        foreach (var key in keys)
            if (key != null) key.Initialize(this);

        Debug.Log($"[VRKeyboard] Awake — keys: {keys.Count}, " +
                  $"space: {(spaceButton != null ? spaceButton.name : "NULL")}, " +
                  $"backspace: {(backspaceButton != null ? backspaceButton.name : "NULL")}, " +
                  $"confirm: {(confirmButton != null ? confirmButton.name : "NULL")}, " +
                  $"cancel: {(cancelButton != null ? cancelButton.name : "NULL")}");

        if (spaceButton != null) spaceButton.onClick.AddListener(OnSpace);
        if (backspaceButton != null) backspaceButton.onClick.AddListener(Backspace);
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
    }

    void OnDestroy()
    {
        if (spaceButton != null) spaceButton.onClick.RemoveListener(OnSpace);
        if (backspaceButton != null) backspaceButton.onClick.RemoveListener(Backspace);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(Cancel);
    }

    // ------------------------------------------------------------------
    // API publica
    // ------------------------------------------------------------------

    public void Open(string initialText = "")
    {
        buffer = initialText ?? "";
        UpdateDisplay();
        SetVisible(true);
    }

    public void Close()
    {
        SetVisible(false);
    }

    public void AppendChar(string c)
    {
        if (string.IsNullOrEmpty(c)) return;
        if (buffer.Length + c.Length > maxLength) return;
        buffer += c;
        UpdateDisplay();
    }

    public void Backspace()
    {
        Debug.Log($"[VRKeyboard] Backspace called. Buffer antes: '{buffer}'");
        if (buffer.Length == 0) return;
        buffer = buffer.Substring(0, buffer.Length - 1);
        UpdateDisplay();
    }

    public void Clear()
    {
        buffer = "";
        UpdateDisplay();
    }

    public void Confirm()
    {
        OnConfirm?.Invoke(buffer);
        Close();
    }

    public void Cancel()
    {
        OnCancel?.Invoke();
        Close();
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void OnSpace()
    {
        Debug.Log($"[VRKeyboard] OnSpace called. Buffer antes: '{buffer}'");
        AppendChar(" ");
    }

    private void UpdateDisplay()
    {
        if (displayText == null) return;

        if (string.IsNullOrEmpty(buffer))
            displayText.text = $"<color=#888888>{placeholder}</color>";
        else
            displayText.text = buffer;
    }

    private void SetVisible(bool visible)
    {
        if (panel != null) panel.SetActive(visible);
        else gameObject.SetActive(visible);
    }
}