using PurrNet;
using UnityEngine;

/*
 * @brief  Contains class declaration for GhostStatus
 * @details Script that is always visible and can be called by anyone to apply status on ghost
 */
public class GhostStatus : NetworkBehaviour
{
    public bool m_hitPlayer = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }
    
    /**
    @brief      Apply slow effect from projectile hit
    */
    //can be called by anyone
    [ServerRpc(requireOwnership:false)]
    public void GotHitByProjectile()
    {
        if (m_hitPlayer) return;
        var ghost = GetComponent<GhostController>();
        ghost.m_isSlowed = true;
        ghost.m_slowed.SetActive(true);
        ghost.m_currentTimerSlowed = ghost.m_timerSlowed;
    }

    /**
    @brief      Apply stop effect from close combat hit
    */
    //can be called by anyone
    [ObserversRpc(runLocally:true)]
    public void GotHitByCac()
    {
        if (m_hitPlayer) return;
        var ghost = GetComponent<GhostController>();
        ghost.m_isStopped = true;
        ghost.m_stopped.SetActive(true);
        ghost.m_currentTimerStop = ghost.m_timerStop;
    }

}
