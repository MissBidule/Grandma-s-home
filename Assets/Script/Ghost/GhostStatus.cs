using PurrNet;
using UnityEngine;

/*
 * @brief  Contains class declaration for GhostStatus
 * @details Script that is always visible and can be called by anyone to apply status on ghost
 */
public class GhostStatus : NetworkBehaviour, IInteractable
{
    private GhostController ghost;

    [Header("Revive")]
    [SerializeField] private float m_baseReviveTime = 5f;
    [SerializeField] private float m_maxReviveTime = 30f;
    private int m_deathCount = 0;

    public bool IsStopped => ghost.m_isStopped;

    public float GetReviveTime()
    {
        return Mathf.Min(m_baseReviveTime * m_deathCount, m_maxReviveTime);
    }

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
        m_deathCount++;
        ghost.m_isStopped = true;
        ghost.m_stoppedLabel.SetActive(true);
    }
    [ServerRpc]
    public void RequestRevive()
    {
        if (ghost.m_isStopped)
            DoRevive();
    }

    [ObserversRpc]
    private void DoRevive()
    {
        ghost.Revive();
    }

    public void OnFocus()
    {
        if (ghost.m_isStopped && InteractPromptUI.m_Instance != null)
        {
            InteractPromptUI.m_Instance.Show("Maintenir E : R\u00e9animer");
        }
    }

    public void OnUnfocus()
    {
        if (InteractPromptUI.m_Instance != null)
        {
            InteractPromptUI.m_Instance.Hide();
        }
    }

    public void OnInteract(GhostInteract _who)
    {
        // Revive is handled by GhostInteract hold logic
    }

    public void OnStopInteract(GhostInteract _who)
    {
        // Do nothing
    }
}
