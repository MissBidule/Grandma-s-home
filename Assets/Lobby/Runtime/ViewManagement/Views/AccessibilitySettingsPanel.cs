using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PurrLobby
{
    /*
     * @brief Settings panel for accessibility options: colorblind correction and mouse sensitivity.
     * Creates a persistent global URP Volume at startup (DontDestroyOnLoad) to apply ChannelMixer
     * overrides for Deuteranopia, Protanopia and Tritanopia. Sensitivity values are broadcast via
     * static events so player controllers can react without polling PlayerPrefs each frame.
     */
    public class AccessibilitySettingsPanel : MonoBehaviour
    {
        [Header("Row Prefabs")]
        [SerializeField] private OptionRowDropdown dropdownRowPrefab;
        [SerializeField] private OptionRowToggle   toggleRowPrefab;
        [SerializeField] private OptionRowSlider   sliderRowPrefab;

        private OptionRowDropdown _colorblindRow;
        private OptionRowSlider   _colorblindIntensityRow;
        private OptionRowSlider   _sensitivityChildRow;
        private OptionRowSlider   _sensitivityGhostRow;

        private static readonly string KeyColorblind          = "Settings_Colorblind";
        private static readonly string KeyColorblindIntensity = "Settings_ColorblindIntensity";
        private static readonly string KeySensitivityChild    = "Settings_MouseSensitivityChild";
        private static readonly string KeySensitivityGhost    = "Settings_MouseSensitivityGhost";
        public  static readonly float  DefaultSensitivity     = 120f;

        public static event System.Action<float> OnChildSensitivityChanged;
        public static event System.Action<float> OnGhostSensitivityChanged;

        private static Volume       _colorblindVolume;
        private static ChannelMixer _channelMixer;
        private static int          _currentColorblindMode;

        private Transform _container;
        private bool      _built;

        /*
         * @brief Injects prefab references from a parent panel, overriding Inspector values.
         * @param dropdown  Prefab used to spawn dropdown rows (colorblind mode).
         * @param toggle    Prefab used to spawn toggle rows (unused currently, reserved).
         * @param slider    Prefab used to spawn slider rows (sensitivity, colorblind intensity).
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

            _sensitivityChildRow    = SpawnSlider("Sensitivity (Child)", 10f, 300f, DefaultSensitivity);
            _sensitivityGhostRow    = SpawnSlider("Sensitivity (Ghost)", 10f, 300f, DefaultSensitivity);
            _colorblindRow          = SpawnDropdown("Colorblind Mode");
            _colorblindIntensityRow = SpawnSlider("Colorblind Intensity", 0f, 1f, 1f);
            _built = true;
        }

        private System.Collections.IEnumerator ScrollToTop()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var sr = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (sr) sr.verticalNormalizedPosition = 1f;
        }

        private OptionRowDropdown SpawnDropdown(string labelText)
        {
            if (!dropdownRowPrefab) return null;
            var row = Instantiate(dropdownRowPrefab, _container, false);
            if (row.m_label) row.m_label.text = labelText;
            return row;
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
            if (_sensitivityChildRow?.m_slider != null)
            {
                float saved = PlayerPrefs.GetFloat(KeySensitivityChild, DefaultSensitivity);
                _sensitivityChildRow.m_slider.value = saved;
                UpdateSensitivityLabel(_sensitivityChildRow, saved);
                _sensitivityChildRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(KeySensitivityChild, v);
                    UpdateSensitivityLabel(_sensitivityChildRow, v);
                    OnChildSensitivityChanged?.Invoke(v);
                });
            }

            if (_sensitivityGhostRow?.m_slider != null)
            {
                float saved = PlayerPrefs.GetFloat(KeySensitivityGhost, DefaultSensitivity);
                _sensitivityGhostRow.m_slider.value = saved;
                UpdateSensitivityLabel(_sensitivityGhostRow, saved);
                _sensitivityGhostRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(KeySensitivityGhost, v);
                    UpdateSensitivityLabel(_sensitivityGhostRow, v);
                    OnGhostSensitivityChanged?.Invoke(v);
                });
            }

            if (_colorblindRow?.m_dropdown != null)
            {
                _colorblindRow.m_dropdown.ClearOptions();
                _colorblindRow.m_dropdown.AddOptions(new List<string> { "Normal", "Deuteranopia", "Protanopia", "Tritanopia" });
                int saved = PlayerPrefs.GetInt(KeyColorblind, 0);
                _colorblindRow.m_dropdown.value = saved;
                ApplyColorblind(saved);
                _colorblindRow.m_dropdown.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(KeyColorblind, v);
                    ApplyColorblind(v);
                    SetIntensityInteractable(v != 0);
                });
            }

            if (_colorblindIntensityRow?.m_slider != null)
            {
                float intensity = PlayerPrefs.GetFloat(KeyColorblindIntensity, 1f);
                _colorblindIntensityRow.m_slider.value = intensity;
                ApplyColorblindIntensity(intensity);
                UpdateLabel(_colorblindIntensityRow, intensity);
                SetIntensityInteractable(PlayerPrefs.GetInt(KeyColorblind, 0) != 0);
                _colorblindIntensityRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(KeyColorblindIntensity, v);
                    ApplyColorblindIntensity(v);
                    UpdateLabel(_colorblindIntensityRow, v);
                });
            }

        }

        /*
         * @brief Clears all accessibility PlayerPrefs keys and reloads default values.
         * Also fires the sensitivity events so player controllers reset immediately.
         */
        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeyColorblind);
            PlayerPrefs.DeleteKey(KeyColorblindIntensity);
            PlayerPrefs.DeleteKey(KeySensitivityChild);
            PlayerPrefs.DeleteKey(KeySensitivityGhost);
            PlayerPrefs.Save();
            ApplyColorblind(0);
            ApplyColorblindIntensity(1f);
            OnChildSensitivityChanged?.Invoke(DefaultSensitivity);
            OnGhostSensitivityChanged?.Invoke(DefaultSensitivity);
            OnDisable();
            LoadAndApply();
        }

        private void OnDisable()
        {
            if (_sensitivityChildRow?.m_slider)     _sensitivityChildRow.m_slider.onValueChanged.RemoveAllListeners();
            if (_sensitivityGhostRow?.m_slider)     _sensitivityGhostRow.m_slider.onValueChanged.RemoveAllListeners();
            if (_colorblindRow?.m_dropdown)        _colorblindRow.m_dropdown.onValueChanged.RemoveAllListeners();
            if (_colorblindIntensityRow?.m_slider) _colorblindIntensityRow.m_slider.onValueChanged.RemoveAllListeners();
        }

        private static void UpdateLabel(OptionRowSlider row, float v)
        {
            if (row?.m_valueLabel != null)
                row.m_valueLabel.text = Mathf.RoundToInt(v * 100f) + "%";
        }

        private static void UpdateSensitivityLabel(OptionRowSlider row, float v)
        {
            if (row?.m_valueLabel != null)
                row.m_valueLabel.text = Mathf.RoundToInt(v).ToString();
        }

        // ── Startup application ───────────────────────────────────────────────

        /*
         * @brief Applies saved accessibility settings automatically after every scene load.
         */
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyOnStartup()
        {
            ApplyColorblind(PlayerPrefs.GetInt(KeyColorblind, 0));
            ApplyColorblindIntensity(PlayerPrefs.GetFloat(KeyColorblindIntensity, 1f));
            OnChildSensitivityChanged?.Invoke(PlayerPrefs.GetFloat(KeySensitivityChild, DefaultSensitivity));
            OnGhostSensitivityChanged?.Invoke(PlayerPrefs.GetFloat(KeySensitivityGhost, DefaultSensitivity));
        }

        // ── Colorblind correction ─────────────────────────────────────────────

        /*
         * @brief Sets the weight of the colorblind Volume to the given intensity.
         * @param intensity  Blend weight in [0, 1]. Has no effect when mode is Normal.
         */
        private static void ApplyColorblindIntensity(float intensity)
        {
            EnsureColorblindVolume();
            if (_currentColorblindMode != 0)
                _colorblindVolume.weight = intensity;
        }

        private void SetIntensityInteractable(bool interactable)
        {
            if (_colorblindIntensityRow == null) return;
            if (_colorblindIntensityRow.m_slider != null)
                _colorblindIntensityRow.m_slider.interactable = interactable;
            if (_colorblindIntensityRow.m_label != null)
                _colorblindIntensityRow.m_label.color = interactable
                    ? Color.white
                    : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        /*
         * @brief Lazily creates the persistent global URP Volume used for colorblind correction.
         * Called before every colorblind apply so the Volume is always available at runtime.
         */
        private static void EnsureColorblindVolume()
        {
            if (_colorblindVolume != null) return;
            var go = new GameObject("[ColorblindVolume]") { layer = 0 };
            DontDestroyOnLoad(go);
            _colorblindVolume          = go.AddComponent<Volume>();
            _colorblindVolume.isGlobal = true;
            _colorblindVolume.priority = 998f;
            _colorblindVolume.weight   = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _channelMixer = profile.Add<ChannelMixer>(true);
            _colorblindVolume.profile = profile;
        }

        /*
         * @brief Activates the colorblind Volume and configures the ChannelMixer for the given mode.
         * @param mode  0 = Normal (volume disabled), 1 = Deuteranopia, 2 = Protanopia, 3 = Tritanopia.
         */
        private static void ApplyColorblind(int mode)
        {
            EnsureColorblindVolume();
            _currentColorblindMode = mode;

            if (mode == 0)
            {
                _colorblindVolume.weight = 0f;
                return;
            }

            _colorblindVolume.weight = PlayerPrefs.GetFloat(KeyColorblindIntensity, 1f);

            switch (mode)
            {
                case 1: // Deuteranopia — green-deficient, confuses red/green
                    SetMixer(100f,   0f,   0f,
                               0f,  30f,  70f,
                               0f,   0f, 100f);
                    break;

                case 2: // Protanopia — red-deficient, confuses red/green
                    SetMixer( 60f,  40f,   0f,
                               0f, 100f,   0f,
                               0f,   0f, 100f);
                    break;

                case 3: // Tritanopia — confuses blue/yellow
                    SetMixer(100f,   0f,   0f,
                               0f, 100f,   0f,
                              60f,   0f,  40f);
                    break;
            }
        }

        /*
         * @brief Overrides all nine ChannelMixer coefficients at once.
         * Row order: red-out, green-out, blue-out. Column order: red-in, green-in, blue-in.
         */
        private static void SetMixer(float rr, float rg, float rb,
                                     float gr, float gg, float gb,
                                     float br, float bg, float bb)
        {
            _channelMixer.redOutRedIn.Override(rr);
            _channelMixer.redOutGreenIn.Override(rg);
            _channelMixer.redOutBlueIn.Override(rb);
            _channelMixer.greenOutRedIn.Override(gr);
            _channelMixer.greenOutGreenIn.Override(gg);
            _channelMixer.greenOutBlueIn.Override(gb);
            _channelMixer.blueOutRedIn.Override(br);
            _channelMixer.blueOutGreenIn.Override(bg);
            _channelMixer.blueOutBlueIn.Override(bb);
        }
    }
}
