using UnityEngine;

public class PencilContactDetector : MonoBehaviour
{
    [SerializeField] private LayerMask blackboardLayer;

    [Tooltip("Small push-out along the board normal so the stroke doesn't look buried.")]
    [SerializeField] private float surfaceOffset = 0.0015f;

    public bool IsTouching { get; private set; }
    public Vector3 ContactPoint { get; private set; }

    private Collider currentBoard;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsBlackboard(other)) return;

        currentBoard = other;
        IsTouching = true;
        UpdateContactPoint();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsBlackboard(other)) return;

        currentBoard = other;
        IsTouching = true;
        UpdateContactPoint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsBlackboard(other)) return;

        if (other == currentBoard) currentBoard = null;
        IsTouching = false;
    }

    private void UpdateContactPoint()
    {
        if (currentBoard == null)
        {
            ContactPoint = transform.position;
            return;
        }

        // Exact point on the board surface
        Vector3 p = currentBoard.ClosestPoint(transform.position);

        // Quad front is +Z, so push out along transform.forward
        Vector3 n = currentBoard.transform.forward;

        ContactPoint = p + n * surfaceOffset;
    }

    private bool IsBlackboard(Collider other)
    {
        return ((1 << other.gameObject.layer) & blackboardLayer) != 0;
    }
}