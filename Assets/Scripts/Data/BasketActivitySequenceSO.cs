using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Activities/Object Basket/Sequence", fileName = "BasketActivitySequence")]
public class BasketActivitySequenceSO : ScriptableObject
{
    public List<BasketActivityItemData> items = new();
}