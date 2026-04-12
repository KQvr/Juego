using UnityEngine;

public class LeftWristProvider_OVR : MonoBehaviour
{
    [SerializeField] private GameObject trackingHand;

    public Transform WristTransform { get; private set; }

    private OVRSkeleton skeleton;
    private bool bound;

    void Awake()
    {
        skeleton = trackingHand != null ? trackingHand.GetComponent<OVRSkeleton>() : null;
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