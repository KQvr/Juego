using Oculus.Interaction;
using UnityEngine;

public class GrabMenuBlockNotifier : MonoBehaviour
{
    [SerializeField] private WristMenuBlocker wristMenuBlocker;
    [SerializeField] private Grabbable grabbable;

    private bool lastGrabState = false;

    void Reset()
    {
        grabbable = GetComponent<Grabbable>();
    }

    void Awake()
    {
        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();
    }

    void Update()
    {
        if (wristMenuBlocker == null || grabbable == null)
            return;

        bool isGrabbed = grabbable.GrabPoints != null && grabbable.GrabPoints.Count > 0;

        if (isGrabbed != lastGrabState)
        {
            wristMenuBlocker.SetGrabBlocked(isGrabbed);
            lastGrabState = isGrabbed;
        }
    }

    void OnDisable()
    {
        if (wristMenuBlocker != null && lastGrabState)
        {
            wristMenuBlocker.SetGrabBlocked(false);
            lastGrabState = false;
        }
    }
}