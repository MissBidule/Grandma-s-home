/*
 * @brief  Contains class to break Decor
 * @details The script causes objects to be broken, and only server adds monetary value to Broke's score.
 */
using UnityEngine;
using PurrNet;

public class BrokeDecor : NetworkBehaviour
{
    [Header("State Meshes")]
    [SerializeField] private GameObject m_normalMesh;
    [SerializeField] private GameObject m_brokenMesh;

    [Header("Score")]
    [SerializeField] private int m_scoreValue = 50;

    public bool m_isBroken;
    public bool m_alreadyBroken=false;

    [ObserversRpc(runLocally:true, requireServer:true)]
    public void Broke()
    {
        m_isBroken = true;
        ApplyState();
    }


    private void ApplyState(RPCInfo info = default)
    {
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
        if (isServer) return;
        if (m_alreadyBroken != true){
            if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                scoreManager.AddPointBroken(info.sender,m_scoreValue);
                m_alreadyBroken = true;
            }
        }
    }
}
