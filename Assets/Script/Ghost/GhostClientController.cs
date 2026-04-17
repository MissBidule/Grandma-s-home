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
    private PredictiveMovement m_predictiveMovement;

    public CinemachineCamera m_playerCamera;
    public DeathEffect m_cameraEffect;

    private bool last_stopped = false;
    private bool last_slowed = false;
    private bool m_isMoving = false;
    
    [Header("References")]
    [SerializeField] public GhostSoundEffects m_soundEffects;
    
    [Header("Canva")]
    [SerializeField] private GameObject m_uiHolder_prefab;
    public GameObject m_uiHolder;
    public WheelController m_wheel;

    private GhostHUDView m_ghostHUDView;
    private QteCircle m_qteCircle;

    private bool m_jumpPressed = false;
    private bool m_morphPressed = false;
    private bool m_dashPressed = false;
    private bool m_sneakPressed = false;
    private bool m_waitingForInputRelease = false;

    private bool m_reviveUIActive = false;
    private ReviveBarUI m_reviveBarUI;
    private float m_reviveTimer = 0f;
    private float m_reviveDuration = 0f;

    void Start()
    {
        m_ghostController = GetComponent<GhostController>();
        m_ghostMorph = GetComponent<GhostMorph>();
        m_ghostMorphPreview = GetComponentInChildren<GhostMorphPreview>();
        m_predictiveMovement = GetComponent<PredictiveMovement>();
        InstanceHandler.TryGetInstance(out m_ghostHUDView);
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
        
        m_soundEffects.InitOwner();
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

        var wishDir = GetDirectionIntention(m_ghostInputController.m_movementInputVector);

        // Client-side freeze: wait for player to fully release movement after morphing
        if (m_waitingForInputRelease)
        {
            if (wishDir == Vector3.zero)
                m_waitingForInputRelease = false;
            else
                wishDir = Vector3.zero;
        }

        var inputData = new PredictiveInputData
        {
            tick = m_predictiveMovement.GetTick(),
            wishDirection = wishDir,
            dashPressed = m_dashPressed,
            sneakPressed = m_sneakPressed,
            position = transform.position,
            jumpPressed = m_jumpPressed
        };

        m_predictiveMovement.NewInput(inputData);

        SendGhostRPC(
            inputData,
            m_morphPressed ? m_ghostMorphPreview.m_currentPrefab : null,                  // Morph Parameters
            m_ghostMorphPreview.transform.localPosition,                                 // Morph Parameters
            m_ghostMorphPreview.transform.localRotation
        );

        // Reset values after sending to server
        if (m_morphPressed) { m_ghostMorphPreview.HidePreview(); m_waitingForInputRelease = true; }
        m_morphPressed = false;
        m_jumpPressed = false;
        m_dashPressed = false;
        
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
        print(m_jumpPressed);
        print(m_morphPressed);
        print(m_morphPressed ? m_ghostMorphPreview.m_currentPrefab : null);
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

    public void OnJump()
    {
        if (!isOwner) return;
        if (!m_qteCircle) m_qteCircle = FindAnyObjectByType<QteCircle>();
        if (m_qteCircle != null && m_qteCircle.m_isRunning) return;
        m_jumpPressed = true;
    }

    public void OnOpenWheel()
    {
        if (!isOwner) return;
        if (m_ghostController.m_isStopped) return;
        if (!m_qteCircle) m_qteCircle = FindAnyObjectByType<QteCircle>();
        if (m_qteCircle != null && m_qteCircle.m_isRunning) return;
        m_wheel.Open();
    }

    public void OnCloseWheel()
    {
        if (!isOwner) return;
        m_wheel.Close();
    }
    public void OnMorph()
    {
        if (!isOwner) return;
        if (m_ghostController.m_isStopped) return;
        if (!m_qteCircle) m_qteCircle = FindAnyObjectByType<QteCircle>();
        if (m_qteCircle != null && m_qteCircle.m_isRunning) return;
        if (!m_ghostMorphPreview.m_canMorph || !m_ghostMorphPreview.m_currentPrefab || m_ghostMorph.m_isMorphed) return;
        if (m_wheel.IsWheelOpen()) m_wheel.Toggle();
        
        m_wheel.ClearSelection();
        m_morphPressed = true;
        InteractPromptUI.m_Instance.Hide();
    }

    /*
     * @brief call the server to dash
     */
    public void OnDash()
    {
        m_dashPressed = true;
    }
    
    /*
     * @brief call the server to sneak
     */
    public void Sneak(bool _sneakStatus)
    {
        m_sneakPressed = _sneakStatus;
    }

    /**
     * From Input Vector to Movement Intention, based on camera placement.
     */
    private Vector3 GetDirectionIntention(Vector2 _movement)
    {
        if(_movement == Vector2.zero)
        {
            if (m_isMoving)
            {
                m_ghostController.callAnimationCrossFade("ghost_idle", 0.2f);
                m_isMoving = false;
            }
        } else
        {
            if (!m_isMoving)
            {
                m_ghostController.callAnimationCrossFade("ghost_walk", 0.2f);
                m_isMoving = true;
            }
        }
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
    private void SendGhostRPC(PredictiveInputData _input, GameObject _prefab, Vector3 _pos, Quaternion _rotation)
    {
        if (_prefab)
        {
            // On morph: freeze movement server-side before passing to PredictiveMovement
            _input.wishDirection = Vector3.zero;
            m_predictiveMovement.ServerReceiveInput(_input);
            m_ghostController.m_wishDir = Vector3.zero;
            m_ghostController.m_morphInputReleased = false;
            m_ghostMorph.Morphing(_prefab, _pos, _rotation);
        }
        else if (!m_ghostController.m_morphInputReleased)
        {
            // Keep frozen until player releases all movement input
            if (_input.wishDirection == Vector3.zero)
                m_ghostController.m_morphInputReleased = true;
            _input.wishDirection = Vector3.zero;
            m_predictiveMovement.ServerReceiveInput(_input);
            m_ghostController.m_wishDir = Vector3.zero;
        }
        else
        {
            m_predictiveMovement.ServerReceiveInput(_input);
            m_ghostController.m_wishDir = _input.wishDirection;
            if (_input.dashPressed)
                m_ghostController.StartDash();
            m_ghostController.m_isSneaking = _input.sneakPressed;
        }
    }

    public void SabotageAnimation(bool _value)
    {
        m_ghostController.callAnimationSetBool("IsSabotaging", _value);
    }
}
