using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(OneDropWaterResource2D))]
    public class OneDropController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private CharacterVFXController vfxController;

        [Header("VFX")]
        [SerializeField, Min(0f)] private float moveVfxSpeedThreshold = 0.15f;
        [SerializeField, Min(0f)] private float moveVfxStopDelay = 0.12f;

        [Header("Movement SFX")]
        [SerializeField] private AudioSource movementSfxSource;
        [SerializeField] private AudioSource actionSfxSource;
        [SerializeField] private AudioClip movementSfxClip;
        [SerializeField] private AudioClip movementSfxLeftClip;
        [SerializeField] private AudioClip movementSfxRightClip;
        [SerializeField] private AudioClip jumpSfxClip;
        [SerializeField] private AudioClip slideSfxClip;
        [SerializeField, Range(0f, 1f)] private float movementSfxVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float jumpSfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float slideSfxVolume = 0.85f;
        [SerializeField, Min(0f)] private float movementSfxFadeIn = 0.08f;
        [SerializeField, Min(0f)] private float movementSfxFadeOut = 0.18f;
        [SerializeField] private bool movementSfxPlayWhileSliding = false;
        [SerializeField] private bool movementSfxRequireGrounded = true;
        [SerializeField] private bool muteMovementLoopDuringActionSfx = true;
        [SerializeField, Min(0f)] private float actionSfxMoveLoopMuteExtraTime = 0.04f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float acceleration = 55f;
        [SerializeField, Min(0f)] private float minAccelerationFromSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpForce = 12f;
        [SerializeField, Min(0f)] private float postJumpGroundIgnoreTime = 0.08f;
        [SerializeField, Min(0f)] private float minJumpInterval = 0.22f;
        [SerializeField, Range(0f, 1f)] private float jumpPointImpulseScale = 0.22f;
        [SerializeField, Range(0f, 1f)] private float wallJumpPointImpulseScale = 0.18f;

        [Header("Physics Tuning")]
        [SerializeField] private bool overrideRigidbodyDamping = true;
        [SerializeField, Min(0f)] private float linearDamping = 0f;
        [SerializeField, Min(0f)] private float angularDamping = 0.05f;

        [Header("Slide Dash")]
        [SerializeField, Min(0f)] private float doubleTapWindow = 0.25f;
        [SerializeField, Min(0f)] private float slideSpeed = 14f;
        [SerializeField, Min(0f)] private float slideDuration = 0.18f;
        [SerializeField, Min(0f)] private float slideCooldown = 0.25f;

        [Header("Climbing")]
        [SerializeField, Min(0f)] private float climbSpeed = 4.8f;
        [SerializeField, Min(0f)] private float climbAcceleration = 55f;
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
        [SerializeField, Range(0.05f, 0.5f)] private float groundProbeHeightFactor = 0.22f;
        [SerializeField, Range(0.5f, 1f)] private float castWidthFactor = 0.95f;
        [SerializeField, Range(0.5f, 1f)] private float castHeightFactor = 0.95f;
        [SerializeField, Range(0f, 90f)] private float jumpMaxGroundAngle = 65f;

        [Header("Rotation")]
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 12f;
        [SerializeField, Range(0f, 90f)] private float maxGroundAngle = 35f;

        [Header("Edge Stick Prevention")]
        [SerializeField, Range(0f, 1f)] private float sideNormalMinX = 0.72f;
        [SerializeField, Range(0f, 1f)] private float sideNormalMaxY = 0.45f;

        [Header("Soft Body Visual")]
        [SerializeField, Min(0f)] private float idleGroundSquash = 0.08f;
        [SerializeField, Min(0f)] private float idleGroundSpread = 0.06f;
        [SerializeField, Min(0f)] private float runSquash = 0.12f;
        [SerializeField, Min(0f)] private float accelerationStretch = 0.1f;
        [SerializeField, Min(0f)] private float decelerationSquash = 0.12f;
        [SerializeField, Min(0f)] private float jumpStretch = 0.2f;
        [SerializeField, Min(0f)] private float fallStretch = 0.16f;
        [SerializeField, Min(0f)] private float jumpPreCompress = 0.16f;
        [SerializeField, Min(0f)] private float landingSquash = 0.22f;
        [SerializeField, Min(0f)] private float landingMinImpactSpeed = 6f;
        [SerializeField, Min(0f)] private float peakStretch = 0.08f;
        [SerializeField, Min(0f)] private float slopeAdaptiveSquash = 0.08f;
        [SerializeField, Min(0f)] private float wallAdhesionFlatten = 0.12f;
        [SerializeField, Min(0f)] private float wallAdhesionStretch = 0.1f;
        [SerializeField, Min(0f)] private float wallDetachStretch = 0.12f;
        [SerializeField, Min(0f)] private float visualMomentumOffset = 0.09f;

        [Header("Soft Body Dynamics")]
        [SerializeField, Min(0f)] private float movementResponseDelay = 0.06f;
        [SerializeField, Min(0f)] private float deformationSpring = 58f;
        [SerializeField, Min(0f)] private float deformationDamping = 10f;
        [SerializeField, Min(0f)] private float wobbleSpring = 44f;
        [SerializeField, Min(0f)] private float wobbleDamping = 7.5f;
        [SerializeField, Min(0f)] private float accelerationWobble = 0.05f;
        [SerializeField, Min(0f)] private float impactWobble = 0.12f;
        [SerializeField, Min(0f)] private float maxWobble = 0.24f;

        [Header("Anti Crush")]
        [Tooltip("Extra layers to test for low ceilings. If empty, uses Ground + Wall masks.")]
        [SerializeField] private LayerMask ceilingMask = 0;
        [SerializeField, Min(0.01f)] private float ceilingCheckDistance = 0.14f;
        [SerializeField, Range(0.3f, 1f)] private float minVisualHeightScale = 0.82f;
        [SerializeField, Range(0f, 1f)] private float ceilingCompressionDampen = 0.25f;
        [SerializeField, Range(0f, 1f)] private float ceilingSpreadDampen = 0.55f;

        private Rigidbody2D rb;
        private BoxCollider2D box;
        private CircleCollider2D coreCircle;
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
        private float accelerationSignal;
        private float slideTimer;
        private float slideCooldownTimer;
        private float postJumpGroundIgnoreTimer;
        private float jumpIntervalTimer;
        private float lastLeftTapTime = -99f;
        private float lastRightTapTime = -99f;
        private float landingPulse;
        private float jumpPulse;
        private float wallDetachPulse;
        private int wallDetachDirection;
        private bool cancelHorizontalForEdgeStick;
        private Vector3 baseVisualScale;
        private Vector3 baseVisualLocalPosition;
        private bool visualRootIsSelf;
        private bool touchingWallThisStep;
        private int touchingWallDirection;
        private bool touchingCeilingThisStep;
        private float ceilingCompression01;
        private Vector2 currentGroundNormal = Vector2.up;
        private OneDropWaterResource2D waterResource;
        private WaterBlobCharacter2D blobCharacter;
        private WaterBlobInput2D blobInput;
        private WaterBlobMeshRenderer2D blobRenderer;
        private Vector2 delayedVelocity;
        private Vector2 delayedVelocityVelocity;
        private Vector2 visualScaleRatio = Vector2.one;
        private Vector2 visualScaleVelocity;
        private Vector2 visualOffset;
        private Vector2 visualOffsetVelocity;
        private float wobbleState;
        private float wobbleVelocity;
        private bool jumpHeldLastFrame;
        private bool blobJumpHeldLastFrame;
        private bool vfxGroundStateInitialized;
        private bool moveVfxActive;
        private float moveVfxStopTimer;
        private float movementSfxCurrentVolume;
        private int movementSfxDirection;
        private float movementSfxMuteTimer;
        private readonly ContactPoint2D[] groundContactBuffer = new ContactPoint2D[24];

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            coreCircle = GetComponent<CircleCollider2D>();
            waterResource = GetComponent<OneDropWaterResource2D>();
            blobInput = GetComponent<WaterBlobInput2D>();
            blobRenderer = GetComponent<WaterBlobMeshRenderer2D>();
            if (vfxController == null)
            {
                vfxController = GetComponent<CharacterVFXController>();
            }
            InitializeAudioSources();
            if (waterResource == null)
            {
                waterResource = gameObject.AddComponent<OneDropWaterResource2D>();
            }
            blobCharacter = GetComponent<WaterBlobCharacter2D>();
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            facingSign = transform.localScale.x >= 0f ? 1f : -1f;
            baseVisualScale = visualRoot.localScale;
            baseVisualLocalPosition = visualRoot.localPosition;
            visualRootIsSelf = visualRoot == transform;
            visualScaleRatio = Vector2.one;
            visualOffset = Vector2.zero;
            delayedVelocity = rb.linearVelocity;
        }

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
            delayedVelocity = rb.linearVelocity;
        }

        private void OnDisable()
        {
            ResetDeformationVisual();
            StopMovementSfxImmediate();
        }

        private void Update()
        {
            if (OneDropGameManager2D.IsGameplayInputBlocked)
            {
                inputX = 0f;
                inputY = 0f;
                jumpPressed = false;
                jumpHeldLastFrame = false;
                blobJumpHeldLastFrame = false;
                return;
            }

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
            float minGroundNormalY = Mathf.Cos(Mathf.Clamp(jumpMaxGroundAngle, 0f, 90f) * Mathf.Deg2Rad);
            bool castGrounded = hasGroundHit && groundHit.collider != null && Mathf.Abs(groundHit.normal.y) >= minGroundNormalY;
            bool contactGrounded = HasGroundContact(box, groundMask, minGroundNormalY) || HasGroundContact(coreCircle, groundMask, minGroundNormalY);
            bool contactGroundedAnyLayer = HasGroundContactAny(box, minGroundNormalY) || HasGroundContactAny(coreCircle, minGroundNormalY);
            bool blobPointGrounded = IsAnyBlobPointTouching(groundMask);
            grounded = postJumpGroundIgnoreTimer <= 0f && (castGrounded || contactGrounded || blobPointGrounded || contactGroundedAnyLayer);
            bool touchingWall = CheckWall(out RaycastHit2D wallHit, out int wallDirection);
            bool touchingCeiling = CheckCeiling(out _, out float ceilingCompress);
            touchingWallThisStep = touchingWall;
            touchingWallDirection = wallDirection;
            touchingCeilingThisStep = touchingCeiling;
            ceilingCompression01 = ceilingCompress;
            currentGroundNormal = castGrounded ? groundHit.normal : Vector2.up;

            if (isSliding && slideTimer <= 0f)
            {
                SetSliding(false);
            }

            bool touchingClimbWall = touchingWall && IsWallClimbable(wallHit);
            UpdateClimbState(touchingClimbWall, wallDirection);

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
                waterResource.ConsumeMove(Time.fixedDeltaTime, isMoving);
            }

            ApplyRotation(groundHit);
            ApplyVisualFlip();
            ApplySoftBodyDeformation();
            UpdateVfxState();
            UpdateMovementSfx(Time.fixedDeltaTime);

            jumpPressed = false;
            groundedLastStep = grounded;
            previousVelY = rb.linearVelocity.y;
            previousVelocity = delayedVelocity;
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
            float directX = 0f;
            float y = 0f;
            bool jumpPressedThisFrame = false;
            bool jumpHeldNow = false;
            bool leftTapDown = false;
            bool rightTapDown = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                bool leftPressed = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                bool rightPressed = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
                if (leftPressed) directX -= 1f;
                if (rightPressed) directX += 1f;
                if (keyboard.wKey.isPressed || keyboard.zKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                jumpPressedThisFrame |= keyboard.spaceKey.wasPressedThisFrame;
                jumpHeldNow |= keyboard.spaceKey.isPressed;
                leftTapDown |= keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                rightTapDown |= keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > Mathf.Abs(directX)) directX = stick.x;
                if (Mathf.Abs(stick.y) > Mathf.Abs(y)) y = stick.y;
                jumpPressedThisFrame |= gamepad.buttonSouth.wasPressedThisFrame;
                jumpHeldNow |= gamepad.buttonSouth.isPressed;
            }
