using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LineRenderer))]
    public class WaterBlobMeshRenderer2D : MonoBehaviour
    {
        [SerializeField] private WaterBlobCharacter2D blob;

        [Header("Visual")]
        public Color fillColor = new(0.19f, 0.72f, 1f, 0.5f);
        public Color edgeColor = new(0.82f, 0.95f, 1f, 0.95f);
        [Min(0.005f)] public float edgeWidth = 0.12f;
        [Range(0f, 0.8f)] public float smoothing = 0.28f;
        public int sortingOrder = 10;

        [Header("Damage Feedback")]
        public Color damageColor = new(1f, 0.4f, 0.4f, 0.8f);
        [Min(0f)] public float flashFrequency = 12f;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private LineRenderer lineRenderer;
        private Material fillMaterial;
        private OneDropWaterResource2D waterResource;

        private readonly List<Vector3> worldPoints = new();
        private readonly List<Vector3> displayPoints = new();

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            lineRenderer = GetComponent<LineRenderer>();

            if (blob == null)
            {
                blob = GetComponent<WaterBlobCharacter2D>();
            }

            if (waterResource == null)
            {
                waterResource = GetComponentInParent<OneDropWaterResource2D>();
            }

            if (mesh == null)
            {
                mesh = new Mesh { name = "WaterBlobMesh" };
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }

            ConfigureMaterial();
            ConfigureLineRenderer();
        }

        private void LateUpdate()
        {
            if (blob == null)
            {
                return;
            }

            IReadOnlyList<Rigidbody2D> points = blob.PointBodies;
            if (points == null || points.Count < 3)
            {
                return;
            }

            BuildPointBuffers(points);
            DrawFillMesh();
            DrawOutline();
            UpdateVisualFeedback();
        }

        private void UpdateVisualFeedback()
        {
            if (waterResource == null || fillMaterial == null) return;

            Color targetFill = fillColor;
            Color targetEdge = edgeColor;

            if (waterResource.IsInvulnerable)
            {
                float pulse = 0.5f + Mathf.Sin(Time.time * flashFrequency) * 0.5f;
                targetFill = Color.Lerp(fillColor, damageColor, pulse);
                targetEdge = Color.Lerp(edgeColor, Color.white, pulse);
            }

            if (fillMaterial.HasProperty("_Color")) fillMaterial.SetColor("_Color", targetFill);
            if (fillMaterial.HasProperty("_BaseColor")) fillMaterial.SetColor("_BaseColor", targetFill);

            lineRenderer.startColor = targetEdge;
            lineRenderer.endColor = targetEdge;
        }

        private void BuildPointBuffers(IReadOnlyList<Rigidbody2D> points)
        {
            worldPoints.Clear();
            displayPoints.Clear();

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null)
                {
                    worldPoints.Add(points[i].position);
                }
            }

            if (worldPoints.Count < 3)
            {
                return;
            }

            for (int i = 0; i < worldPoints.Count; i++)
            {
                int prev = (i - 1 + worldPoints.Count) % worldPoints.Count;
                int next = (i + 1) % worldPoints.Count;

                Vector3 neighborMid = (worldPoints[prev] + worldPoints[next]) * 0.5f;
                Vector3 smoothed = Vector3.Lerp(worldPoints[i], neighborMid, smoothing);
                displayPoints.Add(smoothed);
            }
        }

        private void DrawFillMesh()
        {
            if (displayPoints.Count < 3)
            {
                return;
            }

            int n = displayPoints.Count;
            Vector3 center = Vector3.zero;

            for (int i = 0; i < n; i++)
            {
                center += displayPoints[i];
            }

            center /= n;

            Vector3[] vertices = new Vector3[n + 1];
            Vector2[] uv = new Vector2[n + 1];
            int[] triangles = new int[n * 3];

            vertices[0] = transform.InverseTransformPoint(center);
            uv[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < n; i++)
            {
                Vector3 local = transform.InverseTransformPoint(displayPoints[i]);
                vertices[i + 1] = local;
                uv[i + 1] = new Vector2(local.x + 0.5f, local.y + 0.5f);

                int t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = (i + 1) % n + 1;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        private void DrawOutline()
        {
            lineRenderer.positionCount = displayPoints.Count;
            lineRenderer.SetPositions(displayPoints.ToArray());
        }

private void ConfigureMaterial()
        {
            Shader shader = FindBestShader();
            if (shader == null)
            {
                Debug.LogWarning("WaterBlobMeshRenderer2D could not find a valid shader.");
                return;
            }

            fillMaterial = new Material(shader) { name = "WaterBlobFillRuntime" };
            ApplyMaterialColor();

            meshRenderer.sharedMaterial = fillMaterial;
            meshRenderer.sortingOrder = sortingOrder;
        }

private void ConfigureLineRenderer()
        {
            Shader lineShader = FindBestShader();
            if (lineShader == null)
            {
                Debug.LogWarning("WaterBlobMeshRenderer2D could not find a valid line shader.");
                return;
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.widthMultiplier = edgeWidth;
            lineRenderer.numCornerVertices = 3;
            lineRenderer.numCapVertices = 3;
            lineRenderer.sortingOrder = sortingOrder + 1;
            lineRenderer.sharedMaterial = new Material(lineShader) { name = "WaterBlobEdgeRuntime" };
            lineRenderer.startColor = edgeColor;
            lineRenderer.endColor = edgeColor;
        }

private static Shader FindBestShader()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Unlit/Color");
        }


        private void OnValidate()
        {
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = sortingOrder;
            }

            if (lineRenderer != null)
            {
                lineRenderer.widthMultiplier = edgeWidth;
                lineRenderer.startColor = edgeColor;
                lineRenderer.endColor = edgeColor;
                lineRenderer.sortingOrder = sortingOrder + 1;
            }

            ApplyMaterialColor();
        }

        private void ApplyMaterialColor()
        {
            if (fillMaterial == null)
            {
                return;
            }

            if (fillMaterial.HasProperty("_Color"))
            {
                fillMaterial.SetColor("_Color", fillColor);
            }

            if (fillMaterial.HasProperty("_BaseColor"))
            {
                fillMaterial.SetColor("_BaseColor", fillColor);
            }
        }
    }
}
