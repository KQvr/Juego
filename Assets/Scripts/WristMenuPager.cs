using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu de muneca — navega entre actividades del bloque actual
/// y tiene un boton para volver al menu de bloques.
///
/// Canvas del menu de muneca:
///   ├── ActivityNameText   (TMP_Text) → nombre de la actividad actual
///   ├── PageIndicatorText  (TMP_Text) → "2 / 4"
///   ├── EnterButton        (Button)   → entra a la actividad seleccionada
///   └── BackToMenuButton   (Button)   → vuelve al menu de bloques
/// </summary>
public class WristMenuPager : MonoBehaviour
{
    [Header("Interaccion")]
    [SerializeField] private IndexPinchGate_OVR pinchGate;
    [SerializeField] private IndexTipProvider_OVR indexTipProvider;

    [Header("UI")]
    [SerializeField] private TMP_Text activityNameText;
    [SerializeField] private TMP_Text pageIndicatorText;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button resetBlockButton;

    [Header("Managers")]
    [SerializeField] private ActivityMenuManager activityMenuManager;
    [SerializeField] private BlockMenuUI blockMenuUI;

    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 0.06f;
    [SerializeField] private float swipeCooldown = 0.4f;

    private readonly string[] activityNames = new string[]
    {
        "Dibujo de Kana",
        "Cesta de Objetos",
        "Ordenar Kanas",
        "Lectura"
    };

    private int currentPage = 0;
    private bool wasPinching = false;
    private Vector3 pinchStartPosition;
    private float lastSwipeTime = -999f;

    void Start()
    {
        if (enterButton != null)
            enterButton.onClick.AddListener(EnterCurrentActivity);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(GoBackToMenu);

        if (resetBlockButton != null)
            resetBlockButton.onClick.AddListener(ResetCurrentBlock);

        UpdateUI();
    }

    void OnDestroy()
    {
        if (enterButton != null)
            enterButton.onClick.RemoveListener(EnterCurrentActivity);

        if (backToMenuButton != null)
            backToMenuButton.onClick.RemoveListener(GoBackToMenu);

        if (resetBlockButton != null)
            resetBlockButton.onClick.RemoveListener(ResetCurrentBlock);
    }

    void Update()
    {
        if (indexTipProvider == null || indexTipProvider.TipTransform == null) return;
        if (pinchGate == null) return;

        bool isPinching = pinchGate.IsPinchingStrong;
        Vector3 tipPos = indexTipProvider.TipTransform.position;

        if (isPinching && !wasPinching)
            pinchStartPosition = tipPos;

        if (!isPinching && wasPinching)
        {
            float delta = tipPos.x - pinchStartPosition.x;

            if (Mathf.Abs(delta) >= swipeThreshold &&
                Time.time - lastSwipeTime > swipeCooldown)
            {
                if (delta > 0)
                    currentPage = (currentPage - 1 + activityNames.Length) % activityNames.Length;
                else
                    currentPage = (currentPage + 1) % activityNames.Length;

                lastSwipeTime = Time.time;
                UpdateUI();
            }
        }

        wasPinching = isPinching;
    }

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    private void UpdateUI()
    {
        if (activityNameText != null)
            activityNameText.text = activityNames[currentPage];

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{currentPage + 1} / {activityNames.Length}";
    }

    // -----------------------------------------------------------------------
    // Acciones
    // -----------------------------------------------------------------------

    private void EnterCurrentActivity()
    {
        if (activityMenuManager == null) return;

        switch (currentPage)
        {
            case 0: activityMenuManager.ShowDrawingActivity(); break;
            case 1: activityMenuManager.ShowBasketActivity(); break;
            case 2: activityMenuManager.ShowOrderingActivity(); break;
            case 3: activityMenuManager.ShowReadingActivity(); break;
        }
    }

    private void GoBackToMenu()
    {
        // Ocultar todas las actividades y volver al menu de bloques
        activityMenuManager?.HideAllActivities();
        blockMenuUI?.ShowMenu();
    }

    private void ResetCurrentBlock()
    {
        BlockManager.Instance?.ResetCurrentBlockProgress();
    }
}