using UnityEngine;

public class IndexPinchGate_OVR : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private HandRole role = HandRole.Dominant;

    [SerializeField, Range(0f, 1f)] private float minPinchStrength = 0.5f;

    private OVRHand hand;

    public bool IsPinchingStrong { get; private set; }
    public bool ConfidenceHigh { get; private set; }

    void OnEnable()
    {
        HandPreference.OnChanged += OnHandChanged;
        ApplyHand();
    }

    void OnDisable()
    {
        HandPreference.OnChanged -= OnHandChanged;
    }

    private void OnHandChanged(Handedness _) => ApplyHand();

    private void ApplyHand()
    {
        var target = role == HandRole.Dominant ? HandPreference.Dominant : HandPreference.NonDominant;
        var go = target == Handedness.Left ? leftHand : rightHand;
        hand = go != null ? go.GetComponent<OVRHand>() : null;
    }

    void Update()
    {
        if (hand == null) return;

        ConfidenceHigh = hand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High;
        if (!ConfidenceHigh)
        {
            IsPinchingStrong = false;
            return;
        }

        bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        float strength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        IsPinchingStrong = pinching && strength >= minPinchStrength;
    }
}