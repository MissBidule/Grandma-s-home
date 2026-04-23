using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PurrLobby
{
    /*
     * @brief Settings panel for keyboard/mouse rebinding.
     * Iterates the InputActionAsset at runtime to build a row for every whitelisted action
     * (Child "Player" map + Ghost map). Interactive rebinding is handled through the
     * InputActionRebindingExtensions API; overrides are serialised to JSON in PlayerPrefs.
     * Each row also displays the corresponding gamepad icon (read-only) next to the keyboard binding.
     */
    public class ControlsSettingsPanel : MonoBehaviour
    {
        [SerializeField] private InputActionAsset m_inputActions;
        [SerializeField] private RectTransform m_scrollContent;

        [Header("Row Prefabs")]
        [SerializeField] private OptionRowKeybinding m_keybindingRowPrefab;
        [SerializeField] private OptionSectionTitle m_sectionTitlePrefab;

        [Header("Colors")]
        [SerializeField] private Color m_buttonNormalColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color m_buttonWaitingColor = new Color(0.7f, 0.4f, 0f, 1f);

        private static readonly string m_SaveKey = "Settings_Keybindings";

        private static readonly Dictionary<string, string> m_ActionLabels = new Dictionary<string, string>
        {
            // Child
            { "Child/Attack", "Attack" },
            { "Child/Interact", "Interact" },
            { "Child/Jump", "Jump" },
            { "Child/Sneak", "Sneak" },
            { "Child/Change_weapon", "Change Weapon" },
            { "Child/Hint", "Show Hint" },
            { "Child/PushToTalk", "Push to Talk" },
            { "Child/Leaderboard", "Leaderboard" },
            // Ghost
            { "Ghost/Interact", "Interact" },
            { "Ghost/Scan", "Scan" },
            { "Ghost/Dash", "Dash" },
            { "Ghost/Sneak", "Sneak" },
            { "Ghost/RotatePreviewLeft", "Rotate Left" },
            { "Ghost/RotatePreviewRight", "Rotate Right" },
            { "Ghost/OpenProps", "Transform Wheel" },
            { "Ghost/Hint", "Show Hint" },
            { "Ghost/PushToTalk", "Push to Talk" },
            { "Ghost/Leaderboard", "Leaderboard" },
        };

        // Composite part labels: "MapName/ActionName/partName" → display name
        private static readonly Dictionary<string, string> m_CompositePartLabels = new Dictionary<string, string>
        {
            { "Child/Move/up",    "Forward" },
            { "Child/Move/down",  "Backward" },
            { "Child/Move/left",  "Left" },
            { "Child/Move/right", "Right" },
            { "Ghost/Move/up",    "Forward" },
            { "Ghost/Move/down",  "Backward" },
            { "Ghost/Move/left",  "Left" },
            { "Ghost/Move/right", "Right" },
        };

        // Maps gamepad binding paths to Xbox icon sprite names in Resources/XboxIcons/
        private static readonly Dictionary<string, string> s_GamepadSprites = new Dictionary<string, string>
        {
            { "<Gamepad>/buttonSouth",     "xbox_button_color_a_outline" },
            { "<Gamepad>/buttonEast",      "xbox_button_color_b_outline" },
            { "<Gamepad>/buttonWest",      "xbox_button_color_x_outline" },
            { "<Gamepad>/buttonNorth",     "xbox_button_color_y_outline" },
            { "<Gamepad>/leftTrigger",     "xbox_lt" },
            { "<Gamepad>/rightTrigger",    "xbox_rt" },
            { "<Gamepad>/leftShoulder",    "xbox_lb" },
            { "<Gamepad>/rightShoulder",   "xbox_rb" },
            { "<Gamepad>/leftStick",       "xbox_stick_l_up" },
            { "<Gamepad>/rightStick",      "xbox_stick_r" },
            { "<Gamepad>/leftStickPress",  "xbox_stick_l_press" },
            { "<Gamepad>/rightStickPress", "xbox_stick_r_press" },
            { "<Gamepad>/startButton",     "xbox_button_menu" },
            { "<Gamepad>/selectButton",    "xbox_button_view" },

            { "<Gamepad>/dpad",            "xbox_dpad_round_all" },
            { "<Gamepad>/dpad/up",         "xbox_dpad_round_all" },
            { "<Gamepad>/dpad/down",       "xbox_dpad_round_all" },
            { "<Gamepad>/dpad/left",       "xbox_dpad_round_all" },
            { "<Gamepad>/dpad/right",      "xbox_dpad_round_all" },
        };

        // Cache loaded sprites to avoid repeated Resources.Load calls
        private static readonly Dictionary<string, Sprite> s_SpriteCache = new Dictionary<string, Sprite>();

        private InputActionRebindingExtensions.RebindingOperation m_rebindOp;
        private Transform m_container;

        /*
         * @brief Injects prefab references from a parent panel, overriding Inspector values.
         * @param _keybinding Prefab used to spawn keybinding rows.
         * @param _sectionTitle Prefab used to spawn section header rows.
         */
        public void Initialize(OptionRowKeybinding _keybinding, OptionSectionTitle _sectionTitle)
        {
            if (_keybinding)
            {
                m_keybindingRowPrefab = _keybinding;
            }
            if (_sectionTitle)
            {
                m_sectionTitlePrefab = _sectionTitle;
            }
        }

        private void Awake()
        {
            if (m_inputActions == null)
            {
                var all = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                if (all.Length > 0)
                {
                    m_inputActions = all[0];
                }
            }
            m_container = m_scrollContent != null
                ? (Transform)m_scrollContent
                : transform.Find("Scroll View/Viewport/Content");
        }

        private void OnEnable()
        {
            LoadBindings();
            RebuildUI();
            StartCoroutine(ScrollToTop());
        }

        private System.Collections.IEnumerator ScrollToTop()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var sr = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (sr)
            {
                sr.verticalNormalizedPosition = 1f;
            }
        }

        /*
         * @brief Removes all binding overrides from the InputActionAsset and clears the saved JSON.
         */
        public void ResetToDefaults()
        {
            if (m_inputActions != null)
            {
                m_inputActions.RemoveAllBindingOverrides();
            }
            PlayerPrefs.DeleteKey(m_SaveKey);
            PlayerPrefs.Save();
            RebuildUI();
        }

        private void OnDisable()
        {
            m_rebindOp?.Cancel();
            m_rebindOp?.Dispose();
            m_rebindOp = null;
        }

        /*
         * @brief Destroys existing rows and rebuilds them from the current binding state.
         */
        private void RebuildUI()
        {
            if (m_container == null)
            {
                return;
            }
            foreach (Transform child in m_container)
            {
                Destroy(child.gameObject);
            }

            SpawnSectionTitle("─── Child ───");
            BuildSection("Child");
            SpawnSectionTitle("─── Ghost ───");
            BuildSection("Ghost");
        }

        private void SpawnSectionTitle(string _text)
        {
            if (!m_sectionTitlePrefab)
            {
                return;
            }
            var row = Instantiate(m_sectionTitlePrefab, m_container, false);
            if (row.m_title)
            {
                row.m_title.text = _text;
            }
        }

        /*
         * @brief Spawns a keybinding row for each whitelisted action in the given action map.
         * @param _mapName Name of the InputActionMap to iterate ("Player" or "Ghost").
         */
        private void BuildSection(string _mapName)
        {
            var map = m_inputActions?.FindActionMap(_mapName);
            if (map == null)
            {
                return;
            }

            var spawnedComposites = new HashSet<string>();
            foreach (var action in map.actions)
            {
                // Simple actions (non-composite)
                string key = $"{_mapName}/{action.name}";
                if (m_ActionLabels.TryGetValue(key, out string displayName))
                {
                    int bindingIndex = FindKeyboardBindingIndex(action);
                    if (bindingIndex >= 0)
                    {
                        SpawnKeybindingRow(action, bindingIndex, displayName);
                    }
                    continue;
                }

                // Composite actions (e.g., Move = WASD)
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (!b.isPartOfComposite)
                    {
                        continue;
                    }
                    string compositeKey = $"{_mapName}/{action.name}/{b.name}";
                    if (!m_CompositePartLabels.TryGetValue(compositeKey, out string partLabel))
                    {
                        continue;
                    }
                    if (!b.path.StartsWith("<Keyboard>"))
                    {
                        continue;
                    }
                    if (!spawnedComposites.Add(compositeKey))
                    {
                        continue;  // duplicate (e.g., WASD + arrow keys)
                    }
                    SpawnKeybindingRow(action, i, partLabel);
                }
            }
        }

        /*
         * @brief Finds the first Keyboard or Mouse binding index for an action, ignoring composites and axes.
         * @param _action  The InputAction to inspect.
         * @return Binding index, or -1 if none was found.
         */
        private int FindKeyboardBindingIndex(InputAction _action)
        {
            for (int i = 0; i < _action.bindings.Count; i++)
            {
                var b = _action.bindings[i];
                if (b.isComposite || b.isPartOfComposite)
                {
                    continue;
                }
                if (b.path.StartsWith("<Keyboard>"))
                {
                    return i;
                }
                if (b.path.StartsWith("<Mouse>") &&
                    !b.path.Contains("delta") &&
                    !b.path.Contains("position") &&
                    !b.path.Contains("scroll"))
                {
                    return i;
                }
            }
            return -1;
        }

        /*
         * @brief Finds the gamepad binding path for an action.
         * @return The binding path (e.g. "<Gamepad>/buttonSouth"), or null if not found.
         */
        private string FindGamepadBindingPath(InputAction _action)
        {
            for (int i = 0; i < _action.bindings.Count; i++)
            {
                var b = _action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (b.path.StartsWith("<Gamepad>")) return b.path;
            }
            return null;
        }

        /*
         * @brief Loads an Xbox icon sprite from Resources/XboxIcons/ by name, with caching.
         */
        private Sprite LoadGamepadSprite(string _spriteName)
        {
            if (s_SpriteCache.TryGetValue(_spriteName, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>($"XboxIcons/{_spriteName}");
            if (sprite != null)
                s_SpriteCache[_spriteName] = sprite;
            return sprite;
        }

        /*
         * @brief Creates a small Image element showing the gamepad icon and appends it to the row.
         */
        private void AppendGamepadIcon(Transform _rowTransform, InputAction _action)
        {
            string gamepadPath = FindGamepadBindingPath(_action);
            if (gamepadPath == null) return;
            if (!s_GamepadSprites.TryGetValue(gamepadPath, out string spriteName)) return;

            var sprite = LoadGamepadSprite(spriteName);
            if (sprite == null) return;

            var iconGO = new GameObject("GamepadIcon", typeof(RectTransform));
            iconGO.transform.SetParent(_rowTransform, false);

            var img = iconGO.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var le = iconGO.AddComponent<LayoutElement>();
            le.preferredWidth = 50;
            le.preferredHeight = 50;
        }

        private void SpawnKeybindingRow(InputAction _action, int _bindingIndex, string _displayName)
        {
            if (!m_keybindingRowPrefab)
            {
                return;
            }
            var row = Instantiate(m_keybindingRowPrefab, m_container, false);
            if (row.m_label)
            {
                row.m_label.text = _displayName;
            }
            if (row.m_buttonLabel)
            {
                row.m_buttonLabel.text = _action.GetBindingDisplayString(_bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }
            if (row.m_button)
            {
                row.m_button.onClick.AddListener(() => StartRebind(_action, _bindingIndex, row.m_buttonImage, row.m_buttonLabel));
            }

            // Add gamepad icon to the right of the keyboard binding
            AppendGamepadIcon(row.transform, _action);
        }

        /*
         * @brief Begins an interactive rebind for the given action binding.
         * Tints the button image and shows "..." until the player presses a key or cancels with Escape.
         * @param _action Action whose binding is being changed.
         * @param _bindingIndex Index of the specific binding to rebind.
         * @param _btnImage Button background image to tint while waiting.
         * @param _btnText Button label to update with "..." and then the new key name.
         */
        private void StartRebind(InputAction _action, int _bindingIndex, Image _btnImage, TextMeshProUGUI _btnText)
        {
            m_rebindOp?.Cancel();
            if (_btnImage)
            {
                _btnImage.color = m_buttonWaitingColor;
            }
            if (_btnText)
            {
                _btnText.text = "...";
            }
            _action.Disable();
            m_rebindOp = _action.PerformInteractiveRebinding(_bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(_ => FinishRebind(_action, _bindingIndex, _btnImage, _btnText))
                .OnCancel(_  => FinishRebind(_action, _bindingIndex, _btnImage, _btnText))
                .Start();
        }

        /*
         * @brief Completes or cancels an interactive rebind, re-enables the action and saves to PlayerPrefs.
         * Also clears any conflicting binding within the same action map.
         */
        private void FinishRebind(InputAction _action, int _bindingIndex, Image _btnImage, TextMeshProUGUI _btnText)
        {
            _action.Enable();
            ResolveConflicts(_action, _bindingIndex);
            if (_btnImage)
            {
                _btnImage.color = m_buttonNormalColor;
            }
            SaveBindings();
            PropagateToPlayerInputs();
            m_rebindOp?.Dispose();
            m_rebindOp = null;
            RebuildUI();
        }

        /*
         * @brief Clears any binding in the same action map that uses the same path as the newly-bound action.
         * Ghost and Child maps are independent: a key bound in Ghost can still be used in Child.
         * Also checks other parts of the same composite action (e.g. Move/up vs Move/right).
         * @param _reboundAction The action that was just rebound.
         * @param _bindingIndex  The binding index that was modified.
         */
        private void ResolveConflicts(InputAction _reboundAction, int _bindingIndex)
        {
            string newPath = _reboundAction.bindings[_bindingIndex].effectivePath;
            if (string.IsNullOrEmpty(newPath))
            {
                return;
            }
            foreach (var action in _reboundAction.actionMap.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action == _reboundAction && i == _bindingIndex)
                    {
                        continue;
                    }
                    if (action.bindings[i].isComposite)
                    {
                        continue;
                    }
                    if (action.bindings[i].effectivePath == newPath)
                    {
                        action.ApplyBindingOverride(i, "");
                    }
                }
            }
        }

        private void PropagateToPlayerInputs()
        {
            if (m_inputActions == null) return;
            string json = m_inputActions.SaveBindingOverridesAsJson();
            foreach (var pi in UnityEngine.InputSystem.PlayerInput.all)
                pi.actions.LoadBindingOverridesFromJson(json);
        }

        private void LoadBindings()
        {
            if (m_inputActions == null)
            {
                return;
            }
            string saved = PlayerPrefs.GetString(m_SaveKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                m_inputActions.LoadBindingOverridesFromJson(saved);
            }
        }

        private void SaveBindings()
        {
            if (m_inputActions == null)
            {
                return;
            }
            PlayerPrefs.SetString(m_SaveKey, m_inputActions.SaveBindingOverridesAsJson());
        }
    }
}
