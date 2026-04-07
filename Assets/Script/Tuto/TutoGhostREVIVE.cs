using UnityEngine;

// C ce fichier

public class TutoGhostREVIVE : MonoBehaviour
{
    [SerializeField] private GameObject m_barRevive;

    void Awake()
    {
        Hide();
    }

    public void Hide()
    {
        if (m_barRevive != null)
            m_barRevive.SetActive(false);
    }

    public void Show()
    {
        if (m_barRevive != null)
            m_barRevive.SetActive(true);
    }
}
