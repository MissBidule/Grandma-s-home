using PurrNet;
using UnityEngine;

public class GhostSimulateMovement : NetworkBehaviour, ISimulateMovement
{
    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;
    [SerializeField] private float m_slowAmplitude = 0.5f;
    [SerializeField] private float m_dashAmplitude = 1.5f;

    [Header("Fly")]
    [SerializeField] private float m_flySpeed = 3.5f;

    [Header("Rotation")]
    [SerializeField] private float m_rotationSpeed = 12f;

    private Rigidbody m_rigidbody;
    private GhostController m_ghostController;
    private GhostMorph m_ghostMorph;
    private bool m_isFlying;

    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_ghostController = GetComponent<GhostController>();
        m_ghostMorph = GetComponent<GhostMorph>();
    }

    public void SimulateMovement(PredictiveInputData _input)
    {
        if (m_ghostController == null) return;
        if (m_ghostController.m_isStopped) return;

        Vector3 wishDir = _input.wishDirection;
        float speedModifier = GetSpeedModifier();

        if (wishDir.sqrMagnitude > 0.0001f
            && !m_ghostController.m_isStopped
            && !m_ghostController.m_isReviving
            && !m_rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY)
        )
        {
            Quaternion targetRotation = Quaternion.LookRotation(wishDir, Vector3.up);
            m_rigidbody.rotation = Quaternion.Slerp(
                m_rigidbody.rotation,
                targetRotation,
                m_rotationSpeed * Time.fixedDeltaTime
            );
        }

        // Re-enable fly when grounded while fly is disabled
        bool isGrounded = m_ghostController.IsGrounded();
        if (m_ghostController.m_isFlyDisabled && isGrounded)
            m_ghostController.RemoveFlyDisabledToAll();

        bool isMorphed = m_ghostMorph != null && m_ghostMorph.m_isMorphed;
        bool forceGravity = m_ghostController.m_isFlyDisabled || isMorphed;

        if (_input.jumpPressed || _input.sneakPressed) m_isFlying = true;
        if (forceGravity || (isGrounded && !_input.jumpPressed && !_input.sneakPressed)) m_isFlying = false;

        m_rigidbody.useGravity = !m_isFlying;

        // Vertical fly
        float verticalVel = 0f;
        if (m_isFlying)
        {
            if (_input.jumpPressed) verticalVel = m_flySpeed * speedModifier;
            else if (_input.sneakPressed) verticalVel = -m_flySpeed * speedModifier;
        }

        // Horizontal movement
        Vector3 targetVel = speedModifier * m_walkSpeed * wishDir;
        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * (m_acceleration * speedModifier), m_acceleration);

        float newY = m_isFlying ? verticalVel : currentVel.y;
        m_rigidbody.linearVelocity = new Vector3(
            currentVel.x + accel.x * Time.fixedDeltaTime,
            newY,
            currentVel.z + accel.z * Time.fixedDeltaTime
        );
    }

    float GetSpeedModifier()
    {
        float speedModifier = 1f;
        if (m_ghostController.m_isSlowed) speedModifier *= m_slowAmplitude;
        if (m_ghostController.m_isDashing) speedModifier *= m_dashAmplitude;
        if (m_ghostController.m_isStopped || m_ghostController.m_isReviving) speedModifier = 0f;
        return speedModifier;
    }
}
