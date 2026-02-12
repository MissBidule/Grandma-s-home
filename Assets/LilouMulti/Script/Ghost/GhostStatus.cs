using PurrNet;
using UnityEngine;

public class GhostStatus : NetworkBehaviour
{
    public bool hitPlayer = false;

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
        var ghost = GetComponent<GhostController>();
        ghost.m_isSlowed = true;
        ghost.m_slowed.SetActive(true);
        ghost.m_currentTimerSlowed = ghost.m_timerSlowed;
        Debug.Log("dead");
    }

    /**
    @brief      Apply stop effect from close combat hit
    */
    [ServerRpc(requireOwnership:false)]
    public void GotHitByCac()
    {
        if (hitPlayer) return;
        var ghost = GetComponent<GhostController>();
        ghost.m_isStopped = true;
        ghost.m_stopped.SetActive(true);
        ghost.m_currentTimerStop = ghost.m_timerStop;
    }

}
