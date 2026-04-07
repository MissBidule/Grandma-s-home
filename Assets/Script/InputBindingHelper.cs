using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Static helper that returns the current keyboard display string for a given action.
 * Always reads from the live InputActionAsset so it reflects any rebind the player has done.
 */
public static class InputBindingHelper
{
    private static InputActionAsset s_asset;

    private static InputActionAsset GetAsset()
    {
        if (s_asset == null)
        {
            var all = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            if (all.Length > 0) s_asset = all[0];
        }
        return s_asset;
    }

    /*
     * @brief Returns the display string (e.g. "E", "Space") for the first keyboard/mouse binding
     *        of mapName/actionName, or fallback if none is found.
     */
    public static string GetKey(string mapName, string actionName, string fallback = "?")
    {
        var asset = GetAsset();
        if (asset == null) return fallback;

        var action = asset.FindAction($"{mapName}/{actionName}");
        if (action == null) return fallback;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (b.path.StartsWith("<Keyboard>") ||
                (b.path.StartsWith("<Mouse>") &&
                 !b.path.Contains("delta") &&
                 !b.path.Contains("position") &&
                 !b.path.Contains("scroll")))
            {
                return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }
        }
        return fallback;
    }

    /*
     * @brief Builds a prompt string like "E : Sabotage" using the current binding for the action.
     */
    public static string BuildPrompt(string mapName, string actionName, string label, string fallback = "?")
    {
        return $"{GetKey(mapName, actionName, fallback)} : {label}";
    }
}
