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
    [SerializeField] private List<GameObject> m_additionalMeshes = new();
    [SerializeField] private GameObject m_brokenPrefab;
    private GameObject m_brokenInstance;

    [Header("Score")]
    [SerializeField] private int m_scoreValue = 50;
    
    [Header("Sound")]
    [SerializeField] private NetworkAudioSource m_breakAudioSource;

    public bool m_isBroken;
    public bool m_alreadyBroken=false;

    public void Start()
    {
        m_breakAudioSource = gameObject.GetComponent<NetworkAudioSource>();
        if (m_brokenPrefab != null) {
            m_brokenInstance = Instantiate(m_brokenPrefab, transform.position, transform.rotation);
            m_brokenInstance.GetComponent<Renderer>().enabled = false;
            m_brokenInstance.GetComponent<Collider>().enabled = false;
        }
    }

    [ObserversRpc(runLocally:true)]
    public void Broke()
    {
        if (!m_isBroken)
            FloatingDamageText.Spawn(transform.position, m_scoreValue);
        m_isBroken = true;
        if (m_breakAudioSource != null && m_breakAudioSource.clip != null)
            m_breakAudioSource.Play();
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

        if (m_brokenInstance != null)
        {
            r = m_brokenInstance.GetComponent<Renderer>();
            if (r != null)
                r.enabled = m_isBroken;
                
            c = m_brokenInstance.GetComponent<Collider>();
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
