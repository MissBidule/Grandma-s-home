using System;
using PurrNet;
using TMPro;
using UnityEngine;

/*
 * @brief  Contains class declaration for InteractPromptUI
 * @details Script that handles text interactions
 */
public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI m_Instance;

    //[SerializeField] private TMP_Text m_promptText;

    [SerializeField] private GameObject m_infoPrefab;
    [SerializeField] private GameObject m_canvasPrefab;
    private InfoPromptUI m_currentInfo;
    private GameObject m_currentCanvas;
    private TMP_Text m_promptText;

    // Stored factory to rebuild the prompt when the input device changes
    private Func<string> m_promptFactory;

    private void Awake()
    {
        m_Instance = this;
        if (m_currentInfo == null)
        {
            m_currentInfo = Instantiate(m_infoPrefab).GetComponent<InfoPromptUI>();
        }
    }

    private void OnEnable()  => InputDeviceTracker.OnDeviceChanged += RefreshPrompt;
    private void OnDisable() => InputDeviceTracker.OnDeviceChanged -= RefreshPrompt;

    private void RefreshPrompt()
    {
        if (m_promptFactory == null || m_promptText == null) return;
        m_promptText.text = m_promptFactory();
    }

    /**
    @brief      Shows a static interaction message (not refreshed on device change).
    @param      _message: text to show
    @return     void
    */
    public void Show(string _message)
    {
        EnsureCanvas();
        m_promptFactory = null;
        m_promptText.text = _message;
    }

    /**
    @brief      Shows a dynamic interaction message built from a factory.
                Automatically refreshes when the input device changes.
    @param      _factory: delegate that returns the prompt string
    @return     void
    */
    public void ShowDynamic(Func<string> _factory)
    {
        EnsureCanvas();
        m_promptFactory = _factory;
        m_promptText.text = _factory();
    }

    private void EnsureCanvas()
    {
        if (m_currentCanvas != null) return;
        m_currentCanvas = Instantiate(m_canvasPrefab);
        m_promptText = m_currentCanvas.GetComponentInChildren<TMP_Text>();
    }

    /**
    @brief      Hides interaction message
    @return     void
    */
    public void Hide()
    {
        if (m_currentCanvas == null) return;
        m_promptFactory = null;
        m_promptText.text = "";
    }

    public void ShowSabotage(string _ghostName)
    {
        m_currentInfo.GhostSabotage(_ghostName);
    }

    public void ShowRepair(string _childName)
    {
        m_currentInfo.ChildRepair(_childName);
    }

    public void ShowKill(string _childName, string _ghostName)
    {
        m_currentInfo.ChildCaughtGhost(_childName, _ghostName);
    }

    public void ShowRes(string _ghostSavior, string _ghostSaved)
    {
        m_currentInfo.GhostResGhost(_ghostSavior, _ghostSaved);
    }
}
