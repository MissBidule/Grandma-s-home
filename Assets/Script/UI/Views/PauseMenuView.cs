using PurrLobby;
using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
 * @brief In-game pause menu built entirely at runtime (no prefab required).
 * Pressing Escape toggles pause, locks/unlocks the cursor and fires OnPauseChanged.
 * Hosts an OptionsView sub-panel; pressing Escape while the options panel is open
 * closes it and returns to the main pause panel instead of resuming.
 */
public class PauseMenuView : MonoBehaviour
{
    public static event System.Action<bool> OnPauseChanged;
    [Header("Option Row Prefabs")]
    [UnityEngine.Serialization.FormerlySerializedAs("dropdownRowPrefab")]
    [SerializeField] private OptionRowDropdown m_dropdownRowPrefab;
    [UnityEngine.Serialization.FormerlySerializedAs("toggleRowPrefab")]
    [SerializeField] private OptionRowToggle m_toggleRowPrefab;
    [UnityEngine.Serialization.FormerlySerializedAs("sliderRowPrefab")]
    [SerializeField] private OptionRowSlider m_sliderRowPrefab;
    [UnityEngine.Serialization.FormerlySerializedAs("buttonRowPrefab")]
    [SerializeField] private OptionRowButton m_buttonRowPrefab;
    [UnityEngine.Serialization.FormerlySerializedAs("keybindingRowPrefab")]
    [SerializeField] private OptionRowKeybinding m_keybindingRowPrefab;
    [UnityEngine.Serialization.FormerlySerializedAs("sectionTitlePrefab")]
    [SerializeField] private OptionSectionTitle m_sectionTitlePrefab;

    private Canvas m_canvas;
    private CanvasGroup m_canvasGroup;
    private GameObject m_mainPanel;
    private OptionsView m_optionsPanel;
    private bool m_isPaused;

    private void Awake()
    {
        BuildCanvas();
        m_optionsPanel = CreateOptionsPanel();
        BuildMainPanel();
        SetVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }
        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (m_optionsPanel.gameObject.activeSelf)
        {
            CloseOptions();
        }
        else if (m_isPaused)
        {
            Resume();
        }
        else
        {
            OpenMenu();
        }
    }

    /*
     * @brief Closes the pause menu, re-locks the cursor and resumes gameplay.
     */
    public void Resume()
    {
        m_isPaused = false;
        SetVisible(false);
        m_optionsPanel.gameObject.SetActive(false);
        m_mainPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        OnPauseChanged?.Invoke(false);
    }

    /*
     * @brief Hides the main panel and shows the OptionsView sub-panel.
     */
    public void OpenOptions()
    {
        m_mainPanel.SetActive(false);
        m_optionsPanel.gameObject.SetActive(true);
    }

    /*
     * @brief Hides the OptionsView sub-panel and returns to the main pause panel.
     */
    public void CloseOptions()
    {
        m_optionsPanel.gameObject.SetActive(false);
        m_mainPanel.SetActive(true);
    }

    /*
     * @brief Quits the application (no save prompt).
     */
    public void QuitGame()
    {
        var networkManager = InstanceHandler.GetInstance<NetworkManager>();
        Application.Quit();
    }

    private void OpenMenu()
    {
        m_isPaused = true;
        m_optionsPanel.gameObject.SetActive(false);
        m_mainPanel.SetActive(true);
        SetVisible(true);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        OnPauseChanged?.Invoke(true);
    }

    private void SetVisible(bool _visible)
    {
        m_canvasGroup.alpha = _visible ? 1f : 0f;
        m_canvasGroup.interactable = _visible;
        m_canvasGroup.blocksRaycasts = _visible;
    }

    /*
     * @brief Creates the Canvas, CanvasScaler, GraphicRaycaster, CanvasGroup and semi-transparent overlay at runtime.
     */
    private void BuildCanvas()
    {
        m_canvas = gameObject.AddComponent<Canvas>();
        m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        m_canvasGroup = gameObject.AddComponent<CanvasGroup>();

        var overlay  = new GameObject("Overlay");
        overlay.transform.SetParent(transform, false);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.65f);
        var overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
    }

    /*
     * @brief Instantiates the OptionsView sub-panel as a child of this Canvas and injects prefab references.
     * @return The OptionsView component added to the new panel GameObject.
     */
    private OptionsView CreateOptionsPanel()
    {
        var go = new GameObject("OptionsPanel", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var panel = go.AddComponent<OptionsView>();
        panel.Initialize(m_dropdownRowPrefab, m_toggleRowPrefab, m_sliderRowPrefab,
                         m_buttonRowPrefab, m_keybindingRowPrefab, m_sectionTitlePrefab);
        panel.OnBack = CloseOptions;
        go.SetActive(false);
        return panel;
    }

    /*
     * @brief Builds the main pause panel with a title and Resume / Options / Quit buttons at runtime.
     */
    private void BuildMainPanel()
    {
        m_mainPanel = new GameObject("MainPanel");
        m_mainPanel.transform.SetParent(transform, false);

        var bg = m_mainPanel.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.13f, 0.16f, 1f);

        var rect = m_mainPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(340f, 300f);

        var title = CreateText(m_mainPanel.transform, "PAUSE", 32, FontStyles.Bold);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(0f, -70f);
        titleRect.offsetMax = new Vector2(0f, -20f);

        CreateButton(m_mainPanel.transform, "Resume",   new Vector2(0f,  80f), Resume);
        CreateButton(m_mainPanel.transform, "Options",  new Vector2(0f,  10f), OpenOptions);
        CreateButton(m_mainPanel.transform, "Quit",     new Vector2(0f, -60f), QuitGame,
                     new Color(0.72f, 0.18f, 0.18f));
    }

    /*
     * @brief Creates a TextMeshProUGUI label as a child of the given transform.
     * @param parent  Parent transform to attach the label to.
     * @param text    String to display.
     * @param size    Font size in points.
     * @param style   Font style flags (bold, italic, etc.).
     * @return The TMP_Text component of the newly created GameObject.
     */
    private TMP_Text CreateText(Transform parent, string text, int size,
                                FontStyles style = FontStyles.Normal)
    {
        var go  = new GameObject("Text_" + text);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    /*
     * @brief Creates a styled UI button as a child of the given transform.
     * @param parent        Parent transform to attach the button to.
     * @param label         Text displayed on the button.
     * @param anchoredPos   Anchored position relative to the parent's centre.
     * @param onClick       Callback invoked when the button is clicked.
     * @param bgColor       Optional background colour override; defaults to the standard grey.
     */
    private void CreateButton(Transform parent, string label, Vector2 anchoredPos,
                              System.Action onClick, Color? bgColor = null)
    {
        var go  = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor ?? new Color(0.25f, 0.25f, 0.30f, 1f);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(260f, 50f);

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.40f, 1f);
        colors.pressedColor = new Color(0.18f, 0.18f, 0.22f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txt = CreateText(go.transform, label, 20);
        var txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
    }
}