#else
            directX = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
            jumpPressedThisFrame = Input.GetButtonDown("Jump");
            jumpHeldNow = Input.GetButton("Jump");
            leftTapDown = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            rightTapDown = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
#endif
            bool directJumpDown = jumpPressedThisFrame || (jumpHeldNow && !jumpHeldLastFrame);
            jumpHeldLastFrame = jumpHeldNow;

            float resolvedX = directX;
            bool resolvedJumpDown = directJumpDown;
            if (blobInput != null)
            {
                resolvedX = blobInput.Move.x;
                bool blobQueued = blobInput.ConsumeJumpPressed();
                bool blobHeld = blobInput.JumpHeld;
                bool blobHeldEdge = blobHeld && !blobJumpHeldLastFrame;
                blobJumpHeldLastFrame = blobHeld;
                resolvedJumpDown = blobQueued || blobHeldEdge || directJumpDown;
            }

            inputX = Mathf.Clamp(resolvedX, -1f, 1f);
            inputY = Mathf.Clamp(y, -1f, 1f);
            if (resolvedJumpDown)
            {
                jumpPressed = true;
            }

            ProcessDoubleTapSlide(leftTapDown, rightTapDown);
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
                    SetSliding(false);
                    climbWallDirection = wallDirection == 0 ? (int)Mathf.Sign(facingSign) : wallDirection;
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                    SetBlobCharacterEnabled(false);
                }

                return;
            }

            // Detach if player presses direction away from wall
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

        private void ProcessDoubleTapSlide(bool leftTapDown, bool rightTapDown)
        {
            if (isClimbing || isSliding || slideCooldownTimer > 0f)
            {
                if (leftTapDown) lastLeftTapTime = Time.time;
                if (rightTapDown) lastRightTapTime = Time.time;
                return;
            }

            if (leftTapDown)
            {
                if (Time.time - lastLeftTapTime <= doubleTapWindow)
                {
                    StartSlide(-1);
                }

                lastLeftTapTime = Time.time;
            }

            if (rightTapDown)
            {
                if (Time.time - lastRightTapTime <= doubleTapWindow)
                {
                    StartSlide(1);
                }

                lastRightTapTime = Time.time;
            }
        }

        private void StartSlide(int direction)
        {
            if (direction == 0)
            {
                return;
            }

            SetSliding(true);
            slideDirection = direction;
            slideTimer = slideDuration;
            slideCooldownTimer = slideCooldown;
            facingSign = direction;
            float slideMuteDuration = Mathf.Max(slideDuration, slideSfxClip != null ? slideSfxClip.length : 0f);
            PlayActionSfx(slideSfxClip, slideSfxVolume, slideMuteDuration);
            if (waterResource != null)
            {
                waterResource.ConsumeSlide();
            }
        }

        private void ApplyHorizontalMovement()
        {
            rb.gravityScale = gravityWhenNotClimbing;
            float targetX = inputX * moveSpeed;
            float effectiveAcceleration = Mathf.Max(acceleration, moveSpeed * minAccelerationFromSpeed);
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
            v.x = slideDirection * slideSpeed;
            rb.linearVelocity = v;
        }

        private void ApplyClimbMovement()
        {
            rb.gravityScale = 0f;

            // Acceleration-based vertical movement, mirroring ground horizontal movement
            float targetY = inputY * climbSpeed;
            float effAccel = Mathf.Max(climbAcceleration, climbSpeed * climbMinAccelFromSpeed);
            float nextY = Mathf.MoveTowards(rb.linearVelocity.y, targetY, effAccel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(0f, nextY);

            if (blobCharacter != null && blobCharacter.PointBodies != null)
            {
                for (int i = 0; i < blobCharacter.PointBodies.Count; i++)
                {
                    Rigidbody2D pointBody = blobCharacter.PointBodies[i];
                    if (pointBody == null)
                    {
                        continue;
                    }

                    Vector2 pv = pointBody.linearVelocity;
                    pv.x = 0f;
                    pv.y = nextY;
                    pointBody.linearVelocity = pv;
                }
            }

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
            jumpPulse = 1f;
            jumpIntervalTimer = minJumpInterval;
            postJumpGroundIgnoreTimer = postJumpGroundIgnoreTime;
            PlayActionSfx(jumpSfxClip, jumpSfxVolume);
            if (vfxController != null)
            {
                vfxController.OnJump();
            }

            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            Vector2 jumpImpulse = Vector2.up * jumpForce;
            rb.AddForce(jumpImpulse, ForceMode2D.Impulse);
            PropagateJumpImpulseToPoints(jumpImpulse, jumpPointImpulseScale);
            if (waterResource != null)
            {
                waterResource.ConsumeJump();
            }
        }

        private void DoWallJump()
        {
            int away = -climbWallDirection;
            StopClimbing();
            wallDetachPulse = 1f;
            wallDetachDirection = away;
            jumpIntervalTimer = minJumpInterval;
            postJumpGroundIgnoreTimer = postJumpGroundIgnoreTime;
            PlayActionSfx(jumpSfxClip, jumpSfxVolume);
            if (vfxController != null)
            {
                vfxController.OnJump();
            }

            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            Vector2 wallImpulse = new Vector2(away * wallJumpHorizontalForce, wallJumpVerticalForce);
            rb.AddForce(wallImpulse, ForceMode2D.Impulse);
            PropagateJumpImpulseToPoints(wallImpulse, wallJumpPointImpulseScale);
            facingSign = away;
            if (waterResource != null)
            {
                waterResource.ConsumeJump();
            }
        }

        private void PropagateJumpImpulseToPoints(Vector2 impulse, float scale)
        {
            if (blobCharacter == null || blobCharacter.PointBodies == null || scale <= 0f)
            {
                return;
            }

            Vector2 pointImpulse = impulse * Mathf.Clamp01(scale);
            for (int i = 0; i < blobCharacter.PointBodies.Count; i++)
            {
                Rigidbody2D pointBody = blobCharacter.PointBodies[i];
                if (pointBody == null)
                {
                    continue;
                }

                Vector2 pv = pointBody.linearVelocity;
                if (pv.y < 0f)
                {
                    pv.y = 0f;
                    pointBody.linearVelocity = pv;
                }

                pointBody.AddForce(pointImpulse, ForceMode2D.Impulse);
            }
        }

        private void StopClimbing()
        {
            isClimbing = false;
            climbWallDirection = 0;
            // Restore gravity: use blob's value if present, otherwise our own.
            rb.gravityScale = (blobCharacter != null) ? blobCharacter.gravityScale : gravityWhenNotClimbing;
            SetBlobCharacterEnabled(true);
        }

        private void SetBlobCharacterEnabled(bool enabled)
        {
            if (blobCharacter == null) return;

            blobCharacter.enabled = enabled;

            // Also manage point body gravity so the blob shape follows the core during climbing
            if (blobCharacter.PointBodies != null)
            {
                float g = enabled ? blobCharacter.gravityScale : 0f;
                for (int i = 0; i < blobCharacter.PointBodies.Count; i++)
                {
                    if (blobCharacter.PointBodies[i] != null)
                    {
                        blobCharacter.PointBodies[i].gravityScale = g;
                    }
                }
            }
        }

        private void SetSliding(bool active)
        {
            if (isSliding == active)
            {
                return;
            }

            isSliding = active;
            if (vfxController == null)
            {
                return;
            }

            if (active)
            {
                vfxController.OnSlideStart();
            }
            else
            {
                vfxController.OnSlideStop();
            }
        }

        private void UpdateVfxState()
        {
            bool hasMoveIntent = Mathf.Abs(inputX) > 0.05f;
            bool moving = !isSliding && (Mathf.Abs(rb.linearVelocity.x) > moveVfxSpeedThreshold || hasMoveIntent);
            bool moveStarted = false;
            bool moveStopped = false;
            if (moving)
            {
                moveVfxStopTimer = moveVfxStopDelay;
                if (!moveVfxActive)
                {
                    moveVfxActive = true;
                    moveStarted = true;
                }
            }
            else if (moveVfxActive)
            {
                moveVfxStopTimer = Mathf.Max(0f, moveVfxStopTimer - Time.fixedDeltaTime);
                if (moveVfxStopTimer <= 0f)
                {
                    moveVfxActive = false;
                    moveStopped = true;
                }
            }

            if (vfxController != null)
            {
                if (vfxGroundStateInitialized && !groundedLastStep && grounded)
                {
                    vfxController.OnLand();
                }

                if (moveStarted)
                {
                    vfxController.OnMoveStart();
                }
                else if (moveStopped)
                {
                    vfxController.OnMoveStop();
                }
            }

            vfxGroundStateInitialized = true;
        }

        private void InitializeAudioSources()
        {
            if (movementSfxSource == null)
            {
                movementSfxSource = GetComponent<AudioSource>();
            }

            if (movementSfxSource == null)
            {
                movementSfxSource = gameObject.AddComponent<AudioSource>();
            }

            movementSfxSource.playOnAwake = false;
            movementSfxSource.loop = false;

            if (actionSfxSource == null || actionSfxSource == movementSfxSource)
            {
                actionSfxSource = CreateActionSfxSource();
            }

            if (actionSfxSource != null)
            {
                actionSfxSource.playOnAwake = false;
                actionSfxSource.loop = false;
                if (actionSfxSource.volume <= 0f)
                {
                    actionSfxSource.volume = 1f;
                }
            }
        }

        private AudioSource CreateActionSfxSource()
        {
            const string actionSourceName = "WaterBlob_ActionSFX";
            Transform child = transform.Find(actionSourceName);
            if (child == null)
            {
                GameObject childObject = new(actionSourceName);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            AudioSource source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            source.volume = Mathf.Max(0.01f, source.volume);
            return source;
        }

        private void UpdateMovementSfx(float dt)
        {
            if (movementSfxSource == null)
            {
                return;
            }

            int desiredDirection = ResolveMovementSfxDirection();
            AudioClip desiredClip = ResolveMovementLoopClip(desiredDirection);
            movementSfxMuteTimer = Mathf.Max(0f, movementSfxMuteTimer - dt);
            bool actionMuteActive = movementSfxMuteTimer > 0f;
            bool groundedGate = !movementSfxRequireGrounded || grounded;
            bool slideGate = !isSliding || movementSfxPlayWhileSliding;
            bool shouldPlay = desiredClip != null && !actionMuteActive && groundedGate && slideGate && moveVfxActive;
            float scaledMovementMaxVolume = OneDropAudioSettings2D.ApplySfx(movementSfxVolume);
            float targetVolume = shouldPlay ? scaledMovementMaxVolume : 0f;
            float fadeDuration = targetVolume > movementSfxCurrentVolume ? movementSfxFadeIn : movementSfxFadeOut;
            float step = fadeDuration > 0f
                ? Mathf.Max(0.0001f, scaledMovementMaxVolume) * (dt / fadeDuration)
                : Mathf.Max(0.0001f, scaledMovementMaxVolume);

            if (shouldPlay && !movementSfxSource.isPlaying)
            {
                movementSfxSource.clip = desiredClip;
                movementSfxSource.loop = true;
                movementSfxSource.playOnAwake = false;
                movementSfxSource.volume = 0f;
                movementSfxCurrentVolume = 0f;
                movementSfxSource.Play();
            }
            else if (shouldPlay && movementSfxSource.clip != desiredClip)
            {
                float keepVolume = movementSfxCurrentVolume;
                movementSfxSource.clip = desiredClip;
                movementSfxSource.Play();
                movementSfxCurrentVolume = keepVolume;
                movementSfxSource.volume = keepVolume;
            }

            movementSfxCurrentVolume = Mathf.MoveTowards(movementSfxCurrentVolume, targetVolume, step);
            movementSfxSource.volume = movementSfxCurrentVolume;
            movementSfxDirection = desiredDirection != 0 ? desiredDirection : movementSfxDirection;

            if (!shouldPlay && movementSfxSource.isPlaying && movementSfxCurrentVolume <= 0.0005f)
            {
                movementSfxSource.Stop();
            }
        }

        private int ResolveMovementSfxDirection()
        {
            if (Mathf.Abs(inputX) > 0.1f)
            {
                return inputX > 0f ? 1 : -1;
            }

            if (Mathf.Abs(rb.linearVelocity.x) > moveVfxSpeedThreshold)
            {
                return rb.linearVelocity.x > 0f ? 1 : -1;
            }

            if (movementSfxDirection != 0)
            {
                return movementSfxDirection;
            }

            return facingSign >= 0f ? 1 : -1;
        }

        private AudioClip ResolveMovementLoopClip(int direction)
        {
            if (direction < 0 && movementSfxLeftClip != null)
            {
                return movementSfxLeftClip;
            }

            if (direction > 0 && movementSfxRightClip != null)
            {
                return movementSfxRightClip;
            }

            return movementSfxClip;
        }

        private void PlayActionSfx(AudioClip clip, float volume, float movementMuteDurationOverride = -1f)
        {
            if (clip == null)
            {
                return;
            }

            if (muteMovementLoopDuringActionSfx)
            {
                float muteDuration = movementMuteDurationOverride >= 0f ? movementMuteDurationOverride : clip.length;
                movementSfxMuteTimer = Mathf.Max(movementSfxMuteTimer, muteDuration + actionSfxMoveLoopMuteExtraTime);
                StopMovementSfxImmediate();
            }

            AudioSource source = actionSfxSource != null ? actionSfxSource : movementSfxSource;
            if (source == null)
            {
                return;
            }

            source.PlayOneShot(clip, OneDropAudioSettings2D.ApplySfx(volume));
        }

        private void StopMovementSfxImmediate()
        {
            movementSfxCurrentVolume = 0f;
            if (movementSfxSource == null)
            {
                return;
            }

            movementSfxSource.volume = 0f;
            if (movementSfxSource.isPlaying)
            {
                movementSfxSource.Stop();
            }
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

        private void ApplyVisualFlip()
        {
            Vector3 scale = transform.localScale;
            float absX = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
            scale.x = absX * (facingSign >= 0f ? 1f : -1f);
            transform.localScale = scale;
        }

        private void ApplySoftBodyDeformation()
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            if (!groundedLastStep && grounded && -previousVelY >= landingMinImpactSpeed)
            {
                float impact = Mathf.Clamp01((-previousVelY - landingMinImpactSpeed) / Mathf.Max(0.01f, landingMinImpactSpeed));
                landingPulse = Mathf.Clamp01(0.55f + impact);
            }

            landingPulse = Mathf.MoveTowards(landingPulse, 0f, 3.2f * dt);
            jumpPulse = Mathf.MoveTowards(jumpPulse, 0f, 7f * dt);
            wallDetachPulse = Mathf.MoveTowards(wallDetachPulse, 0f, 6f * dt);

            delayedVelocity = Vector2.SmoothDamp(
                delayedVelocity,
                rb.linearVelocity,
                ref delayedVelocityVelocity,
                Mathf.Max(0.0001f, movementResponseDelay),
                Mathf.Infinity,
                dt
            );

            Vector2 velocity = delayedVelocity;
            accelerationSignal = Mathf.MoveTowards(accelerationSignal, (velocity.x - previousVelocity.x) / dt, acceleration * 2.8f * dt);

            float speed01 = Mathf.Clamp01(Mathf.Abs(velocity.x) / Mathf.Max(0.01f, moveSpeed));
            float rise01 = Mathf.Clamp01(velocity.y / Mathf.Max(0.01f, jumpForce));
            float fall01 = Mathf.Clamp01(-velocity.y / Mathf.Max(0.01f, jumpForce));
            float accel01 = Mathf.Clamp01(Mathf.Abs(accelerationSignal) / Mathf.Max(0.01f, acceleration * 2f));
            bool accelerating = Mathf.Abs(inputX) > 0.05f && Mathf.Sign(inputX) == Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.08f;
            bool decelerating = Mathf.Abs(velocity.x) > 0.08f && (Mathf.Abs(inputX) < 0.05f || Mathf.Sign(inputX) != Mathf.Sign(velocity.x));
            float peak01 = (!grounded && !isClimbing) ? 1f - Mathf.Clamp01(Mathf.Abs(velocity.y) / 1.4f) : 0f;
            float slope01 = grounded && !isClimbing ? Mathf.Clamp01(Mathf.Abs(Vector2.SignedAngle(Vector2.up, currentGroundNormal)) / Mathf.Max(1f, maxGroundAngle)) : 0f;
            float wall01 = (isClimbing || touchingWallThisStep) ? 1f : 0f;
            float idle01 = grounded && Mathf.Abs(velocity.x) < 0.08f && !isClimbing ? 1f : 0f;

            float horizontalSpread = 0f;
            float verticalCompress = 0f;
            float verticalStretch = 0f;

            horizontalSpread += idleGroundSpread * idle01;
            verticalCompress += idleGroundSquash * idle01;

            horizontalSpread += runSquash * speed01;
            verticalCompress += runSquash * speed01 * 0.58f;

            if (accelerating)
            {
                horizontalSpread += accelerationStretch * accel01;
            }

            if (decelerating)
            {
                verticalCompress += decelerationSquash * accel01;
            }

            verticalStretch += jumpStretch * rise01;
            verticalStretch += fallStretch * fall01;
            verticalStretch += peakStretch * peak01;

            horizontalSpread += jumpPreCompress * jumpPulse;
            verticalCompress += jumpPreCompress * jumpPulse;

            horizontalSpread += landingSquash * landingPulse;
            verticalCompress += landingSquash * landingPulse * 0.95f;

            horizontalSpread += slopeAdaptiveSquash * slope01;
            verticalCompress += slopeAdaptiveSquash * slope01 * 0.6f;

            horizontalSpread += wallAdhesionFlatten * wall01;
            verticalStretch += wallAdhesionStretch * wall01;

            horizontalSpread += wallDetachStretch * wallDetachPulse;
            verticalStretch += wallDetachStretch * wallDetachPulse * 0.32f;

            float wobbleTarget = Mathf.Clamp(
                Mathf.Clamp(accelerationSignal / Mathf.Max(0.01f, acceleration * 2f), -1f, 1f) * accelerationWobble
                + landingPulse * impactWobble
                + wallDetachPulse * (impactWobble * 0.4f),
                -maxWobble,
                maxWobble
            );
            SpringStep(ref wobbleState, ref wobbleVelocity, wobbleTarget, wobbleSpring, wobbleDamping, dt);
            wobbleState = Mathf.Clamp(wobbleState, -maxWobble, maxWobble);

            horizontalSpread += Mathf.Abs(wobbleState) * 0.75f;
            verticalStretch += wobbleState;
            verticalCompress += Mathf.Max(0f, -wobbleState) * 0.55f;

            if (touchingCeilingThisStep)
            {
                float damp = Mathf.Lerp(1f, ceilingCompressionDampen, ceilingCompression01);
                verticalCompress *= damp;
                horizontalSpread *= Mathf.Lerp(1f, ceilingSpreadDampen, ceilingCompression01);
            }

            float targetXAbs = baseVisualScale.x * (1f + horizontalSpread - verticalStretch * 0.35f);
            float targetY = baseVisualScale.y * (1f - verticalCompress + verticalStretch);
            targetY = Mathf.Max(baseVisualScale.y * minVisualHeightScale, targetY);
            float targetSign = visualRootIsSelf ? Mathf.Sign(facingSign) : Mathf.Sign(baseVisualScale.x);
            if (targetSign == 0f)
            {
                targetSign = 1f;
            }

            Vector2 targetScaleRatio = new(
                Mathf.Max(0.08f, targetXAbs) / Mathf.Max(0.0001f, Mathf.Abs(baseVisualScale.x)),
                Mathf.Max(0.08f, targetY) / Mathf.Max(0.0001f, baseVisualScale.y)
            );

            Vector3 targetPos = baseVisualLocalPosition;
            float momentumSign = Mathf.Abs(velocity.x) > 0.1f ? Mathf.Sign(velocity.x) : Mathf.Sign(facingSign);
            float momentumOffset = visualMomentumOffset * speed01;
            float accelOffset = visualMomentumOffset * 0.7f * Mathf.Clamp(accelerationSignal / Mathf.Max(0.01f, acceleration * 2f), -1f, 1f);
            float wallOffset = wall01 > 0f ? 0.04f * Mathf.Sign(touchingWallDirection == 0 ? climbWallDirection : touchingWallDirection) : 0f;
            float detachOffset = 0.06f * wallDetachPulse * wallDetachDirection;
            float groundSink = (idleGroundSquash * idle01 + landingSquash * landingPulse * 0.5f) * 0.2f;

            targetPos.x += momentumOffset * momentumSign + accelOffset + wallOffset - detachOffset;
            targetPos.y -= groundSink;

            Vector2 targetOffset = new(
                targetPos.x - baseVisualLocalPosition.x,
                targetPos.y - baseVisualLocalPosition.y
            );

            SpringStep(ref visualScaleRatio.x, ref visualScaleVelocity.x, targetScaleRatio.x, deformationSpring, deformationDamping, dt);
            SpringStep(ref visualScaleRatio.y, ref visualScaleVelocity.y, targetScaleRatio.y, deformationSpring, deformationDamping, dt);
            SpringStep(ref visualOffset.x, ref visualOffsetVelocity.x, targetOffset.x, deformationSpring * 0.75f, deformationDamping, dt);
            SpringStep(ref visualOffset.y, ref visualOffsetVelocity.y, targetOffset.y, deformationSpring * 0.75f, deformationDamping, dt);

            if (visualRootIsSelf && blobRenderer != null)
            {
                blobRenderer.SetMotionVisual(visualScaleRatio, visualOffset);
                return;
            }

            if (blobRenderer != null)
            {
                blobRenderer.ResetMotionVisual();
            }

            Vector3 targetScale = new(
                Mathf.Max(0.08f, Mathf.Abs(baseVisualScale.x) * visualScaleRatio.x) * targetSign,
                Mathf.Max(0.08f, baseVisualScale.y * visualScaleRatio.y),
                baseVisualScale.z
            );

            visualRoot.localScale = targetScale;
            if (!visualRootIsSelf)
            {
                visualRoot.localPosition = baseVisualLocalPosition + (Vector3)visualOffset;
            }
        }

        private void ResetDeformationVisual()
        {
            if (blobRenderer != null)
            {
                blobRenderer.ResetMotionVisual();
            }

            if (visualRoot == null)
            {
                return;
            }

            Vector3 resetScale = baseVisualScale;
            if (visualRootIsSelf)
            {
                float sign = facingSign >= 0f ? 1f : -1f;
                resetScale.x = Mathf.Abs(resetScale.x) * sign;
            }

            visualRoot.localScale = resetScale;
            if (!visualRootIsSelf)
            {
                visualRoot.localPosition = baseVisualLocalPosition;
            }

            visualScaleRatio = Vector2.one;
            visualScaleVelocity = Vector2.zero;
            visualOffset = Vector2.zero;
            visualOffsetVelocity = Vector2.zero;
            wobbleState = 0f;
            wobbleVelocity = 0f;
        }

        private static void SpringStep(ref float current, ref float velocity, float target, float spring, float damping, float dt)
        {
            float s = Mathf.Max(0f, spring);
            float d = Mathf.Max(0f, damping);
            float displacement = target - current;

            velocity += displacement * s * dt;
            velocity *= Mathf.Exp(-d * dt);
            current += velocity * dt;
        }

        private bool CheckGround(out RaycastHit2D hit)
        {
            Bounds b = box.bounds;
            float probeHeight = Mathf.Max(0.05f, b.size.y * groundProbeHeightFactor);
            Vector2 size = new Vector2(b.size.x * castWidthFactor, probeHeight);
            Vector2 origin = b.center;
            origin.y = b.min.y + probeHeight * 0.5f + 0.005f;
            hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundMask);
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

        private bool HasGroundContact(Collider2D sourceCollider, LayerMask mask, float minGroundNormalY)
        {
            if (sourceCollider == null)
            {
                return false;
            }

            int count = sourceCollider.GetContacts(groundContactBuffer);
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = groundContactBuffer[i];
                Collider2D other = contact.collider;
                if (other == null || !IsLayerInMask(other.gameObject.layer, mask))
                {
                    continue;
                }

                if (other.attachedRigidbody == rb)
                {
                    continue;
                }

                if (Mathf.Abs(contact.normal.y) >= minGroundNormalY)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasGroundContactAny(Collider2D sourceCollider, float minGroundNormalY)
        {
            if (sourceCollider == null)
            {
                return false;
            }

            int count = sourceCollider.GetContacts(groundContactBuffer);
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = groundContactBuffer[i];
                Collider2D other = contact.collider;
                if (other == null || other.isTrigger || other.attachedRigidbody == rb)
                {
                    continue;
                }

                if (Mathf.Abs(contact.normal.y) >= minGroundNormalY)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAnyBlobPointTouching(LayerMask mask)
        {
            if (blobCharacter == null || blobCharacter.PointBodies == null)
            {
                return false;
            }

            for (int i = 0; i < blobCharacter.PointBodies.Count; i++)
            {
                Rigidbody2D pointBody = blobCharacter.PointBodies[i];
                if (pointBody == null)
                {
                    continue;
                }

                Collider2D pointCollider = pointBody.GetComponent<Collider2D>();
                if (pointCollider != null && pointCollider.IsTouchingLayers(mask))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckWall(out RaycastHit2D hit, out int wallDirection)
        {
            Vector2 size = GetCastSize();
            int preferredDir = isClimbing ? climbWallDirection : (facingSign >= 0f ? 1 : -1);
            LayerMask detectionMask = GetWallDetectionMask();
            Vector2 castOrigin = box.bounds.center;

            Vector2 leftDir = Vector2.left;
            Vector2 rightDir = Vector2.right;

            bool TrySide(int dir, out RaycastHit2D sideHit)
            {
                Vector2 castDir = dir > 0 ? rightDir : leftDir;
                sideHit = Physics2D.BoxCast(castOrigin, size, 0f, castDir, wallCheckDistance, detectionMask);
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

        private bool CheckCeiling(out RaycastHit2D hit, out float compression01)
        {
            Bounds b = box.bounds;
            float probeHeight = Mathf.Max(0.05f, b.size.y * groundProbeHeightFactor);
            Vector2 size = new Vector2(b.size.x * castWidthFactor, probeHeight);
            Vector2 origin = b.center;
            origin.y = b.max.y - probeHeight * 0.5f - 0.005f;

            LayerMask mask = ceilingMask.value == 0 ? (groundMask | wallMask) : ceilingMask;
            hit = Physics2D.BoxCast(origin, size, 0f, Vector2.up, ceilingCheckDistance, mask);
            if (hit.collider == null)
            {
                compression01 = 0f;
                return false;
            }

            compression01 = 1f - Mathf.Clamp01(hit.distance / Mathf.Max(0.0001f, ceilingCheckDistance));
            return true;
        }

        private Vector2 GetCastSize()
        {
            Bounds b = box.bounds;
            return new Vector2(b.size.x * castWidthFactor, b.size.y * castHeightFactor);
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

    }
}
