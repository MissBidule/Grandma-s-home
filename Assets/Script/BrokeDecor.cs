/*
 * @brief  Contains class to break Decor
 * @details The script causes objects to be broken, and only server adds monetary value to Broke's score.
 */
using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public class BrokeDecor : NetworkBehaviour
{
    [Header("State Meshes")]
    [SerializeField] private GameObject m_brokenMesh = null;
    [SerializeField] private List<GameObject> m_additionalMeshes = new();
    [SerializeField] private GameObject m_brokenPrefab;

    [Header("Score")]
    [SerializeField] private int m_scoreValue = 50;

    public bool m_isBroken;
    public bool m_alreadyBroken=false;

    public void Start()
    {
        if (m_brokenPrefab != null) {
            m_brokenMesh = UnityProxy.Instantiate(m_brokenPrefab, transform);
            m_brokenMesh.transform.localPosition = Vector3.zero;
            m_brokenMesh.GetComponent<MeshRenderer>().enabled = false;
            m_brokenMesh.GetComponent<MeshCollider>().enabled = false;
        }
    }

    [ObserversRpc(runLocally:true)]
    public void Broke()
    {
        if (!m_isBroken)
            FloatingDamageText.Spawn(transform.position, m_scoreValue);
        m_isBroken = true;
        ApplyState();
    }


    private void ApplyState(RPCInfo info = default)
    {
        var r = GetComponent<Renderer>();
        if (r != null)
            r.enabled = !m_isBroken;

        var c = GetComponent<Collider>();
        if (c != null)
            c.enabled = !m_isBroken;

        if (m_additionalMeshes.Count > 0)
        {
            foreach (var m in m_additionalMeshes)
            {
                r = m.GetComponent<Renderer>();
                if (r != null)
                    r.enabled = !m_isBroken;
                
                c = m.GetComponent<Collider>();
                if (c != null)
                    c.enabled = !m_isBroken;
            }
        }

        if (m_brokenMesh != null)
        {
            r = m_brokenMesh.GetComponent<Renderer>();
            if (r != null)
                r.enabled = m_isBroken;
                
            c = m_brokenMesh.GetComponent<Collider>();
            if (c != null)
                c.enabled = m_isBroken;
        }
        if (!isServer) return;
        if (m_alreadyBroken != true){
            if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                scoreManager.AddPointBroken(info.sender,m_scoreValue);
                m_alreadyBroken = true;
            }
        }
    }
}
