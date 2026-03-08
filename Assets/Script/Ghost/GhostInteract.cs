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


    private void Update()
    {
        if (!isOwner) return;

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
        print(m_onFocus);
    }

    private float SqDistanceTo(Transform _transform)
    {
        return (_transform.position - transform.position).sqrMagnitude;
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

    /*
     * @brief Interact with the current target if available
     * @details If target is a downed ghost, starts a hold-to-revive. Otherwise delegates to OnInteract.
     * @return void
     */
    [ServerRpc]
    public void Interact(IInteractable currentFocus)
    {
        if (!isServer) return;
        if (currentFocus == null) return;
        currentFocus.OnInteract(this);
    }

    /**
    @brief      Called when the interact button is released
    */
    [ServerRpc]
    public void StopInteract(IInteractable currentFocus)
    {
        if (currentFocus == null) return;
        currentFocus?.OnStopInteract(this);
    }



    public void OnSabotageOver(bool success)
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
        }
    }
}