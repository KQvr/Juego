using UnityEngine;

public class ActivityMenuManager : MonoBehaviour
{
    [Header("Activity Roots")]
    [SerializeField] private GameObject drawingActivityRoot;
    [SerializeField] private GameObject objectBasketActivityRoot;

    [Header("Optional")]
    [SerializeField] private bool hideAllOnStart = false;

    void Start()
    {
        if (hideAllOnStart)
            HideAllActivities();
        else
            ShowDrawingActivity();
    }

    public void ShowDrawingActivity()
    {
        SetActivityState(true, false);
    }

    public void ShowObjectBasketActivity()
    {
        SetActivityState(false, true);
    }

    public void HideAllActivities()
    {
        SetActivityState(false, false);
    }

    private void SetActivityState(bool drawingActive, bool basketActive)
    {
        if (drawingActivityRoot != null)
            drawingActivityRoot.SetActive(drawingActive);

        if (objectBasketActivityRoot != null)
            objectBasketActivityRoot.SetActive(basketActive);
    }
}