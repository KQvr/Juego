using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente para cada boton de letra del VRKeyboard.
/// Va en el mismo GameObject que el Button. Pone el caracter en el
/// label automaticamente.
/// </summary>
[RequireComponent(typeof(Button))]
public class VRKeyboardKey : MonoBehaviour
{
    [Tooltip("Caracter que este boton agrega al texto al ser pulsado.")]
    [SerializeField] private string character = "A";

    [Tooltip("Si esta marcado, el caracter se imprime en mayuscula. Si no, minuscula.")]
    [SerializeField] private bool uppercase = false;

    [Tooltip("Label del boton (TMP_Text). Si esta vacio, se busca en hijos.")]
    [SerializeField] private TMP_Text label;

    private VRKeyboard keyboard;
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(includeInactive: true);
    }

    /// <summary>
    /// Llamado por VRKeyboard al inicializar. Asigna la referencia al
    /// teclado padre y conecta el onClick.
    /// </summary>
    public void Initialize(VRKeyboard owner)
    {
        keyboard = owner;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
        ApplyLabel();
    }

    void OnValidate()
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(includeInactive: true);
        ApplyLabel();
    }

    private void OnClick()
    {
        if (keyboard == null || string.IsNullOrEmpty(character)) return;
        string c = uppercase ? character.ToUpper() : character.ToLower();
        keyboard.AppendChar(c);
    }

    private void ApplyLabel()
    {
        if (label == null || string.IsNullOrEmpty(character)) return;
        label.text = uppercase ? character.ToUpper() : character.ToLower();
    }
}