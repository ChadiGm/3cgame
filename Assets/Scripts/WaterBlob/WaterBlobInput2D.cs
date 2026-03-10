using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class WaterBlobInput2D : MonoBehaviour
    {
        [Header("Input Smoothing")]
        [Min(0f)] public float riseRate = 12f;
        [Min(0f)] public float fallRate = 18f;
        [Range(0f, 1f)] public float stickDeadzone = 0.2f;

        public Vector2 Move { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool ShootHeld { get; private set; }

        private bool jumpQueued;
        private bool slideQueued;
        private float smoothedMoveX;
        private float upDoubleTapTimer = 0f;
        private const float DOUBLE_TAP_TIME = 0.25f;
        private bool upWasPressedLastFrame = false;

        public bool ConsumeJumpPressed()
        {
            bool pressed = jumpQueued;
            jumpQueued = false;
            return pressed;
        }

        public bool ConsumeSlidePressed()
        {
            bool pressed = slideQueued;
            slideQueued = false;
            return pressed;
        }

        private void Update()
        {
            ReadInput();
        }

        private void ReadInput()
        {
            float rawMoveX = 0f;
            float rawMoveY = 0f;
            bool jumpDown = false;
            bool jumpHold = false;
            bool slideDown = false;
            bool shootHold = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    rawMoveX -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    rawMoveX += 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.zKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    rawMoveY += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    rawMoveY -= 1f;
                }

                jumpDown |= keyboard.spaceKey.wasPressedThisFrame;
                jumpHold |= keyboard.spaceKey.isPressed;
                slideDown |= keyboard.shiftKey.wasPressedThisFrame;
                shootHold |= keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                shootHold |= mouse.leftButton.isPressed;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float stickX = gamepad.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) < stickDeadzone)
                {
                    stickX = 0f;
                }

                if (Mathf.Abs(stickX) > Mathf.Abs(rawMoveX))
                {
                    rawMoveX = stickX;
                }

                float stickY = gamepad.leftStick.y.ReadValue();
                if (Mathf.Abs(stickY) > Mathf.Abs(rawMoveY))
                {
                    rawMoveY = stickY;
                }

                jumpDown |= gamepad.buttonSouth.wasPressedThisFrame;
                jumpHold |= gamepad.buttonSouth.isPressed;
                slideDown |= gamepad.rightTrigger.wasPressedThisFrame || gamepad.leftTrigger.wasPressedThisFrame;
                shootHold |= gamepad.rightShoulder.isPressed || gamepad.buttonWest.isPressed;
            }
#else
            rawMoveX = Input.GetAxisRaw("Horizontal");
            rawMoveY = Input.GetAxisRaw("Vertical");
            jumpDown = Input.GetKeyDown(KeyCode.Space);
            jumpHold = Input.GetKey(KeyCode.Space);
            slideDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            shootHold = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetMouseButton(0);
#endif

            rawMoveX = Mathf.Clamp(rawMoveX, -1f, 1f);
            rawMoveY = Mathf.Clamp(rawMoveY, -1f, 1f);
            float rate = Mathf.Abs(rawMoveX) > Mathf.Abs(smoothedMoveX) ? riseRate : fallRate;
            smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, rawMoveX, rate * Time.unscaledDeltaTime);

            Move = new Vector2(smoothedMoveX, rawMoveY);

            bool upPressedThisFrame = rawMoveY > 0.5f;
            bool upNewlyPressed = upPressedThisFrame && !upWasPressedLastFrame;
            upWasPressedLastFrame = upPressedThisFrame;

            upDoubleTapTimer = Mathf.Max(0f, upDoubleTapTimer - Time.unscaledDeltaTime);
            if (upNewlyPressed)
            {
                if (upDoubleTapTimer > 0f)
                {
                    jumpDown = true;
                    upDoubleTapTimer = 0f;
                }
                else
                {
                    upDoubleTapTimer = DOUBLE_TAP_TIME;
                }
            }

            JumpHeld = jumpHold || upPressedThisFrame;
            ShootHeld = shootHold;

            if (jumpDown)
            {
                jumpQueued = true;
            }

            if (slideDown)
            {
                slideQueued = true;
            }
        }
    }
}
