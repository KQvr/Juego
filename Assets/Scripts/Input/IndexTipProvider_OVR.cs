using UnityEngine;

public class IndexTipProvider_OVR : MonoBehaviour
{
    [SerializeField] private GameObject trackingHand;

    public Transform TipTransform { get; private set; }

    private OVRSkeleton skeleton;
    private bool bound;

    void Awake()
    {
        skeleton = trackingHand.GetComponent<OVRSkeleton>();
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
