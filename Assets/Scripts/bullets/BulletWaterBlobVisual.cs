using UnityEngine;

/// <summary>
/// Procedurally generates a small water-blob shaped mesh for the bullet,
/// mimicking the player character's look. Attach to the bullet prefab.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class BulletWaterBlobVisual : MonoBehaviour
{
    [Header("Blob Shape")]
    [SerializeField, Range(8, 32)] private int segments = 16;
    [SerializeField, Min(0.05f)] private float blobRadius = 0.22f;
    [SerializeField, Range(0f, 0.15f)] private float wobbleAmount = 0.04f;
    [SerializeField, Min(0.5f)] private float wobbleSpeed = 6f;

    [Header("Colors")]
    [SerializeField] private Color fillColor = new Color(0.19f, 0.72f, 1f, 0.65f);
    [SerializeField] private Color edgeColor = new Color(0.82f, 0.95f, 1f, 0.95f);

    [Header("Outline")]
    [SerializeField, Min(0.005f)] private float edgeWidth = 0.04f;
    [SerializeField] private int sortingOrder = 12;

    [Header("Trail")]
    [SerializeField] private bool enableTrail = true;
    [SerializeField] private float trailTime = 0.15f;
    [SerializeField, Min(0.01f)] private float trailStartWidth = 0.18f;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private LineRenderer lineRenderer;
    private float timeOffset;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh { name = "BulletBlobMesh" };
        mesh.MarkDynamic();
        meshFilter.sharedMesh = mesh;

        timeOffset = Random.Range(0f, 100f);

        SetupFillMaterial();
        SetupOutline();

        if (enableTrail)
        {
            SetupTrail();
        }
    }

    private void Update()
    {
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        int n = segments;
        Vector3[] vertices = new Vector3[n + 1];
        Vector2[] uv = new Vector2[n + 1];
        int[] triangles = new int[n * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        float t = Time.time + timeOffset;

        Vector3[] outlinePositions = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            float angle = i * Mathf.PI * 2f / n;
            // Wobble the radius slightly for organic feel
            float wobble = Mathf.Sin(t * wobbleSpeed + angle * 2.3f) * wobbleAmount
                         + Mathf.Sin(t * wobbleSpeed * 1.7f + angle * 3.1f) * wobbleAmount * 0.5f;
            float r = blobRadius + wobble;

            float x = Mathf.Cos(angle) * r;
            float y = Mathf.Sin(angle) * r;

            vertices[i + 1] = new Vector3(x, y, 0f);
            uv[i + 1] = new Vector2(x / blobRadius * 0.5f + 0.5f, y / blobRadius * 0.5f + 0.5f);
            outlinePositions[i] = transform.TransformPoint(new Vector3(x, y, 0f));

            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = (i + 1) % n + 1;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = n;
            lineRenderer.SetPositions(outlinePositions);
        }
    }

    private void SetupFillMaterial()
    {
        Shader shader = FindBestShader();
        if (shader == null) return;

        Material mat = new Material(shader) { name = "BulletBlobFill" };
        mat.color = fillColor;
        meshRenderer.sharedMaterial = mat;
        meshRenderer.sortingOrder = sortingOrder;
    }

    private void SetupOutline()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        Shader shader = FindBestShader();
        if (shader == null) return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.widthMultiplier = edgeWidth;
        lineRenderer.numCornerVertices = 3;
        lineRenderer.numCapVertices = 3;
        lineRenderer.sortingOrder = sortingOrder + 1;

        Material edgeMat = new Material(shader) { name = "BulletBlobEdge" };
        edgeMat.color = edgeColor;
        lineRenderer.sharedMaterial = edgeMat;
        lineRenderer.startColor = edgeColor;
        lineRenderer.endColor = edgeColor;
    }

    private void SetupTrail()
    {
        TrailRenderer trail = gameObject.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        Shader shader = FindBestShader();
        if (shader == null) return;

        Material trailMat = new Material(shader) { name = "BulletTrail" };
        trailMat.color = fillColor;
        trail.sharedMaterial = trailMat;
        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = 0f;
        trail.startColor = new Color(fillColor.r, fillColor.g, fillColor.b, 0.5f);
        trail.endColor = new Color(fillColor.r, fillColor.g, fillColor.b, 0f);
        trail.sortingOrder = sortingOrder - 1;
        trail.minVertexDistance = 0.05f;
    }

    private static Shader FindBestShader()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) return shader;
        shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) return shader;
        return Shader.Find("Unlit/Color");
    }
}
