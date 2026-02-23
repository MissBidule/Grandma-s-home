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

    private Vector3 lastWishDir = Vector3.zero;
    private Vector3 lastCameraForward = Vector3.zero;

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

        var cameraForward = GetCameraForward();
        if (cameraForward != lastCameraForward) // Only send RPC when the camera forward direction changes
        {
            LookRPC(cameraForward);
            m_childController.LocalRotation(cameraForward);
            lastCameraForward = cameraForward;

        }


        if (m_childInputController.m_movementInputVector != Vector2.zero)
        {
            Vector3 wishDirection = GetDirectionIntention(m_childInputController.m_movementInputVector);
            if (wishDirection != lastWishDir) // Prevents sending RPCs every frame when the direction hasn't changed
                UpdateDirectionIntentionRPC(wishDirection);

            lastWishDir = wishDirection;

        }
        else
        {
            if (lastWishDir != Vector3.zero) // Only send RPC when changing from moving to not moving
                UpdateDirectionIntentionRPC(Vector2.zero);

            lastWishDir = Vector3.zero;
        }
    }

    public void OnJump()
    {
        if (!isOwner) return;
        JumpRPC();
    }

    public void OnSwitchWeapon()
    {
        if (!isOwner) return;
        SwitchWeaponRPC();
    }

    public void OnAttack()
    {
        if (!isOwner) return;
        AttackRPC();
    }

    private Vector3 GetDirectionIntention(Vector2 _movement)
    {
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
    private void UpdateDirectionIntentionRPC(Vector3 _wishDirection)
    {
        m_childController.m_wishDir.value = _wishDirection;
    }

    [ServerRpc]
    private void LookRPC(Vector3 _forward)
    {
        m_childController.m_lookDir.value = _forward;
    }

    [ServerRpc]
    private void JumpRPC()
    {
        m_childController.Jump();
    }

    [ServerRpc]
    private void SwitchWeaponRPC()
    {
        m_childController.SwitchAttackType();
    }

    [ServerRpc]
    private void AttackRPC()
    {
        m_childController.Attack();
    }
}
