using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WaterBlobInput2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(WaterBlobWaterLevel2D))]
    public class WaterBlobCharacter2D : MonoBehaviour, IGameplay2DProxySource
    {
        private enum LocomotionState
        {
            Normal,
            Crouch,
            Slide,
            Dash
        }

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
        [Min(0f)] public float directionalLeanStrength = 1.35f;
        public bool invertDirectionalRoll = true;

        [Header("Slide")]
        [Min(0f)] public float slideInitialBoost = 5.5f;
        [Min(0.01f)] public float slideMaxDuration = 0.55f;
        [Min(0f)] public float slideMinEnterSpeed = 2.25f;
        [Min(0f)] public float slideFrictionMultiplier = 1.6f;
        [Range(0f, 1f)] public float slideSteerControl = 0.3f;
        [Min(0f)] public float slideExitSpeed = 1f;
        [Min(0f)] public float slideReleaseFalloff = 26f;
        [Min(0f)] public float slideMinSpeedForFalloff = 0.35f;

        [Header("Crouch")]
        [Range(0.2f, 1f)] public float crouchCompressionRatio = 0.72f;
        [Range(0.1f, 1f)] public float crouchMoveSpeedMultiplier = 0.5f;
        [Min(0f)] public float crouchEnterSharpness = 13f;
        [Min(0f)] public float crouchExitSharpness = 10f;
        [Min(1f)] public float crouchDampingMultiplier = 1.35f;

        [Header("Dash")]
        [Min(0f)] public float dashSpeed = 18f;
        [Min(0.01f)] public float dashDuration = 0.14f;
        [Min(0f)] public float dashCooldown = 0.28f;
        [Min(0)] public int dashGroundCharges = 1;
        [Min(0)] public int dashAirCharges = 1;
        [Range(0f, 1f)] public float dashGravityScaleMultiplier = 0.12f;
        [Min(0f)] public float dashPostLockout = 0.08f;
        [Min(0f)] public float dashNodeVelocityBlend = 0.45f;

        [Header("State Gates")]
        public bool allowCrouchInAir = false;
        public bool allowSlideInAir = false;
        public bool allowDashDuringSlide = false;
        public bool allowSlideAfterDash = true;

        [Header("Input Buffer")]
        [Min(0f)] public float slideBufferTime = 0.12f;
        [Min(0f)] public float dashBufferTime = 0.12f;
        [Min(0f)] public float attackBufferTime = 0.12f;

        [Header("Attack Projectile")]
        [Min(0f)] public float projectileCooldown = 0.3f;
        [Min(0.2f)] public float projectileSpeed = 11f;
        [Range(5f, 80f)] public float projectileLaunchAngleDeg = 28f;
        [Min(0f)] public float projectileForwardSpawnOffset = 1.2f;
        [Min(0f)] public float projectileUpSpawnOffset = 0.35f;
        [Range(0.2f, 2.2f)] public float projectileRadius = 0.72f;
        [Min(6)] public int projectilePointCount = 10;
        [Min(0.1f)] public float projectileLifetime = 3.5f;
        [Min(0f)] public float projectileGravityScale = 1.7f;
        [Min(0f)] public float projectileSpringFrequency = 11f;
        [Range(0f, 1f)] public float projectileSpringDamping = 0.58f;
        [Range(0f, 1f)] public float projectileInheritVelocity = 0.45f;
        [Range(0f, 12f)] public float projectileLaunchRandomnessDeg = 1.5f;
        [Min(0f)] public float projectileArmTime = 0.05f;
        [Min(0f)] public float projectileMinImpactSpeed = 1f;
        public LayerMask projectileHitMask = ~0;
        public bool projectileHitTriggers = false;

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

        [Header("Water Level")]
        [Tooltip("How much radius is gained per unit of waterLevel.  At 1.0 the blob is " +
                 "(1 + waterSizeFactor) times the base radius.  Zero = no size change.")]
        public float waterSizeFactor = 0.5f;
        [SerializeField] private WaterBlobWaterLevel2D waterLevelSource;
        [FormerlySerializedAs("waterLevel")]
        [Range(0f, 1f)]
        [SerializeField]
        [HideInInspector]
        private float legacyWaterLevel = 1f;
        [SerializeField]
        [HideInInspector]
        private bool legacyWaterLevelMigrated;

        // internal storage of the radius the designer set in the inspector; we use
        // this as the base when modifying size due to water level.
        private float baseRadius;

        [Header("Gameplay Plane")]
        public bool lockToGameplayPlane = true;
        public float gameplayPlaneZ = 0f;
        [Min(0f)] public float zPositionLerpSharpness = 18f;

        public IReadOnlyList<Rigidbody2D> PointBodies => pointBodies;
        public Rigidbody2D CoreBody => coreBody;
        public float Radius => radius;
        public float WaterLevel => waterLevelSource != null ? waterLevelSource.WaterLevel : 1f;
        public float GameplayPlaneZ => gameplayPlaneZ;
        public bool PlaneLockEnabled => lockToGameplayPlane;
        public Transform ProxySourceTransform => transform;
        public string LocomotionStateName => currentLocomotionState.ToString();
        public bool IsDashing => currentLocomotionState == LocomotionState.Dash;
        public bool IsSliding => currentLocomotionState == LocomotionState.Slide;
        public bool IsCrouching => currentLocomotionState == LocomotionState.Crouch;

        /// <summary>
        /// Adjusts the blob's stored water level by the given amount (delta may be
        /// positive or negative).  The value is clamped between 0 and 1 and the blob's
        /// physical radius is updated immediately (a rebuild is triggered automatically
        /// if necessary).
        /// </summary>
        public void ChangeWaterLevel(float delta)
        {
            EnsureWaterLevelSource();
            waterLevelSource?.ChangeWaterLevel(delta);
        }

        /// <summary>
        /// Convenience wrapper for <see cref="ChangeWaterLevel(float)"/> with a positive
        /// amount.
        /// </summary>
        public void AddWater(float amount)
        {
            ChangeWaterLevel(amount);
        }

        /// <summary>
        /// Convenience wrapper for <see cref="ChangeWaterLevel(float)"/> with a negative
        /// amount.
        /// </summary>
        public void RemoveWater(float amount)
        {
            ChangeWaterLevel(-amount);
        }

        private bool needsRebuildDueToWater;
        private bool isValidating;

        private void ApplyWaterSize()
        {
            // scale radius based on waterLevel and factor.  Rebuild is deferred when
            // the call comes from OnValidate so we don't modify the hierarchy during a
            // validation callback (which triggers SendMessage errors).
            radius = baseRadius * (1f + WaterLevel * waterSizeFactor);

            if (!Application.isPlaying)
            {
                return;
            }

            if (isValidating)
            {
                needsRebuildDueToWater = true;
            }
            else
            {
                RebuildBlob();
            }
        }

        private readonly List<Rigidbody2D> pointBodies = new();
        private readonly List<CircleCollider2D> pointColliders = new();
        private readonly List<SpringJoint2D> coreSprings = new();

        private Rigidbody2D coreBody;
        private CircleCollider2D coreCollider;
        private WaterBlobInput2D input;
        private PhysicsMaterial2D runtimeMaterial;
        private Transform pointsRoot;

        private LocomotionState currentLocomotionState = LocomotionState.Normal;
        private float jumpBufferCounter;
        private float slideBufferCounter;
        private float dashBufferCounter;
        private float coyoteCounter;
        private float slideTimeRemaining;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private float dashPostLockoutRemaining;
        private float attackBufferCounter;
        private float projectileCooldownRemaining;
        private float currentCompression;
        private float dashDirectionSign = 1f;
        private float lastFacingSign = 1f;
        private bool wasGrounded;
        private int pendingJumpLaunchFrames;
        private int groundDashChargesRemaining;
        private int airDashChargesRemaining;

        private void Awake()
        {
            input = GetComponent<WaterBlobInput2D>();
            EnsureWaterLevelSource();
            if (GetComponent<WaterBlobMovementVfx2D>() == null)
            {
                gameObject.AddComponent<WaterBlobMovementVfx2D>();
            }

            // Remember the designer radius so we can scale it when water changes.
            baseRadius = radius;
            radius = baseRadius * (1f + WaterLevel * waterSizeFactor);

            EnsureCore();
            ResetLocomotionState();
        }

        private void OnEnable()
        {
            EnsureWaterLevelSource();
            if (waterLevelSource != null)
            {
                waterLevelSource.WaterLevelChanged += HandleWaterLevelChanged;
            }
        }

        private void OnDisable()
        {
            if (waterLevelSource != null)
            {
                waterLevelSource.WaterLevelChanged -= HandleWaterLevelChanged;
            }
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
            isValidating = true;

            if (pointCount < 8)
            {
                pointCount = 8;
            }

            crouchCompressionRatio = Mathf.Clamp(crouchCompressionRatio, 0.2f, 1f);
            crouchMoveSpeedMultiplier = Mathf.Clamp(crouchMoveSpeedMultiplier, 0.1f, 1f);
            waterSizeFactor = Mathf.Max(0f, waterSizeFactor);
            EnsureWaterLevelSource();

            if (!Application.isPlaying)
            {
                // preserve base radius when editing; adjust effective radius so the
                // inspector shows the scaled size but avoid creating points.
                float scale = 1f + WaterLevel * waterSizeFactor;
                baseRadius = scale > 0.0001f ? radius / scale : radius;
                radius = baseRadius * scale;
            }
            else
            {
                ApplyWaterSize(); // rebuild is deferred if still validating
            }

            projectilePointCount = Mathf.Max(6, projectilePointCount);
            projectileRadius = Mathf.Clamp(projectileRadius, 0.2f, 2.2f);
            projectileLaunchAngleDeg = Mathf.Clamp(projectileLaunchAngleDeg, 5f, 80f);
            projectileSpringDamping = Mathf.Clamp01(projectileSpringDamping);
            projectileInheritVelocity = Mathf.Clamp01(projectileInheritVelocity);
            projectileLaunchRandomnessDeg = Mathf.Clamp(projectileLaunchRandomnessDeg, 0f, 12f);
            projectileArmTime = Mathf.Max(0f, projectileArmTime);
            projectileMinImpactSpeed = Mathf.Max(0f, projectileMinImpactSpeed);
            zPositionLerpSharpness = Mathf.Max(0f, zPositionLerpSharpness);

            EnsureCore();
            ApplyColliderMaterial();
            ApplyGameplayPlaneConstraint(0f);

            isValidating = false;
        }

        private void Update()
        {
            // if water-level changes were requested during validation, run the rebuild
            // now outside of the OnValidate callback.  this avoids the "SendMessage
            // cannot be called during Awake/CheckConsistency/OnValidate" errors.
            if (needsRebuildDueToWater)
            {
                needsRebuildDueToWater = false;
                RebuildBlob();
            }

            if (input == null)
            {
                return;
            }

            if (input.ConsumeJumpPressed())
            {
                jumpBufferCounter = jumpBufferTime;
            }

            if (input.ConsumeSlidePressed())
            {
                slideBufferCounter = slideBufferTime;
            }

            if (input.ConsumeDashPressed())
            {
                dashBufferCounter = dashBufferTime;
            }

            if (input.ConsumeAttackPressed())
            {
                attackBufferCounter = attackBufferTime;
            }
        }

        private void HandleWaterLevelChanged(float _)
        {
            ApplyWaterSize();
        }

        private void EnsureWaterLevelSource()
        {
            waterLevelSource ??= GetComponent<WaterBlobWaterLevel2D>();
            if (waterLevelSource == null)
            {
                waterLevelSource = gameObject.AddComponent<WaterBlobWaterLevel2D>();
            }

            if (!legacyWaterLevelMigrated && waterLevelSource != null)
            {
                waterLevelSource.SetWaterLevel(legacyWaterLevel);
                legacyWaterLevelMigrated = true;
            }
        }

        private void FixedUpdate()
        {
            if (coreBody == null || input == null)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;
            TickTimers(dt);

            bool grounded = IsGrounded();
            HandleGrounding(grounded, dt);
            UpdateFacingDirection();
            TryFireProjectile();

            if (currentLocomotionState != LocomotionState.Dash)
            {
                TryEnterDash(grounded);
            }

            if (currentLocomotionState == LocomotionState.Dash)
            {
                UpdateDashState(grounded);
            }
            else
            {
                UpdateSlideAndCrouchState(grounded);
            }

            UpdateCompression(dt);
            float targetRadius = GetTargetRadius();

            ApplyStateBodyModifiers();
            ApplySpringDistances(targetRadius);
            ApplyRadialStabilization(targetRadius);

            ApplyStateMovement(grounded);
            UpdatePendingJumpLaunch();

            if (CanStartJumpNow())
            {
                if (currentLocomotionState == LocomotionState.Slide || currentLocomotionState == LocomotionState.Crouch)
                {
                    currentLocomotionState = LocomotionState.Normal;
                }

                ApplyJumpCompression();
                pendingJumpLaunchFrames = 1;
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
            }

            ClampHorizontalSpeed();
            ApplyGameplayPlaneConstraint(dt);
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
            coreSprings.Clear();

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

                SpringJoint2D coreSpring = AttachSpringToCore(pointBody.gameObject, radius);
                coreSprings.Add(coreSpring);

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
            ApplyGameplayPlaneConstraint(0f);
            ResetLocomotionState();
        }

        private void ApplyGameplayPlaneConstraint(float dt)
        {
            if (!lockToGameplayPlane)
            {
                return;
            }

            float sharpness = Mathf.Max(0.0001f, zPositionLerpSharpness);
            float t = dt <= 0f ? 1f : 1f - Mathf.Exp(-sharpness * dt);
            Vector3 corePosition = transform.position;
            corePosition.z = Mathf.Lerp(corePosition.z, gameplayPlaneZ, t);
            transform.position = corePosition;

            if (pointsRoot != null)
            {
                Vector3 rootPosition = pointsRoot.position;
                rootPosition.z = Mathf.Lerp(rootPosition.z, gameplayPlaneZ, t);
                pointsRoot.position = rootPosition;
            }

            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                Vector3 nodePosition = node.transform.position;
                nodePosition.z = Mathf.Lerp(nodePosition.z, gameplayPlaneZ, t);
                node.transform.position = nodePosition;
            }
        }

        private void TickTimers(float dt)
        {
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - dt);
            slideBufferCounter = Mathf.Max(0f, slideBufferCounter - dt);
            dashBufferCounter = Mathf.Max(0f, dashBufferCounter - dt);
            attackBufferCounter = Mathf.Max(0f, attackBufferCounter - dt);
            slideTimeRemaining = Mathf.Max(0f, slideTimeRemaining - dt);
            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - dt);
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - dt);
            dashPostLockoutRemaining = Mathf.Max(0f, dashPostLockoutRemaining - dt);
            projectileCooldownRemaining = Mathf.Max(0f, projectileCooldownRemaining - dt);
        }

        private void TryFireProjectile()
        {
            if (attackBufferCounter <= 0f || projectileCooldownRemaining > 0f)
            {
                return;
            }

            FireProjectile();
            attackBufferCounter = 0f;
            projectileCooldownRemaining = projectileCooldown;
        }

        private void FireProjectile()
        {
            float directionSign = ResolveDashDirectionSign();
            float angleRad = projectileLaunchAngleDeg * Mathf.Deg2Rad;
            if (projectileLaunchRandomnessDeg > 0f)
            {
                float randomAngleRad = Random.Range(-projectileLaunchRandomnessDeg, projectileLaunchRandomnessDeg) * Mathf.Deg2Rad;
                angleRad += randomAngleRad;
            }

            Vector2 launchDir = new(directionSign * Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Vector2 spawnPos = coreBody.position + new Vector2(directionSign * projectileForwardSpawnOffset, projectileUpSpawnOffset);

            GameObject projectileGo = new("SoftWaterProjectile");
            projectileGo.transform.position = spawnPos;
            projectileGo.transform.rotation = Quaternion.identity;

            WaterBlobProjectileSoftBody2D projectile = projectileGo.AddComponent<WaterBlobProjectileSoftBody2D>();
            projectile.Configure(
                projectilePointCount,
                projectileRadius,
                projectileLifetime,
                projectileGravityScale,
                projectileSpringFrequency,
                projectileSpringDamping,
                projectileArmTime,
                projectileMinImpactSpeed,
                projectileHitMask,
                projectileHitTriggers,
                WaterLevel);
            projectile.ApplyGameplayPlaneSettings(lockToGameplayPlane, gameplayPlaneZ, zPositionLerpSharpness);

            Vector2 initialVelocity = (launchDir * projectileSpeed) + (coreBody.linearVelocity * projectileInheritVelocity);
            projectile.Launch(initialVelocity, GatherSelfCollidersForProjectileIgnore());
        }

        private Collider2D[] GatherSelfCollidersForProjectileIgnore()
        {
            List<Collider2D> colliders = new(2 + pointBodies.Count);
            if (coreCollider != null)
            {
                colliders.Add(coreCollider);
            }

            for (int i = 0; i < pointColliders.Count; i++)
            {
                if (pointColliders[i] != null)
                {
                    colliders.Add(pointColliders[i]);
                }
            }

            return colliders.ToArray();
        }

        private void HandleGrounding(bool grounded, float dt)
        {
            coyoteCounter = grounded ? coyoteTime : Mathf.Max(0f, coyoteCounter - dt);

            if (grounded)
            {
                groundDashChargesRemaining = dashGroundCharges;
                if (!wasGrounded)
                {
                    airDashChargesRemaining = dashAirCharges;
                }
            }

            if (!grounded && !allowSlideInAir && currentLocomotionState == LocomotionState.Slide)
            {
                currentLocomotionState = LocomotionState.Normal;
            }

            if (!grounded && !allowCrouchInAir && currentLocomotionState == LocomotionState.Crouch)
            {
                currentLocomotionState = LocomotionState.Normal;
            }

            wasGrounded = grounded;
        }

        private void UpdateFacingDirection()
        {
            float inputX = input.Move.x;
            if (Mathf.Abs(inputX) > 0.05f)
            {
                lastFacingSign = Mathf.Sign(inputX);
                return;
            }

            float velocityX = coreBody.linearVelocity.x;
            if (Mathf.Abs(velocityX) > 0.1f)
            {
                lastFacingSign = Mathf.Sign(velocityX);
            }
        }

        private void TryEnterDash(bool grounded)
        {
            if (dashBufferCounter <= 0f || dashCooldownRemaining > 0f)
            {
                return;
            }

            if (currentLocomotionState == LocomotionState.Slide && !allowDashDuringSlide)
            {
                return;
            }

            bool hasCharge = grounded ? groundDashChargesRemaining > 0 : airDashChargesRemaining > 0;
            if (!hasCharge)
            {
                return;
            }

            EnterDashState(grounded);
            dashBufferCounter = 0f;
        }

        private void EnterDashState(bool grounded)
        {
            currentLocomotionState = LocomotionState.Dash;
            dashTimeRemaining = Mathf.Max(0.01f, dashDuration);
            dashCooldownRemaining = dashCooldown;
            dashDirectionSign = ResolveDashDirectionSign();

            if (grounded)
            {
                groundDashChargesRemaining = Mathf.Max(0, groundDashChargesRemaining - 1);
            }
            else
            {
                airDashChargesRemaining = Mathf.Max(0, airDashChargesRemaining - 1);
            }

            float dashVelocity = dashDirectionSign * dashSpeed;
            SetCoreHorizontalVelocity(dashVelocity);
            AlignNodeHorizontalVelocity(dashVelocity, dashNodeVelocityBlend);
            coreBody.angularVelocity = 0f;
        }

        private void UpdateDashState(bool grounded)
        {
            if (dashTimeRemaining > 0f)
            {
                return;
            }

            currentLocomotionState = LocomotionState.Normal;
            dashPostLockoutRemaining = dashPostLockout;

            if (grounded && allowSlideAfterDash && input.SlideHeld)
            {
                if (Mathf.Abs(coreBody.linearVelocity.x) >= slideMinEnterSpeed)
                {
                    EnterSlideState();
                    return;
                }

                currentLocomotionState = LocomotionState.Crouch;
            }
        }

        private void UpdateSlideAndCrouchState(bool grounded)
        {
            if (dashPostLockoutRemaining > 0f)
            {
                return;
            }

            if (!grounded)
            {
                return;
            }

            bool wantsSlideOrCrouch = input.SlideHeld;
            float speedAbs = Mathf.Abs(coreBody.linearVelocity.x);

            if (currentLocomotionState == LocomotionState.Slide)
            {
                bool wantsJump = jumpBufferCounter > 0f && coyoteCounter > 0f;
                bool shouldExit = !wantsSlideOrCrouch || wantsJump || slideTimeRemaining <= 0f || speedAbs < slideExitSpeed;

                if (shouldExit)
                {
                    currentLocomotionState = wantsSlideOrCrouch ? LocomotionState.Crouch : LocomotionState.Normal;
                }

                return;
            }

            if (currentLocomotionState == LocomotionState.Crouch)
            {
                if (!wantsSlideOrCrouch)
                {
                    currentLocomotionState = LocomotionState.Normal;
                    return;
                }

                if (speedAbs >= slideMinEnterSpeed)
                {
                    EnterSlideState();
                }

                return;
            }

            if (!wantsSlideOrCrouch)
            {
                return;
            }

            if (slideBufferCounter > 0f && speedAbs >= slideMinEnterSpeed)
            {
                EnterSlideState();
            }
            else
            {
                currentLocomotionState = LocomotionState.Crouch;
            }
        }

        private void EnterSlideState()
        {
            currentLocomotionState = LocomotionState.Slide;
            slideTimeRemaining = slideMaxDuration;
            slideBufferCounter = 0f;

            float sign = ResolveDashDirectionSign();
            float speedAbs = Mathf.Abs(coreBody.linearVelocity.x);
            float targetSpeedAbs = Mathf.Max(speedAbs, slideMinEnterSpeed) + slideInitialBoost;
            float targetSpeed = Mathf.Clamp(sign * targetSpeedAbs, -maxHorizontalSpeed * 1.4f, maxHorizontalSpeed * 1.4f);

            SetCoreHorizontalVelocity(targetSpeed);
            AlignNodeHorizontalVelocity(targetSpeed, 0.35f);
        }

        private void ApplyStateMovement(bool grounded)
        {
            if (currentLocomotionState == LocomotionState.Dash)
            {
                ApplyDashMotion();
                ApplyRotationControl(grounded, 0f);
                return;
            }

            if (currentLocomotionState == LocomotionState.Slide)
            {
                ApplySmoothMovement(grounded, 1f, slideSteerControl);
                ApplySlideDeceleration();
                ApplyRotationControl(grounded, 0.35f);
                return;
            }

            if (currentLocomotionState == LocomotionState.Crouch)
            {
                ApplySmoothMovement(grounded, crouchMoveSpeedMultiplier, crouchMoveSpeedMultiplier);
                ApplyRotationControl(grounded, 0.45f);
                return;
            }

            ApplySmoothMovement(grounded, 1f, 1f);
            ApplyRotationControl(grounded, 1f);
        }

        private void ApplyStateBodyModifiers()
        {
            float dampingMultiplier = (currentLocomotionState == LocomotionState.Crouch || currentLocomotionState == LocomotionState.Slide)
                ? crouchDampingMultiplier
                : 1f;
            float gravityMultiplier = currentLocomotionState == LocomotionState.Dash ? dashGravityScaleMultiplier : 1f;
            float frictionMultiplier = currentLocomotionState == LocomotionState.Slide ? slideFrictionMultiplier : 1f;

            coreBody.linearDamping = coreLinearDamping * dampingMultiplier;
            coreBody.angularDamping = coreAngularDamping;
            coreBody.gravityScale = gravityScale * gravityMultiplier;

            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                node.linearDamping = pointLinearDamping * dampingMultiplier;
                node.angularDamping = pointAngularDamping;
                node.gravityScale = gravityScale * gravityMultiplier;
            }

            CreateRuntimeMaterial();
            runtimeMaterial.friction = Mathf.Clamp01(friction * frictionMultiplier);
            runtimeMaterial.bounciness = bounciness;
        }

        private void ApplyDashMotion()
        {
            float targetX = dashDirectionSign * dashSpeed;
            float moveRate = dashSpeed * 45f * Time.fixedDeltaTime;
            float nextX = Mathf.MoveTowards(coreBody.linearVelocity.x, targetX, moveRate);
            SetCoreHorizontalVelocity(nextX);
            AlignNodeHorizontalVelocity(targetX, dashNodeVelocityBlend);
        }

        private void ApplySlideDeceleration()
        {
            float coreVelX = coreBody.linearVelocity.x;
            float strength = slideReleaseFalloff * Mathf.Max(0.1f, slideFrictionMultiplier);
            float releaseForceX = ComputeReleaseFalloffForceX(coreVelX, coreBody.mass, maxControlForce * 1.8f, strength);
            if (Mathf.Abs(releaseForceX) > 0f)
            {
                coreBody.AddForce(new Vector2(releaseForceX, 0f), ForceMode2D.Force);
            }

            float nodeForceCap = maxControlForce * 0.75f;
            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                float nodeForceX = ComputeReleaseFalloffForceX(node.linearVelocity.x, node.mass, nodeForceCap, strength);
                if (Mathf.Abs(nodeForceX) > 0f)
                {
                    node.AddForce(new Vector2(nodeForceX, 0f), ForceMode2D.Force);
                }
            }
        }

        private void UpdateCompression(float dt)
        {
            float targetCompression = 0f;
            if (currentLocomotionState == LocomotionState.Crouch)
            {
                targetCompression = 1f;
            }
            else if (currentLocomotionState == LocomotionState.Slide)
            {
                targetCompression = 0.55f;
            }

            float sharpness = targetCompression > currentCompression ? crouchEnterSharpness : crouchExitSharpness;
            float lerp = DampedLerp(sharpness, dt);
            currentCompression = Mathf.Lerp(currentCompression, targetCompression, lerp);
        }

        private float GetTargetRadius()
        {
            float crouchedRadius = radius * crouchCompressionRatio;
            return Mathf.Lerp(radius, crouchedRadius, currentCompression);
        }

        private void ApplySpringDistances(float targetRadius)
        {
            for (int i = 0; i < coreSprings.Count; i++)
            {
                SpringJoint2D spring = coreSprings[i];
                if (spring != null)
                {
                    spring.distance = targetRadius;
                }
            }
        }

        private bool CanStartJumpNow()
        {
            if (currentLocomotionState == LocomotionState.Dash)
            {
                return false;
            }

            return pendingJumpLaunchFrames == 0 && jumpBufferCounter > 0f && coyoteCounter > 0f;
        }

        private void ApplySmoothMovement(bool grounded, float speedMultiplier, float steerMultiplier)
        {
            float inputX = input.Move.x;
            float control = grounded ? 1f : airControl;
            float desiredX = inputX * maxHorizontalSpeed * Mathf.Max(0f, speedMultiplier);
            float response = moveResponse * control * Mathf.Max(0f, steerMultiplier);
            bool hasInput = Mathf.Abs(inputX) >= 0.05f;

            float coreVelX = coreBody.linearVelocity.x;
            float coreAccelRequest = (desiredX - coreVelX) * response;
            float coreForceCap = maxControlForce * Mathf.Max(0.2f, steerMultiplier);
            float coreForceX = Mathf.Clamp(coreAccelRequest * coreBody.mass, -coreForceCap, coreForceCap);
            coreBody.AddForce(new Vector2(coreForceX, 0f), ForceMode2D.Force);

            if (!hasInput)
            {
                float releaseForceX = ComputeReleaseFalloffForceX(coreVelX, coreBody.mass, coreForceCap * 1.35f, slideReleaseFalloff);
                if (Mathf.Abs(releaseForceX) > 0f)
                {
                    coreBody.AddForce(new Vector2(releaseForceX, 0f), ForceMode2D.Force);
                }
            }

            float nodeForceCap = coreForceCap * 0.45f;
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

        private void ApplyRotationControl(bool grounded, float controlMultiplier)
        {
            float clampedControl = Mathf.Clamp01(controlMultiplier);
            if (clampedControl <= 0f)
            {
                coreBody.angularVelocity = Mathf.MoveTowards(coreBody.angularVelocity, 0f, angularDecelGrounded * Time.fixedDeltaTime);
                return;
            }

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

            float desiredAngularVelocity = Mathf.Clamp((inputTarget + velocityTarget) * clampedControl, -maxAngularVelocityDeg, maxAngularVelocityDeg);
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
            float stateLimit = maxHorizontalSpeed;
            if (currentLocomotionState == LocomotionState.Crouch)
            {
                stateLimit *= crouchMoveSpeedMultiplier;
            }
            else if (currentLocomotionState == LocomotionState.Slide)
            {
                stateLimit *= 1.2f;
            }
            else if (currentLocomotionState == LocomotionState.Dash)
            {
                stateLimit = Mathf.Max(stateLimit, dashSpeed * 1.05f);
            }

            Vector2 coreVelocity = coreBody.linearVelocity;
            coreVelocity.x = Mathf.Clamp(coreVelocity.x, -stateLimit, stateLimit);
            coreBody.linearVelocity = coreVelocity;

            float nodeSpeedLimit = stateLimit * 1.25f;
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

        private void ApplyRadialStabilization(float targetRadius)
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
                float radiusError = targetRadius - distance;

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

        private SpringJoint2D AttachSpringToCore(GameObject node, float restDistance)
        {
            SpringJoint2D coreSpring = node.AddComponent<SpringJoint2D>();
            coreSpring.connectedBody = coreBody;
            coreSpring.autoConfigureDistance = false;
            coreSpring.distance = restDistance;
            coreSpring.frequency = coreSpringFrequency;
            coreSpring.dampingRatio = coreSpringDamping;
            return coreSpring;
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

        private void ResetLocomotionState()
        {
            currentLocomotionState = LocomotionState.Normal;
            jumpBufferCounter = 0f;
            slideBufferCounter = 0f;
            dashBufferCounter = 0f;
            coyoteCounter = 0f;
            slideTimeRemaining = 0f;
            dashTimeRemaining = 0f;
            dashCooldownRemaining = 0f;
            dashPostLockoutRemaining = 0f;
            attackBufferCounter = 0f;
            projectileCooldownRemaining = 0f;
            currentCompression = 0f;
            pendingJumpLaunchFrames = 0;
            dashDirectionSign = 1f;
            lastFacingSign = 1f;
            groundDashChargesRemaining = dashGroundCharges;
            airDashChargesRemaining = dashAirCharges;
            wasGrounded = false;
        }

        private float ResolveDashDirectionSign()
        {
            float inputX = input != null ? input.Move.x : 0f;
            if (Mathf.Abs(inputX) > 0.05f)
            {
                return Mathf.Sign(inputX);
            }

            float velocityX = coreBody != null ? coreBody.linearVelocity.x : 0f;
            if (Mathf.Abs(velocityX) > 0.1f)
            {
                return Mathf.Sign(velocityX);
            }

            return Mathf.Abs(lastFacingSign) > 0f ? Mathf.Sign(lastFacingSign) : 1f;
        }

        private void SetCoreHorizontalVelocity(float x)
        {
            Vector2 velocity = coreBody.linearVelocity;
            velocity.x = x;
            coreBody.linearVelocity = velocity;
        }

        private void AlignNodeHorizontalVelocity(float targetX, float blend)
        {
            float clampedBlend = Mathf.Clamp01(blend);
            for (int i = 0; i < pointBodies.Count; i++)
            {
                Rigidbody2D node = pointBodies[i];
                if (node == null)
                {
                    continue;
                }

                Vector2 velocity = node.linearVelocity;
                velocity.x = Mathf.Lerp(velocity.x, targetX, clampedBlend);
                node.linearVelocity = velocity;
            }
        }

        private static float DampedLerp(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0.0001f, sharpness) * dt);
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
