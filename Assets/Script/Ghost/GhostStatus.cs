using PurrNet;
using UnityEngine;

/*
 * @brief  Contains class declaration for GhostStatus
 * @details Script that is always visible and can be called by anyone to apply status on ghost
 */
public class GhostStatus : NetworkBehaviour, IInteractable
{
    public bool m_hitPlayer = false;
    private GhostController ghost;

    private void Awake()
    {
        ghost = GetComponent<GhostController>();
    }


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
        ghost.m_isSlowed = true;
        ghost.m_slowedLabel.SetActive(true);
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
        ghost.m_isStopped = true;
        ghost.m_stoppedLabel.SetActive(true);
        ghost.m_currentTimerStop = ghost.m_timerStop;
    }

    public void OnFocus()
    {
        // Do nothing
    }

    public void OnUnfocus()
    { 
        // Do nothing
    }
    
    public void OnInteract(GhostInteract _who)
    {
        if (m_hitPlayer) return;
        // ghost.m_isStopped = false;
        // ghost.m_stoppedLabel.SetActive(false);
        ghost.m_currentTimerStop = 0;
    }
    
    public void OnStopInteract(GhostInteract _who) {
        // Do nothing
    }
}
