using PurrNet;
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
    private List<IInteractable> m_interactable = new List<IInteractable>();

    public IInteractable m_onFocus; // Can be either GhostStatus or SabotageObject

    private GhostController m_ghostController;
    private Rigidbody m_rigidbody;
    private ReviveBarUI m_reviveBarUI;

    // Revive state
    private bool m_isReviving = false;
    private GhostStatus m_reviveTarget;
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

        enabled = isOwner;
    }

    private void Update()
    {
        // Handle ongoing revive
        if (m_isReviving)
        {
            UpdateRevive();
            return;
        }

        if (m_interactable.Count <= 0) {

            if (m_onFocus != null)
            {
                m_onFocus.OnUnfocus();
                m_onFocus = null;
            }
            return;
        }

        IInteractable closest = CheckClosest();

        if (closest != m_onFocus)
        {
            closest.OnFocus();

            if (m_onFocus != null)
            {
                m_onFocus.OnUnfocus();
            }
            m_onFocus = closest;
        }
    }

    private void UpdateRevive()
    {
        // Cancel if reviver got stopped or target no longer valid
        if (m_ghostController.m_isStopped || m_reviveTarget == null || !m_reviveTarget.IsStopped)
        {
            CancelRevive();
            return;
        }

        m_reviveTimer += Time.deltaTime;
        float progress = m_reviveTimer / m_reviveDuration;

        if (m_reviveBarUI != null)
            m_reviveBarUI.SetProgress(progress);

        if (m_reviveTimer >= m_reviveDuration)
        {
            CompleteRevive();
        }
    }

    private float SqDistanceTo(Transform _transform)
    {
        Vector3 closest = transform.position;
        float sqrDistance = (closest - transform.position).sqrMagnitude;
        return sqrDistance;
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

    /*
     * @brief Interact with the current target if available
     * @details If the target is a downed ghost, starts a hold-to-revive process.
     *          Otherwise, delegates to the target's OnInteract (sabotage QTE, etc.)
     * @return void
     */
    public void Interact()
    {
        if (m_isReviving) return;
        if (m_onFocus == null) return;

        // Check if target is a downed ghost -> start revive
        if (m_onFocus is GhostStatus gs && gs.IsStopped)
        {
            StartRevive(gs);
            return;
        }

        // Otherwise, normal interact (sabotage, etc.)
        m_onFocus.OnInteract(this);
    }

    /**
    @brief      Start reviving a downed ghost
    @param      _target: The downed ghost's GhostStatus
    */
    private void StartRevive(GhostStatus _target)
    {
        if (m_ghostController.m_isStopped) return;

        m_reviveTarget = _target;
        m_reviveDuration = _target.GetReviveTime();
        m_reviveTimer = 0f;
        m_isReviving = true;

        // Freeze reviver
        m_ghostController.m_isReviving = true;
        m_rigidbody.constraints = RigidbodyConstraints.FreezeAll;

        // Show revive bar, hide interact prompt
        if (m_reviveBarUI != null)
        {
            m_reviveBarUI.SetProgress(0f);
            m_reviveBarUI.Show();
        }

        if (InteractPromptUI.m_Instance != null)
            InteractPromptUI.m_Instance.Hide();
    }

    /**
    @brief      Complete the revive and notify the server
    */
    private void CompleteRevive()
    {
        m_reviveTarget.RequestRevive();
        CancelRevive();
    }

    /**
    @brief      Cancel revive and restore reviver state
    */
    private void CancelRevive()
    {
        m_isReviving = false;
        m_reviveTarget = null;
        m_reviveTimer = 0f;

        m_ghostController.m_isReviving = false;

        // Only unfreeze rigidbody if not stopped (dead)
        if (!m_ghostController.m_isStopped)
        {
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (m_reviveBarUI != null)
            m_reviveBarUI.Hide();
    }

    /**
    @brief      Called when the interact button is released
    */
    public void StopInteract()
    {
        if (m_isReviving)
        {
            CancelRevive();
        }
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
        if (_other.GetComponentInParent<IInteractable>() is IInteractable interactable)
        {
            m_interactable.Remove(interactable);

            // Cancel revive if target left range
            if (m_isReviving && interactable is GhostStatus gs && gs == m_reviveTarget)
            {
                CancelRevive();
            }
        }
    }
}
