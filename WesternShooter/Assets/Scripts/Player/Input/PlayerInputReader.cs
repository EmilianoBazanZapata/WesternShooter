using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsRunPressed { get; private set; }
    public bool IsAimPressed { get; private set; }
    public bool IsShootPressed { get; private set; }
    public bool IsShootPressedThisFrame { get; private set; }

    private bool wasShootPressedLastFrame;
    private bool wasGamepadShoulderSwapPressedLastFrame;
    private bool isGamepadShoulderSwapPressedThisFrame;
    private int lastPolledFrame = int.MinValue;

    public bool GetShoulderSwapPressedThisFrame(Key shoulderSwapKey)
    {
        EnsurePolledThisFrame();

        bool keyboardPressed = Keyboard.current != null
            && Keyboard.current[shoulderSwapKey].wasPressedThisFrame;

        return keyboardPressed || isGamepadShoulderSwapPressedThisFrame;
    }

    public void EnsurePolledThisFrame()
    {
        if (lastPolledFrame == Time.frameCount)
        {
            return;
        }

        lastPolledFrame = Time.frameCount;

        Vector2 keyboardInput = Vector2.zero;
        Vector2 gamepadInput = Vector2.zero;
        Vector2 lookInput = Vector2.zero;
        bool mouseAimPressed = false;
        bool mouseShootPressed = false;
        bool gamepadAimPressed = false;
        bool gamepadShootPressed = false;
        bool gamepadShoulderSwapPressed = false;

        if (Keyboard.current != null)
        {
            float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float y = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            keyboardInput = new Vector2(x, y);

            IsRunPressed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        }
        else
        {
            IsRunPressed = false;
        }

        if (Gamepad.current != null)
        {
            gamepadInput = Gamepad.current.leftStick.ReadValue();
            lookInput += Gamepad.current.rightStick.ReadValue() * 15f;
            IsRunPressed = IsRunPressed || Gamepad.current.leftStickButton.isPressed;
            gamepadAimPressed = Gamepad.current.leftTrigger.ReadValue() > 0.2f;
            gamepadShootPressed = Gamepad.current.rightTrigger.ReadValue() > 0.2f;
            gamepadShoulderSwapPressed = Gamepad.current.rightStickButton.isPressed;
        }

        if (Mouse.current != null)
        {
            lookInput += Mouse.current.delta.ReadValue();
            mouseAimPressed = Mouse.current.rightButton.isPressed;
            mouseShootPressed = Mouse.current.leftButton.isPressed;
        }

        isGamepadShoulderSwapPressedThisFrame = gamepadShoulderSwapPressed
            && !wasGamepadShoulderSwapPressedLastFrame;
        wasGamepadShoulderSwapPressedLastFrame = gamepadShoulderSwapPressed;

        LookInput = lookInput;

        IsAimPressed = mouseAimPressed || gamepadAimPressed;
        IsShootPressed = (mouseAimPressed && mouseShootPressed) || (gamepadAimPressed && gamepadShootPressed);
        IsShootPressedThisFrame = IsShootPressed && !wasShootPressedLastFrame;
        wasShootPressedLastFrame = IsShootPressed;

        if (IsAimPressed)
        {
            MoveInput = Vector2.zero;
            IsRunPressed = false;
            return;
        }

        MoveInput = Vector2.ClampMagnitude(keyboardInput + gamepadInput, 1f);
    }
}
