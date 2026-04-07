using UnityEngine;
using UnityEngine.UI;

public class TutoREANIM : MonoBehaviour
{
    [SerializeField] private GameObject m_barRoot;
    [SerializeField] private Image m_fillImage;
    private bool m_reviveUIActive = false;
    private float m_reviveDuration = 0f;
    private GhostController m_ghostController;
    private float m_reviveTimer = 0f;
    private ReviveBarUI m_reviveBarUI;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ReanimGhost();
        }
        if (Input.GetMouseButtonUp(0))
        {
            CancelReanimGhost();
        }
    }
    void Awake()
    {
        m_reviveUIActive = true;
        m_reviveDuration = m_ghostController.m_reviveDuration;
        m_reviveTimer = 0f;
        if (m_reviveBarUI != null) { m_reviveBarUI.SetProgress(0f); m_reviveBarUI.Show();}
    }

    public void ReanimGhost()
    {
        
    }

    public void CompleteReanimGhost()
    {
        
    }
    
    public void CancelReanimGhost()
    {
        
    }
}
