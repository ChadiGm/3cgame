using System.Collections.Generic;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WaterBlobInput2D))]
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

        [Header("Controller")]
        [Min(0f)] public float maxHorizontalSpeed = 8.8f;
        [Min(0f)] public float moveResponse = 6f;
        [Min(0f)] public float maxControlForce = 86f;
        [Range(0f, 1f)] public float pointMoveAssist = 0.2f;
        [Min(0f)] public float rollTorque = 16f;
        [Min(0f)] public float directionalLeanStrength = 1.35f;
        public bool invertDirectionalRoll = true;

        [Header("Slide")]
        [Min(0f)] public float slideReleaseFalloff = 26f;
        [Min(0f)] public float slideMinSpeedForFalloff = 0.35f;

        [Header("Rotation Control")]
        [Min(0f)] public float maxAngularVelocityDeg = 140f;
        [Min(0f)] public float inputRollSpeedDeg = 78f;
        [Min(0f)] public float velocityRollInfluence = 2.6f;
        [Min(0f)] public float maxVelocityRollContribution = 24f;
        [Min(0f)] public float angularAccelGrounded = 520f;
        [Min(0f)] public float angularAccelAir = 280f;
        [Min(0f)] public float angularDecelGrounded = 760f;
        [Min(0f)] public float angularDecelAir = 360f;
        [Min(1f)] public float noInputAngularDragBoost = 1.65f;

        [Header("Jump")]
        [Min(0f)] public float jumpImpulse = 10.8f;
        [Min(0f)] public float jumpCompressionImpulse = 7.5f;
        [Range(0f, 1f)] public float jumpCompressionPointAssist = 0.5f;
        [Min(0f)] public float jumpLaunchBoost = 1.22f;
        [Range(0f, 1f)] public float pointJumpAssist = 0.2f;
        [Range(0f, 1f)] public float airControl = 0.72f;
        [Min(0f)] public float coyoteTime = 0.16f;
        [Min(0f)] public float jumpBufferTime = 0.16f;
        public LayerMask groundMask = ~0;

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
        private WaterBlobInput2D input;
        private PhysicsMaterial2D runtimeMaterial;
        private Transform pointsRoot;

        private float jumpBufferCounter;
        private float coyoteCounter;
        private int pendingJumpLaunchFrames;

        private void Awake()
        {
            input = GetComponent<WaterBlobInput2D>();
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

        private void Update()
        {
            if (input != null && input.ConsumeJumpPressed())
            {
                jumpBufferCounter = jumpBufferTime;
            }
        }

        private void FixedUpdate()
        {
            if (coreBody == null || input == null)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - dt);

            bool grounded = IsGrounded();
            coyoteCounter = grounded ? coyoteTime : Mathf.Max(0f, coyoteCounter - dt);

            ApplyRadialStabilization();
            ApplySmoothMovement(grounded);
            ApplyRotationControl(grounded);

            UpdatePendingJumpLaunch();

            if (pendingJumpLaunchFrames == 0 && jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                ApplyJumpCompression();
                pendingJumpLaunchFrames = 1;
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
            }

            ClampHorizontalSpeed();
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

        private void ApplySmoothMovement(bool grounded)
        {
            float inputX = input.Move.x;
            float control = grounded ? 1f : airControl;
            float desiredX = inputX * maxHorizontalSpeed;
            float response = moveResponse * control;
            bool hasInput = Mathf.Abs(inputX) >= 0.05f;

            float coreVelX = coreBody.linearVelocity.x;
            float coreAccelRequest = (desiredX - coreVelX) * response;
            float coreForceX = Mathf.Clamp(coreAccelRequest * coreBody.mass, -maxControlForce, maxControlForce);
            coreBody.AddForce(new Vector2(coreForceX, 0f), ForceMode2D.Force);
            if (!hasInput)
            {
                float releaseForceX = ComputeReleaseFalloffForceX(coreVelX, coreBody.mass, maxControlForce * 1.35f, slideReleaseFalloff);
                if (Mathf.Abs(releaseForceX) > 0f)
                {
                    coreBody.AddForce(new Vector2(releaseForceX, 0f), ForceMode2D.Force);
                }
            }

            float nodeForceCap = maxControlForce * 0.4f;
            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                float nodeVelX = node.linearVelocity.x;
                float nodeAccelRequest = (desiredX - nodeVelX) * response;
                float nodeForceX = Mathf.Clamp(nodeAccelRequest * node.mass * pointMoveAssist, -nodeForceCap, nodeForceCap);
                if (!hasInput)
                {
                    float nodeFalloffStrength = slideReleaseFalloff * Mathf.Clamp01(0.45f + pointMoveAssist);
                    nodeForceX += ComputeReleaseFalloffForceX(nodeVelX, node.mass, nodeForceCap * 1.2f, nodeFalloffStrength);
                }

                node.AddForce(new Vector2(nodeForceX, 0f), ForceMode2D.Force);
            }
        }

        private void ApplyRotationControl(bool grounded)
        {
            float inputX = input.Move.x;
            float velocityX = coreBody.linearVelocity.x;
            float rollSign = invertDirectionalRoll ? -1f : 1f;
            float directionalLean = Mathf.Max(0f, directionalLeanStrength);

            float inputTarget = rollSign * inputX * inputRollSpeedDeg * directionalLean;
            float velocityTarget = Mathf.Clamp(rollSign * velocityX * velocityRollInfluence * directionalLean, -maxVelocityRollContribution, maxVelocityRollContribution);
            if (Mathf.Abs(velocityX) < 0.05f)
            {
                velocityTarget = 0f;
            }

            float desiredAngularVelocity = Mathf.Clamp(inputTarget + velocityTarget, -maxAngularVelocityDeg, maxAngularVelocityDeg);
            float currentAngularVelocity = coreBody.angularVelocity;

            bool hasInput = Mathf.Abs(inputX) >= 0.05f;
            float accelRate = grounded ? angularAccelGrounded : angularAccelAir;
            float decelRate = grounded ? angularDecelGrounded : angularDecelAir;

            bool pushingAwayFromCurrent = Mathf.Sign(desiredAngularVelocity) != Mathf.Sign(currentAngularVelocity);
            bool increasingMagnitude = Mathf.Abs(desiredAngularVelocity) > Mathf.Abs(currentAngularVelocity);

            float moveRate = (pushingAwayFromCurrent || increasingMagnitude) ? accelRate : decelRate;
            if (!hasInput)
            {
                moveRate *= noInputAngularDragBoost;
            }

            float nextAngularVelocity = Mathf.MoveTowards(currentAngularVelocity, desiredAngularVelocity, moveRate * Time.fixedDeltaTime);
            coreBody.angularVelocity = Mathf.Clamp(nextAngularVelocity, -maxAngularVelocityDeg, maxAngularVelocityDeg);
        }

        private void UpdatePendingJumpLaunch()
        {
            if (pendingJumpLaunchFrames <= 0)
            {
                return;
            }

            pendingJumpLaunchFrames = Mathf.Max(0, pendingJumpLaunchFrames - 1);
            if (pendingJumpLaunchFrames == 0)
            {
                ApplyJumpLaunch();
            }
        }

        private void ApplyJumpCompression()
        {
            float compressionImpulse = Mathf.Max(0f, jumpCompressionImpulse);
            if (compressionImpulse <= 0f)
            {
                return;
            }

            coreBody.AddForce(Vector2.down * compressionImpulse, ForceMode2D.Impulse);

            float pointCompressionImpulse = compressionImpulse * jumpCompressionPointAssist;
            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                node.AddForce(Vector2.down * pointCompressionImpulse, ForceMode2D.Impulse);
            }
        }

        private void ApplyJumpLaunch()
        {
            float launchImpulse = jumpImpulse * Mathf.Max(0f, jumpLaunchBoost);

            if (coreBody.linearVelocity.y < 0f)
            {
                Vector2 coreVelocity = coreBody.linearVelocity;
                coreVelocity.y *= 0.35f;
                coreBody.linearVelocity = coreVelocity;
            }

            Vector2 coreImpulse = Vector2.up * launchImpulse;
            coreBody.AddForce(coreImpulse, ForceMode2D.Impulse);

            Vector2 pointImpulse = coreImpulse * pointJumpAssist;
            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                if (node.linearVelocity.y < 0f)
                {
                    Vector2 nodeVelocity = node.linearVelocity;
                    nodeVelocity.y *= 0.35f;
                    node.linearVelocity = nodeVelocity;
                }

                node.AddForce(pointImpulse, ForceMode2D.Impulse);
            }

            ClampVerticalVelocityAfterJump(launchImpulse);
        }

        private float ComputeReleaseFalloffForceX(float velocityX, float mass, float forceCap, float strength)
        {
            if (Mathf.Abs(velocityX) <= slideMinSpeedForFalloff || strength <= 0f)
            {
                return 0f;
            }

            float accel = -velocityX * strength;
            return Mathf.Clamp(accel * mass, -forceCap, forceCap);
        }

        private void ClampVerticalVelocityAfterJump(float launchImpulse)
        {
            float maxCoreUp = Mathf.Max(6f, launchImpulse * 1.9f);
            Vector2 coreVelocity = coreBody.linearVelocity;
            if (coreVelocity.y > maxCoreUp)
            {
                coreVelocity.y = maxCoreUp;
                coreBody.linearVelocity = coreVelocity;
            }

            float maxNodeUp = maxCoreUp * 1.1f;
            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                Vector2 velocity = node.linearVelocity;
                if (velocity.y > maxNodeUp)
                {
                    velocity.y = maxNodeUp;
                    node.linearVelocity = velocity;
                }
            }
        }

        private void ClampHorizontalSpeed()
        {
            Vector2 coreVelocity = coreBody.linearVelocity;
            coreVelocity.x = Mathf.Clamp(coreVelocity.x, -maxHorizontalSpeed, maxHorizontalSpeed);
            coreBody.linearVelocity = coreVelocity;

            float nodeSpeedLimit = maxHorizontalSpeed * 1.35f;
            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                Vector2 velocity = node.linearVelocity;
                velocity.x = Mathf.Clamp(velocity.x, -nodeSpeedLimit, nodeSpeedLimit);
                node.linearVelocity = velocity;
            }
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

        private bool IsGrounded()
        {
            if (coreCollider != null && coreCollider.IsTouchingLayers(groundMask))
            {
                return true;
            }

            for (int i = 0; i < pointColliders.Count; i++)
            {
                CircleCollider2D nodeCollider = pointColliders[i];
                if (nodeCollider != null && nodeCollider.IsTouchingLayers(groundMask))
                {
                    return true;
                }
            }

            return false;
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
            input = GetComponent<WaterBlobInput2D>();

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
