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

    [SerializeField] private GameObject m_canvasPrefab;
    private GameObject m_currentCanvas;
    private TMP_Text m_promptText;
    private void Awake()
    {
        m_Instance = this;
    }

    /**
    @brief      Shows interaction message
    @param      _message: text to show
    @return     void
    */
    public void Show(string _message)
    {
        if (m_currentCanvas == null)
        {
            m_currentCanvas = Instantiate(m_canvasPrefab);

            m_promptText = m_currentCanvas.GetComponentInChildren<TMP_Text>();
        }

        m_promptText.text = _message;
    }

    /**
    @brief      Hides interaction message
    @return     void
    */
    public void Hide()
    {
        if (m_currentCanvas == null) return;

        Destroy(m_currentCanvas);
        m_currentCanvas = null;
    }
}
