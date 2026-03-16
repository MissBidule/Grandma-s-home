using NUnit.Framework;
using PurrNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * @brief  Contains class declaration for Interact
 * @details The Interact class handles interactions with sabotageable objects for all players and downed ghosts for the Ghost player.
 */
public class Interact : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] public bool m_isGhost = true;
    public IInteractable m_onFocus; // Can be either GhostStatus or SabotageObject
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
                if (!m_isGhost || !ghost.m_isStopped) continue; // Only interact with downed ghosts
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
    public void OnInteract(IInteractable _currentFocus)
    {
        if (_currentFocus == null) return;
        if (_currentFocus is GhostController ghost)
        {
            OnRevive(_currentFocus);
            return;
        }
        _currentFocus.OnInteract(this);
    }

    [ServerRpc]
    public void OnRevive(IInteractable _currentFocus)
    {
        if (!isServer) return;
        _currentFocus.OnInteract(this);
    }

    public void OnSuccessSabotage()
    {
        if (m_isGhost)
        {
            GetComponentInParent<GhostController>().ApplyDashToAll(false, true);
        }
        else
        {
            // If one day we give a score or something for repairing as the child
            // It should be put here.
        }
    }

    /**
    @brief      Called when the interact button is released
    */
    public void StopInteract(IInteractable _currentFocus)
    {
        if (_currentFocus == null) return;
        _currentFocus?.OnStopInteract(this);
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