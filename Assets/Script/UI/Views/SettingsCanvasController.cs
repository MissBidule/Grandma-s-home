using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using PurrLobby;

/*
 * @brief Wires John's Canvas_Settings prefab (visual only) to the real settings logic.
 * Finds widgets by path in the prefab hierarchy and binds them directly to PlayerPrefs
 * + engine calls. Tabs are Toggles in a ToggleGroup.
 */
public class SettingsCanvasController : MonoBehaviour
{
    public Action OnBack;

    [SerializeField] private InputActionAsset m_inputActions;

    private GameObject m_panelVideo, m_panelAudio, m_panelAccessibility, m_panelControls;
    private Toggle m_tabVideo;
    private readonly List<(Toggle tog, Graphic g, Color normal, Color selected)> m_tabTints = new();
    private static Volume s_brightnessVolume;
    private static ColorAdjustments s_brightnessCA;
    private static GameObject s_fpsCounter;
    private static Volume s_colorblindVolume;
    private static ChannelMixer s_colorblindCM;
    private static bool s_appliedOnce;

    private Resolution[] m_resolutions;

    public static event Action<float> OnSensitivityChanged;

    private void Awake()
    {
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

    private void WireTabs(Transform bg)
    {
        var tabs = bg.Find("Tabs_Container");
        m_tabVideo = tabs.Find("Tab_Video")?.GetComponent<Toggle>();
        BindTab(tabs, "Tab_Video",         m_panelVideo);
        BindTab(tabs, "Tab_Audio",         m_panelAudio);
        BindTab(tabs, "Tab_Accessibility", m_panelAccessibility);
        BindTab(tabs, "Tab_Controls",      m_panelControls);
    }

    private void BindTab(Transform tabs, string name, GameObject panel)
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
        tog.onValueChanged.AddListener(on => { if (on) ShowTab(panel); });
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
        if (back != null) back.onClick.AddListener(() => OnBack?.Invoke());

        var reset = bg.Find("Reset Button")?.GetComponent<Button>();
        if (reset != null) reset.onClick.AddListener(ResetAll);
    }

    public void ResetAll()
    {
        foreach (var k in new[] {
            "Settings_DisplayMode","Settings_Resolution","Settings_TextureQuality","Settings_VSync",
            "Settings_FpsCounter","Settings_FPSLimit","Settings_Gamma","Settings_RenderScale",
            "Settings_VolMaster","Settings_VolMusic","Settings_VolSFX","Settings_InputDevice","Settings_VoiceMode",
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
        foreach (Transform c in p)
        {
            if (SliderOf(c) != null) sliders.Add(c);
            else if (DropdownOf(c) != null) dropdowns.Add(c);
        }
        string[] labels = { "Master Volume", "Music", "Sound Effects" };
        string[] keys   = { "Settings_VolMaster", "Settings_VolMusic", "Settings_VolSFX" };
        for (int i = 0; i < sliders.Count && i < 3; i++) BindVolumeSlider(sliders[i], labels[i], keys[i]);
        if (dropdowns.Count >= 1) BindInputDevice(dropdowns[0]);
        if (dropdowns.Count >= 2) BindVoiceMode(dropdowns[1]);
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
        dd.onValueChanged.AddListener(v => PlayerPrefs.SetInt("Settings_VoiceMode", v));
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

    private static readonly (string map, string action, string label)[] ChildBinds = {
        ("Child","Attack","Attack"), ("Child","Interact","Interact"), ("Child","Jump","Jump"),
        ("Child","Sneak","Sneak"), ("Child","Change_weapon","Change Weapon"), ("Child","Hint","Show Hint"),
    };
    private static readonly (string map, string action, string label)[] GhostBinds = {
        ("Ghost","Interact","Interact"), ("Ghost","Scan","Scan"), ("Ghost","Dash","Dash"),
        ("Ghost","Sneak","Sneak"), ("Ghost","RotatePreviewLeft","Rotate Left"), ("Ghost","RotatePreviewRight","Rotate Right"),
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

    private void BindColumn(Transform column, string header, (string map,string action,string label)[] binds)
    {
        if (column == null) return;
        // Optional header: first Row_Banner/Columns_Banner child with TMP text
        var banner = column.Find("Image") ?? column.Find("Columns_Banner");
        if (banner != null)
        {
            var bt = banner.GetComponentInChildren<TMP_Text>(true);
            if (bt != null) bt.text = header;
        }
        int i = 0;
        foreach (Transform row in column)
        {
            if (row.name.StartsWith("Row_Keybind") == false) continue;
            if (i >= binds.Length) break;
            var b = binds[i]; i++;
            SetRowLabel(row, b.label);
            var btn = row.GetComponentInChildren<Button>(true);
            var btnLbl = btn?.GetComponentInChildren<TMP_Text>(true);
            var action = m_inputActions.FindActionMap(b.map)?.FindAction(b.action);
            if (action == null) continue;
            UpdateKeyLabel(btnLbl, action, 0);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => StartRebind(action, 0, btnLbl));
        }
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
            var go = new GameObject("[ColorblindVolume]");
            DontDestroyOnLoad(go);
            s_colorblindVolume = go.AddComponent<Volume>();
            s_colorblindVolume.isGlobal = true; s_colorblindVolume.priority = 1001f; s_colorblindVolume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            s_colorblindCM = profile.Add<ChannelMixer>(true);
            s_colorblindVolume.profile = profile;
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
                s_colorblindCM.redOutRedIn.Override(95);    s_colorblindCM.redOutGreenIn.Override(5);   s_colorblindCM.redOutBlueIn.Override(0);
                s_colorblindCM.greenOutRedIn.Override(0);   s_colorblindCM.greenOutGreenIn.Override(43);s_colorblindCM.greenOutBlueIn.Override(57);
                s_colorblindCM.blueOutRedIn.Override(0);    s_colorblindCM.blueOutGreenIn.Override(47); s_colorblindCM.blueOutBlueIn.Override(53);
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
