using UnityEngine;

[DisallowMultipleComponent]
public class TubeRenderer : MonoBehaviour
{
    [Header("Geometry")]
    [SerializeField] private Vector3[] _positions;
    [Min(3)] public int _sides = 8;
    public float _radiusOne = 0.01f;
    public float _radiusTwo = 0.01f;

    [Header("Options")]
    [SerializeField] private bool _useWorldSpace = true;
    [SerializeField] private bool _useTwoRadii = false;

    // Collider is optional
    [SerializeField] private bool _colliderTrigger = false;

    public bool ColliderTrigger
    {
        get => _colliderTrigger;
        set
        {
            _colliderTrigger = value;
            ApplyColliderSettings();
        }
    }

    private Vector3[] _vertices;
    private int[] _indices;
    private Vector2[] _uvs;

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    private bool _initialized;

    public Material material
    {
        get => _meshRenderer != null ? _meshRenderer.material : null;
        set { if (_meshRenderer != null) _meshRenderer.material = value; }
    }

    private void Awake()
    {
        InitIfNeeded();
    }

    /// <summary>
    /// Safe to call multiple times. Ensures MeshFilter/MeshRenderer/Mesh/MeshCollider exist.
    /// Call this right after AddComponent&lt;TubeRenderer&gt; when creating tubes at runtime.
    /// </summary>
    public void InitIfNeeded()
    {
        if (_initialized) return;
        _initialized = true;

        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();

        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
        }

        _mesh = new Mesh { name = "TubeMesh" };
        _mesh.MarkDynamic(); // cambia mucho (cada punto)
        _meshFilter.sharedMesh = _mesh;

        _meshCollider = GetComponent<MeshCollider>();
        if (_meshCollider == null) _meshCollider = gameObject.AddComponent<MeshCollider>();

        _meshCollider.enabled = false; // por defecto off
        ApplyColliderSettings();
    }

    private void OnValidate()
    {
        _sides = Mathf.Max(3, _sides);
    }

    private void OnEnable()
    {
        if (_meshRenderer != null) _meshRenderer.enabled = true;
    }

    private void OnDisable()
    {
        if (_meshRenderer != null) _meshRenderer.enabled = false;
    }

    public void SetPositions(Vector3[] positions)
    {
        InitIfNeeded();
        _positions = positions;
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        InitIfNeeded();

        if (_positions == null || _positions.Length < 2)
        {
            _mesh.Clear();
            return;
        }

        if (_sides < 3) _sides = 3;

        int verticesLength = _sides * _positions.Length;

        // Re-alloc solo si cambió el tamaño
        if (_vertices == null || _vertices.Length != verticesLength)
        {
            _vertices = new Vector3[verticesLength];
            _indices = GenerateIndices(_positions.Length, _sides);
            _uvs = GenerateUVs(_positions.Length, _sides);

            _mesh.Clear();
            _mesh.vertices = _vertices;
            _mesh.triangles = _indices;
            _mesh.uv = _uvs;
        }

        int currentVertIndex = 0;

        for (int i = 0; i < _positions.Length; i++)
        {
            var circle = CalculateCircle(i);
            for (int j = 0; j < circle.Length; j++)
            {
                _vertices[currentVertIndex++] = _useWorldSpace
                    ? transform.InverseTransformPoint(circle[j])
                    : circle[j];
            }
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        // Solo actualiza collider si está habilitado (evita warning + costo)
        if (_meshCollider != null && _meshCollider.enabled)
        {
            _meshCollider.sharedMesh = null; // fuerza refresh
            _meshCollider.sharedMesh = _mesh;
        }
    }

    private void ApplyColliderSettings()
    {
        if (_meshCollider == null) return;

        _meshCollider.enabled = _colliderTrigger;

        if (_meshCollider.enabled)
        {
            _meshCollider.convex = true;
            _meshCollider.isTrigger = true;
        }
    }

    private static Vector2[] GenerateUVs(int segments, int sides)
    {
        var uvs = new Vector2[segments * sides];

        for (int segment = 0; segment < segments; segment++)
        {
            for (int side = 0; side < sides; side++)
            {
                int vertIndex = (segment * sides + side);
                float u = side / (sides - 1f);
                float v = segment / (segments - 1f);
                uvs[vertIndex] = new Vector2(u, v);
            }
        }
        return uvs;
    }

    private static int[] GenerateIndices(int segments, int sides)
    {
        var indices = new int[segments * sides * 2 * 3];

        int k = 0;
        for (int segment = 1; segment < segments; segment++)
        {
            for (int side = 0; side < sides; side++)
            {
                int vertIndex = (segment * sides + side);
                int prevVertIndex = vertIndex - sides;

                int nextVert = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
                int nextPrevVert = (side == sides - 1) ? (prevVertIndex - (sides - 1)) : (prevVertIndex + 1);

                indices[k++] = prevVertIndex;
                indices[k++] = nextVert;
                indices[k++] = vertIndex;

                indices[k++] = nextPrevVert;
                indices[k++] = (side == sides - 1) ? (vertIndex - (sides - 1)) : (vertIndex + 1);
                indices[k++] = prevVertIndex;
            }
        }

        return indices;
    }

    private Vector3[] CalculateCircle(int index)
    {
        int dirCount = 0;
        Vector3 forward = Vector3.zero;

        if (index > 0)
        {
            forward += (_positions[index] - _positions[index - 1]).normalized;
            dirCount++;
        }

        if (index < _positions.Length - 1)
        {
            forward += (_positions[index + 1] - _positions[index]).normalized;
            dirCount++;
        }

        forward = (forward / Mathf.Max(1, dirCount)).normalized;

        Vector3 side = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
        Vector3 up = Vector3.Cross(forward, side).normalized;

        var circle = new Vector3[_sides];
        float angle = 0f;
        float angleStep = (2 * Mathf.PI) / _sides;

        float t = index / (_positions.Length - 1f);
        float radius = _useTwoRadii ? Mathf.Lerp(_radiusOne, _radiusTwo, t) : _radiusOne;

        for (int i = 0; i < _sides; i++)
        {
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            circle[i] = _positions[index] + side * x * radius + up * y * radius;
            angle += angleStep;
        }

        return circle;
    }

    public void EnableGravity()
    {
        _useWorldSpace = false;

        if (!TryGetComponent<Rigidbody>(out _))
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
        }
    }
}
