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
        public bool SlideHeld { get; private set; }
        public bool DashHeld { get; private set; }
        public bool AttackHeld { get; private set; }

        private bool jumpQueued;
        private bool slideQueued;
        private bool dashQueued;
        private bool attackQueued;
        private float smoothedMoveX;

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

        public bool ConsumeDashPressed()
        {
            bool pressed = dashQueued;
            dashQueued = false;
            return pressed;
        }

        public bool ConsumeAttackPressed()
        {
            bool pressed = attackQueued;
            attackQueued = false;
            return pressed;
        }

        private void Update()
        {
            ReadInput();
        }

        private void ReadInput()
        {
            float rawMoveX = 0f;
            bool jumpDown = false;
            bool jumpHold = false;
            bool slideDown = false;
            bool slideHold = false;
            bool dashDown = false;
            bool dashHold = false;
            bool attackDown = false;
            bool attackHold = false;

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

                jumpDown |= keyboard.spaceKey.wasPressedThisFrame;
                jumpHold |= keyboard.spaceKey.isPressed;
                slideDown |= keyboard.leftCtrlKey.wasPressedThisFrame;
                slideHold |= keyboard.leftCtrlKey.isPressed;
                dashDown |= keyboard.leftShiftKey.wasPressedThisFrame;
                dashHold |= keyboard.leftShiftKey.isPressed;
                attackDown |= keyboard.eKey.wasPressedThisFrame;
                attackHold |= keyboard.eKey.isPressed;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                attackDown |= mouse.leftButton.wasPressedThisFrame;
                attackHold |= mouse.leftButton.isPressed;
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

                jumpDown |= gamepad.buttonSouth.wasPressedThisFrame;
                jumpHold |= gamepad.buttonSouth.isPressed;
                slideDown |= gamepad.leftShoulder.wasPressedThisFrame;
                slideHold |= gamepad.leftShoulder.isPressed;
                dashDown |= gamepad.rightShoulder.wasPressedThisFrame;
                dashHold |= gamepad.rightShoulder.isPressed;
                attackDown |= gamepad.buttonWest.wasPressedThisFrame;
                attackHold |= gamepad.buttonWest.isPressed;
            }
#else
            rawMoveX = Input.GetAxisRaw("Horizontal");
            jumpDown = Input.GetButtonDown("Jump");
            jumpHold = Input.GetButton("Jump");
            slideDown = Input.GetKeyDown(KeyCode.LeftControl);
            slideHold = Input.GetKey(KeyCode.LeftControl);
            dashDown = Input.GetKeyDown(KeyCode.LeftShift);
            dashHold = Input.GetKey(KeyCode.LeftShift);
            attackDown = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
            attackHold = Input.GetKey(KeyCode.E) || Input.GetMouseButton(0);
#endif

            rawMoveX = Mathf.Clamp(rawMoveX, -1f, 1f);
            float rate = Mathf.Abs(rawMoveX) > Mathf.Abs(smoothedMoveX) ? riseRate : fallRate;
            smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, rawMoveX, rate * Time.unscaledDeltaTime);

            Move = new Vector2(smoothedMoveX, 0f);
            JumpHeld = jumpHold;
            SlideHeld = slideHold;
            DashHeld = dashHold;
            AttackHeld = attackHold;

            if (jumpDown)
            {
                jumpQueued = true;
            }

            if (slideDown)
            {
                slideQueued = true;
            }

            if (dashDown)
            {
                dashQueued = true;
            }

            if (attackDown)
            {
                attackQueued = true;
            }
        }
    }
}

