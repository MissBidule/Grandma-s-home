using PurrNet;
using UnityEngine;

public class GhostStatus : NetworkBehaviour
{
    public bool hitPlayer = false;

    public bool m_isSlowed = false;
    public bool m_isStopped = false;
    public float m_timerSlowed = 5f;
    public float m_timerStop = 5f;
    public float m_currentTimerSlowed = 5f;
    public float m_currentTimerStop = 5f;


    [Header("Canva")]
    [SerializeField] public GameObject m_stopped;
    [SerializeField] public GameObject m_slowed;

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }
    
    /**
    @brief      Apply slow effect from projectile hit
    */
    [ServerRpc(requireOwnership:false)]
    public void GotHitByProjectile()
    {
        if (hitPlayer) return;
        m_isSlowed = true;
        m_slowed.SetActive(true);
        m_currentTimerSlowed = m_timerSlowed;
    }

    /**
    @brief      Apply stop effect from close combat hit
    */
    [ObserversRpc(runLocally:true)]
    public void GotHitByCac()
    {
        if (hitPlayer) return;
        m_isStopped = true;
        m_stopped.SetActive(true);
        m_currentTimerStop = m_timerStop;
    }

}
