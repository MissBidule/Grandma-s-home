using System.Collections.Generic;
using UnityEngine;
using PurrNet;

/*
 * @brief Player Core class inherited by Child & phantom
 * Only multiplayer components
 */
public class PlayerControllerCore : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Camera m_playerCamera;
    [SerializeField] private NetworkAnimator m_playerAnimator;
    [SerializeField] private List<Renderer> m_renderersToHide = new();
    
    /*
     * @brief Spawning player logic manage ownership, hide rendered to hide if needed
     */
    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;

        if (!isOwner) {
            Destroy(m_playerCamera.gameObject);
            
            // Change color of non-owned players for better visibility 
            /*foreach (var renderer in renderersToHide)
            {
                renderer.material.color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), 0.8f, 0.9f);
            }*/
        }

        // Hiding component blocking the vision of the player but leaving shadows
        if (isOwner)
        {
            foreach (var renderer in m_renderersToHide)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
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
        // Need to check this later
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (m_playerCamera == null)
        {
            enabled = false;
            return;
        }
    }
}
