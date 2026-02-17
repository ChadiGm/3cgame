using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class WaterBlobProjectileSoftBody2D : MonoBehaviour
    {
        [Header("Shape")]
        [Min(6)] public int pointCount = 10;
        [Min(0.08f)] public float radius = 0.72f;
        [Min(0.02f)] public float coreColliderRadius = 0.2f;
        [Min(0.02f)] public float pointColliderRadius = 0.12f;

        [Header("Physics")]
        [Min(0.01f)] public float coreMass = 0.36f;
        [Min(0.005f)] public float pointMass = 0.095f;
        [Min(0f)] public float gravityScale = 1.6f;
        [Min(0f)] public float linearDamping = 0.4f;
        [Min(0f)] public float angularDamping = 0.9f;
        [Min(0f)] public float coreSpringFrequency = 11f;
        [Range(0f, 1f)] public float coreSpringDamping = 0.58f;
        [Min(0f)] public float ringSpringFrequency = 14f;
        [Range(0f, 1f)] public float ringSpringDamping = 0.5f;
        [Min(0f)] public float radialStiffness = 22f;
        [Min(0f)] public float radialDamping = 7f;

        [Header("Lifetime")]
        [Min(0.05f)] public float lifeSeconds = 4f;
        [Range(0f, 1f)] public float bounciness = 0.1f;
        [Range(0f, 1f)] public float friction = 0.2f;
        [Min(0f)] public float armTime = 0.05f;
        [Min(0f)] public float minImpactSpeed = 1f;
        public LayerMask hitMask = ~0;
        public bool hitTriggers;
        [Range(1, 3)] public int ringSpringStep = 1;

        [Header("Visuals")]
        [SerializeField] private Color fillColor = new(0.58f, 0.89f, 1f, 0.78f);
        [SerializeField] private Color edgeColor = new(0.82f, 0.97f, 1f, 0.95f);
        [SerializeField] private float edgeWidth = 0.075f;

        private readonly List<Rigidbody2D> nodeBodies = new();
        private readonly List<CircleCollider2D> nodeColliders = new();
        private readonly List<SpringJoint2D> coreSprings = new();
        private readonly List<SpringJoint2D> ringSprings = new();

        private Rigidbody2D coreBody;
        private CircleCollider2D coreCollider;
        private PhysicsMaterial2D runtimeMaterial;
        private ParticleSystem splashParticles;
        private SpriteRenderer fillRenderer;
        private LineRenderer edgeRenderer;
        private Material edgeMaterial;
        private Material fillMaterial;
        private Texture2D dropletTexture;
        private Sprite dropletSprite;
        private bool built;
        private bool launched;
        private bool impacted;
        private float lifeRemaining;
        private float armTimer;

        private void Awake()
        {
            EnsureCore();
            EnsureVisuals();
            EnsureSplashParticles();
            ApplyRadiusDrivenSettings();
        }

        private void FixedUpdate()
        {
            if (!launched)
            {
                return;
            }

            lifeRemaining -= Time.fixedDeltaTime;
            armTimer = Mathf.Max(0f, armTimer - Time.fixedDeltaTime);
            if (lifeRemaining <= 0f)
            {
                DestroySelf();
                return;
            }

            ApplyRadialStabilization();
        }

        private void LateUpdate()
        {
            UpdateVisuals();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ReportImpact(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ReportImpact(other);
        }

        public void Configure(
            int projectilePointCount,
            float projectileRadius,
            float projectileLifetime,
            float projectileGravityScale,
            float springFrequency,
            float springDamp,
            float projectileArmTime,
            float projectileMinImpactSpeed,
            LayerMask projectileHitMask,
            bool projectileHitTriggers,
            float waterLevel01)
        {
            pointCount = Mathf.Max(6, projectilePointCount);
            radius = Mathf.Max(0.08f, projectileRadius);
            lifeSeconds = Mathf.Max(0.05f, projectileLifetime);
            gravityScale = Mathf.Max(0f, projectileGravityScale);
            coreSpringFrequency = Mathf.Max(0f, springFrequency);
            ringSpringFrequency = Mathf.Max(0f, springFrequency * 1.15f);
            coreSpringDamping = Mathf.Clamp01(springDamp);
            ringSpringDamping = Mathf.Clamp01(springDamp * 0.92f);
            armTime = Mathf.Max(0f, projectileArmTime);
            minImpactSpeed = Mathf.Max(0f, projectileMinImpactSpeed);
            hitMask = projectileHitMask;
            hitTriggers = projectileHitTriggers;
            ringSpringStep = radius > 0.95f ? 2 : 1;

            float waterT = Mathf.Clamp01(waterLevel01);
            bounciness = Mathf.Lerp(0.05f, 0.22f, waterT);
            friction = Mathf.Lerp(0.18f, 0.05f, waterT);
            fillColor = Color.Lerp(new Color(0.42f, 0.77f, 1f, 0.72f), new Color(0.65f, 0.94f, 1f, 0.85f), waterT);
            edgeColor = Color.Lerp(new Color(0.7f, 0.9f, 1f, 0.9f), new Color(0.9f, 0.99f, 1f, 1f), waterT);

            ApplyRadiusDrivenSettings();
            ApplyParticleScaleProfile();
            ApplyVisualStyle();
        }

        public void Launch(Vector2 initialVelocity, Collider2D[] ignoreColliders)
        {
            EnsureBuilt();

            if (ignoreColliders != null)
            {
                IgnoreWithColliders(ignoreColliders);
            }

            coreBody.linearVelocity = initialVelocity;
            for (int i = 0; i < nodeBodies.Count; i++)
            {
                Rigidbody2D node = nodeBodies[i];
                if (node == null)
                {
                    continue;
                }

                Vector2 randomJitter = Random.insideUnitCircle * 0.45f;
                node.linearVelocity = initialVelocity + randomJitter;
            }

            lifeRemaining = lifeSeconds;
            armTimer = armTime;
            launched = true;
            int launchSplashCount = Mathf.RoundToInt(Mathf.Lerp(14f, 28f, Mathf.InverseLerp(0.35f, 1.4f, radius)));
            EmitSplash(launchSplashCount, initialVelocity * 0.12f);
        }

        public void ReportImpact(Collider2D other)
        {
            if (impacted || other == null || !launched)
            {
                return;
            }

            if (armTimer > 0f)
            {
                return;
            }

            if (other.isTrigger && !hitTriggers)
            {
                return;
            }

            if ((hitMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            float impactSpeed = coreBody != null ? coreBody.linearVelocity.magnitude : 0f;
            if (impactSpeed < minImpactSpeed)
            {
                return;
            }

            impacted = true;
            launched = false;
            Vector2 impactVelocity = coreBody != null ? coreBody.linearVelocity : Vector2.zero;
            int impactSplashCount = Mathf.RoundToInt(Mathf.Lerp(22f, 40f, Mathf.InverseLerp(0.35f, 1.4f, radius)));
            EmitSplash(impactSplashCount, impactVelocity * 0.08f);
            DisableProjectilePhysicsAndVisuals();

            float splashLife = splashParticles != null ? splashParticles.main.startLifetime.constantMax : 0.4f;
            DestroySelf(Mathf.Max(0.12f, splashLife + 0.08f));
        }

        private void EnsureBuilt()
        {
            if (built)
            {
                return;
            }

            EnsureCore();
            CreateRuntimeMaterial();

            nodeBodies.Clear();
            nodeColliders.Clear();
            coreSprings.Clear();
            ringSprings.Clear();

            for (int i = 0; i < pointCount; i++)
            {
                float angle = i * Mathf.PI * 2f / pointCount;
                Vector2 offset = new(Mathf.Cos(angle), Mathf.Sin(angle));

                GameObject nodeGo = new($"Node_{i:00}");
                nodeGo.transform.SetParent(transform);
                nodeGo.transform.localPosition = offset * radius;
                nodeGo.transform.localRotation = Quaternion.identity;
                nodeGo.transform.localScale = Vector3.one;

                Rigidbody2D nodeBody = nodeGo.AddComponent<Rigidbody2D>();
                nodeBody.mass = pointMass;
                nodeBody.gravityScale = gravityScale;
                nodeBody.linearDamping = linearDamping;
                nodeBody.angularDamping = angularDamping;
                nodeBody.interpolation = RigidbodyInterpolation2D.Interpolate;
                nodeBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                CircleCollider2D nodeCollider = nodeGo.AddComponent<CircleCollider2D>();
                nodeCollider.radius = pointColliderRadius;
                nodeCollider.sharedMaterial = runtimeMaterial;

                SpringJoint2D coreSpring = nodeGo.AddComponent<SpringJoint2D>();
                coreSpring.connectedBody = coreBody;
                coreSpring.autoConfigureDistance = false;
                coreSpring.distance = radius;
                coreSpring.frequency = coreSpringFrequency;
                coreSpring.dampingRatio = coreSpringDamping;

                SpringJoint2D ringSpring = nodeGo.AddComponent<SpringJoint2D>();
                ringSpring.connectedBody = null;
                ringSpring.autoConfigureDistance = false;
                ringSpring.frequency = ringSpringFrequency;
                ringSpring.dampingRatio = ringSpringDamping;

                ProjectileNodeCollisionRelay relay = nodeGo.AddComponent<ProjectileNodeCollisionRelay>();
                relay.owner = this;

                nodeBodies.Add(nodeBody);
                nodeColliders.Add(nodeCollider);
                coreSprings.Add(coreSpring);
                ringSprings.Add(ringSpring);
            }

            for (int i = 0; i < nodeBodies.Count; i++)
            {
                int next = (i + Mathf.Clamp(ringSpringStep, 1, 3)) % nodeBodies.Count;
                SpringJoint2D ringSpring = ringSprings[i];
                ringSpring.connectedBody = nodeBodies[next];
                ringSpring.distance = GetChordLength(Mathf.Clamp(ringSpringStep, 1, 3));
            }

            IgnoreInternalCollisions();
            EnsureVisuals();
            ApplyVisualStyle();
            built = true;
        }

        private float GetChordLength(int step)
        {
            return 2f * radius * Mathf.Sin(step * Mathf.PI / pointCount);
        }

        private void EnsureCore()
        {
            coreBody = GetComponent<Rigidbody2D>();
            coreCollider = GetComponent<CircleCollider2D>();

            CreateRuntimeMaterial();

            coreBody.mass = coreMass;
            coreBody.gravityScale = gravityScale;
            coreBody.linearDamping = linearDamping;
            coreBody.angularDamping = angularDamping;
            coreBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            coreBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            coreCollider.radius = coreColliderRadius;
            coreCollider.sharedMaterial = runtimeMaterial;
        }

        private void ApplyRadiusDrivenSettings()
        {
            radius = Mathf.Max(0.08f, radius);
            coreColliderRadius = Mathf.Max(0.02f, radius * 0.32f);
            pointColliderRadius = Mathf.Max(0.02f, radius * 0.18f);
            coreMass = Mathf.Max(0.01f, radius * 0.5f);
            pointMass = Mathf.Max(0.005f, radius * 0.13f);
            edgeWidth = Mathf.Clamp(radius * 0.11f, 0.03f, 0.18f);

            if (coreCollider != null)
            {
                coreCollider.radius = coreColliderRadius;
            }
        }

        private void CreateRuntimeMaterial()
        {
            if (runtimeMaterial == null)
            {
                runtimeMaterial = new PhysicsMaterial2D("WaterBlobProjectileRuntime");
            }

            runtimeMaterial.bounciness = bounciness;
            runtimeMaterial.friction = friction;
        }

        private void IgnoreInternalCollisions()
        {
            for (int i = 0; i < nodeColliders.Count; i++)
            {
                CircleCollider2D a = nodeColliders[i];
                if (a == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(coreCollider, a, true);

                for (int j = i + 1; j < nodeColliders.Count; j++)
                {
                    CircleCollider2D b = nodeColliders[j];
                    if (b != null)
                    {
                        Physics2D.IgnoreCollision(a, b, true);
                    }
                }
            }
        }

        private void IgnoreWithColliders(IReadOnlyList<Collider2D> colliders)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider2D owner = colliders[i];
                if (owner == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(coreCollider, owner, true);
                for (int j = 0; j < nodeColliders.Count; j++)
                {
                    CircleCollider2D node = nodeColliders[j];
                    if (node != null)
                    {
                        Physics2D.IgnoreCollision(node, owner, true);
                    }
                }
            }
        }

        private void ApplyRadialStabilization()
        {
            if (coreBody == null || nodeBodies.Count == 0)
            {
                return;
            }

            int nodeCount = nodeBodies.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                Rigidbody2D node = nodeBodies[i];
                if (node == null)
                {
                    continue;
                }

                Vector2 radial = node.position - coreBody.position;
                float distance = radial.magnitude;
                if (distance <= 0.0001f)
                {
                    continue;
                }

                Vector2 radialDir = radial / distance;
                float radiusError = radius - distance;
                Vector2 relativeVelocity = node.linearVelocity - coreBody.linearVelocity;
                float radialSpeed = Vector2.Dot(relativeVelocity, radialDir);

                float forceAmount = (radiusError * radialStiffness) - (radialSpeed * radialDamping);
                Vector2 force = radialDir * forceAmount;
                node.AddForce(force, ForceMode2D.Force);
                coreBody.AddForce(-force / nodeCount, ForceMode2D.Force);
            }
        }

        private void EnsureSplashParticles()
        {
            if (splashParticles != null)
            {
                return;
            }

            Transform existing = transform.Find("__SplashParticles");
            GameObject go = existing != null ? existing.gameObject : new GameObject("__SplashParticles");
            if (existing == null)
            {
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            splashParticles = go.GetComponent<ParticleSystem>();
            if (splashParticles == null)
            {
                splashParticles = go.AddComponent<ParticleSystem>();
            }

            ParticleSystem.MainModule main = splashParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.42f;
            main.startSpeed = 2.7f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.65f, 0.92f, 1f, 0.85f);
            main.maxParticles = 220;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = splashParticles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = splashParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            ApplyParticleScaleProfile();
        }

        private void ApplyParticleScaleProfile()
        {
            if (splashParticles == null)
            {
                return;
            }

            float t = Mathf.InverseLerp(0.35f, 1.4f, radius);

            ParticleSystem.MainModule main = splashParticles.main;
            main.startLifetime = Mathf.Lerp(0.32f, 0.56f, t);
            main.startSpeed = Mathf.Lerp(2.2f, 3.8f, t);
            main.startSize = Mathf.Lerp(0.1f, 0.22f, t);
            main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(120f, 260f, t));

            ParticleSystem.ShapeModule shape = splashParticles.shape;
            shape.radius = Mathf.Lerp(0.13f, 0.34f, t);
        }

        private void EmitSplash(int count, Vector2 inheritVelocity)
        {
            if (splashParticles == null)
            {
                return;
            }

            ParticleSystem.VelocityOverLifetimeModule velocity = splashParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = inheritVelocity.x;
            velocity.y = inheritVelocity.y;
            splashParticles.Emit(Mathf.Max(1, count));
        }

        private void DisableProjectilePhysicsAndVisuals()
        {
            if (coreCollider != null)
            {
                coreCollider.enabled = false;
            }

            if (coreBody != null)
            {
                coreBody.simulated = false;
            }

            for (int i = 0; i < nodeColliders.Count; i++)
            {
                if (nodeColliders[i] != null)
                {
                    nodeColliders[i].enabled = false;
                }
            }

            for (int i = 0; i < nodeBodies.Count; i++)
            {
                if (nodeBodies[i] != null)
                {
                    nodeBodies[i].simulated = false;
                }
            }

            if (fillRenderer != null)
            {
                fillRenderer.enabled = false;
            }

            if (edgeRenderer != null)
            {
                edgeRenderer.enabled = false;
            }
        }

        private void EnsureVisuals()
        {
            Transform fillTransform = transform.Find("__ProjectileFill");
            GameObject fillGo = fillTransform != null ? fillTransform.gameObject : new GameObject("__ProjectileFill");
            if (fillTransform == null)
            {
                fillGo.transform.SetParent(transform);
                fillGo.transform.localPosition = Vector3.zero;
                fillGo.transform.localRotation = Quaternion.identity;
                fillGo.transform.localScale = Vector3.one;
            }

            fillRenderer = fillGo.GetComponent<SpriteRenderer>();
            if (fillRenderer == null)
            {
                fillRenderer = fillGo.AddComponent<SpriteRenderer>();
            }

            fillRenderer.sprite = GetDropletSprite();
            fillRenderer.sortingOrder = 25;

            Transform edgeTransform = transform.Find("__ProjectileEdge");
            GameObject edgeGo = edgeTransform != null ? edgeTransform.gameObject : new GameObject("__ProjectileEdge");
            if (edgeTransform == null)
            {
                edgeGo.transform.SetParent(transform);
                edgeGo.transform.localPosition = Vector3.zero;
                edgeGo.transform.localRotation = Quaternion.identity;
                edgeGo.transform.localScale = Vector3.one;
            }

            edgeRenderer = edgeGo.GetComponent<LineRenderer>();
            if (edgeRenderer == null)
            {
                edgeRenderer = edgeGo.AddComponent<LineRenderer>();
            }

            edgeRenderer.loop = true;
            edgeRenderer.positionCount = Mathf.Max(6, pointCount);
            edgeRenderer.numCornerVertices = 3;
            edgeRenderer.numCapVertices = 2;
            edgeRenderer.useWorldSpace = true;
            edgeRenderer.textureMode = LineTextureMode.Stretch;
            edgeRenderer.alignment = LineAlignment.View;
            edgeRenderer.sortingOrder = 26;
        }

        private void ApplyVisualStyle()
        {
            if (fillRenderer != null)
            {
                fillMaterial ??= new Material(Shader.Find("Sprites/Default"));
                fillRenderer.sharedMaterial = fillMaterial;
                fillRenderer.color = fillColor;
                float diameter = radius * 2.1f;
                fillRenderer.transform.localScale = new Vector3(diameter, diameter, 1f);
            }

            if (edgeRenderer != null)
            {
                edgeMaterial ??= new Material(Shader.Find("Sprites/Default"));
                edgeRenderer.sharedMaterial = edgeMaterial;
                edgeRenderer.startColor = edgeColor;
                edgeRenderer.endColor = edgeColor;
                edgeRenderer.widthMultiplier = edgeWidth;
            }
        }

        private void UpdateVisuals()
        {
            if (coreBody != null && fillRenderer != null)
            {
                fillRenderer.transform.position = coreBody.position;
            }

            if (edgeRenderer == null || nodeBodies.Count == 0)
            {
                return;
            }

            int count = nodeBodies.Count;
            if (edgeRenderer.positionCount != count)
            {
                edgeRenderer.positionCount = count;
            }

            for (int i = 0; i < count; i++)
            {
                Rigidbody2D node = nodeBodies[i];
                edgeRenderer.SetPosition(i, node != null ? (Vector3)node.position : transform.position);
            }
        }

        private Sprite GetDropletSprite()
        {
            if (dropletSprite != null)
            {
                return dropletSprite;
            }

            const int size = 96;
            dropletTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "ProjectileDropletRuntime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new(size * 0.5f, size * 0.56f);
            float radiusPx = size * 0.32f;
            Vector2 tip = new(size * 0.5f, size * 0.1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new(x + 0.5f, y + 0.5f);
                    float circle = Vector2.Distance(p, center) - radiusPx;
                    float sideA = SignedDistanceToLine(p, tip, new Vector2(size * 0.2f, size * 0.56f));
                    float sideB = SignedDistanceToLine(new Vector2(size - p.x, p.y), tip, new Vector2(size * 0.2f, size * 0.56f));
                    float triangle = Mathf.Max(sideA, sideB);
                    float sdf = Mathf.Min(circle, triangle);

                    float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(-2f, 2f, sdf));
                    Color col = Color.Lerp(new Color(0.45f, 0.82f, 1f, 0f), new Color(0.92f, 0.99f, 1f, 1f), y / (size - 1f));
                    col.a *= alpha;
                    dropletTexture.SetPixel(x, y, col);
                }
            }

            dropletTexture.Apply(false, false);
            dropletSprite = Sprite.Create(dropletTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return dropletSprite;
        }

        private static float SignedDistanceToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 n = new(a.y - b.y, b.x - a.x);
            n.Normalize();
            return Vector2.Dot(p - a, n);
        }

        private void DestroySelf(float delay = 0f)
        {
            if (delay <= 0f)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }

                return;
            }

            Destroy(gameObject, delay);
        }

        private void OnDestroy()
        {
            if (fillMaterial != null)
            {
                DestroySafe(fillMaterial);
            }

            if (edgeMaterial != null)
            {
                DestroySafe(edgeMaterial);
            }

            if (dropletSprite != null)
            {
                DestroySafe(dropletSprite);
            }

            if (dropletTexture != null)
            {
                DestroySafe(dropletTexture);
            }
        }

        private static void DestroySafe(Object target)
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

        private sealed class ProjectileNodeCollisionRelay : MonoBehaviour
        {
            public WaterBlobProjectileSoftBody2D owner;

            private void OnCollisionEnter2D(Collision2D collision)
            {
                if (owner != null)
                {
                    owner.ReportImpact(collision.collider);
                }
            }

            private void OnTriggerEnter2D(Collider2D other)
            {
                if (owner != null)
                {
                    owner.ReportImpact(other);
                }
            }
        }
    }
}
