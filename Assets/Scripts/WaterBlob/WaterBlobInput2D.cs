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

        private bool jumpQueued;
        private float smoothedMoveX;

        public bool ConsumeJumpPressed()
        {
            bool pressed = jumpQueued;
            jumpQueued = false;
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
            }
#else
            rawMoveX = Input.GetAxisRaw("Horizontal");
            jumpDown = Input.GetButtonDown("Jump");
            jumpHold = Input.GetButton("Jump");
#endif

            rawMoveX = Mathf.Clamp(rawMoveX, -1f, 1f);
            float rate = Mathf.Abs(rawMoveX) > Mathf.Abs(smoothedMoveX) ? riseRate : fallRate;
            smoothedMoveX = Mathf.MoveTowards(smoothedMoveX, rawMoveX, rate * Time.unscaledDeltaTime);

            Move = new Vector2(smoothedMoveX, 0f);
            JumpHeld = jumpHold;

            if (jumpDown)
            {
                jumpQueued = true;
            }
        }
    }
}
