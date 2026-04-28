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
    [SerializeField] private float m_interactRange = 3f;
    [SerializeField] private LayerMask m_interactableLayer;

    private Transform m_cameraTransform;
    private PlayerControllerCore m_core;

    private void Start()
    {
        m_core = GetComponentInParent<PlayerControllerCore>();
        if (m_core != null && m_core.m_playerCamera != null)
            m_cameraTransform = m_core.m_playerCamera.transform;
    }

    private void Update()
    {
        if (!isOwner) return;

        IInteractable hit = RaycastForInteractable();

        if (hit != m_onFocus)
        {
            hit?.OnFocus(this);
            m_onFocus?.OnUnfocus(this);
            m_onFocus = hit;
        }
    }

    private IInteractable RaycastForInteractable()
    {
        if (m_cameraTransform == null) return null;

        if (!Physics.Raycast(m_cameraTransform.position, m_cameraTransform.forward, out RaycastHit hit, m_interactRange, m_interactableLayer))
            return null;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return null;

        if (interactable is GhostController ghost)
        {
            if (!m_isGhost || !ghost.m_isStopped) return null;
        }

        return interactable;
    }

    /*
     * @brief Interact with the current target if available
     * @details If target is a downed ghost, starts a hold-to-revive. Otherwise delegates to OnInteract.
     * @return void
     */
    public void OnInteract(IInteractable _currentFocus)
    {
        if (_currentFocus == null) return;
        if (_currentFocus is GhostController)
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
            GhostController ghostController = GetComponentInParent<GhostController>();
            if (ghostController != null)
            {
                ghostController.ResetDashCooldown();
            }
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
    [ServerRpc]
    public void StopInteract(IInteractable _currentFocus)
    {
        if (_currentFocus == null) return;
        _currentFocus?.OnStopInteract(this);
    }

    public void OnSabotageOver(bool success)
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (success)
            m_onFocus = null;
    }
}

