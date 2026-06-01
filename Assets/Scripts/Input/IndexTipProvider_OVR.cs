using UnityEngine;

public class IndexTipProvider_OVR : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private HandRole role = HandRole.Dominant;

    public Transform TipTransform { get; private set; }

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
        TipTransform = null;
    }

    void Update()
    {
        if (!bound) bound = TryBind();
    }

    bool TryBind()
    {
        if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0) return false;
        var t = skeleton.GetSkeletonType();
        var id = (t == OVRSkeleton.SkeletonType.XRHandLeft || t == OVRSkeleton.SkeletonType.XRHandRight)
            ? OVRSkeleton.BoneId.XRHand_IndexTip
            : OVRSkeleton.BoneId.Hand_IndexTip;
        foreach (var b in skeleton.Bones)
            if (b.Id == id) { TipTransform = b.Transform; return TipTransform != null; }
        return false;
    }
}