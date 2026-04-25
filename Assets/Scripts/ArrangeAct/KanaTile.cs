using Oculus.Interaction;
using TMPro;
using UnityEngine;

/// <summary>
/// Colócalo en el prefab de la tile kana grabable.
/// El prefab necesita: Rigidbody, Collider, Grabbable (Meta XR), TMP_Text hijo.
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
    }

    /// <summary>
    /// Llamado por el manager al crear la tile para esta ronda.
    /// </summary>
    public void Initialize(string character, Vector3 position, Quaternion rotation)
    {
        kanaCharacter = character;
        homePosition = position;
        homeRotation = rotation;

        if (kanaLabel != null)
            kanaLabel.text = character;

        transform.position = position;
        transform.rotation = rotation;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Fija la tile en la posición del slot.
    /// </summary>
    public void SnapToSlot(Vector3 position, Quaternion rotation)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        transform.position = position;
        transform.rotation = rotation;
    }

    /// <summary>
    /// Libera la tile del slot (cuando el jugador la agarra de nuevo).
    /// </summary>
    public void ReleaseFromSlot()
    {
        if (rb != null)
            rb.isKinematic = false;
    }

    /// <summary>
    /// Regresa la tile a su posición inicial de la ronda.
    /// </summary>
    public void Respawn()
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = homePosition;
        transform.rotation = homeRotation;
    }
}
