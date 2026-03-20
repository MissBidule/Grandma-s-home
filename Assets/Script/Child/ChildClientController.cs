using PurrNet;
using PurrNet.Logging;
using Script.UI.Views;
using UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChildClientController : NetworkBehaviour
{
    [SerializeField] private GameObject m_uiHolder_prefab;
    public GameObject m_uiHolder;
    private CinemachineCamera m_playerCamera;
    private ChildInputController m_childInputController;
    private ChildController m_childController;
    private QteCircle m_qteCircle;

    private bool m_jumpPressed = false;
    private bool m_switchWeaponPressed = false;
    private bool m_attackPressed = false;

    //Animations
    [SerializeField]private NetworkAnimator m_animator;
    [SerializeField] private GameObject m_racket;
    [SerializeField] private GameObject m_gun;
    private bool m_isMovingForward;
    private bool m_isMovingBackward;
    private bool m_isMovingLeft;
    private bool m_isMovingRight;
    private bool m_isAttacking;
    private bool m_isSneaking;
    private float m_attackTime;


    private bool m_sneakPressed = false;
    
    protected override void OnSpawned()
    {
        base.OnSpawned();
        m_childController = GetComponent<ChildController>();

        if (isOwner) InitOwner();
    }

    protected override void OnOwnerChanged(PurrNet.PlayerID? oldOwner, PurrNet.PlayerID? newOwner, bool asServer)
    {
        if (isOwner) InitOwner();
    }

    private void InitOwner()
    {
        m_childInputController = GetComponent<ChildInputController>();
        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.MuteGhostByChild();
        }
        if (m_uiHolder == null)
            m_uiHolder = UnityProxy.InstantiateDirectly(m_uiHolder_prefab);
            m_qteCircle = m_uiHolder.GetComponentInChildren<QteCircle>();
        // Use PlayerControllerCore.m_playerCamera (Inspector-assigned, always valid)
        // instead of GetComponentInChildren which can fail in multi-instance scenarios
        var core = GetComponent<PlayerControllerCore>();
        if (core != null) m_playerCamera = core.m_playerCamera;
        Debug.Log($"[ChildClientController] InitOwner - m_playerCamera: {m_playerCamera}, m_childInputController: {m_childInputController}");

        if (InstanceHandler.TryGetInstance(out UIsManager  uisManager))
            uisManager.ShowView<ChildHUDView>();
    }

    void Update()
    {
        if (!isOwner) return;

        m_childController.PingClient();

        if (m_childInputController != null)
        {

            UpdateHUD();

            // DebugPrintTrafic();

            if (m_childController.m_isScared && m_qteCircle.m_isRunning)
                m_qteCircle.CancelQte();

            if (m_qteCircle.m_isRunning) return;
            var moveVec = m_childInputController.m_movementInputVector;
            var wishDir = GetDirectionIntention(moveVec);
            var cameraYaw = m_playerCamera.transform.eulerAngles.y;


            SendChildRPC(
                wishDir,
                m_playerCamera.transform.eulerAngles.y,
                m_playerCamera.transform.position,
                m_playerCamera.transform.forward,
                m_jumpPressed,
                m_switchWeaponPressed,
                m_attackPressed,
                m_sneakPressed
            );

            m_jumpPressed = false;
            m_switchWeaponPressed = false;
            m_attackPressed = false;
        }
    }

    public void DebugPrintTrafic()
    {
        print("sended");
        print(m_childInputController.m_movementInputVector);
        print(GetDirectionIntention(m_childInputController.m_movementInputVector));
        print(m_playerCamera.transform.eulerAngles.y);
        print(m_jumpPressed);
        print(m_switchWeaponPressed);
        print(m_attackPressed);
    }

    void UpdateHUD()
    {
        if (!InstanceHandler.TryGetInstance(out ChildHUDView childHUDView))
            return;
        
        if (m_childController.m_isScared)
            childHUDView.StartScared(m_childController.GetScaredDuration());
        else childHUDView.m_isScared = false;
    }

    public void OnJump()
    {
        if (!isOwner) return;
        if (m_qteCircle.m_isRunning) return;
        m_jumpPressed = true;
    }

    public void OnValidation()
    {
        if (!isOwner) return;
        if (!m_qteCircle.m_isRunning) return;
        if (m_qteCircle.CheckSuccess())
        {
            //QTE finished
            RepairNotification();
        }
    }

    [ObserversRpc (requireServer: false)]
    public void RepairNotification()
    {
        InteractPromptUI.m_Instance.ShowRepair(m_childController.m_username);
    }

    public void OnEscape()
    {
        if (!isOwner) return;
        if (m_qteCircle != null && m_qteCircle.m_isRunning)
            m_qteCircle.CancelQte();
    }

    public void OnSwitchWeapon()
    {
        if (!isOwner) return;
        if(!m_childController.m_shootAnimRunning)
        {
            m_switchWeaponPressed = true;
            m_animator.SetTrigger("OnSwitch");
        }
    }



    /*
     * @brief  This function allows you to change the visible weapon in the player's hand.
     * @return void
     */

    public void ChangeVisibleWeapon()
    {
        m_racket.SetActive(!m_racket.activeInHierarchy);
        m_gun.SetActive(!m_gun.activeInHierarchy);
    }


    public void OnAttack()
    {
        if (!isOwner) return;
        m_attackPressed = true;
        m_animator.SetTrigger("OnAttack");
    }
    
    /*
     * @brief call the server to sneak
     */
    public void Sneak(bool _sneakStatus)
    {
        m_sneakPressed = _sneakStatus;
    }

    private Vector3 GetDirectionIntention(Vector2 _movement)
    {
        if(m_isSneaking != m_sneakPressed)
        {
            m_isMovingForward = false;
            m_isMovingBackward = false;
            m_isMovingLeft = false;
            m_isMovingRight = false;
            m_isSneaking = m_sneakPressed;
        }
        if (_movement == Vector2.zero)
        {
            if (m_isMovingForward || m_isMovingBackward || m_isMovingRight || m_isMovingLeft)
            {
                m_isMovingForward = false;
                m_isMovingBackward = false;
                m_isMovingLeft = false;
                m_isMovingRight = false;
                if (m_childController.m_isRanged)
                {
                    m_animator.CrossFadeInFixedTime("gun_idle", 0.2f, 0);
                }
                else
                {
                    m_animator.CrossFadeInFixedTime("cac_idle", 0.2f, 0);
                }
                m_childController.changeFaceMat(new Vector2(0,0));
            }
            return Vector3.zero;
        }
        if (Mathf.Abs(_movement.x) > Mathf.Abs(_movement.y))
        {
            if(m_isMovingRight == false && _movement.x > 0)
            {
                m_isMovingRight = true;
                m_isMovingLeft = false;
                m_isMovingForward = false;
                m_isMovingBackward = false;
                if (m_sneakPressed)
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_sideWalk_R", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_sideWalk_R", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.33f, 0));
                }
                else
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_sideRun_R", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_sideRun_R", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.66f, 0));
                }
            }
            else if(m_isMovingLeft == false && _movement.x < 0)
            {
                m_isMovingRight = false;
                m_isMovingLeft = true;
                m_isMovingForward = false;
                m_isMovingBackward = false;
                if (m_sneakPressed)
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_sideWalk_L", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_sideWalk_L", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.33f, 0));
                }
                else
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_sideRun_L", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_sideRun_L", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.66f, 0));
                }
            }
        }
        else
        {
            if (m_isMovingForward == false && _movement.y > 0)
            {
                m_isMovingForward = true;
                m_isMovingBackward = false;
                m_isMovingLeft = false;
                m_isMovingRight = false;
                if (m_sneakPressed)
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_walk", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_walk", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.33f, 0));
                }
                else
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_run", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_run", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.66f, 0));
                }
            }
            else if (m_isMovingBackward == false && _movement.y < 0)
            {
                m_isMovingForward = false;
                m_isMovingBackward = true;
                m_isMovingLeft = false;
                m_isMovingRight = false;
                if (m_sneakPressed)
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_bwalk", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_bwalk", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.33f, 0));
                }
                else
                {
                    if (m_childController.m_isRanged)
                    {
                        m_animator.CrossFadeInFixedTime("gun_brun", 0.2f, 0);
                    }
                    else
                    {
                        m_animator.CrossFadeInFixedTime("cac_brun", 0.2f, 0);
                    }
                    m_childController.changeFaceMat(new Vector2(.66f, 0));
                }
            }
        }
            
        var wishDir = Vector3.zero;

        if (_movement.sqrMagnitude < 0.001f) return wishDir;

        Transform cameraTransform = m_playerCamera.transform;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        wishDir = (forward * _movement.y + right * _movement.x).normalized;

        return wishDir;
    }

    private Vector3 GetCameraForward(){
        Transform cameraTransform = m_playerCamera.transform;
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    [ServerRpc]
    private void SendChildRPC(Vector3 _wishDirection, float _cameraYaw, Vector3 _cameraPosition, Vector3 _cameraForward,  bool _jumpPressed, bool _switchPressed, bool _attackPressed, bool _sneakPressed)
    {
        m_childController.m_wishDir = _wishDirection;
        m_childController.m_cameraYaw = _cameraYaw;
        m_childController.m_cameraPosition = _cameraPosition;
        m_childController.m_cameraForward = _cameraForward;
        if (_jumpPressed) m_childController.Jump();
        if (_switchPressed) m_childController.SwitchAttackType();
        if (_attackPressed) m_childController.Attack();
        m_childController.m_isSneaking = _sneakPressed;
    }
}
