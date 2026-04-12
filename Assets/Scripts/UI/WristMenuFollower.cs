using UnityEngine;

public class WristMenuFollower : MonoBehaviour
{
    [Header("Providers")]
    [SerializeField] private LeftWristProvider_OVR wristProvider;
    [SerializeField] private LeftPalmProvider_OVR palmProvider;
    [SerializeField] private Camera mainCamera;

    [Header("Canvas")]
    [SerializeField] private Canvas wristCanvas;

    [Header("Blocking")]
    [SerializeField] private WristMenuBlocker menuBlocker;

    [Header("Offsets")]
    [SerializeField] private Vector3 wristLocalOffset = new Vector3(0.00f, 0.03f, 0.00f);
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(0f, 180f, 90f);

    [Header("Visibility")]
    [SerializeField] private float palmFacingCameraThreshold = 0.15f;
    [SerializeField] private float minHandHeight = 0.75f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 18f;
    [SerializeField] private float rotationSmooth = 14f;

    void LateUpdate()
    {
        if (wristProvider == null || palmProvider == null || wristCanvas == null || mainCamera == null)
            return;

        Transform wrist = wristProvider.WristTransform;
        Transform palm = palmProvider.PalmTransform;

        if (wrist == null || palm == null)
            return;

        bool shouldShow = ShouldShowMenu(palm);

        if (wristCanvas.gameObject.activeSelf != shouldShow)
            wristCanvas.gameObject.SetActive(shouldShow);

        if (!shouldShow) return;

        Vector3 targetPos = wrist.TransformPoint(wristLocalOffset);

        Quaternion wristRot = wrist.rotation * Quaternion.Euler(rotationOffsetEuler);

        Vector3 toCamera = (mainCamera.transform.position - targetPos).normalized;
        Quaternion lookRot = Quaternion.LookRotation(-toCamera, Vector3.up);

        Quaternion targetRot = Quaternion.Slerp(wristRot, lookRot, 0.55f);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionSmooth);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmooth);
    }

    private bool ShouldShowMenu(Transform palm)
    {
        if (menuBlocker != null && menuBlocker.IsBlocked)
            return false;

        Vector3 palmNormal = palm.up;
        Vector3 toCamera = (mainCamera.transform.position - palm.position).normalized;

        float facing = Vector3.Dot(palmNormal, toCamera);
        bool palmFacingUser = facing > palmFacingCameraThreshold;
        bool handHighEnough = palm.position.y > minHandHeight;

        return palmFacingUser && handHighEnough;
    }
}