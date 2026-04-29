using UnityEngine;

namespace PurrLobby
{
    /*
     * @brief Settings panel for accessibility options: mouse sensitivity.
     * Sensitivity values are broadcast via static events so player controllers can react
     * without polling PlayerPrefs each frame.
     * Colorblind correction is handled by SettingsCanvasController (Assets/Script/UI/Views).
     */
    public class AccessibilitySettingsPanel : MonoBehaviour
    {
        [Header("Row Prefabs")]
        [SerializeField] private OptionRowDropdown dropdownRowPrefab;
        [SerializeField] private OptionRowToggle   toggleRowPrefab;
        [SerializeField] private OptionRowSlider   sliderRowPrefab;

        private OptionRowSlider _sensitivityRow;

        private static readonly string KeySensitivity     = "Settings_MouseSensitivity";
        public  static readonly float  DefaultSensitivity = 50f;

        public static event System.Action<float> OnSensitivityChanged;

        private Transform _container;
        private bool      _built;

        /*
         * @brief Injects prefab references from a parent panel, overriding Inspector values.
         * @param dropdown  Prefab used to spawn dropdown rows (unused now, reserved).
         * @param toggle    Prefab used to spawn toggle rows (unused currently, reserved).
         * @param slider    Prefab used to spawn slider rows (sensitivity).
         */
        public void Initialize(OptionRowDropdown dropdown, OptionRowToggle toggle, OptionRowSlider slider)
        {
            if (dropdown) dropdownRowPrefab = dropdown;
            if (toggle)   toggleRowPrefab   = toggle;
            if (slider)   sliderRowPrefab   = slider;
        }

        private void Awake()
        {
            _container = transform.Find("Scroll View/Viewport/Content");
            if (_container == null) return;

            foreach (Transform child in _container)
                Destroy(child.gameObject);

            _sensitivityRow = SpawnSlider("Sensitivity", 1f, 100f, DefaultSensitivity);
            _built = true;
        }

        private System.Collections.IEnumerator ScrollToTop()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var sr = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (sr) sr.verticalNormalizedPosition = 1f;
        }

        private OptionRowSlider SpawnSlider(string labelText, float min, float max, float def)
        {
            if (!sliderRowPrefab) return null;
            var row = Instantiate(sliderRowPrefab, _container, false);
            if (row.m_label) row.m_label.text = labelText;
            if (row.m_slider) { row.m_slider.minValue = min; row.m_slider.maxValue = max; row.m_slider.value = def; }
            return row;
        }

        private void OnEnable()
        {
            if (!_built) return;
            StartCoroutine(ScrollToTop());
            LoadAndApply();
        }

        private void LoadAndApply()
        {
            if (_sensitivityRow?.m_slider != null)
            {
                float saved = PlayerPrefs.GetFloat(KeySensitivity, DefaultSensitivity);
                _sensitivityRow.m_slider.value = saved;
                UpdateSensitivityLabel(_sensitivityRow, saved);
                _sensitivityRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(KeySensitivity, v);
                    UpdateSensitivityLabel(_sensitivityRow, v);
                    OnSensitivityChanged?.Invoke(v);
                });
            }
        }

        /*
         * @brief Clears sensitivity PlayerPref and reloads default value.
         * Fires the sensitivity event so player controllers reset immediately.
         */
        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeySensitivity);
            PlayerPrefs.Save();
            OnSensitivityChanged?.Invoke(DefaultSensitivity);
            OnDisable();
            LoadAndApply();
        }

        private void OnDisable()
        {
            if (_sensitivityRow?.m_slider) _sensitivityRow.m_slider.onValueChanged.RemoveAllListeners();
        }

        private static void UpdateSensitivityLabel(OptionRowSlider row, float v)
        {
            if (row?.m_valueLabel != null)
                row.m_valueLabel.text = Mathf.RoundToInt(v).ToString();
        }

        /*
         * @brief Broadcasts saved sensitivity after every scene load so player controllers init correctly.
         */
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyOnStartup()
        {
            OnSensitivityChanged?.Invoke(PlayerPrefs.GetFloat(KeySensitivity, DefaultSensitivity));
        }
    }
}
