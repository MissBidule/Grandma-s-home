using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class OptionsView : View
    {
        [Header("Panels")]
        [SerializeField] private VideoSettingsPanel         videoPanel;
        [SerializeField] private AudioSettingsPanel         audioPanel;
        [SerializeField] private AccessibilitySettingsPanel accessibilityPanel;
        [SerializeField] private ControlsSettingsPanel      controlsPanel;

        [Header("Tab Buttons")]
        [SerializeField] private Image videoTabImage;
        [SerializeField] private Image audioTabImage;
        [SerializeField] private Image accessTabImage;
        [SerializeField] private Image controlsTabImage;

        [Header("Tab Colors")]
        [SerializeField] private Color activeTabColor   = Color.white;
        [SerializeField] private Color inactiveTabColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        [Header("Reset")]
        [SerializeField] private Button resetButton;

        // Programmatic mode only
        public System.Action OnBack;
        private bool _programmatic;

        private static readonly Color TabActive   = Color.white;
        private static readonly Color TabInactive = new Color(0.55f, 0.55f, 0.55f, 1f);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_programmatic) OnShow();
        }

        // ── Lobby API (serialized-field path) ─────────────────────────────────

        public override void OnShow()
        {
            ShowPanel(videoPanel);
            if (resetButton) resetButton.onClick.AddListener(ResetAll);
        }

        public override void OnHide()
        {
            HideAllPanels();
            if (resetButton) resetButton.onClick.RemoveAllListeners();
        }

        public void ResetAll()
        {
            if (videoPanel)         videoPanel.ResetToDefaults();
            if (audioPanel)         audioPanel.ResetToDefaults();
            if (accessibilityPanel) accessibilityPanel.ResetToDefaults();
            if (controlsPanel)      controlsPanel.ResetToDefaults();
        }

        public void OnVideoTabClicked()    => ShowPanel(videoPanel);
        public void OnAudioTabClicked()    => ShowPanel(audioPanel);
        public void OnAccessTabClicked()   => ShowPanel(accessibilityPanel);
        public void OnControlsTabClicked() => ShowPanel(controlsPanel);

        private void ShowPanel(MonoBehaviour panel)
        {
            if (videoPanel)         videoPanel.gameObject.SetActive(panel == videoPanel);
            if (audioPanel)         audioPanel.gameObject.SetActive(panel == audioPanel);
            if (accessibilityPanel) accessibilityPanel.gameObject.SetActive(panel == accessibilityPanel);
            if (controlsPanel)      controlsPanel.gameObject.SetActive(panel == controlsPanel);

            SetTabActive(videoTabImage,    panel == videoPanel);
            SetTabActive(audioTabImage,    panel == audioPanel);
            SetTabActive(accessTabImage,   panel == accessibilityPanel);
            SetTabActive(controlsTabImage, panel == controlsPanel);
        }

        private void HideAllPanels()
        {
            if (videoPanel)         videoPanel.gameObject.SetActive(false);
            if (audioPanel)         audioPanel.gameObject.SetActive(false);
            if (accessibilityPanel) accessibilityPanel.gameObject.SetActive(false);
            if (controlsPanel)      controlsPanel.gameObject.SetActive(false);
        }

        private void SetTabActive(Image img, bool active)
        {
            if (img) img.color = active ? activeTabColor : inactiveTabColor;
        }

        // ── Programmatic API (in-game pause path) ─────────────────────────────

        public void Initialize(OptionRowDropdown dropdownPrefab, OptionRowToggle togglePrefab,
                               OptionRowSlider sliderPrefab, OptionRowButton buttonPrefab,
                               OptionRowKeybinding keybindingPrefab, OptionSectionTitle sectionTitlePrefab)
        {
            _programmatic = true;
            BuildLayout(dropdownPrefab, togglePrefab, sliderPrefab, buttonPrefab, keybindingPrefab, sectionTitlePrefab);
        }

        private void BuildLayout(OptionRowDropdown dropdownPrefab, OptionRowToggle togglePrefab,
                                 OptionRowSlider sliderPrefab, OptionRowButton buttonPrefab,
                                 OptionRowKeybinding keybindingPrefab, OptionSectionTitle sectionTitlePrefab)
        {
            var panel    = MakeGO("Panel", transform);
            panel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);
            var pr = panel.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(60f,  40f);
            pr.offsetMax = new Vector2(-60f, -40f);

            // ── Title ──────────────────────────────────────────────────────────
            var titleGO = MakeGO("Title", panel.transform);
            FillAnchored(titleGO, new Vector2(0f, 1f), new Vector2(1f, 1f),
                         new Vector2(10f, -60f), new Vector2(-10f, -10f));
            var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
            titleTxt.text      = "SETTINGS";
            titleTxt.fontSize  = 26;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color     = Color.white;

            // ── Tab bar ────────────────────────────────────────────────────────
            var tabBar = MakeGO("TabBar", panel.transform);
            FillAnchored(tabBar, new Vector2(0f, 1f), new Vector2(1f, 1f),
                         new Vector2(0f, -115f), new Vector2(0f, -65f));
            var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 2;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            tabBar.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);

            videoTabImage    = MakeTabButton(tabBar.transform, "Video",         () => ShowPanel(videoPanel));
            audioTabImage    = MakeTabButton(tabBar.transform, "Audio",         () => ShowPanel(audioPanel));
            accessTabImage   = MakeTabButton(tabBar.transform, "Accessibility", () => ShowPanel(accessibilityPanel));
            controlsTabImage = MakeTabButton(tabBar.transform, "Controls",      () => ShowPanel(controlsPanel));

            // ── Content area ───────────────────────────────────────────────────
            var content = MakeGO("Content", panel.transform);
            FillAnchored(content, new Vector2(0f, 0f), new Vector2(1f, 1f),
                         new Vector2(0f, 60f), new Vector2(0f, -115f));

            videoPanel         = CreateVideoPanel(content.transform, dropdownPrefab, togglePrefab, sliderPrefab, buttonPrefab, sectionTitlePrefab);
            audioPanel         = CreateAudioPanel(content.transform, sliderPrefab, dropdownPrefab);
            accessibilityPanel = CreateAccessibilityPanel(content.transform, dropdownPrefab, togglePrefab, sliderPrefab);
            controlsPanel      = CreateControlsPanel(content.transform, keybindingPrefab, sectionTitlePrefab);

            // ── Back button ────────────────────────────────────────────────────
            var backGO = MakeGO("BackButton", panel.transform);
            FillAnchored(backGO, new Vector2(0f, 0f), new Vector2(0f, 0f),
                         new Vector2(20f, 10f), new Vector2(180f, 50f));
            StyleButton(backGO, new Color(0.25f, 0.25f, 0.30f, 1f),
                                new Color(0.35f, 0.35f, 0.40f, 1f),
                                new Color(0.18f, 0.18f, 0.22f, 1f));
            backGO.GetComponent<Button>().onClick.AddListener(() => OnBack?.Invoke());
            AddLabel(backGO.transform, "< Back", 18);

            // ── Reset button ───────────────────────────────────────────────────
            var resetGO = MakeGO("ResetButton", panel.transform);
            FillAnchored(resetGO, new Vector2(1f, 0f), new Vector2(1f, 0f),
                         new Vector2(-180f, 10f), new Vector2(-20f, 50f));
            StyleButton(resetGO, new Color(0.35f, 0.18f, 0.08f, 1f),
                                 new Color(0.45f, 0.28f, 0.12f, 1f),
                                 new Color(0.25f, 0.10f, 0.04f, 1f));
            resetGO.GetComponent<Button>().onClick.AddListener(ResetAll);
            AddLabel(resetGO.transform, "Reset", 18);
        }

        // ── Panel creators ────────────────────────────────────────────────────

        private VideoSettingsPanel CreateVideoPanel(Transform parent,
            OptionRowDropdown dropdown, OptionRowToggle toggle, OptionRowSlider slider,
            OptionRowButton button, OptionSectionTitle sectionTitle)
        {
            var go = MakeGO("VideoPanel", parent);
            go.SetActive(false);
            FillStretch(go);
            BuildScrollView(go.transform);
            var p = go.AddComponent<VideoSettingsPanel>();
            p.Initialize(dropdown, toggle, slider, button, sectionTitle);
            return p;
        }

        private AudioSettingsPanel CreateAudioPanel(Transform parent, OptionRowSlider slider, OptionRowDropdown dropdown)
        {
            var go = MakeGO("AudioPanel", parent);
            go.SetActive(false);
            FillStretch(go);
            BuildScrollView(go.transform);
            var p = go.AddComponent<AudioSettingsPanel>();
            p.Initialize(slider, dropdown);
            return p;
        }

        private AccessibilitySettingsPanel CreateAccessibilityPanel(Transform parent,
            OptionRowDropdown dropdown, OptionRowToggle toggle, OptionRowSlider slider)
        {
            var go = MakeGO("AccessibilityPanel", parent);
            go.SetActive(false);
            FillStretch(go);
            BuildScrollView(go.transform);
            var p = go.AddComponent<AccessibilitySettingsPanel>();
            p.Initialize(dropdown, toggle, slider);
            return p;
        }

        private ControlsSettingsPanel CreateControlsPanel(Transform parent,
            OptionRowKeybinding keybinding, OptionSectionTitle sectionTitle)
        {
            var go = MakeGO("ControlsPanel", parent);
            go.SetActive(false);
            FillStretch(go);
            BuildScrollView(go.transform);
            var p = go.AddComponent<ControlsSettingsPanel>();
            p.Initialize(keybinding, sectionTitle);
            return p;
        }

        // ── ScrollView builder ────────────────────────────────────────────────

        private void BuildScrollView(Transform parent)
        {
            var sv = MakeGO("Scroll View", parent);
            FillStretch(sv);
            var sr       = sv.AddComponent<ScrollRect>();
            sr.horizontal   = false;
            sr.vertical     = true;
            sr.movementType = ScrollRect.MovementType.Elastic;

            var vp = MakeGO("Viewport", sv.transform);
            FillStretch(vp);
            vp.AddComponent<RectMask2D>();
            sr.viewport = vp.GetComponent<RectTransform>();

            var ct     = MakeGO("Content", vp.transform);
            var ctRect = ct.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0f, 1f);
            ctRect.anchorMax = new Vector2(1f, 1f);
            ctRect.pivot     = new Vector2(0.5f, 1f);
            ctRect.offsetMin = Vector2.zero;
            ctRect.offsetMax = Vector2.zero;

            var vlg = ct.AddComponent<VerticalLayoutGroup>();
            vlg.padding                = new RectOffset(10, 10, 8, 8);
            vlg.spacing                = 4;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;

            ct.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = ctRect;
        }

        // ── Widget helpers ────────────────────────────────────────────────────

        private Image MakeTabButton(Transform parent, string label, System.Action onClick)
        {
            var go  = MakeGO("Tab_" + label, parent);
            var img = go.AddComponent<Image>();
            img.color = TabInactive;
            var btn = go.AddComponent<Button>();
            var bc  = btn.colors;
            bc.highlightedColor = new Color(0.45f, 0.45f, 0.50f, 1f);
            bc.pressedColor     = new Color(0.30f, 0.30f, 0.35f, 1f);
            btn.colors = bc;
            btn.onClick.AddListener(() => onClick?.Invoke());
            AddLabel(go.transform, label, 18);
            return img;
        }

        private void StyleButton(GameObject go, Color normal, Color hover, Color pressed)
        {
            go.AddComponent<Image>().color = normal;
            var btn = go.AddComponent<Button>();
            var bc  = btn.colors;
            bc.highlightedColor = hover;
            bc.pressedColor     = pressed;
            btn.colors = bc;
        }

        private void AddLabel(Transform parent, string text, int size)
        {
            var go  = MakeGO("Label", parent);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ── RectTransform helpers ─────────────────────────────────────────────

        private static GameObject MakeGO(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void FillStretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void FillAnchored(GameObject go,
                                         Vector2 anchorMin, Vector2 anchorMax,
                                         Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
