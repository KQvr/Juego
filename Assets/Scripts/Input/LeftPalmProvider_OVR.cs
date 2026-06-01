using UnityEngine;

/// <summary>
/// Provee un Transform en el centro aproximado de la palma de la mano.
/// A pesar de llamarse "Left", soporta ambas manos segun HandPreference.
/// </summary>
public class LeftPalmProvider_OVR : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightHand;
    [SerializeField] private HandRole role = HandRole.NonDominant;

    [Header("Generated Palm Anchor")]
    [SerializeField] private bool createPalmAnchorObject = true;
    [SerializeField] private string palmAnchorName = "PalmAnchor";

    public Transform PalmTransform => palmAnchor;

    private OVRSkeleton skeleton;
    private Transform wrist;
    private Transform indexMetacarpal;
    private Transform pinkyMetacarpal;
    private Transform palmAnchor;
    private bool bound;

    void Awake()
    {
        if (createPalmAnchorObject && palmAnchor == null)
        {
            var go = new GameObject(palmAnchorName);
            palmAnchor = go.transform;
        }
    }

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
        wrist = null;
        indexMetacarpal = null;
        pinkyMetacarpal = null;
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

        Vector3 knuckleMid = (indexMetacarpal.position + pinkyMetacarpal.position) * 0.5f;
        Vector3 palmPos = Vector3.Lerp(wrist.position, knuckleMid, 0.6f);

        Vector3 acrossPalm = (pinkyMetacarpal.position - indexMetacarpal.position).normalized;
        Vector3 forwardPalm = (knuckleMid - wrist.position).normalized;
        Vector3 palmNormal = Vector3.Cross(acrossPalm, forwardPalm).normalized;

        Quaternion palmRot = Quaternion.LookRotation(forwardPalm, palmNormal);

        palmAnchor.position = palmPos;
        palmAnchor.rotation = palmRot;
    }
}