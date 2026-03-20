using System.Collections;
using PurrNet;
using Script.UI.Views;
using UI;
using Unity.Cinemachine;
using UnityEngine;

public class GhostClientController : NetworkBehaviour
{
    private GhostInputController m_ghostInputController;
    private GhostController m_ghostController;
    private GhostMorph m_ghostMorph;
    private GhostMorphPreview m_ghostMorphPreview;

    public CinemachineCamera m_playerCamera;
    public DeathEffect m_cameraEffect;

    private bool last_stopped = false;
    private bool last_slowed = false;


    [Header("Canva")]
    [SerializeField] private GameObject m_uiHolder_prefab;
    public GameObject m_uiHolder;
    public WheelController m_wheel;

    private GhostHUDView m_ghostHUDView;

    private bool morphPressed = false;
    private bool dashPressed = false;
    private bool sneakPressed = false;

    private bool m_reviveUIActive = false;
    private ReviveBarUI m_reviveBarUI;
    private float m_reviveTimer = 0f;
    private float m_reviveDuration = 0f;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        m_ghostController = GetComponent<GhostController>();
        m_ghostMorph = GetComponent<GhostMorph>();
        m_ghostMorphPreview = GetComponentInChildren<GhostMorphPreview>();

        if (isOwner) InitOwner();
    }

    protected override void OnOwnerChanged(PurrNet.PlayerID? oldOwner, PurrNet.PlayerID? newOwner, bool asServer)
    {
        if (isOwner && (m_ghostInputController == null || m_playerCamera == null)) InitOwner();
        if (!isOwner) DestroyUI();
    }

    private void InitOwner()
    {
        m_ghostInputController = GetComponent<GhostInputController>();
        // Use PlayerControllerCore.m_playerCamera (Inspector-assigned, always valid)
        // instead of GetComponentInChildren which can fail in multi-instance scenarios
        var core = GetComponent<PlayerControllerCore>();
        if (core != null) m_playerCamera = core.m_playerCamera;
        if (m_uiHolder == null)
            m_uiHolder = UnityProxy.InstantiateDirectly(m_uiHolder_prefab);
        m_reviveBarUI = m_uiHolder.GetComponentInChildren<ReviveBarUI>(true);
        m_wheel = m_uiHolder.GetComponentInChildren<WheelController>();
        if (m_playerCamera != null) m_cameraEffect = m_playerCamera.GetComponent<DeathEffect>();
        m_wheel.LinkWithGhost(this);
        Debug.Log($"[GhostClientController] InitOwner - m_playerCamera: {m_playerCamera}");
        
        // Displaying the HUD
        if (InstanceHandler.TryGetInstance(out UIsManager  uisManager))
            uisManager.ShowView<GhostHUDView>();
        
        // Getting the HUD refference. (moved here as it could try to get it before it was instanced)
        InstanceHandler.TryGetInstance(out m_ghostHUDView);
    }

    private void DestroyUI()
    {
        if (m_uiHolder) UnityProxy.DestroyDirectly(m_uiHolder);
        m_uiHolder = null;
    }

    void Update()
    {
        if (!isOwner) return;
        m_ghostController.PingClient();
        if (m_ghostController == null || m_ghostInputController == null || m_playerCamera == null) return; // "just in case"

        UpdateHUD();

        if (last_stopped != m_ghostController.m_isStopped)
        {
            print("dead: " + m_ghostController.m_isStopped);
            m_ghostHUDView.ShowMessage(m_ghostController.m_isStopped ? "You've been stopped!" : "You're no longer stopped.");
            m_cameraEffect.SetDeathEffect(m_ghostController.m_isStopped);
            last_stopped = m_ghostController.m_isStopped;
        }

        if (last_slowed != m_ghostController.m_isSlowed)
        {
            print("slowed: " + m_ghostController.m_isSlowed);
            m_ghostHUDView.ShowMessage(m_ghostController.m_isSlowed ? "You've been slowed!" : "You're no longer slowed.");
            last_slowed = m_ghostController.m_isSlowed;
        }

        // DebugPrintTrafic();

        SendGhostRPC(
            GetDirectionIntention(m_ghostInputController.m_movementInputVector),
            morphPressed ? m_ghostMorphPreview.m_currentPrefab : null,                  // Morph Parameters
            m_ghostMorphPreview.transform.localPosition,                                 // Morph Parameters
            dashPressed,
            sneakPressed,
            m_ghostMorphPreview.transform.localRotation
        );

        // Reset values after sending to server
        if (morphPressed) m_ghostMorphPreview.HidePreview();
        morphPressed = false;
        
        // Dash 
        dashPressed = false;
        
        if (m_reviveUIActive)
        {
            UpdateReviveUI();
        }

        if (!m_reviveUIActive && (m_ghostController.m_beingRevived || m_ghostController.m_isReviving))
        {
            OnReviveStart();
        }
        else if (m_reviveUIActive && !(m_ghostController.m_beingRevived || m_ghostController.m_isReviving))
        {
            OnReviveEnd();
        }
    }

    void DebugPrintTrafic()
    {
        print("sended");
        print(m_ghostInputController.m_movementInputVector);
        print(GetDirectionIntention(m_ghostInputController.m_movementInputVector));
        print(morphPressed);
        print(morphPressed ? m_ghostMorphPreview.m_currentPrefab : null);
        print(m_ghostMorphPreview.transform.localPosition);
    }

    void UpdateHUD()
    {
        if (m_ghostHUDView == null)
            return;

        switch (m_ghostController.m_isDashing)
        {
            case true:
                m_ghostHUDView.DashActivate();
                break;
            case false when !m_ghostController.m_canDash:
            {
                if (m_ghostHUDView.m_dash_disabled)
                    return;
                m_ghostHUDView.DashDisabled();
                break;
            }
        }
        
        if (!m_ghostController.m_canScareChild)
            m_ghostHUDView.ScaredActivate(m_ghostController.GetScaryCooldownDuration());
        else m_ghostHUDView.m_canScare = true;
    }

    void UpdateReviveUI()
    {
        m_reviveTimer += Time.deltaTime;
        float progress = m_reviveTimer / m_reviveDuration;
        if (m_reviveBarUI != null)
        {
            m_reviveBarUI.SetProgress(progress);
        }

    }

    void OnReviveStart()
    {
        m_reviveUIActive = true;
        m_reviveDuration = m_ghostController.m_reviveDuration;
        m_reviveTimer = 0f;
        if (m_reviveBarUI != null) { m_reviveBarUI.SetProgress(0f); m_reviveBarUI.Show(); }
    }

    void OnReviveEnd()
    {
        m_reviveUIActive = false;
        if (m_reviveBarUI != null) m_reviveBarUI.Hide();
    }

    public void OnScan()
    {
        if (!isOwner) return;
        if (m_ghostController.m_isStopped) return;
        if (m_ghostMorph.m_isMorphed) return; // Prevent scanning if already morphed
        m_ghostMorphPreview.ScanForPrefab();
    }

    public void OnOpenWheel()
    {
        if (!isOwner) return;
        if (m_ghostController.m_isStopped) return;
        m_wheel.Toggle();
    }
    public void OnMorph()
    {
        if (!isOwner) return;
        if (m_ghostController.m_isStopped) return;
        if (!m_ghostMorphPreview.m_canMorph || !m_ghostMorphPreview.m_currentPrefab || m_ghostMorph.m_isMorphed) return;
        if (m_wheel.IsWheelOpen()) m_wheel.Toggle();
        
        m_wheel.ClearSelection();
        morphPressed = true;
        InteractPromptUI.m_Instance.Hide();
    }

    /*
     * @brief call the server to dash
     */
    public void OnDash()
    {
        dashPressed = true;
    }
    
    /*
     * @brief call the server to sneak
     */
    public void Sneak(bool _sneakStatus)
    {
        sneakPressed = _sneakStatus;
    }

    /**
     * From Input Vector to Movement Intention, based on camera placement.
     */
    private Vector3 GetDirectionIntention(Vector2 _movement)
    {
        Transform cam = m_playerCamera.transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 wishDir = Vector3.zero;
        if (_movement.sqrMagnitude > 0.0001f)
            wishDir = (forward * _movement.y + right * _movement.x).normalized;

        return wishDir;
    }

    [ObserversRpc (requireServer: false)]
    public void SabotageNotification()
    {
        InteractPromptUI.m_Instance.ShowSabotage(m_ghostController.m_username);
    }

    [ServerRpc]
    private void SendGhostRPC(Vector3 _movement, GameObject _prefab, Vector3 _pos, bool _dashPressed, bool _sneakPressed, Quaternion _rotation)
    {
        if (_prefab)
        {
            // On morph: freeze movement and require input release before allowing revert
            m_ghostController.m_wishDir = Vector3.zero;
            m_ghostController.m_morphInputReleased = false;
            m_ghostMorph.Morphing(_prefab, _pos, _rotation);
        }
        else if (!m_ghostController.m_morphInputReleased)
        {
            // Keep frozen until player actually releases all movement input
            if ((Vector2)_movement == Vector2.zero)
                m_ghostController.m_morphInputReleased = true;
            m_ghostController.m_wishDir = Vector3.zero;
        }
        else
        {
            m_ghostController.m_wishDir = _movement;
        if (_dashPressed)
            m_ghostController.StartDash();
        
        m_ghostController.m_isSneaking = _sneakPressed;
        }
    }
}
