using UnityEngine;

/// <summary>
/// Gestiona la navegacion entre las 4 actividades del proyecto.
/// Cada boton del menu de muneca llama al metodo correspondiente.
///
/// Conexion en el Inspector:
///   Boton "Dibujo"   → ShowDrawingActivity()
///   Boton "Cesta"    → ShowBasketActivity()
///   Boton "Ordenar"  → ShowOrderingActivity()
///   Boton "Lectura"  → ShowReadingActivity()
/// </summary>
public class ActivityMenuManager : MonoBehaviour
{
    [Header("Activity Roots")]
    [SerializeField] private GameObject drawingActivityRoot;
    [SerializeField] private GameObject basketActivityRoot;
    [SerializeField] private GameObject orderingActivityRoot;
    [SerializeField] private GameObject readingActivityRoot;

    [Header("Opciones")]
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
        SetActivityState(true, false, false, false);
    }

    public void ShowBasketActivity()
    {
        SetActivityState(false, true, false, false);
    }

    public void ShowOrderingActivity()
    {
        SetActivityState(false, false, true, false);
    }

    public void ShowReadingActivity()
    {
        SetActivityState(false, false, false, true);
    }

    public void HideAllActivities()
    {
        SetActivityState(false, false, false, false);
    }

    private void SetActivityState(bool drawing, bool basket, bool ordering, bool reading)
    {
        if (drawingActivityRoot != null) drawingActivityRoot.SetActive(drawing);
        if (basketActivityRoot != null) basketActivityRoot.SetActive(basket);
        if (orderingActivityRoot != null) orderingActivityRoot.SetActive(ordering);
        if (readingActivityRoot != null) readingActivityRoot.SetActive(reading);
    }
}