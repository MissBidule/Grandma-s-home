using UnityEngine;
using TMPro;

public class TutoCanvas : MonoBehaviour
{
    public static InteractPromptUI m_Instance;
    [SerializeField] private GameObject m_canvasPrefab;
    private GameObject m_currentCanvas;
    private TMP_Text m_promptText;

    public void Show(string _message)
    {
        if (m_currentCanvas == null)
        {
            m_currentCanvas = Instantiate(m_canvasPrefab);

            m_promptText = m_currentCanvas.GetComponentInChildren<TMP_Text>();
        }

        m_promptText.text = _message;
    }

    public void Hide()
    {
        if (m_currentCanvas == null) return;

       m_promptText.text = "";
    }
}
