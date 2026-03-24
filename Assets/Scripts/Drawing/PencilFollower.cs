using UnityEngine;

public class PencilFollower : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private IndexTipProvider_OVR indexTipProvider;

    [Header("Pencil")]
    [SerializeField] private Transform pencilRoot;   // el lápiz visual
    [SerializeField] private Transform pencilTip;    // empty en la punta

    [Header("Offset (local to finger tip)")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0.015f, -0.010f, 0.030f);
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(-35f, 20f, 90f);

    [Header("Stabilization")]
    [SerializeField] private float positionSmooth = 20f;
    [SerializeField] private float rotationSmooth = 20f;

    public Transform TipTransform => pencilTip != null ? pencilTip : pencilRoot;

    void LateUpdate()
    {
        if (indexTipProvider == null) return;

        var fingerTip = indexTipProvider.TipTransform;
        if (fingerTip == null) return;
        if (pencilRoot == null) return;

        Vector3 targetPos = fingerTip.TransformPoint(positionOffset);
        Quaternion targetRot = fingerTip.rotation * Quaternion.Euler(rotationOffsetEuler);

        pencilRoot.position = Vector3.Lerp(
            pencilRoot.position,
            targetPos,
            Time.deltaTime * positionSmooth
        );

        pencilRoot.rotation = Quaternion.Slerp(
            pencilRoot.rotation,
            targetRot,
            Time.deltaTime * rotationSmooth
        );
    }
}