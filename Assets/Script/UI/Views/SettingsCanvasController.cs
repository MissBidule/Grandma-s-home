using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using PurrLobby;
using System.Linq;

/*
 * @brief Wires John's Canvas_Settings prefab (visual only) to the real settings logic.
 * Finds widgets by path in the prefab hierarchy and binds them directly to PlayerPrefs
 * + engine calls. Tabs are Toggles in a ToggleGroup.
 */
public class SettingsCanvasController : MonoBehaviour
{
    bool AwakeCalled = false;
    public Action OnBack;

    [SerializeField] private InputActionAsset m_inputActions;

    [Header("Main-menu integration (leave null for pause menu)")]
    [SerializeField] private SceneMenuNavigator m_navigator;
    [SerializeField] private CinemachineVirtualCameraBase m_vcamVideo;
    [SerializeField] private CinemachineVirtualCameraBase m_vcamAudio;
    [SerializeField] private CinemachineVirtualCameraBase m_vcamAccessibility;
    [SerializeField] private CinemachineVirtualCameraBase m_vcamControls;
    [SerializeField] private CinemachineVirtualCameraBase m_vcamBack;

    private GameObject m_panelVideo, m_panelAudio, m_panelAccessibility, m_panelControls;
    private Toggle m_tabVideo, m_tabAudio, m_tabAccessibility, m_tabControls;
    private readonly List<(Toggle tog, Graphic g, Color normal, Color selected)> m_tabTints = new();
    private static Volume s_brightnessVolume;
    private static ColorAdjustments s_brightnessCA;
    private static GameObject s_fpsCounter;
    private static Volume s_colorblindVolume;
    private static ChannelMixer s_colorblindCM;
    private static bool s_appliedOnce;

    private Resolution[] m_resolutions;
    private AudioManager audioManager;

    public static event Action<float> OnSensitivityChanged;

    public void Awake()
    {
        if (AwakeCalled) return;
        ForceAwake();
    }

    //NO COMMENT.
    public void ForceAwake()
    {
        AwakeCalled = true;

        //audioManager = FindFirstObjectByType<AudioManager>();
        //if (audioManager != null)
        //{
          //  Debug.Log("SAY HEEEEEEY");
        //}
        //else
        //{
          //  Debug.Log("NOOOOOOO");
        //}

        var bg = transform.Find("Settings_Background");
        if (bg == null) { Debug.LogError("SettingsCanvasController: Settings_Background not found"); return; }

        var content = bg.Find("Scroll View/Viewport/Content");
        m_panelVideo         = content.Find("Panel_Video").gameObject;
        m_panelAudio         = content.Find("Panel_Audio").gameObject;
        m_panelAccessibility = content.Find("Panel_Accessibility").gameObject;
        m_panelControls      = content.Find("Panel_Controls").gameObject;

        EnsureGlobalVolumes();
        WireTabs(bg);
        WireBackReset(bg);
        WireVideo();
        WireAudio();
        WireAccessibility();
        WireControls();
        m_panelVideo.SetActive(true);
        m_panelAudio.SetActive(true);
        m_panelAccessibility.SetActive(true);
        m_panelControls.SetActive(true);
        ApplyOutlineToAllLabels();
        ShowTab(m_panelVideo);

        if (!s_appliedOnce) { s_appliedOnce = true; ApplyAllOnStartup(); }
    }

