using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu flotante con boton de toggle.
/// Por defecto solo se muestra un boton pequenio; al pulsarlo se despliega
/// el panel completo con tabs de actividades + back + reset.
///
/// Estructura recomendada del Canvas:
///   GameMenuCanvas (World Space, posicion fija frente al usuario)
///   ├── ToggleButton            (siempre activo, con label "Menu" o icono)
///   └── ExpandedPanel           (active/inactive segun toggle)
///         ├── TabsRow
///         │     ├── DrawingTab
///         │     ├── BasketTab
///         │     ├── OrderingTab
///         │     └── ReadingTab
///         ├── BackToMenuButton
///         └── ResetBlockButton
/// </summary>
public class ActivityMenuPanel : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject expandedPanel;
    [Tooltip("True: el panel empieza colapsado. False: empieza visible.")]
    [SerializeField] private bool startCollapsed = true;
    [Tooltip("True: cerrar el panel automaticamente al seleccionar una actividad.")]
    [SerializeField] private bool collapseAfterSelection = true;

    [Header("Visibilidad global")]
    [Tooltip("GameObject contenedor que se oculta cuando no hay un bloque activo (menu de bloques visible). Si esta vacio, se intenta usar el Canvas del mismo GameObject.")]
    [SerializeField] private GameObject menuRoot;

    private Canvas menuCanvas;

    [Header("Tabs de actividad")]
    [SerializeField] private Button drawingTab;
    [SerializeField] private Button basketTab;
    [SerializeField] private Button orderingTab;
    [SerializeField] private Button readingTab;

    [Header("Visual del tab activo")]
    [SerializeField] private Color activeTabColor = new Color(0.4f, 0.7f, 1f, 1f);
    [SerializeField] private Color normalTabColor = Color.white;

    [Header("Otros botones")]
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button resetBlockButton;

    [Header("Managers")]
    [SerializeField] private ActivityMenuManager activityMenuManager;
    [SerializeField] private BlockMenuUI blockMenuUI;

    private int currentActivity = -1;
    private string lastKnownBlockId = null;

    void Awake()
    {
        if (menuRoot == null)
        {
            // Busca Canvas en self primero, luego en padres
            menuCanvas = GetComponent<Canvas>();
            if (menuCanvas == null)
                menuCanvas = GetComponentInParent<Canvas>();

            if (menuCanvas == null)
                Debug.LogWarning($"[ActivityMenuPanel] No se encontro ni Menu Root ni Canvas. " +
                                 $"El menu no podra ocultarse/mostrarse automaticamente. " +
                                 $"Asigna 'Menu Root' en el Inspector o pon este script en un GameObject con Canvas.",
                                 this);
        }
    }

    void Update()
    {
        bool shouldShow = BlockManager.Instance != null && BlockManager.Instance.HasActiveBlock;

        // Log cuando cambia el estado
        if (shouldShow != lastLoggedShouldShow)
        {
            Debug.Log($"[ActivityMenuPanel] shouldShow cambio a: {shouldShow} " +
                      $"(Instance: {(BlockManager.Instance != null ? "OK" : "NULL")}, " +
                      $"HasActiveBlock: {(BlockManager.Instance?.HasActiveBlock ?? false)}, " +
                      $"menuRoot: {(menuRoot != null ? menuRoot.name : "NULL")}, " +
                      $"menuCanvas: {(menuCanvas != null ? menuCanvas.name : "NULL")})");
            lastLoggedShouldShow = shouldShow;
        }

        if (menuRoot != null)
        {
            if (menuRoot.activeSelf != shouldShow)
                menuRoot.SetActive(shouldShow);
        }
        else if (menuCanvas != null)
        {
            if (menuCanvas.enabled != shouldShow)
                menuCanvas.enabled = shouldShow;
        }

        // Refrescar tabs si cambio el bloque activo (de null a real o entre bloques)
        string currentBlockId = BlockManager.Instance?.GetCurrentBlock()?.blockId;
        if (currentBlockId != lastKnownBlockId)
        {
            lastKnownBlockId = currentBlockId;
            currentActivity = -1; // resetear seleccion al cambiar de bloque
            RefreshTabsAvailability();
        }
    }

    private bool lastLoggedShouldShow = false;

    void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        if (drawingTab != null) drawingTab.onClick.AddListener(() => SelectActivity(0));
        if (basketTab != null) basketTab.onClick.AddListener(() => SelectActivity(1));
        if (orderingTab != null) orderingTab.onClick.AddListener(() => SelectActivity(2));
        if (readingTab != null) readingTab.onClick.AddListener(() => SelectActivity(3));

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(GoBackToMenu);

        if (resetBlockButton != null)
            resetBlockButton.onClick.AddListener(ResetCurrentBlock);

        if (expandedPanel != null)
            expandedPanel.SetActive(!startCollapsed);

        RefreshTabsAvailability();
    }

    void OnEnable()
    {
        RefreshTabsAvailability();
    }

    void OnDestroy()
    {
        if (toggleButton != null) toggleButton.onClick.RemoveListener(TogglePanel);

        if (drawingTab != null) drawingTab.onClick.RemoveAllListeners();
        if (basketTab != null) basketTab.onClick.RemoveAllListeners();
        if (orderingTab != null) orderingTab.onClick.RemoveAllListeners();
        if (readingTab != null) readingTab.onClick.RemoveAllListeners();

        if (backToMenuButton != null) backToMenuButton.onClick.RemoveListener(GoBackToMenu);
        if (resetBlockButton != null) resetBlockButton.onClick.RemoveListener(ResetCurrentBlock);
    }

    // -----------------------------------------------------------------------
    // Toggle
    // -----------------------------------------------------------------------

    public void TogglePanel()
    {
        if (expandedPanel == null) return;
        expandedPanel.SetActive(!expandedPanel.activeSelf);
    }

    public void OpenPanel()
    {
        if (expandedPanel != null) expandedPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (expandedPanel != null) expandedPanel.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Disponibilidad de tabs segun el bloque actual
    // -----------------------------------------------------------------------

    private void RefreshTabsAvailability()
    {
        var block = BlockManager.Instance?.GetCurrentBlock();

        SetTabInteractable(drawingTab, block != null && block.hasDrawing);
        SetTabInteractable(basketTab, block != null && block.hasBasket);
        SetTabInteractable(orderingTab, block != null && block.hasOrdering);
        SetTabInteractable(readingTab, block != null && block.hasReading);

        UpdateTabHighlights();
    }

    private void SetTabInteractable(Button btn, bool interactable)
    {
        if (btn == null) return;
        btn.interactable = interactable;
    }

    private void UpdateTabHighlights()
    {
        SetTabColor(drawingTab, currentActivity == 0);
        SetTabColor(basketTab, currentActivity == 1);
        SetTabColor(orderingTab, currentActivity == 2);
        SetTabColor(readingTab, currentActivity == 3);
    }

    private void SetTabColor(Button btn, bool isActive)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img == null) return;
        img.color = isActive ? activeTabColor : normalTabColor;
    }

    // -----------------------------------------------------------------------
    // Acciones
    // -----------------------------------------------------------------------

    private void SelectActivity(int index)
    {
        if (activityMenuManager == null) return;

        switch (index)
        {
            case 0: activityMenuManager.ShowDrawingActivity(); break;
            case 1: activityMenuManager.ShowBasketActivity(); break;
            case 2: activityMenuManager.ShowOrderingActivity(); break;
            case 3: activityMenuManager.ShowReadingActivity(); break;
        }

        currentActivity = index;
        UpdateTabHighlights();

        if (collapseAfterSelection)
            ClosePanel();
    }

    private void GoBackToMenu()
    {
        activityMenuManager?.HideAllActivities();
        blockMenuUI?.ShowMenu();
        currentActivity = -1;
        ClosePanel();
    }

    private void ResetCurrentBlock()
    {
        BlockManager.Instance?.ResetCurrentBlockProgress();
    }
}