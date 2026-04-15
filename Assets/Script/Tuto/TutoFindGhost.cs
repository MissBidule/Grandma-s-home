using UnityEngine;

/*
     * @brief  Contains class declaration for Finding a hiding ghost in the Tuto
     */
     
public class TutoFindGhost : MonoBehaviour
{
    public GameObject m_ghost;
    public MeshRenderer m_meshrender;
    public MeshCollider m_meshcollider;

    public void FindGhost()
    {
        if (m_ghost != null && m_meshcollider != null && m_meshrender != null)
        {
            m_ghost.SetActive(true);
            m_meshrender.enabled=false;
            m_meshcollider.enabled=false;
        }
    }
}
