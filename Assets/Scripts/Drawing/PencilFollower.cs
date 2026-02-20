using UnityEngine;

public class PencilFollower : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private IndexTipProvider_OVR indexTipProvider;

    [Header("Pencil")]
    [SerializeField] private Transform pencilRoot;   // el prefab instanciado en escena (o hijo)
    [SerializeField] private Transform pencilTip;    // el empty en la punta

    [Header("Offset (local to finger tip)")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0f, 0.02f);
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(0f, 0f, 0f);

    public Transform TipTransform => pencilTip != null ? pencilTip : pencilRoot;

    void LateUpdate()
    {
        if (indexTipProvider == null) return;
        var fingerTip = indexTipProvider.TipTransform;
        if (fingerTip == null) return;
        if (pencilRoot == null) return;

        // pega el lápiz a la punta del dedo
        pencilRoot.position = fingerTip.TransformPoint(positionOffset);
        pencilRoot.rotation = fingerTip.rotation * Quaternion.Euler(rotationOffsetEuler);
    }
}
