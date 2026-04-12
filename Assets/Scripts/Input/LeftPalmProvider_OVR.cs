using UnityEngine;

public class LeftPalmProvider_OVR : MonoBehaviour
{
    [SerializeField] private GameObject trackingHand;

    [Header("Generated Palm Anchor")]
    [SerializeField] private bool createPalmAnchorObject = true;
    [SerializeField] private string palmAnchorName = "LeftPalmAnchor";

    public Transform PalmTransform => palmAnchor;

    private OVRSkeleton skeleton;
    private Transform wrist;
    private Transform indexMetacarpal;
    private Transform pinkyMetacarpal;

    private Transform palmAnchor;
    private bool bound;

    void Awake()
    {
        skeleton = trackingHand != null ? trackingHand.GetComponent<OVRSkeleton>() : null;

        if (createPalmAnchorObject)
        {
            var go = new GameObject(palmAnchorName);
            palmAnchor = go.transform;
        }
    }

    void Update()
    {
        if (!bound)
        {
            bound = TryBind();
            if (!bound) return;
        }

        UpdatePalmAnchor();
    }

    private bool TryBind()
    {
        if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
            return false;

        foreach (var b in skeleton.Bones)
        {
            switch (b.Id)
            {
                case OVRSkeleton.BoneId.Hand_WristRoot:
                    wrist = b.Transform;
                    break;

                case OVRSkeleton.BoneId.Hand_Index1:
                    indexMetacarpal = b.Transform;
                    break;

                case OVRSkeleton.BoneId.Hand_Pinky1:
                    pinkyMetacarpal = b.Transform;
                    break;
            }
        }

        return wrist != null && indexMetacarpal != null && pinkyMetacarpal != null && palmAnchor != null;
    }

    private void UpdatePalmAnchor()
    {
        if (wrist == null || indexMetacarpal == null || pinkyMetacarpal == null || palmAnchor == null)
            return;

        // Centro aproximado de la palma
        Vector3 knuckleMid = (indexMetacarpal.position + pinkyMetacarpal.position) * 0.5f;
        Vector3 palmPos = Vector3.Lerp(wrist.position, knuckleMid, 0.6f);

        // Ejes aproximados de la palma
        Vector3 acrossPalm = (pinkyMetacarpal.position - indexMetacarpal.position).normalized;
        Vector3 forwardPalm = (knuckleMid - wrist.position).normalized;
        Vector3 palmNormal = Vector3.Cross(acrossPalm, forwardPalm).normalized;

        // Ajuste para que el "up" de la palma apunte hacia afuera
        Quaternion palmRot = Quaternion.LookRotation(forwardPalm, palmNormal);

        palmAnchor.position = palmPos;
        palmAnchor.rotation = palmRot;
    }
}
