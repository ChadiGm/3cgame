using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(WaterBlobInput2D))]
    [RequireComponent(typeof(OneDropWaterResource2D))]
    public class OneDropController2D : MonoBehaviour
    {
#region Fields
        [Header("References")]
        [SerializeField] private WaterBlobInput2D inputSource;
        [SerializeField] private OneDropDeformation2D deformation;
        [SerializeField] private WaterBlobCharacter2D blobCharacter;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float acceleration = 55f;
        [SerializeField, Min(0f)] private float minAccelerationFromSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpForce = 12f;
        [SerializeField, Min(0f)] private float postJumpGroundIgnoreTime = 0.08f;
        [SerializeField, Min(0f)] private float minJumpInterval = 0.22f;

        [Header("Scaling")]
        [Tooltip("Minimum speed multiplier when water is empty.")]
        [SerializeField, Range(0.1f, 1f)] private float minAgilityScale = 0.65f;

        [Header("Physics Tuning")]
        [SerializeField] private bool overrideRigidbodyDamping = true;
        [SerializeField, Min(0f)] private float linearDamping = 0f;
        [SerializeField, Min(0f)] private float angularDamping = 0.05f;

        [Header("Slide Dash")]
        [SerializeField, Min(0f)] private float slideSpeed = 14f;
        [SerializeField, Min(0f)] private float slideDuration = 0.18f;
        [SerializeField, Min(0f)] private float slideCooldown = 0.25f;

        [Header("Climbing")]
        [SerializeField, Min(0f)] private float climbSpeed = 7.5f;
        [SerializeField, Min(0f)] private float climbAcceleration = 70f;
        [SerializeField] private Vector2 climbEntryBoost = new(0f, 8.5f);
        [SerializeField] private Vector2 climbConstantFlow = new(0f, 3.5f);
        [SerializeField, Min(0f)] private float climbMinAccelFromSpeed = 4f;
        [SerializeField, Min(0f)] private float wallJumpHorizontalForce = 9.5f;
        [SerializeField, Min(0f)] private float wallJumpVerticalForce = 11f;
        [SerializeField] private float gravityWhenNotClimbing = 3f;
        [Tooltip("Only walls in this layer mask can be climbed. If empty, Wall Mask is used.")]
        [SerializeField] private LayerMask climbableWallMask = 0;
        [SerializeField] private bool allowWallJumpWhileClimbing = false;
        [Tooltip("Allow leaving the wall by pressing the direction away from the wall.")]
        [SerializeField] private bool allowDetachByPressingAway = true;

        [Header("Detection")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask wallMask;
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.12f;
        [SerializeField, Min(0.01f)] private float wallCheckDistance = 0.12f;

        [Header("Rotation")]
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 12f;
        [SerializeField, Range(0f, 90f)] private float maxGroundAngle = 35f;


        [Header("Edge Stick Prevention")]
        [SerializeField, Range(0f, 1f)] private float sideNormalMinX = 0.72f;
        [SerializeField, Range(0f, 1f)] private float sideNormalMaxY = 0.45f;

        [Header("Soft Body Visual (Deprecated - logic moved to OneDropDeformation2D)")]
        [SerializeField, HideInInspector] private float landingMinImpactSpeed = 6f;

        [Header("Anti Crush (Deprecated - logic moved to OneDropDeformation2D)")]
        [SerializeField] private LayerMask ceilingMask = 0;
        [SerializeField, Min(0.01f)] private float ceilingCheckDistance = 0.14f;

        private Rigidbody2D rb;
        private BoxCollider2D box;
        private PhysicsMaterial2D runtimeMaterial;

        private float inputX;
        private float inputY;
        private bool jumpPressed;
        private bool isClimbing;
        private bool isSliding;
        private int climbWallDirection;
        private int slideDirection;
        private float facingSign = 1f;
        private bool grounded;
        private bool groundedLastStep;
        private float previousVelY;
        private Vector2 previousVelocity;
        private float slideTimer;
        private float slideCooldownTimer;
        private float postJumpGroundIgnoreTimer;
        private float jumpIntervalTimer;
        private bool cancelHorizontalForEdgeStick;
        private bool touchingWallThisStep;
        private int touchingWallDirection;
        private bool touchingCeilingThisStep;
        private float ceilingCompression01;
        private Vector2 currentGroundNormal = Vector2.up;
        private OneDropWaterResource2D waterResource;

        public enum SoftBodyReactionMode
        {
            Free,
            SlideSync,
            ClimbSync,
        }

        public readonly struct DeformationState
        {
            public DeformationState(
                Vector2 velocity,
                Vector2 previousVelocity,
                float inputX,
                float facingSign,
                bool isGrounded,
                bool isClimbing,
                bool isSliding,
                bool touchingWall,
                int touchingWallDirection,
                bool touchingCeiling,
                float ceilingCompression01,
                Vector2 groundNormal,
                float maxGroundAngle,
                int climbWallDirection,
                float acceleration,
                float moveSpeed,
                float jumpForce,
                Vector2 softBodySyncVelocity,
                SoftBodyReactionMode softBodyMode)
            {
                Velocity = velocity;
                PreviousVelocity = previousVelocity;
                InputX = inputX;
                FacingSign = facingSign;
                IsGrounded = isGrounded;
                IsClimbing = isClimbing;
                IsSliding = isSliding;
                TouchingWall = touchingWall;
                TouchingWallDirection = touchingWallDirection;
                TouchingCeiling = touchingCeiling;
                CeilingCompression01 = ceilingCompression01;
                GroundNormal = groundNormal;
                MaxGroundAngle = maxGroundAngle;
                ClimbWallDirection = climbWallDirection;
                Acceleration = acceleration;
                MoveSpeed = moveSpeed;
                JumpForce = jumpForce;
                SoftBodySyncVelocity = softBodySyncVelocity;
                SoftBodyMode = softBodyMode;
            }

            public Vector2 Velocity { get; }
            public Vector2 PreviousVelocity { get; }
            public float InputX { get; }
            public float FacingSign { get; }
            public bool IsGrounded { get; }
            public bool IsClimbing { get; }
            public bool IsSliding { get; }
            public bool TouchingWall { get; }
            public int TouchingWallDirection { get; }
            public bool TouchingCeiling { get; }
            public float CeilingCompression01 { get; }
            public Vector2 GroundNormal { get; }
            public float MaxGroundAngle { get; }
            public int ClimbWallDirection { get; }
            public float Acceleration { get; }
            public float MoveSpeed { get; }
            public float JumpForce { get; }
            public Vector2 SoftBodySyncVelocity { get; }
            public SoftBodyReactionMode SoftBodyMode { get; }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            waterResource = GetComponent<OneDropWaterResource2D>();
            if (waterResource == null)
            {
                waterResource = gameObject.AddComponent<OneDropWaterResource2D>();
            }

            BindReferences();
            if (inputSource == null)
            {
                Debug.LogError("[OneDropController2D] Missing required WaterBlobInput2D reference.", this);
                enabled = false;
                return;
            }

            // Ensure groundMask includes Default layer for ground detection compatibility
            // This addresses cases where ground objects are on Default layer instead of dedicated ground layer
            // References: RELEASE_NOTES_ONE_DROP.txt v0.3 - "ground detection now validates floor normals"
            groundMask |= LayerMask.GetMask("Default");

            facingSign = transform.localScale.x >= 0f ? 1f : -1f;
        }

        private void OnValidate()
        {
            BindReferences();
        }

        private void BindReferences()
        {
            if (inputSource == null)
            {
                inputSource = GetComponent<WaterBlobInput2D>();
            }

            if (deformation == null)
            {
                deformation = GetComponent<OneDropDeformation2D>();
            }

            if (blobCharacter == null)
            {
                blobCharacter = GetComponent<WaterBlobCharacter2D>();
            }
        }
#endregion

#region Input
        private void Start()
        {
            rb.gravityScale = gravityWhenNotClimbing;
            if (overrideRigidbodyDamping)
            {
                rb.linearDamping = linearDamping;
                rb.angularDamping = angularDamping;
            }

            CreateAndApplyZeroFrictionMaterial();
            previousVelocity = rb.linearVelocity;
        }

        private void Update()
        {
            ReadInput();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            slideCooldownTimer = Mathf.Max(0f, slideCooldownTimer - dt);
            slideTimer = Mathf.Max(0f, slideTimer - dt);
            postJumpGroundIgnoreTimer = Mathf.Max(0f, postJumpGroundIgnoreTimer - dt);
            jumpIntervalTimer = Mathf.Max(0f, jumpIntervalTimer - dt);

            RaycastHit2D groundHit = default;
            bool hasGroundHit = CheckGround(out groundHit);
            grounded = postJumpGroundIgnoreTimer <= 0f && hasGroundHit && IsGroundHitValid(groundHit);
            bool touchingWall = CheckWall(out RaycastHit2D wallHit, out int wallDirection);
            bool touchingCeiling = CheckCeiling(out _, out float ceilingCompress);
            touchingWallThisStep = touchingWall;
            touchingWallDirection = wallDirection;
            touchingCeilingThisStep = touchingCeiling;
            ceilingCompression01 = ceilingCompress;
            currentGroundNormal = groundHit.collider != null ? groundHit.normal : Vector2.up;

            if (isSliding && slideTimer <= 0f)
            {
                isSliding = false;
            }

            bool touchingClimbWall = touchingWall && IsWallClimbable(wallHit);
            UpdateClimbState(touchingClimbWall, wallDirection);

            if (!groundedLastStep && grounded && -previousVelY >= landingMinImpactSpeed)
            {
                deformation?.NotifyLanding(-previousVelY);
            }

            if (isClimbing)
            {
                ApplyClimbMovement();
                if (allowWallJumpWhileClimbing && jumpPressed && jumpIntervalTimer <= 0f)
                {
                    DoWallJump();
                }
            }
            else
            {
                if (isSliding)
                {
                    ApplySlideMovement();
                }
                else
                {
                    ApplyHorizontalMovement();
                }

                if (jumpPressed && grounded && jumpIntervalTimer <= 0f)
                {
                    DoGroundJump();
                }
            }

            if (cancelHorizontalForEdgeStick && !grounded && !isClimbing)
            {
                if (Mathf.Abs(inputX) < 0.1f)
                {
                    Vector2 v = rb.linearVelocity;
                    v.x = 0f;
                    rb.linearVelocity = v;
                }
                cancelHorizontalForEdgeStick = false;
            }

            if (waterResource != null)
            {
                bool isMoving = Mathf.Abs(inputX) > 0.1f || isSliding || isClimbing;
                waterResource.ConsumeMove(dt, isMoving);
            }

            ApplyRotation(groundHit);
            deformation?.ApplyControllerState(BuildDeformationState(), dt);

            jumpPressed = false;
            groundedLastStep = grounded;
            previousVelY = rb.linearVelocity.y;
            previousVelocity = rb.linearVelocity;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision == null || grounded || isClimbing)
            {
                return;
            }

            if (Mathf.Abs(inputX) > 0.1f)
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector2 n = collision.GetContact(i).normal;
                if (Mathf.Abs(n.x) >= sideNormalMinX && Mathf.Abs(n.y) <= sideNormalMaxY)
                {
                    cancelHorizontalForEdgeStick = true;
                    return;
                }
            }
        }

        private void ReadInput()
        {
            if (inputSource == null)
            {
                inputX = 0f;
                inputY = 0f;
                jumpPressed = false;
                return;
            }

            Vector2 move = inputSource.Move;
            inputX = Mathf.Clamp(move.x, -1f, 1f);
            inputY = Mathf.Clamp(move.y, -1f, 1f);

            if (inputSource.ConsumeJumpPressed())
            {
                jumpPressed = true;
            }

            ProcessSlideInput(inputSource.ConsumeSlidePressed());
        }

        private void UpdateClimbState(bool touchingWall, int wallDirection)
        {
            bool horizontalInput = Mathf.Abs(inputX) > 0.1f;
            bool movingTowardWall = touchingWall && horizontalInput && (wallDirection == 0 || Mathf.Sign(inputX) == wallDirection);
            bool pressingUp = inputY > 0.1f;

            if (!isClimbing)
            {
                bool pressingDown = inputY < -0.1f;
                bool pushingIntoSomeWall = touchingWall && horizontalInput;
                if (!pressingDown && touchingWall && (pressingUp || movingTowardWall || pushingIntoSomeWall))
                {
                    isClimbing = true;
                    isSliding = false;
                    climbWallDirection = wallDirection == 0 ? (int)Mathf.Sign(facingSign) : wallDirection;
                    rb.gravityScale = 0f;

                    Vector2 boost = new(climbWallDirection * 0.2f, climbEntryBoost.y);
                    rb.linearVelocity = new Vector2(0f, Mathf.Max(rb.linearVelocity.y, 0f));
                    rb.AddForce(boost, ForceMode2D.Impulse);
                    Debug.Log($"[OneDropController] Climbing started. Kinetic Boost: {boost}");
                }

                return;
            }

            bool hasVerticalIntent = Mathf.Abs(inputY) > 0.1f;
            if (allowDetachByPressingAway && horizontalInput && !hasVerticalIntent && climbWallDirection != 0 && Mathf.Sign(inputX) != climbWallDirection)
            {
                StopClimbing();
                return;
            }

            if (!touchingWall)
            {
                StopClimbing();
            }
        }

        private void ProcessSlideInput(bool shiftDown)
        {
            if (isClimbing || isSliding || slideCooldownTimer > 0f)
            {
                return;
            }

            if (shiftDown)
            {
                int dir = Mathf.Abs(inputX) > 0.1f ? (int)Mathf.Sign(inputX) : (int)Mathf.Sign(facingSign);
                StartSlide(dir);
            }
        }

        private void StartSlide(int direction)
        {
            if (direction == 0)
            {
                return;
            }

            isSliding = true;
            slideDirection = direction;
            slideTimer = slideDuration;
            slideCooldownTimer = slideCooldown;
            facingSign = direction;

            if (waterResource != null)
            {
                waterResource.ConsumeSlide();
            }
        }

        private void ApplyHorizontalMovement()
        {
            rb.gravityScale = gravityWhenNotClimbing;
            float agility = GetAgilityScale();
            float targetX = inputX * moveSpeed * agility;
            float effectiveAcceleration = Mathf.Max(acceleration, moveSpeed * minAccelerationFromSpeed) * agility;
            float nextX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, effectiveAcceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(nextX, rb.linearVelocity.y);

            if (Mathf.Abs(inputX) > 0.01f)
            {
                facingSign = Mathf.Sign(inputX);
            }
        }

        private void ApplySlideMovement()
        {
            rb.gravityScale = gravityWhenNotClimbing;
            Vector2 v = rb.linearVelocity;
            v.x = slideDirection * slideSpeed * GetAgilityScale();
            rb.linearVelocity = v;
        }

        private void ApplyClimbMovement()
        {
            rb.gravityScale = 0f;

            float agility = GetAgilityScale();
            float targetY = (inputY * climbSpeed * agility) + (climbConstantFlow.y * agility);
            float effAccel = Mathf.Max(climbAcceleration, climbSpeed * climbMinAccelFromSpeed) * agility;
            float nextY = Mathf.MoveTowards(rb.linearVelocity.y, targetY, effAccel * Time.fixedDeltaTime);
            float nextX = climbConstantFlow.x * agility;

            rb.linearVelocity = new Vector2(nextX, nextY);

            if (inputY < -0.05f)
            {
                facingSign = -Mathf.Sign(climbWallDirection);
            }
            else
            {
                facingSign = Mathf.Sign(climbWallDirection);
            }
        }

        private void DoGroundJump()
        {
            float scaledJump = jumpForce * GetAgilityScale();
            EventBus.Publish(new AudioTriggerEvent(AudioEventType.Jump));
            deformation?.NotifyJump(Vector2.up * (scaledJump * 0.2f));
            jumpIntervalTimer = minJumpInterval;
            postJumpGroundIgnoreTimer = postJumpGroundIgnoreTime;

            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(Vector2.up * scaledJump, ForceMode2D.Impulse);

            // Reset edge stick prevention when jumping to prevent movement lag near platforms
            // References: RELEASE_NOTES_ONE_DROP.txt v0.2 - "anti-edge-stick collision handling"
            cancelHorizontalForEdgeStick = false;

            if (waterResource != null)
            {
                waterResource.ConsumeJump();
            }
        }

        private void DoWallJump()
        {
            int away = -climbWallDirection;
            StopClimbing();
            float agility = GetAgilityScale();
            Vector2 jumpImp = new(away * wallJumpHorizontalForce, wallJumpVerticalForce);
            Vector2 scaledJumpImpulse = jumpImp * agility;
            EventBus.Publish(new AudioTriggerEvent(AudioEventType.Jump));
            deformation?.NotifyWallDetach(away, scaledJumpImpulse * 0.2f);
            jumpIntervalTimer = minJumpInterval;
            postJumpGroundIgnoreTimer = postJumpGroundIgnoreTime;

            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(scaledJumpImpulse, ForceMode2D.Impulse);

            // Reset edge stick prevention when wall jumping to prevent movement lag near platforms
            // References: RELEASE_NOTES_ONE_DROP.txt v0.2 - "anti-edge-stick collision handling"
            cancelHorizontalForEdgeStick = false;

            facingSign = away;
            if (waterResource != null)
            {
                waterResource.ConsumeJump();
            }
        }

        private void StopClimbing()
        {
            isClimbing = false;
            climbWallDirection = 0;
            rb.gravityScale = gravityWhenNotClimbing;
        }

        private void ApplyRotation(RaycastHit2D groundHit)
        {
            float targetZ = 0f;

            if (isClimbing)
            {
                targetZ = climbWallDirection > 0 ? -90f : 90f;
            }
            else if (grounded && groundHit.collider != null)
            {
                float normalAngle = Mathf.Atan2(groundHit.normal.y, groundHit.normal.x) * Mathf.Rad2Deg;
                float groundAngle = normalAngle - 90f;
                targetZ = Mathf.Clamp(Mathf.DeltaAngle(0f, groundAngle), -maxGroundAngle, maxGroundAngle);
            }

            Quaternion target = Quaternion.Euler(0f, 0f, targetZ);
            Quaternion current = Quaternion.Euler(0f, 0f, rb.rotation);
            Quaternion next = Quaternion.Lerp(current, target, Mathf.Clamp01(rotationLerpSpeed * Time.fixedDeltaTime));
            rb.MoveRotation(next.eulerAngles.z);
        }

        private DeformationState BuildDeformationState()
        {
            Vector2 velocity = rb.linearVelocity;
            SoftBodyReactionMode softBodyMode = SoftBodyReactionMode.Free;

            if (isClimbing)
            {
                softBodyMode = SoftBodyReactionMode.ClimbSync;
            }
            else if (isSliding)
            {
                softBodyMode = SoftBodyReactionMode.SlideSync;
            }

            return new DeformationState(
                velocity,
                previousVelocity,
                inputX,
                facingSign,
                grounded,
                isClimbing,
                isSliding,
                touchingWallThisStep,
                touchingWallDirection,
                touchingCeilingThisStep,
                ceilingCompression01,
                currentGroundNormal,
                maxGroundAngle,
                climbWallDirection,
                acceleration,
                moveSpeed,
                jumpForce,
                velocity,
                softBodyMode);
        }

        private bool CheckGround(out RaycastHit2D hit)
        {
            Vector2 origin = (Vector2)transform.position;
            float radius = (blobCharacter != null) ? blobCharacter.Radius : 0.45f;
            hit = Physics2D.CircleCast(origin, radius, Vector2.down, groundCheckDistance, groundMask);
            return hit.collider != null;
        }

        private bool IsGroundHitValid(RaycastHit2D hit)
        {
            if (hit.collider == null)
            {
                return false;
            }

            float minGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            return hit.normal.y >= minGroundNormalY;
        }

        private bool CheckWall(out RaycastHit2D hit, out int wallDirection)
        {
            int preferredDir = isClimbing ? climbWallDirection : (facingSign >= 0f ? 1 : -1);
            LayerMask detectionMask = GetWallDetectionMask();
            Vector2 castOrigin = (Vector2)transform.position;

            bool TrySide(int dir, out RaycastHit2D sideHit)
            {
                Vector2 castDir = dir > 0 ? Vector2.right : Vector2.left;
                float radius = (blobCharacter != null) ? blobCharacter.Radius : 0.45f;
                sideHit = Physics2D.CircleCast(castOrigin, radius, castDir, wallCheckDistance, detectionMask);
                return sideHit.collider != null;
            }

            if (TrySide(preferredDir, out hit))
            {
                wallDirection = preferredDir;
                return true;
            }

            if (TrySide(1, out hit))
            {
                wallDirection = 1;
                return true;
            }

            if (TrySide(-1, out hit))
            {
                wallDirection = -1;
                return true;
            }

            wallDirection = 0;
            return false;
        }

        private LayerMask GetWallDetectionMask()
        {
            if (climbableWallMask.value == 0)
            {
                return wallMask;
            }

            return wallMask | climbableWallMask;
        }

        private bool IsWallClimbable(RaycastHit2D hit)
        {
            if (hit.collider == null)
            {
                return false;
            }

            LayerMask effectiveClimbMask = climbableWallMask.value == 0 ? wallMask : climbableWallMask;
            return IsLayerInMask(hit.collider.gameObject.layer, effectiveClimbMask);
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private float GetAgilityScale()
        {
            if (waterResource == null)
            {
                return 1f;
            }

            return Mathf.Lerp(minAgilityScale, 1f, waterResource.WaterRatio);
        }

        private bool CheckCeiling(out RaycastHit2D hit, out float compression01)
        {
            Vector2 origin = (Vector2)transform.position;
            LayerMask mask = ceilingMask.value == 0 ? (groundMask | wallMask) : ceilingMask;
            float radius = (blobCharacter != null) ? blobCharacter.Radius : 0.45f;
            hit = Physics2D.CircleCast(origin, radius, Vector2.up, ceilingCheckDistance, mask);

            if (hit.collider == null)
            {
                compression01 = 0f;
                return false;
            }

            compression01 = 1f - Mathf.Clamp01(hit.distance / Mathf.Max(0.0001f, ceilingCheckDistance));
            return true;
        }


        private void CreateAndApplyZeroFrictionMaterial()
        {
            runtimeMaterial = new PhysicsMaterial2D("OneDropZeroFriction")
            {
                friction = 0f,
                bounciness = 0f
            };

            box.sharedMaterial = runtimeMaterial;
            rb.sharedMaterial = runtimeMaterial;
        }
#endregion
    }
}
