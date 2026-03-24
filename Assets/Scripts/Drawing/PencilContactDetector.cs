using UnityEngine;

public class PencilContactDetector : MonoBehaviour
{
    [SerializeField] private LayerMask blackboardLayer;

    [Header("Geometry offsets")]
    [Tooltip("Radio del SphereCollider del PencilTip (para que no se hunda).")]
    [SerializeField] private float tipRadius = 0.004f;

    [Tooltip("Mitad del ancho del trazo (strokeWidth).")]
    [SerializeField] private float halfStrokeWidth = 0.004f;

    [Tooltip("Extra pequeño para separar visualmente.")]
    [SerializeField] private float extraOffset = 0.0008f;

    public bool IsTouching { get; private set; }
    public Vector3 ContactPoint { get; private set; }

    private Transform boardT;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsBlackboard(other)) return;
        boardT = other.transform;
        IsTouching = true;
        UpdateContactPoint();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsBlackboard(other)) return;
        boardT = other.transform;
        IsTouching = true;
        UpdateContactPoint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsBlackboard(other)) return;
        IsTouching = false;
        boardT = null;
    }

    private void UpdateContactPoint()
    {
        if (boardT == null)
        {
            ContactPoint = transform.position;
            return;
        }

        // 1) Proyecta al plano local del pizarrón (z = 0 en local)
        Vector3 local = boardT.InverseTransformPoint(transform.position);
        local.z = 0f;

        // 2) Vuelve a world y empuja hacia +Z del pizarrón
        Vector3 worldOnPlane = boardT.TransformPoint(local);
        Vector3 n = boardT.forward; // Quad frente +Z

        float pushOut = tipRadius + halfStrokeWidth + extraOffset;
        ContactPoint = worldOnPlane + n * pushOut;
    }

    private bool IsBlackboard(Collider other)
    {
        return ((1 << other.gameObject.layer) & blackboardLayer) != 0;
    }
}