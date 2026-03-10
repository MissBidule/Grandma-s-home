using UnityEngine;
using PurrNet;

public class BrokeDecor : NetworkBehaviour
{
    [Header("State Meshes")]
    [SerializeField] private GameObject m_normalMesh;
    [SerializeField] private GameObject m_brokenMesh;

    public bool m_isBroken;


    public void Broke()
    {
        m_isBroken = true;
        ApplyState();
        Debug.Log("hello there");
    }

    private void ApplyState()
    {
        Debug.Log("SO?????");
        if (m_normalMesh != null)
        {
            var r = m_normalMesh.GetComponent<Renderer>();
            if (r != null)
                r.enabled = !m_isBroken;

            var c = m_normalMesh.GetComponent<Collider>();
            if (c != null)
                c.enabled = !m_isBroken;
        }
        

        if (m_brokenMesh != null)
        {
            var r = m_brokenMesh.GetComponent<Renderer>();
            if (r != null)
                r.enabled = m_isBroken;
                
            var c = m_brokenMesh.GetComponent<Collider>();
            if (c != null)
                c.enabled = m_isBroken;
        }
    }
}
