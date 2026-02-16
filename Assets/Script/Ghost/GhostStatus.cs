using PurrNet;
using UnityEngine;

/*
 * @brief  Contains class declaration for GhostStatus
 * @details Script that is always visible and can be called by anyone to apply status on ghost
 */
public class GhostStatus : NetworkBehaviour, IInteractable
{
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
    @brief      Prevents cheating and lagging
    */
    [ServerRpc]
    public void GotHitByProjectile()
    {
        Debug.Log("hit approved by server (I guess)");
        GhostHitRanged();
    }

    /**
    @brief      Apply slow effect from projectile hit
    */
    [ObserversRpc]
    private void GhostHitRanged()
    {
        Debug.Log("Ghost hit");
        ghost.m_isSlowed = true;
        ghost.m_slowedLabel.SetActive(true);
        ghost.m_currentTimerSlowed = ghost.m_timerSlowed;
    }

    /**
    @brief      Prevents cheating and lagging
    */
    [ServerRpc]
    public void GotHitByCac()
    {
        Debug.Log("hit approved by server (I guess)");
        GhostHitCloseCombat();
    }

    /**
    @brief      Apply stop effect from close combat hit
    */
    [ObserversRpc]
    public void GhostHitCloseCombat()
    {
        Debug.Log("Ghost hit");
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
        // ghost.m_isStopped = false;
        // ghost.m_stoppedLabel.SetActive(false);
        ghost.m_currentTimerStop = 0;
    }
    
    public void OnStopInteract(GhostInteract _who) {
        // Do nothing
    }
}
