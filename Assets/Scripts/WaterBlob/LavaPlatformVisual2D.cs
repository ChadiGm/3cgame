using UnityEngine;

namespace WaterBlob
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public class LavaPlatformVisual2D : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color rockColor = new(0.15f, 0.08f, 0.07f, 1f);
        [SerializeField] private Color hotColor = new(0.95f, 0.33f, 0.08f, 1f);
        [SerializeField] private Color emissionColor = new(1f, 0.24f, 0.03f, 1f);
        [SerializeField] private Color topReadableColor = new(0.32f, 0.21f, 0.16f, 1f);
        [SerializeField] private Color mossColor = new(0.34f, 0.48f, 0.22f, 1f);
        [SerializeField] private Color crystalColor = new(0.32f, 0.92f, 1f, 1f);

        [Header("Pulse")]
        [SerializeField, Min(0f)] private float pulseSpeed = 2.2f;
        [SerializeField, Min(0f)] private float emissionMin = 0.35f;
        [SerializeField, Min(0f)] private float emissionMax = 1.65f;
        [SerializeField, Min(0f)] private float hotEdgeBlend = 0.2f;

        [Header("Atmosphere")]
        [SerializeField] private bool spawnEmbers = true;
        [SerializeField, Min(0f)] private float emberRate = 7f;
        [SerializeField] private bool spawnLavaDrips = true;
        [SerializeField, Min(0f)] private float dripRate = 5f;

        [Header("Platform Detail")]
        [SerializeField, Min(0)] private int edgeChunkCount = 4;
        [SerializeField, Range(0f, 1f)] private float crystalChance = 0.45f;
        [SerializeField] private bool addTopMossOrCrystals = true;

        private MeshRenderer meshRenderer;
        private Material runtimeMaterial;
        private Material topSurfaceMaterial;
        private Material detailMaterial;
        private Transform decoRoot;
        private float pulseOffset;
        private bool visualsBuilt;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            SetupMaterial();
            BuildDecorativeGeometry();

            pulseOffset = Random.value * 6.28318f;
            if (spawnEmbers)
            {
                EnsureEmbers();
            }

            if (spawnLavaDrips)
            {
                EnsureLavaDrips();
            }
        }

        private void OnEnable()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            SetupMaterial();
            BuildDecorativeGeometry();
        }

        private void Update()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            float t = Mathf.Sin(Time.time * pulseSpeed + pulseOffset) * 0.5f + 0.5f;
            float glow = Mathf.Lerp(emissionMin, emissionMax, t);
            Color litColor = Color.Lerp(rockColor, hotColor, hotEdgeBlend + t * 0.2f);
            Color rimLit = Color.Lerp(topReadableColor, hotColor, t * 0.18f);

            SetMaterialColor("_BaseColor", litColor);
            SetMaterialColor("_Color", litColor);
            SetMaterialColor("_EmissionColor", emissionColor * glow);

            if (topSurfaceMaterial != null)
            {
                SetMaterialColor(topSurfaceMaterial, "_BaseColor", rimLit);
                SetMaterialColor(topSurfaceMaterial, "_Color", rimLit);
                SetMaterialColor(topSurfaceMaterial, "_EmissionColor", emissionColor * (glow * 0.25f));
            }

            if (detailMaterial != null)
            {
                Color detailEmission = Color.Lerp(crystalColor, emissionColor, 0.22f) * (0.35f + t * 0.35f);
                SetMaterialColor(detailMaterial, "_EmissionColor", detailEmission);
            }
        }

        private void SetupMaterial()
        {
            if (meshRenderer == null)
            {
                return;
            }

            Material src = meshRenderer.sharedMaterial;
            if (src == null)
            {
                return;
            }

            runtimeMaterial = new Material(src)
            {
                name = src.name + "_LavaRuntime"
            };

            runtimeMaterial.EnableKeyword("_EMISSION");
            meshRenderer.sharedMaterial = runtimeMaterial;

            topSurfaceMaterial = new Material(src)
            {
                name = src.name + "_TopSurfaceRuntime"
            };
            topSurfaceMaterial.EnableKeyword("_EMISSION");

            detailMaterial = new Material(src)
            {
                name = src.name + "_DetailRuntime"
            };
            detailMaterial.EnableKeyword("_EMISSION");
            SetMaterialColor(detailMaterial, "_BaseColor", crystalColor);
            SetMaterialColor(detailMaterial, "_Color", crystalColor);
        }

        private void SetMaterialColor(string prop, Color value)
        {
            if (runtimeMaterial != null && runtimeMaterial.HasProperty(prop))
            {
                runtimeMaterial.SetColor(prop, value);
            }
        }

        private void SetMaterialColor(Material mat, string prop, Color value)
        {
            if (mat != null && mat.HasProperty(prop))
            {
                mat.SetColor(prop, value);
            }
        }

        private void BuildDecorativeGeometry()
        {
            if (visualsBuilt)
            {
                return;
            }

            decoRoot = transform.Find("__VolcanicDeco");
            if (decoRoot == null)
            {
                GameObject root = new("__VolcanicDeco");
                decoRoot = root.transform;
                decoRoot.SetParent(transform, false);
            }
            else
            {
                for (int i = decoRoot.childCount - 1; i >= 0; i--)
                {
                    SafeDestroy(decoRoot.GetChild(i).gameObject);
                }
            }

            Bounds localBounds = meshRenderer.localBounds;
            float halfX = Mathf.Max(0.4f, localBounds.extents.x);
            float halfY = Mathf.Max(0.2f, localBounds.extents.y);

            CreateTopSurface(halfX, halfY);
            CreateIrregularEdges(halfX, halfY);
            if (addTopMossOrCrystals)
            {
                CreateTopAccents(halfX, halfY);
            }

            visualsBuilt = true;
        }

        private void CreateTopSurface(float halfX, float halfY)
        {
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "TopSurface";
            top.transform.SetParent(decoRoot, false);
            top.transform.localPosition = new Vector3(0f, halfY + 0.02f, 0f);
            top.transform.localScale = new Vector3(halfX * 1.85f, 0.07f, 1.02f);

            MeshRenderer mr = top.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = topSurfaceMaterial;
            }

            Collider c = top.GetComponent<Collider>();
            if (c != null)
            {
                SafeDestroy(c);
            }
        }

        private void CreateIrregularEdges(float halfX, float halfY)
        {
            int count = Mathf.Max(0, edgeChunkCount);
            float xRange = halfX * 0.9f;
            float yBase = -halfY - 0.05f;

            for (int i = 0; i < count; i++)
            {
                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = $"EdgeChunk_{i:00}";
                chunk.transform.SetParent(decoRoot, false);

                float tx = Mathf.Lerp(-xRange, xRange, count == 1 ? 0.5f : (float)i / (count - 1));
                float noise = (Mathf.PerlinNoise(i * 0.81f, transform.position.x * 0.17f) - 0.5f) * 0.18f;
                chunk.transform.localPosition = new Vector3(tx + noise, yBase - Random.Range(0.02f, 0.14f), 0f);
                chunk.transform.localScale = new Vector3(Random.Range(0.2f, 0.55f), Random.Range(0.12f, 0.36f), 1.02f);
                chunk.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));

                MeshRenderer mr = chunk.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = runtimeMaterial;
                }

                Collider c = chunk.GetComponent<Collider>();
                if (c != null)
                {
                    SafeDestroy(c);
                }
            }
        }

        private void CreateTopAccents(float halfX, float halfY)
        {
            int accentCount = Random.Range(2, 5);
            for (int i = 0; i < accentCount; i++)
            {
                bool crystal = Random.value < crystalChance;
                GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                accent.name = crystal ? $"Crystal_{i:00}" : $"Moss_{i:00}";
                accent.transform.SetParent(decoRoot, false);

                float ax = Random.Range(-halfX * 0.75f, halfX * 0.75f);
                float ay = halfY + 0.08f;
                accent.transform.localPosition = new Vector3(ax, ay, 0f);
                accent.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));

                if (crystal)
                {
                    accent.transform.localScale = new Vector3(Random.Range(0.08f, 0.18f), Random.Range(0.12f, 0.3f), 1.03f);
                }
                else
                {
                    accent.transform.localScale = new Vector3(Random.Range(0.2f, 0.55f), Random.Range(0.04f, 0.1f), 1.03f);
                }

                MeshRenderer mr = accent.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    if (crystal)
                    {
                        mr.sharedMaterial = detailMaterial;
                        mr.sharedMaterial.SetColor("_BaseColor", crystalColor);
                        mr.sharedMaterial.SetColor("_Color", crystalColor);
                    }
                    else
                    {
                        mr.sharedMaterial = detailMaterial;
                        mr.sharedMaterial.SetColor("_BaseColor", mossColor);
                        mr.sharedMaterial.SetColor("_Color", mossColor);
                    }
                }

                Collider c = accent.GetComponent<Collider>();
                if (c != null)
                {
                    SafeDestroy(c);
                }
            }
        }

        private void EnsureEmbers()
        {
            Transform fxRoot = transform.Find("__LavaEmbers");
            if (fxRoot == null)
            {
                GameObject go = new("__LavaEmbers");
                fxRoot = go.transform;
                fxRoot.SetParent(transform, false);
                fxRoot.localPosition = new Vector3(0f, 0.35f, 0f);
            }

            ParticleSystem ps = fxRoot.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = fxRoot.gameObject.AddComponent<ParticleSystem>();
            }

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f, 0.85f), new Color(1f, 0.24f, 0.1f, 0.9f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emberRate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.4f, 0.12f, 0.12f);

            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(0.75f, 1.8f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.8f, 0.35f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.1f), 0.6f), new GradientColorKey(new Color(0.2f, 0.05f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            ParticleSystemRenderer r = fxRoot.GetComponent<ParticleSystemRenderer>();
            if (r != null)
            {
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.sortingOrder = 8;
            }

            if (!ps.isPlaying)
            {
                ps.Play();
            }
        }

        private void EnsureLavaDrips()
        {
            Transform dripsRoot = transform.Find("__LavaDrips");
            if (dripsRoot == null)
            {
                GameObject go = new("__LavaDrips");
                dripsRoot = go.transform;
                dripsRoot.SetParent(transform, false);
                dripsRoot.localPosition = new Vector3(0f, -0.28f, 0f);
            }

            ParticleSystem ps = dripsRoot.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = dripsRoot.gameObject.AddComponent<ParticleSystem>();
            }

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.66f, 0.23f, 0.9f), new Color(1f, 0.23f, 0.08f, 0.95f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.85f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = dripRate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.15f, 0.04f, 0.08f);

            ParticleSystemRenderer r = dripsRoot.GetComponent<ParticleSystemRenderer>();
            if (r != null)
            {
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.sortingOrder = 7;
            }

            if (!ps.isPlaying)
            {
                ps.Play();
            }
        }

        private static void SafeDestroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
