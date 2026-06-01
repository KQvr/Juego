using UnityEngine;

/// <summary>
/// Provee el Transform de la muneca. A pesar de llamarse "Left",
/// soporta ambas manos segun HandPreference.
/// </summary>
public class LeftWristProvider_OVR : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private HandRole role = HandRole.NonDominant;

    public Transform WristTransform { get; private set; }

    private OVRSkeleton skeleton;
    private bool bound;

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
        skeleton = go != null ? go.GetComponent<OVRSkeleton>() : null;
        bound = false;
        WristTransform = null;
    }

    void Update()
    {
        if (!bound)
            bound = TryBind();
    }

    private bool TryBind()
    {
        if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
            return false;
        foreach (var b in skeleton.Bones)
        {
            if (b.Id == OVRSkeleton.BoneId.Hand_WristRoot)
            {
                WristTransform = b.Transform;
                return WristTransform != null;
            }
        }
        return false;
    }
}