using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using PurrNet;
using UnityEngine.InputSystem;

/*
 * @brief Player Core class inherited by Child & phantom
 * Only multiplayer components
 */
[DefaultExecutionOrder(-1000)]
public class PlayerControllerCore : NetworkBehaviour
{
    [Header("References")]
    public CinemachineCamera m_playerCamera;
    [SerializeField] private GameObject m_localRenderCamera;
    [SerializeField] private NetworkAnimator m_playerAnimator;
    [SerializeField] private List<Renderer> m_renderers = new();

    protected virtual void Awake()
    {
        // Disable PlayerInput immediately so it can't grab a device before we know ownership.
        // OnSpawned() will re-enable it for the local owner only.
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }

    /*
     * @brief Spawning player logic manage ownership, hide rendered to hide if needed
     */
    protected override void OnSpawned()
    {
        base.OnSpawned();

        Debug.Log($"[{gameObject.name}] OnSpawned - isOwner: {isOwner}, localPlayer: {localPlayer}, owner: {owner}");

        ApplyOwnership();

        if (!isOwner)
        {
            // Change color of non-owned players for better visibility
            foreach (var renderer in m_renderers)
            {
                renderer.material.color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 0.8f, 0.9f);
            }
        }
    }

    /*
     * @brief Fallback for when GiveOwnership is called after OnSpawned
     */
    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        ApplyOwnership();
    }

    private void ApplyOwnership()
    {
        var audioListener = GetComponentInChildren<AudioListener>();
        if (audioListener != null) audioListener.enabled = isOwner;

        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = isOwner;

        if (m_playerCamera != null)
            m_playerCamera.gameObject.SetActive(isOwner);

        // Also deactivate the local render camera (CinemachineBrain) for non-owners
        // to prevent multiple active cameras and duplicate AudioListeners
        if (m_localRenderCamera != null)
            m_localRenderCamera.SetActive(isOwner);
    }
    
    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (m_playerCamera == null)
        {
            return;
        }
    }
}
