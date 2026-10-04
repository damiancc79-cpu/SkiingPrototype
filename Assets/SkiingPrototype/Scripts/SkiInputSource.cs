using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SkiingPrototype
{
    public class SkiInputSource : MonoBehaviour
    {
        [Header("Keyboard")]
        [SerializeField] private Key carveLeftKey = Key.A;
        [SerializeField] private Key carveRightKey = Key.D;
        [SerializeField] private Key alternateCarveLeftKey = Key.LeftArrow;
        [SerializeField] private Key alternateCarveRightKey = Key.RightArrow;
        [SerializeField] private Key brakeKey = Key.Space;
        [SerializeField] private Key alternateBrakeKey = Key.S;
        [SerializeField] private Key resetKey = Key.R;
        [SerializeField] private Key spinLeftKey = Key.Q;
        [SerializeField] private Key spinRightKey = Key.E;
        [SerializeField] private Key grabKey = Key.W;
        [SerializeField] private Key focusKey = Key.LeftShift;
        [SerializeField] private Key weatherCycleKey = Key.F;

        [Header("Gamepad")]
        [SerializeField] private bool enableGamepad = true;
        [SerializeField] private float stickDeadzone = 0.18f;
        [SerializeField] private float triggerDeadzone = 0.08f;

        public float Carve { get; private set; }
        public float Brake { get; private set; }
        public bool ResetPressed { get; private set; }
        public bool SpinLeftPressed { get; private set; }
        public bool SpinRightPressed { get; private set; }
        public bool GrabHeld { get; private set; }
        public bool FocusHeld { get; private set; }
        public bool WeatherCyclePressed { get; private set; }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = enableGamepad ? Gamepad.current : null;

            float keyboardCarve = 0f;
            float keyboardBrake = 0f;
            ResetPressed = false;
            SpinLeftPressed = false;
            SpinRightPressed = false;
            GrabHeld = false;
            FocusHeld = false;
            WeatherCyclePressed = false;

            if (keyboard != null)
            {
                keyboardCarve += IsPressed(keyboard, carveRightKey) || IsPressed(keyboard, alternateCarveRightKey) ? 1f : 0f;
                keyboardCarve -= IsPressed(keyboard, carveLeftKey) || IsPressed(keyboard, alternateCarveLeftKey) ? 1f : 0f;
                keyboardBrake = IsPressed(keyboard, brakeKey) || IsPressed(keyboard, alternateBrakeKey) || keyboard.downArrowKey.isPressed ? 1f : 0f;
                ResetPressed = WasPressedThisFrame(keyboard, resetKey);
                SpinLeftPressed = WasPressedThisFrame(keyboard, spinLeftKey);
                SpinRightPressed = WasPressedThisFrame(keyboard, spinRightKey);
                GrabHeld = IsPressed(keyboard, grabKey);
                FocusHeld = IsPressed(keyboard, focusKey);
                WeatherCyclePressed = WasPressedThisFrame(keyboard, weatherCycleKey);
            }

            float gamepadCarve = 0f;
            float gamepadBrake = 0f;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                gamepadCarve = Mathf.Abs(stick.x) >= stickDeadzone ? stick.x : 0f;
                gamepadBrake = Mathf.Max(
                    stick.y <= -stickDeadzone ? -stick.y : 0f,
                    gamepad.leftTrigger.ReadValue() >= triggerDeadzone ? gamepad.leftTrigger.ReadValue() : 0f);

                ResetPressed |= gamepad.startButton.wasPressedThisFrame;
                SpinLeftPressed |= gamepad.leftShoulder.wasPressedThisFrame;
                SpinRightPressed |= gamepad.rightShoulder.wasPressedThisFrame;
                GrabHeld |= gamepad.buttonSouth.isPressed;
                FocusHeld |= gamepad.rightTrigger.ReadValue() >= triggerDeadzone;
            }

            Carve = Mathf.Clamp(Mathf.Abs(gamepadCarve) > Mathf.Abs(keyboardCarve) ? gamepadCarve : keyboardCarve, -1f, 1f);
            Brake = Mathf.Clamp01(Mathf.Max(keyboardBrake, gamepadBrake));
        }

        private static bool IsPressed(Keyboard keyboard, Key key)
        {
            KeyControl control = keyboard[key];
            return control != null && control.isPressed;
        }

        private static bool WasPressedThisFrame(Keyboard keyboard, Key key)
        {
            KeyControl control = keyboard[key];
            return control != null && control.wasPressedThisFrame;
        }
    }
}
