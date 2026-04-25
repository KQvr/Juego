using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Kana Ordering/Sequence", fileName = "KanaWordSequence")]
public class KanaWordSequenceSO : ScriptableObject
{
    public List<KanaWordItemData> words = new();
}
