using UnityEngine;

public class BoardProjector : MonoBehaviour
{
    [Tooltip("Transform del Quad/pizarrón. Sus coords locales (x,y) serán el plano de escritura.")]
    [SerializeField] private Transform boardTransform;

    public Transform BoardTransform => boardTransform;

    public Vector2 WorldToBoard2D(Vector3 worldPoint)
    {
        // Convierte a coords locales del pizarrón:
        // local.x y local.y son tu 2D para reconocer
        Vector3 local = boardTransform.InverseTransformPoint(worldPoint);
        return new Vector2(local.x, local.y);
    }

    public Vector3 Board2DToWorld(Vector2 boardPoint)
    {
        // Útil si luego quieres dibujar un "ghost" desde plantillas
        Vector3 local = new Vector3(boardPoint.x, boardPoint.y, 0f);
        return boardTransform.TransformPoint(local);
    }
}