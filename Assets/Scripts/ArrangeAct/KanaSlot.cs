using System;
using UnityEngine;

/// <summary>
/// Zona receptora de una KanaTile. Colócalo en cada slot del pizarrón.
/// Necesita un Collider con isTrigger = true.
///
/// Lógica:
/// - Cuando una tile entra al trigger, se registra como candidata.
/// - Cuando el jugador suelta la tile (deja de agarrarla), se snappea al slot.
/// - Cuando el jugador agarra una tile ya colocada, el slot se libera automáticamente.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KanaSlot : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int slotIndex;

    [Header("Visual")]
    [SerializeField] private Renderer slotVisual;
    [SerializeField] private Color emptyColor  = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color filledColor = new Color(0.3f, 0.9f, 0.3f, 0.6f);

    public int SlotIndex => slotIndex;
    public KanaTile OccupiedBy { get; private set; }

    public event Action OnSlotChanged;

    private KanaTile candidateTile;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        UpdateVisual();
    }

    void Update()
    {
        // Si hay una tile colocada y el jugador la agarra → liberar el slot
        if (OccupiedBy != null)
        {
            if (OccupiedBy.IsBeingGrabbed)
            {
                OccupiedBy.ReleaseFromSlot();
                OccupiedBy = null;
                candidateTile = null;
                UpdateVisual();
                OnSlotChanged?.Invoke();
            }

            return;
        }

        // Si hay una tile candidata y el jugador la soltó → snappear
        if (candidateTile != null && !candidateTile.IsBeingGrabbed)
            PlaceTile(candidateTile);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (OccupiedBy != null) return;

        var tile = other.GetComponentInParent<KanaTile>();
        if (tile != null)
            candidateTile = tile;
    }

    private void OnTriggerExit(Collider other)
    {
        if (OccupiedBy != null) return;

        var tile = other.GetComponentInParent<KanaTile>();
        if (tile == candidateTile)
            candidateTile = null;
    }

    private void PlaceTile(KanaTile tile)
    {
        OccupiedBy = tile;
        candidateTile = null;

        tile.SnapToSlot(transform.position, transform.rotation);
        UpdateVisual();
        OnSlotChanged?.Invoke();
    }

    /// <summary>
    /// Libera el slot y la tile que contenía (llamado por el manager al resetear).
    /// </summary>
    public void ClearSlot()
    {
        if (OccupiedBy != null)
            OccupiedBy.ReleaseFromSlot();

        OccupiedBy = null;
        candidateTile = null;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (slotVisual == null) return;
        slotVisual.material.color = OccupiedBy != null ? filledColor : emptyColor;
    }
}
