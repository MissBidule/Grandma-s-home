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
    //unused
    //[SerializeField] private float m_radius = 2.0f;
    [SerializeField] private LayerMask m_interactableMask;
    public IInteractable m_onFocus; // Can be either GhostStatus or SabotageObject
    private List<IInteractable> m_interactable = new List<IInteractable>();

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;
    }

    private void Update()
    {
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
     * @return void
     */
    public void Interact()
    {
        if (m_onFocus == null) return;
        m_onFocus.OnInteract(this);
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
        }
    }
}