using UnityEngine;

public class IndexPinchGate_OVR : MonoBehaviour
{
    [SerializeField] private GameObject trackingHand;
    [SerializeField, Range(0f, 1f)] private float minPinchStrength = 0.5f;

    private OVRHand hand;

    public bool IsPinchingStrong { get; private set; }
    public bool ConfidenceHigh { get; private set; }

    void Awake()
    {
        hand = trackingHand.GetComponent<OVRHand>();
    }

    void Update()
    {
        if (hand == null) return;

        ConfidenceHigh = hand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High;
        if (!ConfidenceHigh) { IsPinchingStrong = false; return; }

        bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float strength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        IsPinchingStrong = pinching && strength >= minPinchStrength;
    }
}
