using UnityEngine;

public class TutoCAC : MonoBehaviour
{
    public GameObject m_deathIcon;

    public void ShowObject()
    {
        if (m_deathIcon != null)
        {
            m_deathIcon.SetActive(true);
        }
    }
}
