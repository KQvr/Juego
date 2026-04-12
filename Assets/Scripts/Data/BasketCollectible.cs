using UnityEngine;

[DisallowMultipleComponent]
public class BasketCollectible : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string itemId;

    [Header("Display")]
    [SerializeField] private string displayName;
    [SerializeField] private string japaneseName;
    [SerializeField] private string romajiName;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string JapaneseName => japaneseName;
    public string RomajiName => romajiName;

    public string GetFullLabel()
    {
        if (!string.IsNullOrWhiteSpace(japaneseName) &&
            !string.IsNullOrWhiteSpace(romajiName))
        {
            return $"{japaneseName}\n{romajiName}";
        }

        if (!string.IsNullOrWhiteSpace(japaneseName))
            return japaneseName;

        if (!string.IsNullOrWhiteSpace(romajiName))
            return romajiName;

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return itemId;
    }

    public bool Matches(string id)
    {
        return itemId == id;
    }
}