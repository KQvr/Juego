using UnityEngine;

public class BasketCollectible : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;

    public string ItemId => itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
}