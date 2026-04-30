using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Script.States;
using PurrNet.Voice;
using UI;

/*
 * @brief In-game pause menu backed by prefab UI (Canvas_Pause_Menu + Canvas_Settings).
 * Pressing Escape toggles pause, locks/unlocks the cursor and fires OnPauseChanged.
 * Buttons are wired at runtime by matching their TMP label text.
 */
public class PauseMenuView : MonoBehaviour
{
    public static event System.Action<bool> OnPauseChanged;
    public static PauseMenuView Instance { get; private set; }

    [SerializeField] private GameObject m_pauseCanvasPrefab;
    [SerializeField] private GameObject m_settingsCanvasPrefab;

    private GameObject m_pauseCanvas;
    private GameObject m_settingsCanvas;
    private CanvasGroup m_pauseCanvasGroup;
    private bool m_isPaused;
    private Button m_resumeButton;
    private float m_escapeLockUntil;
    private readonly System.Collections.Generic.List<Canvas> m_hiddenCanvases = new();
    private OutlineSuppressor.Handle m_outlineHandle;

    private void Awake()
    {
        Instance = this;

        m_pauseCanvas = Instantiate(m_pauseCanvasPrefab, transform);
        m_pauseCanvas.name = "Canvas_Pause_Menu";
        m_pauseCanvasGroup = m_pauseCanvas.GetComponent<CanvasGroup>();
        if (m_pauseCanvasGroup == null) m_pauseCanvasGroup = m_pauseCanvas.AddComponent<CanvasGroup>();

        m_settingsCanvas = Instantiate(m_settingsCanvasPrefab, transform);
        m_settingsCanvas.name = "Canvas_Settings";

        WirePauseButtons();
        var sc = m_settingsCanvas.GetComponent<SettingsCanvasController>();
        if (sc != null) sc.OnBack = CloseOptions;
        else WireSettingsButtons();
        try { ApplyOutlineToLabels(m_pauseCanvas); } catch { }
        ForceUIDepthAlways(m_pauseCanvas);
        ForceUIDepthAlways(m_settingsCanvas);

        SetPauseVisible(false);
        m_settingsCanvas.SetActive(false);
    }
    public void InitAudioMode()
    {
        var sc = m_settingsCanvas.GetComponent<SettingsCanvasController>();
        sc.InitAudioKa();
    }


    /*
     * @brief Configures both pause canvases to render through the player camera (so post-process applies)
     * @params Camera cam the player camera that will draw the pause UI
     * @return void
    */
    public void SetCameraForCanvases(Camera cam)
    {
        Canvas pauseCanvas = m_pauseCanvas.GetComponent<Canvas>();
        pauseCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        pauseCanvas.worldCamera = cam;
        pauseCanvas.planeDistance = 0.58f;
        Canvas settingsCanvas = m_settingsCanvas.GetComponent<Canvas>();
        settingsCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        settingsCanvas.worldCamera = cam;
        settingsCanvas.planeDistance = 0.58f;
    }

