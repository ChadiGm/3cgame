using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WaterBlob
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class OneDropController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float acceleration = 55f;
        [SerializeField, Min(0f)] private float minAccelerationFromSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpForce = 12f;
        [SerializeField, Min(0f)] private float postJumpGroundIgnoreTime = 0.08f;
        [SerializeField, Min(0f)] private float minJumpInterval = 0.22f;

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
        [SerializeField, Min(0f)] private float wallJumpHorizontalForce = 9.5f;
        [SerializeField, Min(0f)] private float wallJumpVerticalForce = 11f;
        [SerializeField] private float gravityWhenNotClimbing = 3f;

        [Header("Detection")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask wallMask;
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.12f;
        [SerializeField, Min(0.01f)] private float wallCheckDistance = 0.12f;
        [SerializeField, Range(0.05f, 0.5f)] private float groundProbeHeightFactor = 0.22f;
        [SerializeField, Range(0.5f, 1f)] private float castWidthFactor = 0.95f;
        [SerializeField, Range(0.5f, 1f)] private float castHeightFactor = 0.95f;

        [Header("Rotation")]
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 12f;
        [SerializeField, Range(0f, 90f)] private float maxGroundAngle = 35f;

        [Header("Edge Stick Prevention")]
        [SerializeField, Range(0f, 1f)] private float sideNormalMinX = 0.72f;
        [SerializeField, Range(0f, 1f)] private float sideNormalMaxY = 0.45f;

        [Header("Soft Body Visual")]
        [SerializeField, Min(0f)] private float deformLerpSpeed = 14f;
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
        private Vector2 currentGroundNormal = Vector2.up;
        private bool wasPressingAwayFromWall;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            facingSign = transform.localScale.x >= 0f ? 1f : -1f;
            baseVisualScale = visualRoot.localScale;
            baseVisualLocalPosition = visualRoot.localPosition;
            visualRootIsSelf = visualRoot == transform;
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
            bool touchingWall = CheckWall(out _, out int wallDirection);
            touchingWallThisStep = touchingWall;
            touchingWallDirection = wallDirection;
            currentGroundNormal = groundHit.collider != null ? groundHit.normal : Vector2.up;

            if (isSliding && slideTimer <= 0f)
            {
                isSliding = false;
            }

            UpdateClimbState(touchingWall, wallDirection);

            if (isClimbing)
            {
                ApplyClimbMovement();
                bool pressingAwayFromWall = Mathf.Abs(inputX) > 0.1f && Mathf.Sign(inputX) == -climbWallDirection;
                bool oppositeInputJump = pressingAwayFromWall && !wasPressingAwayFromWall;
                wasPressingAwayFromWall = pressingAwayFromWall;
                if ((jumpPressed || oppositeInputJump) && jumpIntervalTimer <= 0f)
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

            ApplyRotation(groundHit);
            ApplyVisualFlip();
            ApplySoftBodyDeformation();

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
            float x = 0f;
            float y = 0f;
            bool jumpDown = false;
            bool leftTapDown = false;
            bool rightTapDown = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                bool leftPressed = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
                bool rightPressed = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
                if (leftPressed) x -= 1f;
                if (rightPressed) x += 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                jumpDown |= keyboard.spaceKey.wasPressedThisFrame;
                leftTapDown |= keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                rightTapDown |= keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > Mathf.Abs(x)) x = stick.x;
                if (Mathf.Abs(stick.y) > Mathf.Abs(y)) y = stick.y;
                jumpDown |= gamepad.buttonSouth.wasPressedThisFrame;
            }
#else
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
            jumpDown = Input.GetButtonDown("Jump");
            leftTapDown = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            rightTapDown = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
