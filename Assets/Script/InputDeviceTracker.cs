using System;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Singleton that tracks which input device (gamepad or keyboard/mouse) was last used.
 * Fires OnDeviceChanged whenever the active scheme switches.
 * Auto-creates itself at startup — no need to place it in the scene.
 */
public class InputDeviceTracker : MonoBehaviour
{
    public static InputDeviceTracker Instance { get; private set; }

    public static event Action OnDeviceChanged;

    public static bool IsGamepadActive { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        var go = new GameObject("[InputDeviceTracker]");
        DontDestroyOnLoad(go);
        go.AddComponent<InputDeviceTracker>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsGamepadActive = Gamepad.current != null;
    }

    private void Update()
    {
        bool gamepadInput = HasGamepadInput();
        bool kbMouseInput = !gamepadInput && HasKeyboardMouseInput();

        bool newIsGamepad = IsGamepadActive;
        if (gamepadInput) newIsGamepad = true;
        else if (kbMouseInput) newIsGamepad = false;

        if (newIsGamepad == IsGamepadActive) return;

        IsGamepadActive = newIsGamepad;

        // Auto-hide cursor when switching to gamepad, show when switching to mouse
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.visible = !IsGamepadActive;

        OnDeviceChanged?.Invoke();
    }

    private static bool HasGamepadInput()
    {
        var gp = Gamepad.current;
        if (gp == null) return false;

        return gp.leftStick.ReadValue().sqrMagnitude > 0.01f
            || gp.rightStick.ReadValue().sqrMagnitude > 0.01f
            || gp.dpad.ReadValue().sqrMagnitude > 0.01f
            || gp.leftTrigger.ReadValue() > 0.1f
            || gp.rightTrigger.ReadValue() > 0.1f
            || gp.buttonSouth.isPressed
            || gp.buttonNorth.isPressed
            || gp.buttonEast.isPressed
            || gp.buttonWest.isPressed
            || gp.leftShoulder.isPressed
            || gp.rightShoulder.isPressed
            || gp.startButton.isPressed
            || gp.selectButton.isPressed
            || gp.leftStickButton.isPressed
            || gp.rightStickButton.isPressed;
    }

    private static bool HasKeyboardMouseInput()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.anyKey.isPressed) return true;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            return mouse.delta.ReadValue().sqrMagnitude > 0.25f
                || mouse.leftButton.isPressed
                || mouse.rightButton.isPressed
                || mouse.middleButton.isPressed;
        }

        return false;
    }
}
