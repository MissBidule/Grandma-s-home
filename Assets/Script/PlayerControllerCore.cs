using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using PurrNet;

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

        enabled = isOwner;
        m_playerCamera.gameObject.SetActive(isOwner);

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
