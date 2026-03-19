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
            { "Player/Attack", "Attack" },
            { "Player/Interact", "Interact" },
            { "Player/Jump", "Jump" },
            { "Player/Sprint", "Sprint" },
            { "Player/Crouch", "Crouch" },
            { "Player/Change_weapon", "Change Weapon" },
            // Ghost
            { "Ghost/Interact", "Interact" },
            { "Ghost/TransformConfirm", "Transform" },
            { "Ghost/Scan", "Scan" },
            { "Ghost/Dash", "Dash" },
            { "Ghost/Sneak", "Sneak" },
            { "Ghost/RotatePreviewLeft", "Rotate Left" },
            { "Ghost/RotatePreviewRight", "Rotate Right" },
            { "Ghost/Hint", "Show Hint" },
        };

        private InputActionRebindingExtensions.RebindingOperation m_rebindOp;
        private Transform m_container;

        /*
         * @brief Injects prefab references from a parent panel, overriding Inspector values.
         * @param _keybinding    Prefab used to spawn keybinding rows.
         * @param _sectionTitle  Prefab used to spawn section header rows.
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
            BuildSection("Player");
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
         * @param _mapName  Name of the InputActionMap to iterate ("Player" or "Ghost").
         */
        private void BuildSection(string _mapName)
        {
            var map = m_inputActions?.FindActionMap(_mapName);
            if (map == null)
            {
                return;
            }

            foreach (var action in map.actions)
            {
                string key = $"{_mapName}/{action.name}";
                if (!m_ActionLabels.TryGetValue(key, out string displayName))
                {
                    continue;
                }

                int bindingIndex = FindKeyboardBindingIndex(action);
                if (bindingIndex < 0)
                {
                    continue;
                }

                SpawnKeybindingRow(action, bindingIndex, displayName);
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
        }

        /*
         * @brief Begins an interactive rebind for the given action binding.
         * Tints the button image and shows "..." until the player presses a key or cancels with Escape.
         * @param _action        Action whose binding is being changed.
         * @param _bindingIndex  Index of the specific binding to rebind.
         * @param _btnImage      Button background image to tint while waiting.
         * @param _btnText       Button label to update with "..." and then the new key name.
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
         */
        private void FinishRebind(InputAction _action, int _bindingIndex, Image _btnImage, TextMeshProUGUI _btnText)
        {
            _action.Enable();
            if (_btnText)
            {
                _btnText.text = _action.GetBindingDisplayString(_bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }
            if (_btnImage)
            {
                _btnImage.color = m_buttonNormalColor;
            }
            SaveBindings();
            m_rebindOp?.Dispose();
            m_rebindOp = null;
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
