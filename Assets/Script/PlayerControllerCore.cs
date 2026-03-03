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

    protected bool isInitialized = false;

    /*
     * @brief Spawning player logic manage ownership, hide rendered to hide if needed
     */
    protected override void OnSpawned()
    {
        foreach (var player in FindObjectsByType<PlayerControllerCore>(FindObjectsSortMode.None))
        {
            StartCoroutine(player.Initialize());
        }

        base.OnSpawned();
    }


    public IEnumerator Initialize()
    {
        yield return new WaitForSeconds(10f);

        Debug.Log($"[{gameObject.name}] OnSpawned - isOwner: {isOwner}, localPlayer: {localPlayer}, owner: {owner}");

        name = $"Player {owner} {id} {localPlayer}";


        //GetComponentInChildren<AudioListener>().enabled = isOwner;
        GetComponent<PlayerInput>().enabled = isOwner;
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
