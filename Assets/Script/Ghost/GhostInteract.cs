using NUnit.Framework;
using PurrNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * @brief  Contains class declaration for GhostInteract
 * @details The GhostInteract class handles interactions with sabotageable objects and downed ghosts for the Ghost player.
 */
public class GhostInteract : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask m_interactableMask;
    public IInteractable m_onFocus;
    private List<IInteractable> m_interactable = new List<IInteractable>();

    private GhostController m_ghostController;
    private Rigidbody m_rigidbody;
    private ReviveBarUI m_reviveBarUI;

    // Revive state
    private bool m_isReviving = false;
    private GhostController m_reviveTarget;
    private float m_reviveTimer = 0f;
    private float m_reviveDuration = 0f;

    private void Awake()
    {
        m_ghostController = GetComponentInParent<GhostController>();
        m_rigidbody = GetComponentInParent<Rigidbody>();
        m_reviveBarUI = m_ghostController.GetComponentInChildren<ReviveBarUI>(true);
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }

    private void Update()
    {
        if (!isOwner) return;

        if (m_isReviving)
        {
            UpdateRevive();
            return;
        }

        if (m_interactable.Count <= 0) { 
            
            if (m_onFocus != null)
            {
                m_onFocus.OnUnfocus(this);
                m_onFocus = null;
            }
            return;
        }
                
        IInteractable closest = CheckClosest();

        if (closest != m_onFocus)
        {
            closest?.OnFocus(this);
            m_onFocus?.OnUnfocus(this);
            m_onFocus = closest;
        }
    }

    private float SqDistanceTo(Transform _transform)
    {
        return (transform.position - _transform.position).sqrMagnitude;
    }

    /*
    @brief      Check closest interactable object
    */
    private IInteractable CheckClosest()
    {
        IInteractable best = null;
        float bestSqrDistance = float.MaxValue;

        foreach (IInteractable interactable in m_interactable)
        {
            var ghost = interactable as GhostController;
            if (ghost != null)
            {
                if (!ghost.m_isStopped) continue; // Only interact with downed ghosts
            }

            MonoBehaviour mono = interactable as MonoBehaviour;
            float sqrDistance = SqDistanceTo(mono.transform);
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = interactable;
            }
        }
        return best;
    }

    private void UpdateRevive()
    {
        if (m_ghostController.m_isStopped || m_reviveTarget == null || !m_reviveTarget.m_isStopped)
        {
            CancelRevive();
            return;
        }
        m_reviveTimer += Time.deltaTime;
        float progress = m_reviveTimer / m_reviveDuration;
        if (m_reviveBarUI != null)
            m_reviveBarUI.SetProgress(progress);
        if (m_reviveTimer >= m_reviveDuration)
            CompleteRevive();
    }

    /*
     * @brief Interact with the current target if available
     * @details If target is a downed ghost, starts a hold-to-revive. Otherwise delegates to OnInteract.
     * @return void
     */
    public void Interact()
    {
        if (m_isReviving) return;
        if (m_onFocus == null) return;
        if (m_onFocus is GhostController gc && gc.m_isStopped)
        {
            StartRevive(gc);
            return;
        }
        m_onFocus.OnInteract(this);
    }

    /**
    @brief      Called when the interact button is released
    */
    public void StopInteract()
    {
        if (m_isReviving)
            CancelRevive();
    }

    private void StartRevive(GhostController _target)
    {
        if (m_ghostController.m_isStopped) return;
        m_reviveTarget = _target;
        m_reviveDuration = _target.GetReviveTime();
        m_reviveTimer = 0f;
        m_isReviving = true;
        m_ghostController.m_isReviving = true;
        FreezeReviverRpc();
        if (m_reviveBarUI != null) { m_reviveBarUI.SetProgress(0f); m_reviveBarUI.Show(); }
        if (InteractPromptUI.m_Instance != null) InteractPromptUI.m_Instance.Hide();
    }

    private void CompleteRevive()
    {
        RequestReviveRpc(m_reviveTarget);
        CancelRevive();
    }

    private void CancelRevive()
    {
        m_isReviving = false;
        m_ghostController.m_isReviving = false;
        m_reviveTarget = null;
        m_reviveTimer = 0f;
        UnfreezeReviverRpc();
        if (m_reviveBarUI != null) m_reviveBarUI.Hide();
    }

    /**
    @brief      Tell the server to freeze the reviver's rigidbody
    */
    [ServerRpc]
    private void FreezeReviverRpc()
    {
        m_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    /**
    @brief      Tell the server to unfreeze the reviver's rigidbody (only if not downed)
    */
    [ServerRpc]
    private void UnfreezeReviverRpc()
    {
        if (!m_ghostController.m_isStopped)
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    /**
    @brief      Tell the server to revive the target ghost
    */
    [ServerRpc]
    private void RequestReviveRpc(GhostController _target)
    {
        if (_target == null || !_target.m_isStopped) return;
        _target.ForceRevive();
        if (!m_ghostController.m_isStopped)
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void OnSabotageOver(bool success)
    {
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (success)
        {
            m_interactable.Remove(m_onFocus);
            m_onFocus = null;
        }
    }

    /*
     * @brief OnTriggerEnter is called when another collider enters the trigger
     * @param _other: The other Collider that entered.
     * @return void
     */
    void OnTriggerEnter(Collider _other)
    {
        if (!isOwner) return;
        if (_other.GetComponentInParent<IInteractable>() is IInteractable interactable)
        {
            m_interactable.Add(interactable);
        }
    }
    /*
     * @brief OnTriggerExit is called when another collider exits the trigger
     * @param _other: The other Collider that exited.
     * @return void
     */
    void OnTriggerExit(Collider _other)
    {
        if (!isOwner) return;
        if (_other.GetComponentInParent<IInteractable>() is IInteractable interactable)
        {
            m_interactable.Remove(interactable);
            if (m_isReviving && interactable is GhostController gc && gc == m_reviveTarget)
                CancelRevive();
        }
    }
}