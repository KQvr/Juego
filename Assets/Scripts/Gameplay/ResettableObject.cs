using System.Collections;
using UnityEngine;

public class ResettableObject : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Transform defaultRespawnPoint;
    [SerializeField] private bool useOwnStartAsFallback = true;

    [Tooltip("Si esta marcado (recomendado), el objeto siempre conserva la rotacion que tiene en el editor/prefab y el respawn point solo controla la POSICION. Si esta desmarcado, la rotacion la toma del respawn point.")]
    [SerializeField] private bool preserveOwnRotation = true;

    [SerializeField] private float respawnDelay = 0.5f;

    [Header("Safety")]
    [SerializeField] private float minYBeforeAutoRespawn = -1.0f;
    [SerializeField] private bool monitorFall = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Quaternion ownInitialRotation;
    private Rigidbody rb;
    private Coroutine respawnRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Siempre capturamos la rotacion inicial propia del objeto (editor/prefab)
        ownInitialRotation = transform.rotation;

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
        transform.rotation = preserveOwnRotation ? ownInitialRotation : startRotation;

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

        // Si preserveOwnRotation esta activo, no copiamos la rotacion del respawn point.
        // El objeto mantiene su rotacion propia (capturada en Awake).
        if (!preserveOwnRotation)
            startRotation = newRespawnPoint.rotation;
    }
}