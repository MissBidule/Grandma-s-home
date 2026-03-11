using PurrNet;
using PurrNet.Logging;
using Script.UI.Views;
using UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public struct ChildInputData
{
    public int tick;
    public Vector3 wishDirection;
    public float cameraYaw;
    public Vector3 cameraPosition;
    public Vector3 cameraForward;
    public bool jumpPressed;
    public bool switchPressed;
    public bool attackPressed;
    public bool sneakPressed;
}

public class ChildClientController : NetworkBehaviour
{
    [SerializeField] private GameObject m_uiHolder_prefab;
    public GameObject m_uiHolder;
    private CinemachineCamera m_playerCamera;
    private ChildInputController m_childInputController;
    private ChildController m_childController;
    private QteCircle m_qteCircle;
    private GameObject m_predictionPrefab;

    private bool m_jumpPressed = false;
    private bool m_switchWeaponPressed = false;
    private bool m_attackPressed = false;
    private bool m_sneakPressed = false;

    private PredictiveMovement m_predictiveMovement;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        m_predictiveMovement = GetComponent<PredictiveMovement>();
        m_childController = GetComponent<ChildController>();
    }

    protected override void OnOwnerChanged(PurrNet.PlayerID? oldOwner, PurrNet.PlayerID? newOwner, bool asServer)
    {
        if (isOwner) InitOwner();
    }

    private void InitOwner()
    {
        m_childInputController = GetComponent<ChildInputController>();
        if (m_uiHolder == null)
        {
            m_uiHolder = UnityProxy.InstantiateDirectly(m_uiHolder_prefab);
            if (!isServer) UnityProxy.InstantiateDirectly(m_predictionPrefab);
        }


        m_qteCircle = m_uiHolder.GetComponentInChildren<QteCircle>();
        // Use PlayerControllerCore.m_playerCamera (Inspector-assigned, always valid)
        // instead of GetComponentInChildren which can fail in multi-instance scenarios
        var core = GetComponent<PlayerControllerCore>();
        if (core != null) m_playerCamera = core.m_playerCamera;
        Debug.Log($"[ChildClientController] InitOwner - m_playerCamera: {m_playerCamera}, m_childInputController: {m_childInputController}");

        if (InstanceHandler.TryGetInstance(out UIsManager uisManager))
            uisManager.ShowView<ChildHUDView>();
    }

    void Update()
    {
        if (!isOwner) return;

        UpdateLabels();

        // DebugPrintTrafic();

        if (m_qteCircle.m_isRunning) return;
        var moveVec = m_childInputController.m_movementInputVector;
        var wishDir = GetDirectionIntention(moveVec);
        var cameraYaw = m_playerCamera.transform.eulerAngles.y;

        var inputData = new ChildInputData
        {
            tick = m_predictiveMovement.GetTick(),
            wishDirection = wishDir,
            cameraYaw = m_playerCamera.transform.eulerAngles.y,
            cameraPosition = m_playerCamera.transform.position,
            cameraForward = GetCameraForward(),
            jumpPressed = m_jumpPressed,
            switchPressed = m_switchWeaponPressed,
            attackPressed = m_attackPressed,
            sneakPressed = m_sneakPressed
        };

        m_predictiveMovement.NewInput(inputData);


        SendChildRPC(
            inputData
        );

        m_jumpPressed = false;
        m_switchWeaponPressed = false;
        m_attackPressed = false;
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

    void UpdateLabels()
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
        m_qteCircle.CheckSuccess();
    }

    public void OnSwitchWeapon()
    {
        if (!isOwner) return;
        m_switchWeaponPressed = true;
    }

    public void OnAttack()
    {
        if (!isOwner) return;
        m_attackPressed = true;
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
        if (_movement == Vector2.zero) return Vector3.zero;

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

    private Vector3 GetCameraForward() {
        Transform cameraTransform = m_playerCamera.transform;
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    [ServerRpc]
    private void SendChildRPC(ChildInputData _data)
    {
        m_childController.m_wishDir = _data.wishDirection;
        m_childController.m_cameraYaw = _data.cameraYaw;
        m_childController.m_cameraPosition = _data.cameraPosition;
        m_childController.m_cameraForward = _data.cameraForward;
        if (_data.jumpPressed) m_childController.Jump();
        if (_data.switchPressed) m_childController.SwitchAttackType();
        if (_data.attackPressed) m_childController.Attack();
        m_childController.m_isSneaking = _data.sneakPressed; 
    }
}
