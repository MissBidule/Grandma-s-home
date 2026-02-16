using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using PurrNet;
using UnityEngine.InputSystem;

/*
 * @brief Player Core class inherited by Child & phantom
 * Only multiplayer components
 */
public class PlayerControllerCore : NetworkBehaviour
{
    [Header("References")]
    public CinemachineCamera m_playerCamera;
    [SerializeField] private NetworkAnimator m_playerAnimator;
    [SerializeField] private List<Renderer> m_renderers = new();

    /*
     * @brief Spawning player logic manage ownership, hide rendered to hide if needed
     */
    protected override void OnSpawned()
    {
        base.OnSpawned();
        
        Debug.Log($"[{gameObject.name}] OnSpawned - isOwner: {isOwner}, localPlayer: {localPlayer}, owner: {owner}");

        enabled = isOwner;
        GetComponent<PlayerInput>().enabled = isOwner;
        GetComponent<AudioSource>().enabled = !isOwner;
        GetComponentInChildren<CinemachineBrain>().gameObject.SetActive(!isOwner);
        
        // Properly manage camera for ownership
        if (m_playerCamera != null)
        {
            if (isOwner)
            {
                m_playerCamera.Priority = 10;
                m_playerCamera.enabled = true;
                m_playerCamera.gameObject.SetActive(true);
            }
            else
            {
                m_playerCamera.Priority = 0;
                m_playerCamera.enabled = false;
                m_playerCamera.gameObject.SetActive(false);
            }
        }

        if (!isOwner) {
            
            // Change color of non-owned players for better visibility 
            foreach (var renderer in m_renderers)
            {
                renderer.material.color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 0.8f, 0.9f);
            }
        }
        else
        {
            gameObject.tag = "Player";
        }
    }
    
    private void OnDisable()
    {
        // Need to check this later
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (m_playerCamera == null)
        {
            enabled = false;
            return;
        }
    }
}
