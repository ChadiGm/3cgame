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
        [SerializeField] private OneDropWaterResource2D waterResource;

        [Header("Visual")]
        public Color fillColor = new(0.19f, 0.72f, 1f, 0.5f);
        public Color edgeColor = new(0.82f, 0.95f, 1f, 0.95f);
        [Min(0.005f)] public float edgeWidth = 0.12f;
        [Range(0f, 0.8f)] public float smoothing = 0.28f;
        public int sortingOrder = 10;

        [Header("Water Fill")]
        public bool useWaterResourceFill = true;
        [Range(0f, 1f)] public float manualFill = 1f;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private LineRenderer lineRenderer;
        private Material fillMaterial;

        private readonly List<Vector3> worldPoints = new();
        private readonly List<Vector3> displayPoints = new();
        private readonly List<Vector3> clippedPoints = new();
        private Vector2 runtimeScale = Vector2.one;
        private Vector2 runtimeOffset = Vector2.zero;
        private Vector2 motionScale = Vector2.one;
        private Vector2 motionOffset = Vector2.zero;

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
                waterResource = GetComponent<OneDropWaterResource2D>();
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
            float fill = manualFill;
            if (useWaterResourceFill && waterResource != null)
            {
                fill = waterResource.WaterRatio;
            }
            DrawFillMesh(fill);
            DrawOutline();
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

        private void DrawFillMesh(float fillAmount)
        {
            if (displayPoints.Count < 3)
            {
                return;
            }

            fillAmount = Mathf.Clamp01(fillAmount);
            if (fillAmount <= 0.001f)
            {
                mesh.Clear();
                return;
            }

            IReadOnlyList<Vector3> source = displayPoints;
            if (fillAmount < 0.999f)
            {
                float minY = displayPoints[0].y;
                float maxY = displayPoints[0].y;
                for (int i = 1; i < displayPoints.Count; i++)
                {
                    float y = displayPoints[i].y;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                float fillY = Mathf.Lerp(minY, maxY, fillAmount);
                ClipPolygonByHorizontalLine(displayPoints, fillY, clippedPoints);
                if (clippedPoints.Count < 3)
                {
                    mesh.Clear();
                    return;
                }
                source = clippedPoints;
            }

            int n = source.Count;
            Vector3 center = Vector3.zero;

            for (int i = 0; i < n; i++)
            {
                center += source[i];
            }

            center /= n;

            Vector3[] vertices = new Vector3[n + 1];
            Vector2[] uv = new Vector2[n + 1];
            int[] triangles = new int[n * 3];

            vertices[0] = transform.InverseTransformPoint(center);
            uv[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < n; i++)
            {
                Vector3 point = ApplyRuntimeVisual(source[i], center);
                Vector3 local = transform.InverseTransformPoint(point);
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

        private static void ClipPolygonByHorizontalLine(IReadOnlyList<Vector3> input, float fillY, List<Vector3> output)
        {
            output.Clear();
            if (input.Count == 0)
            {
                return;
            }

            Vector3 prev = input[input.Count - 1];
            bool prevInside = prev.y <= fillY;

            for (int i = 0; i < input.Count; i++)
            {
                Vector3 curr = input[i];
                bool currInside = curr.y <= fillY;

                if (currInside)
                {
                    if (!prevInside)
                    {
                        output.Add(IntersectAtY(prev, curr, fillY));
                    }
                    output.Add(curr);
                }
                else if (prevInside)
                {
                    output.Add(IntersectAtY(prev, curr, fillY));
                }

                prev = curr;
                prevInside = currInside;
            }
        }

        private static Vector3 IntersectAtY(Vector3 a, Vector3 b, float y)
        {
            float dy = b.y - a.y;
            if (Mathf.Abs(dy) < 0.0001f)
            {
                return a;
            }
            float t = (y - a.y) / dy;
            return Vector3.Lerp(a, b, t);
        }

        private void DrawOutline()
        {
            if (displayPoints.Count == 0)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            Vector3 center = Vector3.zero;
            for (int i = 0; i < displayPoints.Count; i++)
            {
                center += displayPoints[i];
            }
            center /= displayPoints.Count;

            Vector3[] outline = new Vector3[displayPoints.Count];
            for (int i = 0; i < displayPoints.Count; i++)
            {
                outline[i] = ApplyRuntimeVisual(displayPoints[i], center);
            }

            lineRenderer.positionCount = outline.Length;
            lineRenderer.SetPositions(outline);
        }

        private Vector3 ApplyRuntimeVisual(Vector3 point, Vector3 center)
        {
            Vector3 result = point;
            Vector2 combinedScale = new(runtimeScale.x * motionScale.x, runtimeScale.y * motionScale.y);
            Vector2 combinedOffset = runtimeOffset + motionOffset;

            if (Mathf.Abs(combinedScale.x - 1f) > 0.0001f || Mathf.Abs(combinedScale.y - 1f) > 0.0001f)
            {
                Vector2 delta = point - center;
                delta = new Vector2(delta.x * combinedScale.x, delta.y * combinedScale.y);
                result = center + (Vector3)delta;
            }

            if (combinedOffset.sqrMagnitude > 0.0000001f)
            {
                result += new Vector3(combinedOffset.x, combinedOffset.y, 0f);
            }

            return result;
        }

        public void SetRuntimeVisual(float scale, Vector2 offset)
        {
            float safeScale = Mathf.Max(0.01f, scale);
            runtimeScale = new Vector2(safeScale, safeScale);
            runtimeOffset = offset;
        }

        public void ResetRuntimeVisual()
        {
            runtimeScale = Vector2.one;
            runtimeOffset = Vector2.zero;
        }

        public void SetMotionVisual(Vector2 axisScale, Vector2 offset)
        {
            motionScale = new Vector2(
                Mathf.Max(0.01f, axisScale.x),
                Mathf.Max(0.01f, axisScale.y)
            );
            motionOffset = offset;
        }

        public void ResetMotionVisual()
        {
            motionScale = Vector2.one;
            motionOffset = Vector2.zero;
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
            if (blob == null)
            {
                blob = GetComponent<WaterBlobCharacter2D>();
            }

            if (waterResource == null)
            {
                waterResource = GetComponent<OneDropWaterResource2D>();
            }

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
