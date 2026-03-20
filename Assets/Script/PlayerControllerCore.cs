using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using PurrNet;
using UnityEngine.InputSystem;
using UI;
using Script.UI.Views;
using Script.States;
using PurrLobby;
using PurrNet.Modules;
using PurrNet.Transports;

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

    [Header("ServerResponse")]
    public float m_PingCooldown = 5f;
    public float m_elapsedTimeSincePing = 0f;
    public bool m_isServerAccessible = true;
    public bool m_isClientAccessible = true;

    public string m_memberID = "";
    public string m_username = "";

    protected virtual void Awake()
    {
        // Disable PlayerInput immediately so it can't grab a device before we know ownership.
        // OnSpawned() will re-enable it for the local owner only.
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }

    /**
    @brief      Server accessibility check
    @details    Will trigger if the server or client is not accessible and trigger the end of the game or the player disconnection 
    */
    public void PingClient()
    {
        if (!m_isServerAccessible || !m_isClientAccessible) return;

        m_elapsedTimeSincePing += Time.deltaTime;

        if (m_elapsedTimeSincePing >= m_PingCooldown)
        {
            PingFromClient();
        }
        
        if (m_elapsedTimeSincePing >= 2 * m_PingCooldown)
        {
            m_isServerAccessible = false;
            Debug.LogWarning("Server is not accessible. Last ping was " + m_elapsedTimeSincePing + " seconds ago.");
            FindAnyObjectByType<EndGameState>().ServerLost();
        }
    }

    public void PingServer()
    {
        if (!m_isClientAccessible || !m_isServerAccessible) return;

        m_elapsedTimeSincePing += Time.deltaTime;

        if (m_elapsedTimeSincePing >= 2 * m_PingCooldown)
        {
            m_isClientAccessible = false;
            Debug.LogWarning("Client is not accessible. Last ping was " + m_elapsedTimeSincePing + " seconds ago.");
            DisconnectPlayer();
            Destroy(gameObject, 2f);
        }
    }

    [ServerRpc (requireOwnership: false)]
    public void PingFromClient()
    {
        PingReceived();
    }

    [ObserversRpc (runLocally: true)]
    private void PingReceived()
    {
        m_elapsedTimeSincePing = 0f;
    }

    [ObserversRpc]
    private void DisconnectPlayer()
    {
        FindAnyObjectByType<RoleKeeper>().setMemberDisconnected(m_memberID);
    }

    /*
     * @brief Spawning player logic manage ownership, hide rendered to hide if needed
     */
    protected override void OnSpawned()
    {
        base.OnSpawned();

        Debug.Log($"[{gameObject.name}] OnSpawned - isOwner: {isOwner}, localPlayer: {localPlayer}, owner: {owner}");

        ApplyOwnership();
        RoleKeeper roleKeeper = FindAnyObjectByType<RoleKeeper>();
        networkManager.GetModule<PlayersManager>(isServer).TryGetConnection((PlayerID)owner, out Connection conn);
        if (conn.connectionId == 0)
        {
            m_memberID = roleKeeper.GetMemberID(conn.connectionId);
            m_username = roleKeeper.GetUsername(conn.connectionId);
        }
    }

    /*
     * @brief Fallback for when GiveOwnership is called after OnSpawned
     */
    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        ApplyOwnership();
    }

    public void DisableWaitUIObserverRPC()
    {
        if (!InstanceHandler.TryGetInstance(out UIsManager uisManager))
            return;
        uisManager.ToggleUIVision();
    }

    private void ApplyOwnership()
    {

        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = isOwner;

        if (!m_playerCamera) m_playerCamera = GetComponentInChildren<CinemachineCamera>();
        if (m_playerCamera != null)
            m_playerCamera.gameObject.SetActive(isOwner);

        // Also deactivate the local render camera (CinemachineBrain) for non-owners
        // to prevent multiple active cameras and duplicate AudioListeners
        if (m_localRenderCamera != null)
            m_localRenderCamera.SetActive(isOwner);

        if (isOwner)
        {
            DisableWaitUIObserverRPC();
            RoleKeeper roleKeeper = FindAnyObjectByType<RoleKeeper>();
            ApplyUserData(roleKeeper.getLocalMemberID(), roleKeeper.getLocalUsername());
        }
    }

    [ObserversRpc (runLocally: true, requireServer: false)]
    private void ApplyUserData(string _memberId, string _username)
    {
        m_memberID = _memberId;
        m_username = _username;
    }
    
    private void OnEnable()
    {
        PauseMenuView.OnPauseChanged += OnPauseChanged;
    }

    private void OnDisable()
    {
        PauseMenuView.OnPauseChanged -= OnPauseChanged;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnPauseChanged(bool paused)
    {
        if (!isOwner) return;
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = !paused;
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
