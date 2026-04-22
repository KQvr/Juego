using UnityEngine;

/// <summary>
/// Evita que la punta del lápiz atraviese el pizarrón mientras se sostiene.
/// Trabaja en LateUpdate directamente sobre el Transform, después de que
/// el sistema Grabbable de Meta XR ya movió el lápiz ese frame.
///
/// Colócalo en el mismo GameObject que tiene el Rigidbody (Pencil root).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PencilDepthLimiter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform pencilTip;
    [SerializeField] private Transform boardTransform;

    [Header("Settings")]
    [Tooltip("Distancia mínima que la punta debe mantener frente al pizarrón (en metros).")]
    [SerializeField] private float surfaceStopOffset = 0.001f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (pencilTip == null || boardTransform == null) return;

        Vector3 boardNormal = boardTransform.forward;
        Vector3 boardPoint = boardTransform.position;

        // Distancia signed de la punta al plano del pizarrón
        // positivo = frente al pizarrón (lado del usuario)
        // negativo = detrás del pizarrón (penetrando)
        float signedDist = Vector3.Dot(pencilTip.position - boardPoint, boardNormal);

        if (signedDist < surfaceStopOffset)
        {
            float correction = surfaceStopOffset - signedDist;

            // Mover el root del lápiz completo hacia afuera del pizarrón
            transform.position += boardNormal * correction;

            // Cancelar la componente de velocidad que va hacia el pizarrón
            float velocityIntoBoard = Vector3.Dot(rb.linearVelocity, -boardNormal);
            if (velocityIntoBoard > 0f)
                rb.linearVelocity -= (-boardNormal) * velocityIntoBoard;
        }
    }
}