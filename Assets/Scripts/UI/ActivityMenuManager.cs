using UnityEngine;

public class ActivityMenuManager : MonoBehaviour
{
    [Header("Activity Roots")]
    [SerializeField] private GameObject drawingActivityRoot;
    [SerializeField] private GameObject objectBasketActivityRoot;
    [SerializeField] private GameObject orderingActivityRoot;
    [SerializeField] private GameObject readingActivityRoot;

    [Header("Opciones")]
    [SerializeField] private bool hideAllOnStart = true;

    void Start()
    {
        if (hideAllOnStart)
            HideAllActivities();
    }

    // -----------------------------------------------------------------------
    // Mostrar actividades individualmente (llamado por WristMenuPager)
    // -----------------------------------------------------------------------

    public void ShowDrawingActivity() => SetActivityState(true, false, false, false);
    public void ShowBasketActivity() => SetActivityState(false, true, false, false);
    public void ShowOrderingActivity() => SetActivityState(false, false, true, false);
    public void ShowReadingActivity() => SetActivityState(false, false, false, true);
    public void HideAllActivities() => SetActivityState(false, false, false, false);

    /// <summary>
    /// Muestra solo la primera actividad disponible para el bloque actual.
    /// Llamado por BlockManager al entrar a un bloque.
    /// </summary>
    public void ShowFirstAvailable(bool hasDrawing, bool hasBasket, bool hasOrdering, bool hasReading)
    {
        if (hasDrawing) { ShowDrawingActivity(); return; }
        if (hasBasket) { ShowBasketActivity(); return; }
        if (hasOrdering) { ShowOrderingActivity(); return; }
        if (hasReading) { ShowReadingActivity(); return; }
        HideAllActivities();
    }

    // -----------------------------------------------------------------------
    // Ocultar actividades no disponibles en el bloque actual
    // Llamado por BlockManager para asegurarse de que el WristMenu
    // no muestre actividades que el bloque no tiene.
    // -----------------------------------------------------------------------

    public void SetAvailableActivities(bool hasDrawing, bool hasBasket, bool hasOrdering, bool hasReading)
    {
        // Desactiva los roots de actividades no disponibles en este bloque
        if (drawingActivityRoot != null && !hasDrawing) drawingActivityRoot.SetActive(false);
        if (objectBasketActivityRoot != null && !hasBasket) objectBasketActivityRoot.SetActive(false);
        if (orderingActivityRoot != null && !hasOrdering) orderingActivityRoot.SetActive(false);
        if (readingActivityRoot != null && !hasReading) readingActivityRoot.SetActive(false);
    }

    private void SetActivityState(bool drawing, bool basket, bool ordering, bool reading)
    {
        if (drawingActivityRoot != null) drawingActivityRoot.SetActive(drawing);
        if (objectBasketActivityRoot != null) objectBasketActivityRoot.SetActive(basket);
        if (orderingActivityRoot != null) orderingActivityRoot.SetActive(ordering);
        if (readingActivityRoot != null) readingActivityRoot.SetActive(reading);
    }
}
