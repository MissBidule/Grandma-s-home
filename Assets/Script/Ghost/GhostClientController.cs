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


    [Header("Canva")]
    [SerializeField] private GameObject m_uiHolder_prefab;
    public GameObject m_uiHolder;
    public WheelController m_wheel;
    public GameObject m_stoppedLabel;
    public GameObject m_slowedLabel;

    public GameObject randomPrefab;

    private bool morphPressed = false;
    private bool dashPressed = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();


        m_ghostController = GetComponent<GhostController>();
        m_ghostMorph = GetComponent<GhostMorph>();
        m_ghostMorphPreview = GetComponentInChildren<GhostMorphPreview>();

        if (!isOwner) return;

        m_ghostInputController = GetComponent<GhostInputController>();
        m_playerCamera = GetComponentInChildren<CinemachineCamera>();
        m_uiHolder = UnityProxy.InstantiateDirectly(m_uiHolder_prefab);
        m_wheel = m_uiHolder.GetComponentInChildren<WheelController>();
        m_cameraEffect = m_playerCamera.GetComponent<DeathEffect>();

        m_wheel.LinkWithGhost(this);
        
        if (InstanceHandler.TryGetInstance(out UIsManager  uisManager))
            uisManager.ShowView<GhostHUDView>();
    }
    void Update()
    {
        if (!isOwner) return;

        UpdateLabels();

        if (last_stopped != m_ghostController.m_isStopped)
        {
            print("dead: " + m_ghostController.m_isStopped);
            m_cameraEffect.SetDeathEffect(m_ghostController.m_isStopped);
            last_stopped = m_ghostController.m_isStopped;
        }


        // DebugPrintTrafic();

        SendGhostRPC(
            GetDirectionIntention(m_ghostInputController.m_movementInputVector),
            morphPressed ? m_ghostMorphPreview.m_currentPrefab : null,                  // Morph Parameters
            m_ghostMorphPreview.transform.localPosition,                                 // Morph Parameters
            dashPressed
        );

        // Reset values after sending to server
        if (morphPressed) m_ghostMorphPreview.HidePreview();
        morphPressed = false;
        
        // Dash 
        dashPressed = false;
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

    void UpdateLabels()
    {
        if (m_ghostController.m_isStopped)
        {
            if (!m_stoppedLabel.activeSelf) m_stoppedLabel.SetActive(true);
        }
        else
        {
            if (m_stoppedLabel.activeSelf) m_stoppedLabel.SetActive(false);
        }

        if (m_ghostController.m_isSlowed)
        {
            if (!m_slowedLabel.activeSelf) m_slowedLabel.SetActive(true);
        }
        else
        {
            if (m_slowedLabel.activeSelf) m_slowedLabel.SetActive(false);
        }

        if (m_ghostController.m_isDashing)
        {
            if (InstanceHandler.TryGetInstance(out GhostHUDView  ghostHUDView)) 
                ghostHUDView.DashActivate();
        }

        if (m_ghostController.m_dashDisabled)
        {
            if (InstanceHandler.TryGetInstance(out GhostHUDView ghostHUDView))
            {
                ghostHUDView.DashDisabled();
            }
        }
    }


    public void OnScan()
    {
        if (!isOwner) return;
        if (m_ghostMorph.m_isMorphed) return; // Prevent scanning if already morphed
        m_ghostMorphPreview.ScanForPrefab();
    }

    public void OnOpenWheel()
    {
        if (!isOwner) return;
        m_wheel.Toggle();
    }

    public void OnMorph()
    {
        if (!isOwner) return;
        if (!m_ghostMorphPreview.m_canMorph || !m_ghostMorphPreview.m_currentPrefab || m_ghostMorph.m_isMorphed) return;
        if (m_wheel.IsWheelOpen()) m_wheel.Toggle();

        m_wheel.ClearSelection();

        morphPressed = true;

    }

    /*
     * @brief call the server to dash
     */
    public void OnDash()
    {
        dashPressed = true;
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

    [ServerRpc]
    private void SendGhostRPC(Vector3 _movement, GameObject _prefab, Vector3 _pos, bool _dashPressed)
    {
        m_ghostController.m_wishDir = _movement;
        // Prefab is not null only when morphPressed is true.
        // This method helps reduce the network traffic by not sending that bool "morphPressed"
        if (_prefab) m_ghostMorph.Morphing(_prefab, _pos);
        
        if (_dashPressed)
        {
            m_ghostController.StartDash();
        }
    }
}
