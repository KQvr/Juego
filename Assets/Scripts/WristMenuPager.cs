using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Navegacion por swipe entre actividades en el menu de muneca.
/// Detecta pinch + movimiento horizontal del dedo indice para cambiar pagina.
/// Muestra el nombre de la actividad actual y un boton para entrar.
///
/// Requiere en el Inspector:
///   - IndexPinchGate_OVR y IndexTipProvider_OVR de la mano derecha
///   - TMP_Text para el nombre de la actividad
///   - TMP_Text para el indicador de pagina (ej: "2 / 4")
///   - Button de entrar
///   - ActivityMenuManager
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

    [Header("Manager")]
    [SerializeField] private ActivityMenuManager activityMenuManager;

    [Header("Swipe Settings")]
    [Tooltip("Distancia horizontal minima (metros) para contar como swipe.")]
    [SerializeField] private float swipeThreshold = 0.06f;
    [Tooltip("Segundos de cooldown entre swipes para evitar saltos multiples.")]
    [SerializeField] private float swipeCooldown = 0.4f;

    // Nombres que se muestran en el menu
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

        UpdateUI();
    }

    void OnDestroy()
    {
        if (enterButton != null)
            enterButton.onClick.RemoveListener(EnterCurrentActivity);
    }

    void Update()
    {
        if (indexTipProvider == null || indexTipProvider.TipTransform == null) return;
        if (pinchGate == null) return;

        bool isPinching = pinchGate.IsPinchingStrong;
        Vector3 tipPos = indexTipProvider.TipTransform.position;

        // Registrar inicio del pinch
        if (isPinching && !wasPinching)
        {
            pinchStartPosition = tipPos;
        }

        // Detectar swipe al soltar el pinch
        if (!isPinching && wasPinching)
        {
            float delta = tipPos.x - pinchStartPosition.x;

            if (Mathf.Abs(delta) >= swipeThreshold && Time.time - lastSwipeTime > swipeCooldown)
            {
                if (delta > 0)
                    NavigateNext();
                else
                    NavigatePrevious();

                lastSwipeTime = Time.time;
            }
        }

        wasPinching = isPinching;
    }

    private void NavigateNext()
    {
        currentPage = (currentPage + 1) % activityNames.Length;
        UpdateUI();
    }

    private void NavigatePrevious()
    {
        currentPage = (currentPage - 1 + activityNames.Length) % activityNames.Length;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (activityNameText != null)
            activityNameText.text = activityNames[currentPage];

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{currentPage + 1} / {activityNames.Length}";
    }

    private void EnterCurrentActivity()
    {
        if (activityMenuManager == null) return;

        switch (currentPage)
        {
            case 0: activityMenuManager.ShowDrawingActivity();   break;
            case 1: activityMenuManager.ShowBasketActivity();    break;
            case 2: activityMenuManager.ShowOrderingActivity();  break;
            case 3: activityMenuManager.ShowReadingActivity();   break;
        }
    }
}
