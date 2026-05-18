using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú de selección de bloques con navegación por swipe (pinch + movimiento horizontal).
/// Muestra un bloque a la vez con nombre, descripción, estrellas y estado de desbloqueo.
///
/// Estructura del Canvas sugerida:
///   BlockMenuCanvas
///   ├── BlockNameText      (TMP_Text)
///   ├── BlockDescText      (TMP_Text)
///   ├── StarsText          (TMP_Text) → ej: "★★☆"
///   ├── PageIndicatorText  (TMP_Text) → ej: "2 / 8"
///   ├── LockIcon           (GameObject) → visible si bloqueado
///   ├── EnterButton        (Button) → activo si desbloqueado
///   └── StatusText         (TMP_Text) → feedback temporal
/// </summary>
public class BlockMenuUI : MonoBehaviour
{
    [Header("Interaccion")]
    [SerializeField] private IndexPinchGate_OVR pinchGate;
    [SerializeField] private IndexTipProvider_OVR indexTipProvider;

    [Header("UI")]
    [SerializeField] private TMP_Text blockNameText;
    [SerializeField] private TMP_Text blockDescText;
    [SerializeField] private TMP_Text starsText;
    [SerializeField] private TMP_Text pageIndicatorText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Button enterButton;

    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 0.06f;
    [SerializeField] private float swipeCooldown  = 0.4f;

    private int currentPage = 0;
    private bool wasPinching = false;
    private Vector3 pinchStartPosition;
    private float lastSwipeTime = -999f;

    void Start()
    {
        if (enterButton != null)
            enterButton.onClick.AddListener(EnterCurrentBlock);

        if (BlockManager.Instance != null)
        {
            BlockManager.Instance.OnBlockStarsChanged += OnStarsChanged;
            BlockManager.Instance.OnBlockUnlocked     += OnBlockUnlocked;
        }

        RefreshUI();
    }

    void OnDestroy()
    {
        if (enterButton != null)
            enterButton.onClick.RemoveListener(EnterCurrentBlock);

        if (BlockManager.Instance != null)
        {
            BlockManager.Instance.OnBlockStarsChanged -= OnStarsChanged;
            BlockManager.Instance.OnBlockUnlocked     -= OnBlockUnlocked;
        }
    }

    void Update()
    {
        if (indexTipProvider == null || indexTipProvider.TipTransform == null) return;
        if (pinchGate == null) return;

        bool isPinching = pinchGate.IsPinchingStrong;
        Vector3 tipPos  = indexTipProvider.TipTransform.position;

        if (isPinching && !wasPinching)
            pinchStartPosition = tipPos;

        if (!isPinching && wasPinching)
        {
            float delta = tipPos.x - pinchStartPosition.x;

            if (Mathf.Abs(delta) >= swipeThreshold &&
                Time.time - lastSwipeTime > swipeCooldown)
            {
                var blocks = BlockManager.Instance?.GetBlocks();
                if (blocks == null || blocks.Count == 0) return;

                if (delta > 0)
                    currentPage = (currentPage - 1 + blocks.Count) % blocks.Count;
                else
                    currentPage = (currentPage + 1) % blocks.Count;

                lastSwipeTime = Time.time;
                RefreshUI();
            }
        }

        wasPinching = isPinching;
    }

    // -----------------------------------------------------------------------
    // UI
    // -----------------------------------------------------------------------

    private void RefreshUI()
    {
        if (BlockManager.Instance == null) return;

        var blocks = BlockManager.Instance.GetBlocks();
        if (blocks == null || blocks.Count == 0) return;

        currentPage = Mathf.Clamp(currentPage, 0, blocks.Count - 1);
        var block = blocks[currentPage];

        bool unlocked = BlockManager.Instance.IsUnlocked(block.blockId);
        int  stars    = BlockManager.Instance.GetStars(block.blockId);

        if (blockNameText     != null) blockNameText.text     = block.blockName;
        if (blockDescText     != null) blockDescText.text     = block.description;
        if (starsText         != null) starsText.text         = BuildStarsString(stars);
        if (pageIndicatorText != null) pageIndicatorText.text = $"{currentPage + 1} / {blocks.Count}";
        if (lockIcon          != null) lockIcon.SetActive(!unlocked);
        if (enterButton       != null) enterButton.interactable = unlocked;
        if (statusText        != null) statusText.text          = unlocked ? "" : "Completa el bloque anterior";
    }

    private string BuildStarsString(int stars)
    {
        return stars switch
        {
            1 => "★☆☆",
            2 => "★★☆",
            3 => "★★★",
            _ => "☆☆☆"
        };
    }

    private void EnterCurrentBlock()
    {
        if (BlockManager.Instance == null) return;
        BlockManager.Instance.ShowBlock(currentPage);
    }

    // -----------------------------------------------------------------------
    // Callbacks del BlockManager
    // -----------------------------------------------------------------------

    private void OnStarsChanged(string blockId, int stars)
    {
        var blocks = BlockManager.Instance?.GetBlocks();
        if (blocks == null) return;

        if (blocks[currentPage].blockId == blockId)
            RefreshUI();
    }

    private void OnBlockUnlocked(string blockId)
    {
        RefreshUI();

        if (statusText != null)
            statusText.text = "Nuevo bloque desbloqueado!";

        Invoke(nameof(ClearStatus), 3f);
    }

    private void ClearStatus()
    {
        if (statusText != null)
            statusText.text = "";
    }
}