    /*
     * @brief Forces every UI Graphic in the hierarchy to use ZTest Always so walls do not clip the pause UI
     * @params GameObject root the canvas root whose Graphics will be patched
     * @return void
    */
    private static void ForceUIDepthAlways(GameObject root)
    {
        if (root == null) return;
        var graphics = root.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            if (g == null) continue;
            var src = g.material != null ? g.material : g.defaultMaterial;
            if (src == null) continue;
            var inst = new Material(src);
            inst.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            g.material = inst;
        }
    }

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (m_isPaused && kb != null && kb.escapeKey.wasPressedThisFrame) { OnEscapePressed(); return; }

        var gp = UnityEngine.InputSystem.Gamepad.current;
        if (gp == null) return;
        if (gp.startButton.wasPressedThisFrame) { OnEscapePressed(); return; }
        if (m_isPaused && gp.buttonEast.wasPressedThisFrame) OnEscapePressed();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void OnEscapePressed()
    {
        if (Time.unscaledTime < m_escapeLockUntil) return;
        m_escapeLockUntil = Time.unscaledTime + 0.25f;

        if (m_settingsCanvas != null && m_settingsCanvas.activeSelf)
            CloseOptions();
        else if (m_isPaused)
            Resume();
        else if (!InstanceHandler.TryGetInstance(out EndGameState endGameState) || !endGameState.IsGameOver)
            OpenMenu();
    }

    public void Resume()
    {
        m_isPaused = false;
        SetPauseVisible(false);
        m_settingsCanvas.SetActive(false);
        RestoreOtherCanvases();
        OutlineSuppressor.Release(ref m_outlineHandle);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EventSystem.current?.SetSelectedGameObject(null);
        foreach (var pi in PlayerInput.all) pi.ActivateInput();
        OnPauseChanged?.Invoke(false);
    }

    public void OpenOptions()
    {
        m_pauseCanvas.SetActive(false);
        m_settingsCanvas.SetActive(true);
        if (InputDeviceTracker.IsGamepadActive)
        {
            var first = m_settingsCanvas.GetComponentInChildren<Selectable>(false);
            EventSystem.current?.SetSelectedGameObject(first?.gameObject);
        }
    }

    public void CloseOptions()
    {
        m_settingsCanvas.SetActive(false);
        m_pauseCanvas.SetActive(true);
        SetPauseVisible(true);
        if (InputDeviceTracker.IsGamepadActive)
            EventSystem.current?.SetSelectedGameObject(m_resumeButton?.gameObject);
    }

    public void BackToMenu()
    {
        if (InstanceHandler.TryGetInstance(out EndGameState endGameState))
            endGameState.BackToMenu();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OpenMenu()
    {
        m_isPaused = true;
        HideOtherCanvases();
        m_settingsCanvas.SetActive(false);
        m_pauseCanvas.SetActive(true);
        SetPauseVisible(true);
        m_outlineHandle = OutlineSuppressor.Acquire(m_outlineHandle);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = !InputDeviceTracker.IsGamepadActive;
        EnsureEventSystem();
        foreach (var pi in PlayerInput.all) pi.DeactivateInput();
        if (InputDeviceTracker.IsGamepadActive)
            EventSystem.current?.SetSelectedGameObject(m_resumeButton?.gameObject);
        OnPauseChanged?.Invoke(true);
    }

    private void HideOtherCanvases()
    {
        m_hiddenCanvases.Clear();
        foreach (var c in GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c == null || !c.isRootCanvas) continue;
            if (c.transform.IsChildOf(transform)) continue;
            c.enabled = false;
            m_hiddenCanvases.Add(c);
        }
    }

    private void RestoreOtherCanvases()
    {
        foreach (var c in m_hiddenCanvases) if (c != null) c.enabled = true;
        m_hiddenCanvases.Clear();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            return;
        }
        if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null)
            EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    private void SetPauseVisible(bool visible)
    {
        m_pauseCanvas.SetActive(visible);
        if (m_pauseCanvasGroup != null)
        {
            m_pauseCanvasGroup.alpha = visible ? 1f : 0f;
            m_pauseCanvasGroup.interactable = visible;
            m_pauseCanvasGroup.blocksRaycasts = visible;
        }
    }

    private void WirePauseButtons()
    {
        foreach (var btn in m_pauseCanvas.GetComponentsInChildren<Button>(true))
        {
            var label = GetLabel(btn);
            switch (label)
            {
                case "resume":
                    m_resumeButton = btn;
                    AddClick(btn, Resume);
                    break;
                case "settings":
                    AddClick(btn, OpenOptions);
                    break;
                case "back to menu":
                    AddClick(btn, BackToMenu);
                    break;
                case "quit game":
                    AddClick(btn, QuitGame);
                    break;
                case "guide":
                    AddClick(btn, ShowTutoInfo);
                    break;
            }
        }
    }

    public void ShowTutoInfo()
    {
        var info = FindAnyObjectByType<TutoInfoController>(FindObjectsInactive.Include);
        if (info == null) return;

        SetPauseVisible(false);
        RestoreOtherCanvases();
        info.Show(TutoInfoController.Category.All, null, OpenMenu);
    }

    private void WireSettingsButtons()
    {
        foreach (var btn in m_settingsCanvas.GetComponentsInChildren<Button>(true))
        {
            if (GetLabel(btn) == "back") AddClick(btn, CloseOptions);
        }
    }

    private static void ApplyOutlineToLabels(GameObject root)
    {
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            try
            {
                if (tmp == null || tmp.font == null) continue;
                if (tmp.font.name.Contains("Outline")) continue;
                var mat = tmp.fontMaterial;
                if (mat == null) continue;
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, Color.black);
                mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.2f);
                tmp.UpdateMeshPadding();
            }
            catch { }
        }
    }

    private static string GetLabel(Button btn)
    {
        var tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null) return string.Empty;
        return tmp.text.Replace("\n", " ").Replace("  ", " ").Trim().ToLowerInvariant();
    }

    private static void AddClick(Button btn, System.Action action)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action?.Invoke());
    }
}
