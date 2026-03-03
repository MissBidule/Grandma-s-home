using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using PurrNet;
using System.Collections;
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

    private bool isInitialized = false;

    /*
     * @brief Spawning player logic manage ownership, hide rendered to hide if needed
     */
    protected override void OnSpawned()
    {
        foreach (var player in FindObjectsByType<PlayerControllerCore>(FindObjectsSortMode.None))
        {
            player.Initialize();
        }

        base.OnSpawned();
    }


    public void Initialize()
    {
        if (isInitialized) return;
        print("JE SUIS INITIALISER, JE M APPELLE " + owner + " ET JE SUIS UN PLAYERCONTROLLER" );

        Debug.Log($"[{gameObject.name}] OnSpawned - isOwner: {isOwner}, localPlayer: {localPlayer}, owner: {owner}");

        name = $"Player {owner} {id} {localPlayer}";


        //GetComponentInChildren<AudioListener>().enabled = isOwner;
        GetComponent<PlayerInput>().enabled = isOwner;
        isInitialized = true;
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
