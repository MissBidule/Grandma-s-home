using UnityEngine;
using UnityEngine.UI;

/**
@brief       UI for the revive progress bar
@details     Displays a green fill bar during revive. Referenced directly by GhostInteract.
             Attach to a Canvas, assign a root GameObject and a fill Image (Type = Filled, Horizontal)
*/
public class ReviveBarUI : MonoBehaviour
{
    [SerializeField] private GameObject m_barRoot;
    [SerializeField] private Image m_fillImage;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (m_barRoot != null)
            m_barRoot.SetActive(true);
    }

    public void Hide()
    {
        if (m_barRoot != null)
            m_barRoot.SetActive(false);
    }

    public void SetProgress(float _progress)
    {
        if (m_fillImage != null)
            m_fillImage.fillAmount = Mathf.Clamp01(_progress);
    }
}
