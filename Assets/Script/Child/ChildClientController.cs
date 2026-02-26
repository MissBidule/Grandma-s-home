using PurrNet;
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

    private bool m_jumpPressed = false;
    private bool m_switchWeaponPressed = false;
    private bool m_attackPressed = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        m_childController = GetComponent<ChildController>();

        if (!isOwner) return;
        m_childInputController = GetComponent<ChildInputController>();
        m_uiHolder = UnityProxy.InstantiateDirectly(m_uiHolder_prefab);
        m_playerCamera = GetComponentInChildren<CinemachineCamera>();
    }
    void Update()
    {
        if (!isOwner) return;

        // DebugPrintTrafic();

        SendChildRPC(
            GetDirectionIntention(m_childInputController.m_movementInputVector),
            m_playerCamera.transform.eulerAngles.y,
            m_jumpPressed,
            m_switchWeaponPressed,
            m_attackPressed
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

    public void OnJump()
    {
        if (!isOwner) return;
        m_jumpPressed = true;
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

    private Vector3 GetCameraForward(){
        Transform cameraTransform = m_playerCamera.transform;
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        return forward.normalized;
    }

    [ServerRpc]
    private void SendChildRPC(Vector3 _wishDirection, float _cameraYaw, bool _jumpPressed, bool _switchPressed, bool _attackPressed)
    {
        m_childController.m_wishDir = _wishDirection;
        m_childController.m_cameraYaw = _cameraYaw;
        if (_jumpPressed) m_childController.Jump();
        if (_switchPressed) m_childController.SwitchAttackType();
        if (_attackPressed) m_childController.Attack();
    }
}
