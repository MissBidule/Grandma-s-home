using UnityEngine;

public class TutoGhostREVIVE : MonoBehaviour
{
    public ReviveBarUI m_reviveBarUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        m_reviveBarUI.SetProgress(1f); m_reviveBarUI.Show();
    }
    void Awake()
    {
        m_reviveBarUI.Show();
        Debug.Log("why");
    }
}
