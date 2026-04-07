using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Static helper that returns the current input display string for a given action.
 * Shows gamepad button icons (TMP sprite tags) when a gamepad is active, keyboard/mouse text otherwise.
 * Always reads from the live InputActionAsset so it reflects any rebind the player has done.
 */
public static class InputBindingHelper
{
    private static InputActionAsset s_asset;

    // Maps <Gamepad>/path → TMP sprite name (= filename without extension in Assets/Resources/XboxIcons/).
    private static readonly Dictionary<string, string> s_gamepadSprites = new()
    {
        { "<Gamepad>/buttonSouth",    "xbox_button_color_a_outline" },
        { "<Gamepad>/buttonEast",     "xbox_button_color_b_outline" },
        { "<Gamepad>/buttonWest",     "xbox_button_color_x_outline" },
        { "<Gamepad>/buttonNorth",    "xbox_button_color_y_outline" },
        { "<Gamepad>/leftTrigger",    "xbox_lt"                     },
        { "<Gamepad>/rightTrigger",   "xbox_rt"                     },
        { "<Gamepad>/leftShoulder",   "xbox_lb"                     },
        { "<Gamepad>/rightShoulder",  "xbox_rb"                     },
        { "<Gamepad>/leftStick",      "xbox_stick_l_up"             },
        { "<Gamepad>/rightStick",     "xbox_stick_r"                },
        { "<Gamepad>/leftStickPress", "xbox_stick_l_press"          },
        { "<Gamepad>/rightStickPress","xbox_stick_r_press"          },
        { "<Gamepad>/startButton",    "xbox_button_menu"            },
        { "<Gamepad>/selectButton",   "xbox_button_view"            },
        { "<Gamepad>/dpad",           "xbox_dpad_round_all"         },
        { "<Gamepad>/dpad/up",        "xbox_dpad_round_all"         },
        { "<Gamepad>/dpad/down",      "xbox_dpad_round_all"         },
        { "<Gamepad>/dpad/left",      "xbox_dpad_round_all"         },
        { "<Gamepad>/dpad/right",     "xbox_dpad_round_all"         },
    };

    private static InputActionAsset GetAsset()
    {
        if (s_asset == null)
        {
            var all = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            if (all.Length > 0) s_asset = all[0];
        }
        return s_asset;
    }

    private static bool IsGamepadActive() =>
        InputDeviceTracker.Instance != null ? InputDeviceTracker.IsGamepadActive : Gamepad.current != null;

    /*
     * @brief Returns the display string for the first relevant binding of mapName/actionName.
     * When a gamepad is active: returns a TMP sprite tag <sprite name="..."> if the binding
     * path is mapped, otherwise falls back to the Input System display string.
     * When keyboard/mouse is active: returns the key name as plain text.
     */
    public static string GetKey(string mapName, string actionName, string fallback = "?")
    {
        var asset = GetAsset();
        if (asset == null) return fallback;

        var action = asset.FindAction($"{mapName}/{actionName}");
        if (action == null) return fallback;

        bool useGamepad = IsGamepadActive();

        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;

            if (useGamepad && b.path.StartsWith("<Gamepad>"))
            {
                if (s_gamepadSprites.TryGetValue(b.path, out var spriteName))
                    return $"<sprite name=\"{spriteName}\">";

                // Fallback: plain display string for unmapped paths
                return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }

            if (!useGamepad && (b.path.StartsWith("<Keyboard>") ||
                (b.path.StartsWith("<Mouse>") &&
                 !b.path.Contains("delta") &&
                 !b.path.Contains("position") &&
                 !b.path.Contains("scroll"))))
                return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }
        return fallback;
    }

    /*
     * @brief Builds a prompt string using the current binding for the action.
     * With gamepad: "<sprite name="xbox_y"> : Interact"
     * With keyboard: "E : Interact"
     */
    public static string BuildPrompt(string mapName, string actionName, string label, string fallback = "?")
    {
        return $"{GetKey(mapName, actionName, fallback)} : {label}";
    }

    /*
     * @brief Returns the display string for the gamepad binding of the given action,
     * regardless of which device is currently active.
     * Returns a TMP sprite tag if mapped, otherwise the Input System display string.
     */
    public static string GetGamepadKey(string mapName, string actionName, string fallback = "?")
    {
        var asset = GetAsset();
        if (asset == null) return fallback;

        var action = asset.FindAction($"{mapName}/{actionName}");
        if (action == null) return fallback;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (!b.path.StartsWith("<Gamepad>")) continue;

            if (s_gamepadSprites.TryGetValue(b.path, out var spriteName))
                return $"<sprite name=\"{spriteName}\">";

            return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }
        return fallback;
    }
}
