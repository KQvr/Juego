using System;
using UnityEngine;

[Serializable]
public class ReadingActivityItemData
{
    [Tooltip("Texto completo con la palabra clave subrayada usando Rich Text. Ej: 'El <u>inu</u> es un animal leal.'")]
    [TextArea(3, 6)]
    public string bodyText;

    [Tooltip("itemId del BasketCollectible que es la respuesta correcta.")]
    public string correctItemId;
}
