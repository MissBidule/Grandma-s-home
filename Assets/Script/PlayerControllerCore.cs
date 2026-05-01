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
    public LatencyDisplay m_latencyDisplay;
    public float m_PingCooldown = 5f;
    public float m_elapsedTimeSincePing = 0f;
    public bool m_isServerAccessible = true;
    public bool m_isClientAccessible = true;

    public string m_memberID = "";
    public string m_username = "";
    private bool m_tutoOn;

    protected virtual void Awake()
    {
        // Disable PlayerInput immediately so it can't grab a device before we know ownership.
        // OnSpawned() will re-enable it for the local owner only.
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;

        foreach(SceneSwitcher sceneSwitcher in FindObjectsByType<SceneSwitcher>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            m_tutoOn=sceneSwitcher._isTuto;
        }
    }

    /**
    @brief      Starts the game music for all clients
    @details    Makes sure everyone gets here at one point 
    */    
    public void StartGameMusic()
    {
        if (Script.Audio.MusicLooper.Instance == null)
            return;
        Script.Audio.MusicLooper.Instance.PlayMusic(Script.Audio.MusicTrack.Game);
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
            DisconnectPlayer(owner.Value);
            UnityProxy.Destroy(gameObject, 2f);
        }
    }

    [ServerRpc (requireOwnership: false)]
    public void PingFromClient()
    {
        PingReceived(owner.Value);
    }

    [TargetRpc (runLocally: true)]
    private void PingReceived(PlayerID _target)
    {
        m_elapsedTimeSincePing = 0f;
    }

    [ObserversRpc]
    private void DisconnectPlayer(PlayerID _playerID)
    {
        FindAnyObjectByType<RoleKeeper>()?.SetMemberDisconnected(m_memberID);
        FindAnyObjectByType<LeaderboardUI>()?.UpdateDisconnected();
        if (_playerID == localPlayer) FindAnyObjectByType<EndGameState>().BackToMenu();
    }

    /*
     * @brief Spawning player logic manage ownership, hide rendered to hide if needed
     */
    protected override void OnSpawned()
    {
        base.OnSpawned();

        Debug.Log($"[{gameObject.name}] OnSpawned - isOwner: {isOwner}, localPlayer: {localPlayer}, owner: {owner}");

        ApplyOwnership();
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
        uisManager.ToggleUIVision(GetComponentInChildren<CinemachineBrain>(true));
        if (IntroVideoPlayer.Instance == null || IntroVideoPlayer.Instance.IsDone)
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void ApplyOwnership()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = isOwner;
            if (isOwner)
            {
                string saved = PlayerPrefs.GetString("Settings_Keybindings", "");
                if (!string.IsNullOrEmpty(saved))
                    playerInput.actions.LoadBindingOverridesFromJson(saved);
            }
        }

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
            ApplyUserData(roleKeeper.GetLocalMemberID(), roleKeeper.GetLocalUsername());
            m_latencyDisplay = FindAnyObjectByType<LatencyDisplay>();
            if (m_latencyDisplay!=null)
                m_latencyDisplay.m_localPlayer = this;
        }
    }


    /*
     FOR LATENCY PING NOT HEARTBEAT
     */
    [ServerRpc]
    public void PingServer(float sentTime, RPCInfo info = default)
    {
        // info.sender = le client qui a envoye
        PongClient(info.sender, sentTime);
    }

    /*
     FOR LATENCY PING NOT HEARTBEAT
     */
    [TargetRpc]
    void PongClient(PlayerID target, float _sentTime)
    {
        m_latencyDisplay.ReceivePong(_sentTime);
    }

    [ObserversRpc (runLocally: true, requireServer: false, bufferLast: true)]
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
        if (!paused && InstanceHandler.TryGetInstance(out EndGameState es) && es.IsGameOver) return;
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || !playerInput.enabled) return;
        foreach (var action in playerInput.actions)
        {
            if (action.name == "Escape") continue;
            if (paused) action.Disable(); else action.Enable();
        }
        if (!paused)
        {
            string saved = PlayerPrefs.GetString("Settings_Keybindings", "");
            if (!string.IsNullOrEmpty(saved))
                playerInput.actions.LoadBindingOverridesFromJson(saved);
        }
    }

    private void Start()
    {
        if (IntroVideoPlayer.Instance == null || IntroVideoPlayer.Instance.IsDone)
            Cursor.lockState = CursorLockMode.Locked;
        if (m_playerCamera == null)
        {
            return;
        }
    }
}