    // ── Tabs ─────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (m_tabVideo != null && UnityEngine.EventSystems.EventSystem.current != null)
        {
            m_tabVideo.SetIsOnWithoutNotify(true);
            ShowTab(m_panelVideo);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(m_tabVideo.gameObject);
        }
    }

    public void OpenOnTabInt(int tab) => OpenOnTab((SettingsTab)tab);

    // Opens the canvas on a specific tab without triggering the camera-switch
    // listener (used when the diegetic button already drove the camera).
    public void OpenOnTab(SettingsTab tab)
    {
        if (tab == SettingsTab.None) return;
        var (tog, panel) = GetTab(tab);
        if (tog == null || panel == null) return;
        tog.SetIsOnWithoutNotify(true);
        ShowTab(panel);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(tog.gameObject);
    }

    private (Toggle, GameObject) GetTab(SettingsTab tab) => tab switch
    {
        SettingsTab.Video         => (m_tabVideo,         m_panelVideo),
        SettingsTab.Audio         => (m_tabAudio,         m_panelAudio),
        SettingsTab.Accessibility => (m_tabAccessibility, m_panelAccessibility),
        SettingsTab.Controls      => (m_tabControls,      m_panelControls),
        _                         => (null, null),
    };

    private CinemachineVirtualCameraBase VCamFor(SettingsTab tab) => tab switch
    {
        SettingsTab.Video         => m_vcamVideo,
        SettingsTab.Audio         => m_vcamAudio,
        SettingsTab.Accessibility => m_vcamAccessibility,
        SettingsTab.Controls      => m_vcamControls,
        _                         => null,
    };

    private void WireTabs(Transform bg)
    {
        var tabs = bg.Find("Tabs_Container");
        m_tabVideo         = tabs.Find("Tab_Video")?.GetComponent<Toggle>();
        m_tabAudio         = tabs.Find("Tab_Audio")?.GetComponent<Toggle>();
        m_tabAccessibility = tabs.Find("Tab_Accessibility")?.GetComponent<Toggle>();
        m_tabControls      = tabs.Find("Tab_Controls")?.GetComponent<Toggle>();
        BindTab(tabs, "Tab_Video",         m_panelVideo,         SettingsTab.Video);
        BindTab(tabs, "Tab_Audio",         m_panelAudio,         SettingsTab.Audio);
        BindTab(tabs, "Tab_Accessibility", m_panelAccessibility, SettingsTab.Accessibility);
        BindTab(tabs, "Tab_Controls",      m_panelControls,      SettingsTab.Controls);
    }

    private void BindTab(Transform tabs, string name, GameObject panel, SettingsTab tab)
    {
        var t = tabs.Find(name);
        if (t == null) return;
        var tog = t.GetComponent<Toggle>();
        if (tog == null) return;
        var graphic = tog.targetGraphic;
        var colors = tog.colors;
        var normal = colors.normalColor;
        var selected = colors.selectedColor;
        if (graphic != null) m_tabTints.Add((tog, graphic, normal, selected));
        tog.onValueChanged.AddListener(on =>
        {
            if (!on) return;
            ShowTab(panel);
            if (m_navigator != null)
            {
                var cam = VCamFor(tab);
                if (cam != null) m_navigator.SwitchToCamera(cam);
            }
        });
    }

    private void LateUpdate()
    {
        foreach (var t in m_tabTints)
        {
            if (t.tog == null) continue;
            var c = t.tog.colors;
            var want = t.tog.isOn ? t.selected : t.normal;
            if (c.normalColor != want) { c.normalColor = want; t.tog.colors = c; }
        }
    }

    private void ShowTab(GameObject panel)
    {
        m_panelVideo.SetActive(panel == m_panelVideo);
        m_panelAudio.SetActive(panel == m_panelAudio);
        m_panelAccessibility.SetActive(panel == m_panelAccessibility);
        m_panelControls.SetActive(panel == m_panelControls);
    }

    // ── Back / Reset ─────────────────────────────────────────────────────

    private void WireBackReset(Transform bg)
    {
        var back = bg.Find("Back Button")?.GetComponent<Button>();
        if (back != null)
        {
            back.onClick.AddListener(() =>
            {
                if (m_navigator != null && m_vcamBack != null)
                {
                    gameObject.SetActive(false);
                    m_navigator.SwitchToCamera(m_vcamBack);
                }
                OnBack?.Invoke();
            });
        }

        var reset = bg.Find("Reset Button")?.GetComponent<Button>();
        if (reset != null) reset.onClick.AddListener(ResetAll);
    }

    public void ResetAll()
    {
        foreach (var k in new[] {
            "Settings_DisplayMode","Settings_Resolution","Settings_TextureQuality","Settings_VSync",
            "Settings_FpsCounter","Settings_FPSLimit","Settings_Gamma","Settings_RenderScale",
            "Settings_VolMaster","Settings_VolMusic","Settings_VolSFX","Settings_InputDevice","Settings_VoiceMode",
            "Settings_VoiceChatEnabled",
            "Settings_MouseSensitivity","Settings_Colorblind","Settings_ColorblindIntensity",
            "Settings_Keybindings"
        }) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        if (m_inputActions != null) m_inputActions.RemoveAllBindingOverrides();
        // Re-init by reloading active panel
        foreach (Transform child in m_panelVideo.transform) RemoveListeners(child);
        foreach (Transform child in m_panelAudio.transform) RemoveListeners(child);
        foreach (Transform child in m_panelAccessibility.transform) RemoveListeners(child);
        WireVideo(); WireAudio(); WireAccessibility(); WireControls();
        ApplyAllOnStartup();
    }

    private static void RemoveListeners(Transform row)
    {
        foreach (var s in row.GetComponentsInChildren<Slider>(true))   s.onValueChanged.RemoveAllListeners();
        foreach (var t in row.GetComponentsInChildren<Toggle>(true))   t.onValueChanged.RemoveAllListeners();
        foreach (var d in row.GetComponentsInChildren<TMP_Dropdown>(true)) d.onValueChanged.RemoveAllListeners();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void ApplyOutlineToAllLabels()
    {
        foreach (var dd in GetComponentsInChildren<TMP_Dropdown>(true))
        {
            if (dd.GetComponent<RectMask2D>() == null) dd.gameObject.AddComponent<RectMask2D>();
            var tmpl = dd.template;
            if (tmpl != null)
            {
                var sd = tmpl.sizeDelta;
                tmpl.sizeDelta = new Vector2(Mathf.Max(sd.x, 300f), Mathf.Max(sd.y, 200f));
                var item = tmpl.Find("Viewport/Content/Item") as RectTransform;
                if (item != null)
                {
                    var isd = item.sizeDelta;
                    item.sizeDelta = new Vector2(isd.x, Mathf.Max(isd.y, 32f));
                }
            }
        }
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
        {
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            var isDropdownItem = tmp.GetComponentInParent<TMP_Dropdown>() != null && tmp.name.Contains("Item");
            tmp.overflowMode = isDropdownItem ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
            if (tmp == null || tmp.font == null) continue;
            if (tmp.font.name.Contains("Outline")) continue;
            var mat = tmp.fontMaterial;
            if (mat == null) continue;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            tmp.UpdateMeshPadding();
        }
    }

    private static void SetRowLabel(Transform row, string text)
    {
        var tmp = row.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = text;
    }

    private static TMP_Text GetValueLabel(Transform row)
    {
        return row.Find("Text (TMP) (1)")?.GetComponent<TMP_Text>();
    }

    private static Slider       SliderOf(Transform row)   => row.GetComponentInChildren<Slider>(true);
    private static Toggle       ToggleOf(Transform row)   => row.GetComponentInChildren<Toggle>(true);
    private static TMP_Dropdown DropdownOf(Transform row) => row.GetComponentInChildren<TMP_Dropdown>(true);

    // ── VIDEO ────────────────────────────────────────────────────────────

    private void WireVideo()
    {
        var p = m_panelVideo.transform;
        // Visual order: DD, DD, Toggle, DD, Toggle, Slider, DD, Slider
        var children = new List<Transform>();
        foreach (Transform c in p) children.Add(c);

        var dropdowns = new List<Transform>();
        var toggles   = new List<Transform>();
        var sliders   = new List<Transform>();
        foreach (var c in children)
        {
            if (DropdownOf(c) != null) dropdowns.Add(c);
            else if (ToggleOf(c) != null) toggles.Add(c);
            else if (SliderOf(c) != null) sliders.Add(c);
        }

        if (dropdowns.Count >= 1) BindDisplayMode(dropdowns[0]);
        if (dropdowns.Count >= 2) BindResolution(dropdowns[1]);
        if (toggles.Count   >= 1) BindVSync(toggles[0]);
        if (dropdowns.Count >= 3) BindFpsLimit(dropdowns[2]);
        if (toggles.Count   >= 2) BindFpsCounter(toggles[1]);
        if (sliders.Count   >= 1) BindBrightness(sliders[0]);
        if (dropdowns.Count >= 4) BindTextureQuality(dropdowns[3]);
        if (sliders.Count   >= 2) BindRenderScale(sliders[1]);
    }

    private void BindDisplayMode(Transform row)
    {
        SetRowLabel(row, "Display Mode");
        var dd = DropdownOf(row);
        dd.ClearOptions();
        dd.AddOptions(new List<string> { "Exclusive Fullscreen", "Borderless Fullscreen", "Windowed" });
        dd.SetValueWithoutNotify(PlayerPrefs.GetInt("Settings_DisplayMode", 1));
        dd.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_DisplayMode", v);
            Screen.fullScreenMode = v == 0 ? FullScreenMode.ExclusiveFullScreen
                                  : v == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        });
    }

    private void BindResolution(Transform row)
    {
        SetRowLabel(row, "Resolution");
        var dd = DropdownOf(row);
        m_resolutions = Screen.resolutions;
        var opts = new List<string>();
        var seen = new HashSet<string>();
        int cur = 0;
        for (int i = 0; i < m_resolutions.Length; i++)
        {
            var r = m_resolutions[i];
            string s = $"{r.width} x {r.height}";
            if (!seen.Add(s)) continue;
            opts.Add(s);
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height) cur = opts.Count - 1;
        }
        dd.ClearOptions();
        dd.AddOptions(opts);
        int saved = PlayerPrefs.GetInt("Settings_Resolution", cur);
        dd.SetValueWithoutNotify(Mathf.Clamp(saved, 0, opts.Count - 1));
        dd.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_Resolution", v);
            if (v < m_resolutions.Length) { var r = m_resolutions[v]; Screen.SetResolution(r.width, r.height, Screen.fullScreenMode); }
        });
    }

    private void BindVSync(Transform row)
    {
        SetRowLabel(row, "VSync");
        var tog = ToggleOf(row);
        tog.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Settings_VSync", 1) == 1);
        tog.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_VSync", v ? 1 : 0);
            QualitySettings.vSyncCount = v ? 1 : 0;
        });
    }

    private void BindFpsLimit(Transform row)
    {
        SetRowLabel(row, "Max Framerate");
        var dd = DropdownOf(row);
        dd.ClearOptions();
        dd.AddOptions(new List<string> { "30", "60", "120", "144", "240", "Unlimited" });
        int[] lim = { 30, 60, 120, 144, 240, -1 };
        dd.SetValueWithoutNotify(PlayerPrefs.GetInt("Settings_FPSLimit", 1));
        dd.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_FPSLimit", v);
            Application.targetFrameRate = v < lim.Length ? lim[v] : -1;
        });
    }

    private void BindFpsCounter(Transform row)
    {
        SetRowLabel(row, "FPS Counter");
        var tog = ToggleOf(row);
        tog.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Settings_FpsCounter", 0) == 1);
        tog.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_FpsCounter", v ? 1 : 0);
            ApplyFpsCounter(v);
        });
    }

    private void BindBrightness(Transform row)
    {
        SetRowLabel(row, "Brightness");
        var sl = SliderOf(row);
        var lbl = GetValueLabel(row);
        sl.minValue = -2f; sl.maxValue = 2f;
        float v0 = PlayerPrefs.GetFloat("Settings_Gamma", 0f);
        sl.SetValueWithoutNotify(v0);
        if (lbl) lbl.text = (v0 >= 0 ? "+" : "") + v0.ToString("F1");
        sl.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat("Settings_Gamma", v);
            if (s_brightnessCA != null) s_brightnessCA.postExposure.Override(v);
            if (lbl) lbl.text = (v >= 0 ? "+" : "") + v.ToString("F1");
        });
    }

    private void BindTextureQuality(Transform row)
    {
        SetRowLabel(row, "Texture Quality");
        var dd = DropdownOf(row);
        dd.ClearOptions();
        dd.AddOptions(new List<string> { "Very Low", "Low", "Medium", "High" });
        int[] mip = { 3, 2, 1, 0 };
        dd.SetValueWithoutNotify(PlayerPrefs.GetInt("Settings_TextureQuality", 3));
        dd.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_TextureQuality", v);
            QualitySettings.globalTextureMipmapLimit = v < mip.Length ? mip[v] : 0;
        });
    }

    private void BindRenderScale(Transform row)
    {
        SetRowLabel(row, "Render Scale");
        var sl = SliderOf(row);
        var lbl = GetValueLabel(row);
        sl.minValue = 0.5f; sl.maxValue = 2f;
        float v0 = PlayerPrefs.GetFloat("Settings_RenderScale", 1f);
        sl.SetValueWithoutNotify(v0);
        if (lbl) lbl.text = Mathf.RoundToInt(v0 * 100f) + "%";
        sl.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat("Settings_RenderScale", v);
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp) urp.renderScale = v;
            if (lbl) lbl.text = Mathf.RoundToInt(v * 100f) + "%";
        });
    }

    // ── AUDIO ────────────────────────────────────────────────────────────

    private void WireAudio()
    {
        var p = m_panelAudio.transform;
        var sliders = new List<Transform>();
        var dropdowns = new List<Transform>();
        var toggles = new List<Transform>();
        foreach (Transform c in p)
        {
            if (SliderOf(c) != null) sliders.Add(c);
            else if (DropdownOf(c) != null) dropdowns.Add(c);
            else if (ToggleOf(c) != null) toggles.Add(c);
        }
        string[] labels = { "Master Volume", "Music", "Sound Effects" };
        string[] keys   = { "Settings_VolMaster", "Settings_VolMusic", "Settings_VolSFX" };
        for (int i = 0; i < sliders.Count && i < 3; i++) BindVolumeSlider(sliders[i], labels[i], keys[i]);
        if (dropdowns.Count >= 1) BindInputDevice(dropdowns[0]);
        if (dropdowns.Count >= 2) BindVoiceMode(dropdowns[1]);
        if (toggles.Count   >= 1) BindVoiceChatEnabled(toggles[0]);
    }

    private void BindVoiceChatEnabled(Transform row)
    {
        SetRowLabel(row, "Enable Voice Chat");
        var tog = ToggleOf(row);
        tog.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Settings_VoiceChatEnabled", 1) == 1);
        tog.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_VoiceChatEnabled", v ? 1 : 0);
            // Kari
        });
    }

    private void BindVolumeSlider(Transform row, string label, string key)
    {
        SetRowLabel(row, label);
        var sl = SliderOf(row);
        var lbl = GetValueLabel(row);
        sl.minValue = 0f; sl.maxValue = 1f;
        float v0 = PlayerPrefs.GetFloat(key, 1f);
        sl.SetValueWithoutNotify(v0);
        if (lbl) lbl.text = Mathf.RoundToInt(v0 * 100f) + "%";
        sl.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat(key, v);
            if (lbl) lbl.text = Mathf.RoundToInt(v * 100f) + "%";
            if (key == "Settings_VolMaster") AudioVolumeManager.SetMaster(v);
            else if (key == "Settings_VolMusic") AudioVolumeManager.SetMusic(v);
            else if (key == "Settings_VolSFX") AudioVolumeManager.SetSFX(v);
        });
    }

    private void BindInputDevice(Transform row)
    {
        SetRowLabel(row, "Input Device");
        var dd = DropdownOf(row);
        var devs = new List<string>(Microphone.devices);
        if (devs.Count == 0) devs.Add("(none)");
        dd.ClearOptions();
        dd.AddOptions(devs);
        string saved = PlayerPrefs.GetString("Settings_InputDevice", devs[0]);
        int idx = devs.IndexOf(saved);
        if (idx < 0) idx = 0;
        dd.SetValueWithoutNotify(idx);
        dd.onValueChanged.AddListener(v => PlayerPrefs.SetString("Settings_InputDevice", devs[v]));
    }

    private void BindVoiceMode(Transform row)
    {
        SetRowLabel(row, "Microphone Mode");
        var dd = DropdownOf(row);
        dd.ClearOptions();
        dd.AddOptions(new List<string> { "Always On", "Push to Talk", "Disabled" });
        dd.SetValueWithoutNotify(PlayerPrefs.GetInt("Settings_VoiceMode", 0));
        dd.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_VoiceMode", v);
            ApplyAudioMode(v);
        
        });
    }

    // ── ACCESSIBILITY ────────────────────────────────────────────────────

    private void WireAccessibility()
    {
        var p = m_panelAccessibility.transform;
        var sliders = new List<Transform>();
        var dropdowns = new List<Transform>();
        foreach (Transform c in p)
        {
            if (SliderOf(c) != null) sliders.Add(c);
            else if (DropdownOf(c) != null) dropdowns.Add(c);
        }
        if (sliders.Count >= 1) BindSensitivity(sliders[0]);
        if (dropdowns.Count >= 1) BindColorblind(dropdowns[0]);
        if (sliders.Count >= 2) BindColorblindIntensity(sliders[1]);
    }

    private void BindSensitivity(Transform row)
    {
        SetRowLabel(row, "Sensitivity");
        var sl = SliderOf(row);
        var lbl = GetValueLabel(row);
        sl.minValue = 10f; sl.maxValue = 300f;
        float v0 = PlayerPrefs.GetFloat("Settings_MouseSensitivity", 120f);
        sl.SetValueWithoutNotify(v0);
        if (lbl) lbl.text = Mathf.RoundToInt(v0).ToString();
        sl.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat("Settings_MouseSensitivity", v);
            OnSensitivityChanged?.Invoke(v);
            if (lbl) lbl.text = Mathf.RoundToInt(v).ToString();
        });
    }

    private void BindColorblind(Transform row)
    {
        SetRowLabel(row, "Colorblind Mode");
        var dd = DropdownOf(row);
        dd.ClearOptions();
        dd.AddOptions(new List<string> { "Normal", "Deuteranopia", "Protanopia", "Tritanopia" });
        dd.SetValueWithoutNotify(PlayerPrefs.GetInt("Settings_Colorblind", 0));
        dd.onValueChanged.AddListener(v => {
            PlayerPrefs.SetInt("Settings_Colorblind", v);
            ApplyColorblind(v, PlayerPrefs.GetFloat("Settings_ColorblindIntensity", 1f));
        });
    }

    private void BindColorblindIntensity(Transform row)
    {
        SetRowLabel(row, "Colorblind Intensity");
        var sl = SliderOf(row);
        var lbl = GetValueLabel(row);
        sl.minValue = 0f; sl.maxValue = 1f;
        float v0 = PlayerPrefs.GetFloat("Settings_ColorblindIntensity", 1f);
        sl.SetValueWithoutNotify(v0);
        if (lbl) lbl.text = Mathf.RoundToInt(v0 * 100f) + "%";
        sl.onValueChanged.AddListener(v => {
            PlayerPrefs.SetFloat("Settings_ColorblindIntensity", v);
            ApplyColorblind(PlayerPrefs.GetInt("Settings_Colorblind", 0), v);
            if (lbl) lbl.text = Mathf.RoundToInt(v * 100f) + "%";
        });
    }

    // ── CONTROLS ─────────────────────────────────────────────────────────

    // (map, action, compositePart or null for simple, label)
    private static readonly (string map, string action, string part, string label)[] ChildBinds = {
        ("Child","Attack",            null, "Attack"),
        ("Child","Interact",          null, "Interact"),
        ("Child","Jump",              null, "Jump"),
        ("Child","Sneak",             null, "Sneak"),
        ("Child","Change_weapon",     null, "Change Weapon"),
        ("Child","Cancel",            null, "Cancel"),
        ("Child","Leaderboard",       null, "Leaderboard"),
        ("Child","PushToTalk",        null, "Push to Talk"),
        ("Child","Move",              "up",    "Forward"),
        ("Child","Move",              "down",  "Backward"),
        ("Child","Move",              "left",  "Left"),
        ("Child","Move",              "right", "Right"),
    };
    private static readonly (string map, string action, string part, string label)[] GhostBinds = {
        ("Ghost","Interact",            null, "Interact"),
        ("Ghost","Jump",                null, "Jump"),
        ("Ghost","Scan",                null, "Scan"),
        ("Ghost","Dash",                null, "Dash"),
        ("Ghost","Sneak",               null, "Sneak"),
        ("Ghost","RotatePreviewLeft",   null, "Rotate Left"),
        ("Ghost","RotatePreviewRight",  null, "Rotate Right"),
        ("Ghost","Cancel",              null, "Cancel"),
        ("Ghost","Leaderboard",         null, "Leaderboard"),
        ("Ghost","PushToTalk",          null, "Push to Talk"),
        ("Ghost","Move",                "up",    "Forward"),
        ("Ghost","Move",                "down",  "Backward"),
        ("Ghost","Move",                "left",  "Left"),
        ("Ghost","Move",                "right", "Right"),
    };

    // Maps gamepad binding paths to Xbox icon sprite names in Resources/GamepadIconsAsset (TMP sprite asset).
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

    private InputActionRebindingExtensions.RebindingOperation m_rebindOp;

    private void WireControls()
    {
        if (m_inputActions == null) return;
        var p = m_panelControls.transform;
        var colGen = p.Find("Column_General");
        var colSpe = p.Find("Column_Specific");
        BindColumn(colGen, "GHOST", GhostBinds);
        BindColumn(colSpe, "CHILD", ChildBinds);

        // Load overrides
        string json = PlayerPrefs.GetString("Settings_Keybindings", null);
        if (!string.IsNullOrEmpty(json)) m_inputActions.LoadBindingOverridesFromJson(json);
    }

    private void BindColumn(Transform column, string header, (string map,string action,string part,string label)[] binds)
    {
        if (column == null) return;
        // Optional header: first Row_Banner/Columns_Banner child with TMP text
        var banner = column.Find("Image") ?? column.Find("Columns_Banner");
        if (banner != null)
        {
            var bt = banner.GetComponentInChildren<TMP_Text>(true);
            if (bt != null) bt.text = header;
        }

        // Collect existing Row_Keybind children in order; first is the template for cloning.
        var existing = new List<Transform>();
        foreach (Transform child in column)
            if (child.name.StartsWith("Row_Keybind"))
                existing.Add(child);
        if (existing.Count == 0) return;
        var template = existing[0];

        for (int i = 0; i < binds.Length; i++)
        {
            var b = binds[i];
            Transform row = i < existing.Count
                ? existing[i]
                : Instantiate(template, column, false).transform;
            if (i >= existing.Count) row.name = $"Row_Keybind_dyn_{i}";
            row.gameObject.SetActive(true);

            var action = m_inputActions.FindActionMap(b.map)?.FindAction(b.action);
            if (action == null) { row.gameObject.SetActive(false); continue; }

            int bindingIndex = b.part == null
                ? FindKeyboardBindingIndex(action)
                : FindCompositePartIndex(action, b.part);
            if (bindingIndex < 0) { row.gameObject.SetActive(false); continue; }

            SetRowLabel(row, b.label);
            var btn = row.GetComponentInChildren<Button>(true);
            var btnLbl = btn?.GetComponentInChildren<TMP_Text>(true);
            UpdateKeyLabel(btnLbl, action, bindingIndex);
            btn.onClick.RemoveAllListeners();
            int idx = bindingIndex;
            btn.onClick.AddListener(() => StartRebind(action, idx, btnLbl));
            AttachGamepadIcon(btn, action);
        }

        // Hide any leftover rows that aren't used.
        for (int i = binds.Length; i < existing.Count; i++)
            existing[i].gameObject.SetActive(false);
    }

    private static int FindKeyboardBindingIndex(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (b.path != null && b.path.StartsWith("<Keyboard>")) return i;
            if (b.path != null && b.path.StartsWith("<Mouse>") &&
                !b.path.Contains("delta") && !b.path.Contains("position") && !b.path.Contains("scroll"))
                return i;
        }
        return -1;
    }

    private static int FindCompositePartIndex(InputAction action, string partName)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (!b.isPartOfComposite) continue;
            if (b.name != partName) continue;
            if (b.path == null || !b.path.StartsWith("<Keyboard>")) continue;
            return i;
        }
        return -1;
    }

    private static string FindGamepadBindingPath(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (!string.IsNullOrEmpty(b.path) && b.path.StartsWith("<Gamepad>")) return b.path;
        }
        return null;
    }

    private void StartRebind(InputAction action, int bindingIndex, TMP_Text label)
    {
        m_rebindOp?.Cancel();
        if (label) label.text = "...";
        action.Disable();
        m_rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => {
                op.Dispose(); m_rebindOp = null;
                action.Enable();
                UpdateKeyLabel(label, action, bindingIndex);
                PlayerPrefs.SetString("Settings_Keybindings", m_inputActions.SaveBindingOverridesAsJson());
            })
            .OnCancel(op => {
                op.Dispose(); m_rebindOp = null;
                action.Enable();
                UpdateKeyLabel(label, action, bindingIndex);
            });
        m_rebindOp.Start();
    }

    private static void UpdateKeyLabel(TMP_Text label, InputAction action, int idx)
    {
        if (label == null) return;
        label.text = action.GetBindingDisplayString(idx);
    }

    private static readonly Dictionary<string, Sprite> s_SpriteCache = new Dictionary<string, Sprite>();
    private const float c_gamepadIconSize = 80f;

    private static Sprite LoadGamepadSprite(string spriteName)
    {
        if (s_SpriteCache.TryGetValue(spriteName, out var cached)) return cached;
        var sprite = Resources.Load<Sprite>($"XboxIcons/{spriteName}");
        if (sprite != null) s_SpriteCache[spriteName] = sprite;
        return sprite;
    }

    private static void AttachGamepadIcon(Button button, InputAction action)
    {
        if (button == null) return;
        string path = FindGamepadBindingPath(action);
        s_GamepadSprites.TryGetValue(path ?? "", out string spriteName);
        var sprite = spriteName != null ? LoadGamepadSprite(spriteName) : null;

        var btnRT = button.transform as RectTransform;
        var existing = button.transform.Find("GamepadIcon");
        RectTransform iconRT;
        Image img;
        if (existing != null)
        {
            iconRT = (RectTransform)existing;
            img = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
        }
        else
        {
            if (sprite == null) return;
            var go = new GameObject("GamepadIcon", typeof(RectTransform));
            go.transform.SetParent(button.transform, false);
            iconRT = (RectTransform)go.transform;
            img = go.AddComponent<Image>();
        }

        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(1f, 0.5f);
        iconRT.anchoredPosition = new Vector2(-15f, 0f);
        iconRT.sizeDelta = new Vector2(c_gamepadIconSize, c_gamepadIconSize);

        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.enabled = sprite != null;
        iconRT.gameObject.SetActive(sprite != null);
    }

    // ── Apply / Volumes ──────────────────────────────────────────────────

    private static void EnsureGlobalVolumes()
    {
        if (s_brightnessVolume == null)
        {
            var go = new GameObject("[SettingsVolume]");
            DontDestroyOnLoad(go);
            s_brightnessVolume = go.AddComponent<Volume>();
            s_brightnessVolume.isGlobal = true; s_brightnessVolume.priority = 1000f; s_brightnessVolume.weight = 1f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            s_brightnessCA = profile.Add<ColorAdjustments>(true);
            s_brightnessCA.postExposure.Override(0f);
            s_brightnessVolume.profile = profile;
        }
        if (s_colorblindVolume == null)
        {
            var existing = FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(v => v.name == "[ColorblindVolume]");
            if (existing != null)
            {
                s_colorblindVolume = existing;
            }
            else {
                GameObject go = new GameObject("[ColorblindVolume]");
                DontDestroyOnLoad(go);
                s_colorblindVolume = go.AddComponent<Volume>();
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                s_colorblindCM = profile.Add<ChannelMixer>(true);
                s_colorblindVolume.profile = profile;
            }
            s_colorblindVolume.isGlobal = true; 
            s_colorblindVolume.priority = 1001f; 
            s_colorblindVolume.weight = 0f;
        }
    }

    private static void ApplyFpsCounter(bool on)
    {
        if (on && s_fpsCounter == null)
        {
            s_fpsCounter = new GameObject("[FpsCounter]");
            DontDestroyOnLoad(s_fpsCounter);
            s_fpsCounter.AddComponent<FpsCounter>();
        }
        else if (s_fpsCounter != null) s_fpsCounter.SetActive(on);
    }

    private static void ApplyColorblind(int mode, float intensity)
    {
        if (s_colorblindVolume == null || s_colorblindCM == null) return;
        s_colorblindVolume.weight = mode == 0 ? 0f : intensity;
        switch (mode)
        {
            case 1: // Deuteranopia
                s_colorblindCM.redOutRedIn.Override(100);   s_colorblindCM.redOutGreenIn.Override(0);   s_colorblindCM.redOutBlueIn.Override(0);
                s_colorblindCM.greenOutRedIn.Override(70);  s_colorblindCM.greenOutGreenIn.Override(30);s_colorblindCM.greenOutBlueIn.Override(0);
                s_colorblindCM.blueOutRedIn.Override(0);    s_colorblindCM.blueOutGreenIn.Override(30); s_colorblindCM.blueOutBlueIn.Override(70);
                break;
            case 2: // Protanopia
                s_colorblindCM.redOutRedIn.Override(55);    s_colorblindCM.redOutGreenIn.Override(45);  s_colorblindCM.redOutBlueIn.Override(0);
                s_colorblindCM.greenOutRedIn.Override(35);  s_colorblindCM.greenOutGreenIn.Override(65);s_colorblindCM.greenOutBlueIn.Override(0);
                s_colorblindCM.blueOutRedIn.Override(0);    s_colorblindCM.blueOutGreenIn.Override(25); s_colorblindCM.blueOutBlueIn.Override(75);
                break;
            case 3: // Tritanopia
                s_colorblindCM.redOutRedIn.Override(95);    
                s_colorblindCM.redOutGreenIn.Override(5);   
                s_colorblindCM.redOutBlueIn.Override(0);
                s_colorblindCM.greenOutRedIn.Override(0);   
                s_colorblindCM.greenOutGreenIn.Override(43);
                s_colorblindCM.greenOutBlueIn.Override(57);
                s_colorblindCM.blueOutRedIn.Override(0);    
                s_colorblindCM.blueOutGreenIn.Override(47); 
                s_colorblindCM.blueOutBlueIn.Override(53);
                break;
        }
    }

    private void ApplyAudioMode(int mode)
    {
        switch (mode)
        {
            case 0: // proximity
                //Debug.Log("hello proximity");
                if (audioManager == null)
                {
                    foreach(AudioManager obj in FindObjectsByType<AudioManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        audioManager=obj; 
                    }
                }
                if(audioManager!=null){
                    audioManager.MuteGhostByChild();
                    Debug.Log("Player Proximity chat");
                }
                break;
            case 1: // push to talk
                if (audioManager == null)
                {
                    foreach(AudioManager obj in FindObjectsByType<AudioManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        audioManager=obj; 
                    }
                }

                if (audioManager != null)
                {
                    audioManager.InitPushToTalk();
                    Debug.Log("push to talk");
                }

                break;
            case 2: // mute single player
                if (audioManager == null)
                {
                    foreach(AudioManager obj in FindObjectsByType<AudioManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        audioManager=obj; 
                    }
                }
                if (audioManager != null)
                {
                    audioManager.MuteSinglePlayer();
                    Debug.Log("Mute single player");
                }
                break;
        }
    }

    private void ApplyAllOnStartup()
    {
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("Settings_VSync", 1) == 1 ? 1 : 0;
        int[] lim = { 30, 60, 120, 144, 240, -1 };
        int fi = PlayerPrefs.GetInt("Settings_FPSLimit", 1);
        Application.targetFrameRate = fi < lim.Length ? lim[fi] : -1;
        int[] mip = { 3, 2, 1, 0 };
        int ti = PlayerPrefs.GetInt("Settings_TextureQuality", 3);
        QualitySettings.globalTextureMipmapLimit = ti < mip.Length ? mip[ti] : 0;
        if (s_brightnessCA != null) s_brightnessCA.postExposure.Override(PlayerPrefs.GetFloat("Settings_Gamma", 0f));
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp) urp.renderScale = PlayerPrefs.GetFloat("Settings_RenderScale", 1f);
        ApplyFpsCounter(PlayerPrefs.GetInt("Settings_FpsCounter", 0) == 1);
        ApplyColorblind(PlayerPrefs.GetInt("Settings_Colorblind", 0), PlayerPrefs.GetFloat("Settings_ColorblindIntensity", 1f));
    }
}
