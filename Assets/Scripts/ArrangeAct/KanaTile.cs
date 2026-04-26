using Oculus.Interaction;
using TMPro;
using UnityEngine;

/// <summary>
/// Colócalo en el prefab de la tile kana grabable.
/// El prefab necesita: Rigidbody (Is Kinematic = true, Use Gravity = false),
/// BoxCollider (Is Trigger = true), ISDK_HandGrabInteraction,
/// ISDK_DistanceHandGrabInteraction, TMP_Text hijo.
/// </summary>
[DisallowMultipleComponent]
public class KanaTile : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TMP_Text kanaLabel;

    private string kanaCharacter;
    private Rigidbody rb;
    private Grabbable grabbable;

    private Vector3 homePosition;
    private Quaternion homeRotation;

    public string KanaCharacter => kanaCharacter;

    public bool IsBeingGrabbed =>
        grabbable != null &&
        grabbable.GrabPoints != null &&
        grabbable.GrabPoints.Count > 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        EnforceKinematic();
    }

    public void Initialize(string character, Vector3 position, Quaternion rotation)
    {
        kanaCharacter = character;
        homePosition = position;
        homeRotation = rotation;

        if (kanaLabel != null)
            kanaLabel.text = character;

        transform.position = position;
        transform.rotation = rotation;
        EnforceKinematic();
    }

    public void SnapToSlot(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        EnforceKinematic();
    }

    public void ReleaseFromSlot()
    {
        EnforceKinematic();
    }

    public void Respawn()
    {
        transform.position = homePosition;
        transform.rotation = homeRotation;
        EnforceKinematic();
    }

    private void EnforceKinematic()
    {
        if (rb == null) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
