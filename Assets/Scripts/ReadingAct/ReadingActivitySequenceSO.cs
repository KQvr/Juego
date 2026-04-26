using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Reading/Sequence", fileName = "ReadingActivitySequence")]
public class ReadingActivitySequenceSO : ScriptableObject
{
    public List<ReadingActivityItemData> items = new();
}
