using UnityEngine;

/// <summary>
/// Define el contenido de un bloque — qué datos usa cada actividad.
/// Crea uno por bloque desde Assets → Create → Blocks → Block Content.
/// Las actividades que no apliquen al bloque se dejan vacías (null).
/// </summary>
[CreateAssetMenu(menuName = "Blocks/Block Content", fileName = "BlockContent")]
public class BlockContentSO : ScriptableObject
{
    [Header("Identidad")]
    public string blockId;
    public string blockName;
    [TextArea(2, 4)]
    public string description;

    [Header("Actividad — Dibujo de Kana")]
    public bool hasDrawing;
    public KanaTemplateSet kanaTemplateSet;

    [Header("Actividad — Cesta de Objetos")]
    public bool hasBasket;
    public BasketActivitySequenceSO basketSequence;

    [Header("Actividad — Ordenar Kanas")]
    public bool hasOrdering;
    public KanaWordSequenceSO orderingSequence;

    [Header("Actividad — Lectura")]
    public bool hasReading;
    public ReadingActivitySequenceSO readingSequence;
}
