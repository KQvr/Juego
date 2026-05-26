using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pon este componente en el prefab del boton de bloque.
/// Asigna TODAS las referencias directamente en el Inspector del prefab.
/// </summary>
public class BlockButtonUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text starsText;
    public TMP_Text descText;
    public GameObject lockOverlay;
    public Image backgroundImage; // el Image que recibe el color
}
