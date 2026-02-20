using UnityEngine;

public class PencilContactDetector : MonoBehaviour
{
    [SerializeField] private LayerMask blackboardLayer;

    public bool IsTouching { get; private set; }
    public Vector3 ContactPoint { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (IsBlackboard(other))
        {
            IsTouching = true;
            ContactPoint = transform.position;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsBlackboard(other))
        {
            IsTouching = true;
            ContactPoint = transform.position;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsBlackboard(other))
        {
            IsTouching = false;
        }
    }

    private bool IsBlackboard(Collider other)
    {
        return ((1 << other.gameObject.layer) & blackboardLayer) != 0;
    }
}
