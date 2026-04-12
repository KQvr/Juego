using System.Collections;
using UnityEngine;

public class ResettableObject : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Transform defaultRespawnPoint;
    [SerializeField] private bool useOwnStartAsFallback = true;
    [SerializeField] private float respawnDelay = 0.5f;

    [Header("Safety")]
    [SerializeField] private float minYBeforeAutoRespawn = -1.0f;
    [SerializeField] private bool monitorFall = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody rb;
    private Coroutine respawnRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (defaultRespawnPoint != null)
        {
            startPosition = defaultRespawnPoint.position;
            startRotation = defaultRespawnPoint.rotation;
        }
        else if (useOwnStartAsFallback)
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }
    }

    void Update()
    {
        if (!monitorFall) return;
        if (!gameObject.activeInHierarchy) return;

        if (transform.position.y < minYBeforeAutoRespawn)
            RespawnNow();
    }

    public void RespawnNow()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }

        ApplyRespawn();
    }

    public void RespawnWithDelay()
    {
        if (respawnRoutine != null)
            StopCoroutine(respawnRoutine);

        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        ApplyRespawn();
        respawnRoutine = null;
    }

    private void ApplyRespawn()
    {
        gameObject.SetActive(true);

        transform.position = startPosition;
        transform.rotation = startRotation;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.Sleep();
        }
    }

    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        if (newRespawnPoint == null) return;

        defaultRespawnPoint = newRespawnPoint;
        startPosition = newRespawnPoint.position;
        startRotation = newRespawnPoint.rotation;
    }
}