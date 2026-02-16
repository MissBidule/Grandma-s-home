using PurrNet;
using TMPro;
using UnityEngine;

/*
 * @brief  Contains class declaration for InteractPromptUI
 * @details Script that handles text interactions
 */
public class InteractPromptUI : NetworkBehaviour
{
    public static InteractPromptUI m_Instance;

    [SerializeField] private TMP_Text m_promptText;

    private void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        m_Instance = this;
        Hide();
    }

    /**
    @brief      Shows interaction message
    @param      _message: text to show
    @return     void
    */
    public void Show(string _message)
    {
        if (m_promptText == null) return;

        m_promptText.text = _message;
        m_promptText.gameObject.SetActive(true);
    }

    /**
    @brief      Hides interaction message
    @return     void
    */
    public void Hide()
    {
        if (m_promptText == null) return;

        m_promptText.gameObject.SetActive(false);
    }
}
