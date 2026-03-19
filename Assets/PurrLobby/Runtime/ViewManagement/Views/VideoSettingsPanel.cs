using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace PurrLobby
{
    /*
     * @brief Settings panel for all video and display options.
     * Covers display mode, resolution, VSync, FPS cap, texture quality, anti-aliasing,
     * shadow quality, brightness (URP post-exposure) and render scale.
     * Manages two persistent global URP Volumes (DontDestroyOnLoad) for brightness and
     * an optional FPS counter overlay. All values are stored in PlayerPrefs.
     */
    public class VideoSettingsPanel : MonoBehaviour
    {
        [Header("Row Prefabs")]
        [SerializeField] private OptionRowDropdown  m_dropdownRowPrefab;
        [SerializeField] private OptionRowToggle m_toggleRowPrefab;
        [SerializeField] private OptionRowSlider m_sliderRowPrefab;
        [SerializeField] private OptionRowButton m_buttonRowPrefab;
        [SerializeField] private OptionSectionTitle m_sectionTitlePrefab;

        private OptionRowDropdown m_displayModeRow;
        private OptionRowDropdown m_resolutionRow;
        private OptionRowDropdown m_textureQualityRow;
        private OptionRowDropdown m_antiAliasingRow;
        private OptionRowDropdown m_shadowQualityRow;
        private OptionRowDropdown m_fpsLimitRow;
        private OptionRowToggle m_vsyncRow;
        private OptionRowToggle m_fpsCounterRow;
        private OptionRowSlider m_gammaRow;
        private OptionRowSlider m_renderScaleRow;

        private TMP_Dropdown DisplayMode => m_displayModeRow?.m_dropdown;
        private TMP_Dropdown Resolution => m_resolutionRow?.m_dropdown;
        private TMP_Dropdown TextureQuality => m_textureQualityRow?.m_dropdown;
        private TMP_Dropdown AntiAliasing => m_antiAliasingRow?.m_dropdown;
        private TMP_Dropdown ShadowQuality => m_shadowQualityRow?.m_dropdown;
        private TMP_Dropdown FpsLimit => m_fpsLimitRow?.m_dropdown;
        private Toggle VSync => m_vsyncRow?.m_toggle;
        private Toggle FpsCounter => m_fpsCounterRow?.m_toggle;
        private Slider Gamma => m_gammaRow?.m_slider;
        private Slider RenderScale => m_renderScaleRow?.m_slider;

        private Resolution[] m_availableResolutions;
        private static Volume m_brightnessVolume;
        private static ColorAdjustments m_brightnessCA;
        private static Volume m_effectsVolume;
        private static GameObject m_fpsCounter;

        private Transform m_container;
        private bool m_built;

        private static readonly string m_KeyDisplayMode = "Settings_DisplayMode";
        private static readonly string m_KeyResolution = "Settings_Resolution";
        private static readonly string m_KeyTextureQuality = "Settings_TextureQuality";
        private static readonly string m_KeyVSync = "Settings_VSync";
        private static readonly string m_KeyFpsCounter = "Settings_FpsCounter";
        private static readonly string m_KeyFPSLimit = "Settings_FPSLimit";
        private static readonly string m_KeyAntiAliasing = "Settings_AntiAliasing";
        private static readonly string m_KeyShadowQuality = "Settings_ShadowQuality";
        private static readonly string m_KeyGamma = "Settings_Gamma";
        private static readonly string m_KeyRenderScale = "Settings_RenderScale";

        /*
         * @brief Injects prefab references from a parent panel, overriding Inspector values.
         * @param _dropdown      Prefab used to spawn dropdown rows.
         * @param _toggle        Prefab used to spawn toggle rows.
         * @param _slider        Prefab used to spawn slider rows.
         * @param _button        Prefab used to spawn button rows.
         * @param _sectionTitle  Prefab used to spawn section header rows.
         */
        public void Initialize(OptionRowDropdown _dropdown, OptionRowToggle _toggle,
                               OptionRowSlider _slider, OptionRowButton _button,
                               OptionSectionTitle _sectionTitle)
        {
            if (_dropdown)
            {
                m_dropdownRowPrefab = _dropdown;
            }
            if (_toggle)
            {
                m_toggleRowPrefab = _toggle;
            }
            if (_slider)
            {
                m_sliderRowPrefab = _slider;
            }
            if (_button)
            {
                m_buttonRowPrefab = _button;
            }
            if (_sectionTitle)
            {
                m_sectionTitlePrefab = _sectionTitle;
            }
        }

        private void Awake()
        {
            m_container = transform.Find("Scroll View/Viewport/Content");
            if (m_container == null)
            {
                return;
            }

            foreach (Transform child in m_container)
            {
                Destroy(child.gameObject);
            }

            SpawnSectionTitle("Display");
            m_displayModeRow = SpawnDropdown("Display Mode");
            m_resolutionRow = SpawnDropdown("Resolution");
            m_vsyncRow = SpawnToggle("VSync");
            m_fpsLimitRow = SpawnDropdown("Max Framerate");
            m_fpsCounterRow = SpawnToggle("FPS Counter");

            SpawnSectionTitle("Quality");
            m_textureQualityRow = SpawnDropdown("Texture Quality");
            m_antiAliasingRow = SpawnDropdown("Anti-Aliasing");
            m_shadowQualityRow = SpawnDropdown("Shadow Quality");
            m_gammaRow = SpawnSlider("Brightness",    -2f,  2f, 0f);
            m_renderScaleRow = SpawnSlider("Render Scale", 0.5f, 2f, 1f);
            m_built = true;
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

        private OptionRowDropdown SpawnDropdown(string _labelText)
        {
            if (!m_dropdownRowPrefab)
            {
                return null;
            }
            var row = Instantiate(m_dropdownRowPrefab, m_container, false);
            if (row.m_label)
            {
                row.m_label.text = _labelText;
            }
            if (row.m_dropdown?.captionText != null)
            {
                row.m_dropdown.captionText.enableAutoSizing = true;
                row.m_dropdown.captionText.fontSizeMin      = 18f;
                row.m_dropdown.captionText.fontSizeMax      = 72f;
                row.m_dropdown.captionText.alignment        = TMPro.TextAlignmentOptions.MidlineLeft;
            }
            return row;
        }

        private OptionRowToggle SpawnToggle(string _labelText)
        {
            if (!m_toggleRowPrefab)
            {
                return null;
            }
            var row = Instantiate(m_toggleRowPrefab, m_container, false);
            if (row.m_label)
            {
                row.m_label.text = _labelText;
            }
            return row;
        }

        private OptionRowSlider SpawnSlider(string _labelText, float _min, float _max, float _def)
        {
            if (!m_sliderRowPrefab)
            {
                return null;
            }
            var row = Instantiate(m_sliderRowPrefab, m_container, false);
            if (row.m_label)
            {
                row.m_label.text = _labelText;
            }
            if (row.m_slider)
            {
                row.m_slider.minValue = _min; row.m_slider.maxValue = _max; row.m_slider.value = _def;
            }
            return row;
        }

        private OptionRowButton SpawnButton(string _labelText)
        {
            if (!m_buttonRowPrefab)
            {
                return null;
            }
            var row = Instantiate(m_buttonRowPrefab, m_container, false);
            if (row.m_label)
            {
                row.m_label.text = _labelText;
            }
            return row;
        }

        private void SpawnSectionTitle(string titleText)
        {
            if (!m_sectionTitlePrefab)
            {
                return;
            }
            var row = Instantiate(m_sectionTitlePrefab, m_container, false);
            if (row.m_title)
            {
                row.m_title.text = titleText;
            }
        }

        private void OnEnable()
        {
            if (!m_built)
            {
                return;
            }

            if (m_brightnessVolume == null)
            {
                var go = new GameObject("[SettingsVolume]") { layer = 0 };
                DontDestroyOnLoad(go);
                m_brightnessVolume          = go.AddComponent<Volume>();
                m_brightnessVolume.isGlobal = true;
                m_brightnessVolume.priority = 1000f;
                m_brightnessVolume.weight   = 1f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                m_brightnessCA = profile.Add<ColorAdjustments>(true);
                m_brightnessCA.postExposure.Override(0f);
                m_brightnessVolume.profile = profile;
            }

            StartCoroutine(ScrollToTop());
            BuildDisplayModeOptions();
            BuildResolutionOptions();
            BuildTextureQualityOptions();
            BuildAntiAliasingOptions();
            BuildShadowQualityOptions();
            BuildFPSLimitOptions();
            LoadAndApply();
        }

        private void BuildDisplayModeOptions()
        {
            if (!m_displayModeRow?.m_dropdown)
            {
                return;
            }
            m_displayModeRow?.m_dropdown.ClearOptions();
            m_displayModeRow?.m_dropdown.AddOptions(new List<string> { "Exclusive Fullscreen", "Borderless Fullscreen", "Windowed" });
        }

        private void BuildResolutionOptions()
        {
            if (!Resolution)
            {
                return;
            }
            m_availableResolutions = Screen.resolutions;
            var options = new List<string>();
            var seen = new HashSet<string>();
            int currentIndex = 0;
            for (int i = 0; i < m_availableResolutions.Length; i++)
            {
                var r = m_availableResolutions[i];
                string lbl = $"{r.width} x {r.height}";
                if (!seen.Add(lbl))
                {
                    continue;
                }
                options.Add(lbl);
                if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                {
                    currentIndex = options.Count - 1;
                }
            }
            Resolution.ClearOptions();
            Resolution.AddOptions(options);
            Resolution.value = PlayerPrefs.GetInt(m_KeyResolution, currentIndex);
            Resolution.RefreshShownValue();
            SetTemplateHeight(Resolution, 300);
        }

        private void BuildTextureQualityOptions()
        {
            if (!TextureQuality)
            {
                return;
            }
            TextureQuality.ClearOptions();
            TextureQuality.AddOptions(new List<string> { "Very Low", "Low", "Medium", "High" });
        }

        private void BuildAntiAliasingOptions()
        {
            if (!AntiAliasing)
            {
                return;
            }
            AntiAliasing.ClearOptions();
            AntiAliasing.AddOptions(new List<string> { "Disabled", "2x MSAA", "4x MSAA", "8x MSAA" });
        }

        private void BuildShadowQualityOptions()
        {
            if (!ShadowQuality)
            {
                return;
            }
            ShadowQuality.ClearOptions();
            ShadowQuality.AddOptions(new List<string> { "Disabled", "Very Low", "Low", "Medium", "High", "Ultra" });
        }

        private void BuildFPSLimitOptions()
        {
            if (!FpsLimit)
            {
                return;
            }
            FpsLimit.ClearOptions();
            FpsLimit.AddOptions(new List<string> { "30", "60", "120", "144", "240", "Unlimited" });
        }

        private void LoadAndApply()
        {
            Apply(DisplayMode, m_KeyDisplayMode,   1,    ApplyDisplayMode);
            Apply(TextureQuality, m_KeyTextureQuality, 3, ApplyTextureQuality);
            Apply(AntiAliasing, m_KeyAntiAliasing,  1,    ApplyAntiAliasing);
            Apply(ShadowQuality, m_KeyShadowQuality, 5,    ApplyShadowQuality);
            Apply(FpsLimit, m_KeyFPSLimit,      1,    ApplyFPSLimit);

            int resIdx = PlayerPrefs.GetInt(m_KeyResolution, (m_availableResolutions?.Length ?? 1) - 1);
            if (Resolution)
            {
                Resolution.value = resIdx; ApplyResolution(resIdx);
            }

            bool vsync = PlayerPrefs.GetInt(m_KeyVSync, 1) == 1;
            if (VSync)
            {
                VSync.isOn = vsync;
            }
            QualitySettings.vSyncCount = vsync ? 1 : 0;
            SetFPSInteractable(!vsync);

            bool fpsCounter = PlayerPrefs.GetInt(m_KeyFpsCounter, 0) == 1;
            if (FpsCounter)
            {
                FpsCounter.isOn = fpsCounter;
            }
            ApplyFpsCounter(fpsCounter);

            float gamma = Mathf.Clamp(PlayerPrefs.GetFloat(m_KeyGamma, 0f), -2f, 2f);
            if (Gamma)
            {
                Gamma.value = gamma; UpdateSliderLabel(m_gammaRow, gamma, "brightness");
            }
            ApplyGamma(gamma);

            float renderScale = Mathf.Clamp(PlayerPrefs.GetFloat(m_KeyRenderScale, 1f), 0.5f, 2f);
            if (RenderScale)
            {
                RenderScale.value = renderScale; UpdateSliderLabel(m_renderScaleRow, renderScale, "percent");
            }
            ApplyRenderScale(renderScale);

            if (DisplayMode)
            {
                DisplayMode.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyDisplayMode, v); ApplyDisplayMode(v);
                });
            }
            if (Resolution)
            {
                Resolution.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyResolution, v); ApplyResolution(v);
                });
            }
            if (TextureQuality)
            {
                TextureQuality.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyTextureQuality, v); ApplyTextureQuality(v);
                });
            }
            if (AntiAliasing)
            {
                AntiAliasing.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyAntiAliasing, v); ApplyAntiAliasing(v);
                });
            }
            if (ShadowQuality)
            {
                ShadowQuality.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyShadowQuality, v); ApplyShadowQuality(v);
                });
            }
            if (FpsLimit)
            {
                FpsLimit.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyFPSLimit, v); ApplyFPSLimit(v);
                });
            }
            if (VSync)
            {
                VSync.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyVSync, v ? 1 : 0);
                    QualitySettings.vSyncCount = v ? 1 : 0; SetFPSInteractable(!v);
                });
            }
            if (FpsCounter)
            {
                FpsCounter.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetInt(m_KeyFpsCounter, v ? 1 : 0); ApplyFpsCounter(v);
                });
            }
            if (Gamma)
            {
                Gamma.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(m_KeyGamma, v); ApplyGamma(v); UpdateSliderLabel(m_gammaRow, v, "brightness");
                });
            }
            if (RenderScale)
            {
                RenderScale.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(m_KeyRenderScale, v); ApplyRenderScale(v); UpdateSliderLabel(m_renderScaleRow, v, "percent");
                });
            }
        }

        /*
         * @brief Clears all video PlayerPrefs keys and reloads default values.
         */
        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(m_KeyDisplayMode);
            PlayerPrefs.DeleteKey(m_KeyResolution);
            PlayerPrefs.DeleteKey(m_KeyTextureQuality);
            PlayerPrefs.DeleteKey(m_KeyVSync);
            PlayerPrefs.DeleteKey(m_KeyFpsCounter);
            PlayerPrefs.DeleteKey(m_KeyFPSLimit);
            PlayerPrefs.DeleteKey(m_KeyAntiAliasing);
            PlayerPrefs.DeleteKey(m_KeyShadowQuality);
            PlayerPrefs.DeleteKey(m_KeyGamma);
            PlayerPrefs.DeleteKey(m_KeyRenderScale);
            PlayerPrefs.Save();
            OnDisable();
            LoadAndApply();
        }

        private void OnDisable()
        {
            DisplayMode?.onValueChanged.RemoveAllListeners();
            Resolution?.onValueChanged.RemoveAllListeners();
            TextureQuality?.onValueChanged.RemoveAllListeners();
            AntiAliasing?.onValueChanged.RemoveAllListeners();
            ShadowQuality?.onValueChanged.RemoveAllListeners();
            FpsLimit?.onValueChanged.RemoveAllListeners();
            VSync?.onValueChanged.RemoveAllListeners();
            FpsCounter?.onValueChanged.RemoveAllListeners();
            Gamma?.onValueChanged.RemoveAllListeners();
            RenderScale?.onValueChanged.RemoveAllListeners();
        }

        /*
         * @brief Loads a saved int from PlayerPrefs, sets the dropdown value and calls the apply callback.
         * @param _dropdown    Target dropdown to set.
         * @param _key         PlayerPrefs key to read.
         * @param _defaultVal  Fallback value if the key does not exist.
         * @param _applyFn     Callback that actually applies the setting to the engine.
         */
        private void Apply(TMP_Dropdown _dropdown, string _key, int _defaultVal, System.Action<int> _applyFn)
        {
            if (!_dropdown)
            {
                return;
            }
            int val = PlayerPrefs.GetInt(_key, _defaultVal);
            _dropdown.value = val;
            _applyFn(val);
        }

        private void ApplyDisplayMode(int _i)
        {
            Screen.fullScreenMode = _i == 0 ? FullScreenMode.ExclusiveFullScreen
                                  : _i == 1 ? FullScreenMode.FullScreenWindow
                                           : FullScreenMode.Windowed;
            if (Resolution) Resolution.interactable = _i != 1;
        }

        private void ApplyResolution(int _i)
        {
            if (m_availableResolutions == null || _i >= m_availableResolutions.Length) return;
            var r = m_availableResolutions[_i];
            Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
        }

        private void ApplyFPSLimit(int _i)
        {
            int[] limits = { 30, 60, 120, 144, 240, -1 };
            Application.targetFrameRate = _i < limits.Length ? limits[_i] : -1;
        }

        private void SetFPSInteractable(bool _v)
        {
            if (FpsLimit)
            {
                FpsLimit.interactable = _v;
            }
        }

        private void ApplyAntiAliasing(int _i)
        {
            int[] vals = { 1, 2, 4, 8 };
            int samples = _i < vals.Length ? vals[_i] : 1;
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null) urpAsset.msaaSampleCount = samples;
        }

        private void ApplyShadowQuality(int _i)
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            int[] resolutions = { 0, 512, 1024, 2048, 4096, 8192 };
            int res = _i < resolutions.Length ? resolutions[_i] : 0;
            QualitySettings.shadowDistance = res == 0 ? 0f : 50f;
            if (urpAsset != null)
            {
                urpAsset.shadowDistance = res == 0 ? 0f : 50f;
                urpAsset.mainLightShadowmapResolution = res == 0 ? 1 : res;
            }
        }

        private void ApplyTextureQuality(int _i)
        {
            // TODO
        }

        /*
         * @brief Spawns or toggles the persistent FPS counter overlay GameObject.
         * @param _enabled  True to show the counter, false to hide it.
         */
        private static void ApplyFpsCounter(bool _enabled)
        {
            if (_enabled && m_fpsCounter == null)
            {
                m_fpsCounter = new GameObject("[FpsCounter]");
                DontDestroyOnLoad(m_fpsCounter);
                m_fpsCounter.AddComponent<FpsCounter>();
            }
            else if (m_fpsCounter != null)
            {
                m_fpsCounter.SetActive(_enabled);
            }
        }

        private void ApplyGamma(float _v)
        {
            if (m_brightnessCA != null)
            {
                m_brightnessCA.postExposure.Override(_v);
            }
        }

        private void ApplyRenderScale(float _v)
        {
            var a = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (a)
            {
                a.renderScale = _v;
            }
        }

        private static void UpdateSliderLabel(OptionRowSlider _row, float _v, string _format)
        {
            if (_row?.m_valueLabel == null)
            {
                return;
            }
            _row.m_valueLabel.text = _format == "brightness"
                ? (_v >= 0 ? $"+{_v:F1}" : $"{_v:F1}")
                : Mathf.RoundToInt(_v * 100f) + "%";
        }

        private void SetTemplateHeight(TMP_Dropdown _dropdown, float _height)
        {
            if (_dropdown?.template != null)
            {
                _dropdown.template.sizeDelta = new Vector2(_dropdown.template.sizeDelta.x, _height);
            }
        }
    }
}
