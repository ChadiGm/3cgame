using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class WaterBlobCharacter2D : MonoBehaviour
    {
        [Header("Blob Shape")]
        [Min(8)] public int pointCount = 18;
        [Min(0.5f)] public float radius = 1.85f;
        [Min(0.05f)] public float coreColliderRadius = 0.24f;
        [Min(0.05f)] public float pointColliderRadius = 0.28f;
        public bool rebuildOnStart = true;

        [Header("Mass & Damping")]
        [Min(0.1f)] public float coreMass = 1.15f;
        [Min(0.01f)] public float pointMass = 0.28f;
        [Min(0f)] public float gravityScale = 3.5f;
        [Min(0f)] public float coreLinearDamping = 1.45f;
        [Min(0f)] public float coreAngularDamping = 1.25f;
        [Min(0f)] public float pointLinearDamping = 1.15f;
        [Min(0f)] public float pointAngularDamping = 2.6f;

        [Header("Spring Setup")]
        [Min(0f)] public float coreSpringFrequency = 5.8f;
        [Range(0f, 1f)] public float coreSpringDamping = 0.9f;
        [Min(0f)] public float ringSpringFrequency = 7.4f;
        [Range(0f, 1f)] public float ringSpringDamping = 0.78f;
        public bool useSecondaryRingSprings = true;
        [Min(0f)] public float secondarySpringFrequency = 4.3f;
        [Range(0f, 1f)] public float secondarySpringDamping = 0.82f;

        [Header("Shape Stabilization")]
        [Min(0f)] public float radialStiffness = 20f;
        [Min(0f)] public float radialDamping = 8.5f;
        [Range(0f, 1f)] public float pointVelocityCarry = 0.2f;
        [Min(0f)] public float maxPointCarrySpeed = 8f;

        [Header("Surface")]
        [Range(0f, 1f)] public float bounciness = 0.12f;
        [Range(0f, 1f)] public float friction = 0.28f;

        public IReadOnlyList<Rigidbody2D> PointBodies => pointBodies;
        public Rigidbody2D CoreBody => coreBody;
        public float Radius => radius;

        private readonly List<Rigidbody2D> pointBodies = new();
        private readonly List<CircleCollider2D> pointColliders = new();

        private Rigidbody2D coreBody;
        private CircleCollider2D coreCollider;
        private PhysicsMaterial2D runtimeMaterial;
        private Transform pointsRoot;

        private void Awake()
        {
            EnsureCore();
        }

        private void Start()
        {
            if (rebuildOnStart)
            {
                RebuildBlob();
            }
        }

        private void OnValidate()
        {
            if (pointCount < 8)
            {
                pointCount = 8;
            }

            EnsureCore();
            ApplyColliderMaterial();
        }

        private void FixedUpdate()
        {
            if (coreBody == null)
            {
                return;
            }

            ApplyRadialStabilization();
            ApplyVelocityCarry();
        }

        [ContextMenu("Rebuild Blob")]
        public void RebuildBlob()
        {
            EnsureCore();
            EnsurePointsRoot();
            CreateRuntimeMaterial();

            ClearExistingPoints();
            pointBodies.Clear();
            pointColliders.Clear();

            float edgeDistance = GetChordLength(1);
            float secondaryDistance = GetChordLength(2);

            for (int i = 0; i < pointCount; i++)
            {
                float angle = i * Mathf.PI * 2f / pointCount;
                Vector2 ringOffset = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                GameObject point = new($"Point_{i:00}");
                point.transform.SetParent(pointsRoot);
                point.transform.position = coreBody.position + ringOffset;
                point.transform.rotation = Quaternion.identity;
                point.transform.localScale = Vector3.one;

                Rigidbody2D pointBody = point.AddComponent<Rigidbody2D>();
                ConfigurePointBody(pointBody);

                CircleCollider2D pointCollider = point.AddComponent<CircleCollider2D>();
                pointCollider.radius = pointColliderRadius;
                pointCollider.sharedMaterial = runtimeMaterial;

                AttachSpringToCore(pointBody.gameObject, radius);

                pointBodies.Add(pointBody);
                pointColliders.Add(pointCollider);
            }

            for (int i = 0; i < pointBodies.Count; i++)
            {
                int next = (i + 1) % pointBodies.Count;
                AttachRingSpring(pointBodies[i].gameObject, pointBodies[next], edgeDistance, ringSpringFrequency, ringSpringDamping);

                if (useSecondaryRingSprings && pointBodies.Count > 8)
                {
                    int next2 = (i + 2) % pointBodies.Count;
                    AttachRingSpring(pointBodies[i].gameObject, pointBodies[next2], secondaryDistance, secondarySpringFrequency, secondarySpringDamping);
                }
            }

            IgnoreInternalCollisions();
            ApplyCoreSettings();
        }

        private void ApplyRadialStabilization()
        {
            if (pointBodies.Count == 0)
            {
                return;
            }

            int nodeCount = pointBodies.Count;

            for (int i = 0; i < nodeCount; i++)
            {
                Rigidbody2D node = pointBodies[i];
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

        private void ApplyVelocityCarry()
        {
            if (pointBodies.Count == 0 || pointVelocityCarry <= 0f)
            {
                return;
            }

            Vector2 coreVelocity = coreBody.linearVelocity;
            float carryStrength = Mathf.Clamp01(pointVelocityCarry);
            float carrySpeedCap = Mathf.Max(0f, maxPointCarrySpeed);

            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                Vector2 velocityDelta = coreVelocity - node.linearVelocity;
                if (carrySpeedCap > 0f)
                {
                    velocityDelta = Vector2.ClampMagnitude(velocityDelta, carrySpeedCap);
                }

                Vector2 carryForce = velocityDelta * (carryStrength * node.mass);
                node.AddForce(carryForce, ForceMode2D.Force);
            }
        }

        private float GetChordLength(int step)
        {
            return 2f * radius * Mathf.Sin(step * Mathf.PI / pointCount);
        }

        private void ConfigurePointBody(Rigidbody2D pointBody)
        {
            pointBody.mass = pointMass;
            pointBody.gravityScale = gravityScale;
            pointBody.linearDamping = pointLinearDamping;
            pointBody.angularDamping = pointAngularDamping;
            pointBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            pointBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void AttachSpringToCore(GameObject node, float restDistance)
        {
            SpringJoint2D coreSpring = node.AddComponent<SpringJoint2D>();
            coreSpring.connectedBody = coreBody;
            coreSpring.autoConfigureDistance = false;
            coreSpring.distance = restDistance;
            coreSpring.frequency = coreSpringFrequency;
            coreSpring.dampingRatio = coreSpringDamping;
        }

        private static void AttachRingSpring(GameObject node, Rigidbody2D target, float restDistance, float frequency, float damping)
        {
            SpringJoint2D spring = node.AddComponent<SpringJoint2D>();
            spring.connectedBody = target;
            spring.autoConfigureDistance = false;
            spring.distance = restDistance;
            spring.frequency = frequency;
            spring.dampingRatio = damping;
        }

        private void EnsureCore()
        {
            coreBody = GetComponent<Rigidbody2D>();
            coreCollider = GetComponent<CircleCollider2D>();

            CreateRuntimeMaterial();
            ApplyCoreSettings();
        }

        private void ApplyCoreSettings()
        {
            if (coreBody == null || coreCollider == null)
            {
                return;
            }

            coreBody.mass = coreMass;
            coreBody.gravityScale = gravityScale;
            coreBody.linearDamping = coreLinearDamping;
            coreBody.angularDamping = coreAngularDamping;
            coreBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            coreBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            coreBody.freezeRotation = false;

            coreCollider.radius = coreColliderRadius;
            coreCollider.sharedMaterial = runtimeMaterial;
        }

        private void CreateRuntimeMaterial()
        {
            if (runtimeMaterial == null)
            {
                runtimeMaterial = new PhysicsMaterial2D("WaterBlobRuntime");
            }

            runtimeMaterial.bounciness = bounciness;
            runtimeMaterial.friction = friction;
        }

        private void ApplyColliderMaterial()
        {
            if (coreCollider != null)
            {
                coreCollider.sharedMaterial = runtimeMaterial;
            }

            for (int i = 0; i < pointColliders.Count; i++)
            {
                if (pointColliders[i] != null)
                {
                    pointColliders[i].sharedMaterial = runtimeMaterial;
                }
            }
        }

        private void EnsurePointsRoot()
        {
            if (pointsRoot != null)
            {
                return;
            }

            Transform legacyChildRoot = transform.Find("__BlobPoints");
            if (legacyChildRoot != null)
            {
                legacyChildRoot.SetParent(transform.parent, true);
                legacyChildRoot.name = $"__BlobPoints_{name}";
                pointsRoot = legacyChildRoot;
                return;
            }

            string rootName = $"__BlobPoints_{name}";
            Transform existing = transform.parent != null ? transform.parent.Find(rootName) : null;

            if (existing != null)
            {
                pointsRoot = existing;
                return;
            }

            GameObject root = new(rootName);
            pointsRoot = root.transform;
            pointsRoot.SetParent(transform.parent, true);
            pointsRoot.position = Vector3.zero;
            pointsRoot.rotation = Quaternion.identity;
            pointsRoot.localScale = Vector3.one;
        }

        private void ClearExistingPoints()
        {
            if (pointsRoot == null)
            {
                return;
            }

            for (int i = pointsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = pointsRoot.GetChild(i);
                SafeDestroy(child.gameObject);
            }
        }

        private void IgnoreInternalCollisions()
        {
            for (int i = 0; i < pointColliders.Count; i++)
            {
                CircleCollider2D a = pointColliders[i];
                if (a == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(coreCollider, a, true);

                for (int j = i + 1; j < pointColliders.Count; j++)
                {
                    CircleCollider2D b = pointColliders[j];
                    if (b != null)
                    {
                        Physics2D.IgnoreCollision(a, b, true);
                    }
                }
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