#endif

            inputX = Mathf.Clamp(x, -1f, 1f);
            inputY = Mathf.Clamp(y, -1f, 1f);
            if (jumpDown)
            {
                jumpPressed = true;
            }

            ProcessDoubleTapSlide(leftTapDown, rightTapDown);
        }

        private void UpdateClimbState(bool touchingWall, int wallDirection)
        {
            bool movingTowardWall = touchingWall && Mathf.Abs(inputX) > 0.1f && Mathf.Sign(inputX) == wallDirection;
            bool pressingUp = inputY > 0.1f;

            if (!isClimbing)
            {
                bool pressingDown = inputY < -0.1f;
                if (!pressingDown && touchingWall && (pressingUp || movingTowardWall))
                {
                    isClimbing = true;
                    isSliding = false;
                    climbWallDirection = wallDirection == 0 ? (int)Mathf.Sign(facingSign) : wallDirection;
                    wasPressingAwayFromWall = false;
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                }

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

            isSliding = true;
            slideDirection = direction;
            slideTimer = slideDuration;
            slideCooldownTimer = slideCooldown;
            facingSign = direction;
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
            float climbVelY = Mathf.Abs(inputY) > 0.05f ? inputY * climbSpeed : 0f;
            rb.linearVelocity = new Vector2(0f, climbVelY);

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

            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        private void DoWallJump()
        {
            int away = -climbWallDirection;
            StopClimbing();
            wallDetachPulse = 1f;
            wallDetachDirection = away;
            jumpIntervalTimer = minJumpInterval;
            postJumpGroundIgnoreTimer = postJumpGroundIgnoreTime;

            Vector2 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(new Vector2(away * wallJumpHorizontalForce, wallJumpVerticalForce), ForceMode2D.Impulse);
            facingSign = away;
        }

        private void StopClimbing()
        {
            isClimbing = false;
            climbWallDirection = 0;
            wasPressingAwayFromWall = false;
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

        private void ApplyVisualFlip()
        {
            Vector3 scale = transform.localScale;
            float absX = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
            scale.x = absX * (facingSign >= 0f ? 1f : -1f);
            transform.localScale = scale;
        }

        private void ApplySoftBodyDeformation()
        {
            if (!groundedLastStep && grounded && -previousVelY >= landingMinImpactSpeed)
            {
                float impact = Mathf.Clamp01((-previousVelY - landingMinImpactSpeed) / Mathf.Max(0.01f, landingMinImpactSpeed));
                landingPulse = Mathf.Clamp01(0.55f + impact);
            }

            landingPulse = Mathf.MoveTowards(landingPulse, 0f, 3.2f * Time.fixedDeltaTime);
            jumpPulse = Mathf.MoveTowards(jumpPulse, 0f, 7f * Time.fixedDeltaTime);
            wallDetachPulse = Mathf.MoveTowards(wallDetachPulse, 0f, 6f * Time.fixedDeltaTime);

            Vector2 velocity = rb.linearVelocity;
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
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

            float targetXAbs = baseVisualScale.x * (1f + horizontalSpread - verticalStretch * 0.35f);
            float targetY = baseVisualScale.y * (1f - verticalCompress + verticalStretch);
            float targetSign = visualRootIsSelf ? Mathf.Sign(facingSign) : Mathf.Sign(baseVisualScale.x);
            if (targetSign == 0f)
            {
                targetSign = 1f;
            }

            Vector3 targetScale = new(Mathf.Max(0.08f, targetXAbs) * targetSign, Mathf.Max(0.08f, targetY), baseVisualScale.z);

            Vector3 targetPos = baseVisualLocalPosition;
            if (!visualRootIsSelf)
            {
                float momentumSign = Mathf.Abs(velocity.x) > 0.1f ? Mathf.Sign(velocity.x) : Mathf.Sign(facingSign);
                float momentumOffset = visualMomentumOffset * speed01;
                float accelOffset = visualMomentumOffset * 0.7f * Mathf.Clamp(accelerationSignal / Mathf.Max(0.01f, acceleration * 2f), -1f, 1f);
                float wallOffset = wall01 > 0f ? 0.04f * Mathf.Sign(touchingWallDirection == 0 ? climbWallDirection : touchingWallDirection) : 0f;
                float detachOffset = 0.06f * wallDetachPulse * wallDetachDirection;
                float groundSink = (idleGroundSquash * idle01 + landingSquash * landingPulse * 0.5f) * 0.2f;

                targetPos.x += momentumOffset * momentumSign + accelOffset + wallOffset - detachOffset;
                targetPos.y -= groundSink;
            }

            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, deformLerpSpeed * Time.fixedDeltaTime);
            if (!visualRootIsSelf)
            {
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPos, deformLerpSpeed * Time.fixedDeltaTime);
            }
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

        private bool CheckWall(out RaycastHit2D hit, out int wallDirection)
        {
            Vector2 size = GetCastSize();
            int preferredDir = isClimbing ? climbWallDirection : (facingSign >= 0f ? 1 : -1);

            Vector2 leftDir = Vector2.left;
            Vector2 rightDir = Vector2.right;

            bool TrySide(int dir, out RaycastHit2D sideHit)
            {
                Vector2 castDir = dir > 0 ? rightDir : leftDir;
                sideHit = Physics2D.BoxCast(transform.position, size, 0f, castDir, wallCheckDistance, wallMask);
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
