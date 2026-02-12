using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
@brief       Controller for the Ghost character
@details     Handles movement, rotation and wall climbing
*/
public class GhostController : PlayerControllerCore
{
    [Header("Ghost references")]
    private GhostInputController m_ghostInputController;
    private GhostMorph m_ghostMorph;

    public bool m_isSlowed = false;
    public bool m_isStopped = false;
    public float m_timerSlowed = 5f;
    public float m_timerStop = 5f;
    public float m_currentTimerSlowed = 5f;
    public float m_currentTimerStop = 5f;

    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;

    [Header("Rotation")]
    [SerializeField] private float m_rotationSpeed = 12f;

    [Header("Auto Climb")]
    [SerializeField] private float m_climbSpeed = 3.5f;
    [SerializeField] private float m_wallNormalMaxY = 0.4f;

    [Header("Canva")]
    [SerializeField] public GameObject m_stopped;
    [SerializeField] public GameObject m_slowed;

    private Rigidbody m_rigidbody;

    private bool m_canClimbThisFrame;
    private Vector3 m_wallNormal;

    private float m_speedModifier = 1f;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;
    }

    private void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_ghostInputController = GetComponent<GhostInputController>();
        m_ghostMorph = GetComponent<GhostMorph>();
    }

    /**
    @brief      Update timers and movement modifiers
    */
    private void Update()
    {
        if (m_isStopped)
        {
            m_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            m_currentTimerStop -= Time.deltaTime;
            m_slowed.SetActive(false);
            if (m_currentTimerStop <= 0f)
            {
                m_rigidbody.constraints = RigidbodyConstraints.None; 
                m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                m_isStopped = false;
                m_stopped.SetActive(false);
            }
        }
        else if (m_isSlowed)
        {
            m_currentTimerSlowed -= Time.deltaTime;
            if (m_currentTimerSlowed <= 0f)
            {
                m_isSlowed = false;
                m_slowed.SetActive(false);
            }
        }

        m_speedModifier = m_isSlowed ? 0.5f : 1f;
        m_speedModifier = m_isStopped ? 0f : m_speedModifier;

        if (m_ghostInputController.m_movementInputVector != Vector2.zero)
        {
            m_ghostMorph.RevertToOriginal();
        }
    }

    /**
    @brief      Check if the ghost is grounded
    @return     True if a surface is detected below the character
    */
    public bool IsGrounded()
    {
        if (m_rigidbody == null) return false;

        if (Physics.Raycast(transform.position, Vector3.down, out _, 1.0f))
            return true;

        return false;
    }

    /**
    @brief      Physics update handling movement and climbing
    @details    Applies camera-relative movement, rotation and automatic climbing
                when colliding with a wal
    */
    private void FixedUpdate()
    {
        if (m_rigidbody == null) return;
        if (m_ghostInputController == null) return;
        if (m_playerCamera == null) return;

        Transform cam = m_playerCamera.transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector2 movementInput = m_ghostInputController.m_movementInputVector;

        Vector3 wishDir = Vector3.zero;
        if (movementInput.sqrMagnitude > 0.0001f)
            wishDir = (forward * movementInput.y + right * movementInput.x).normalized;

        if (wishDir.sqrMagnitude > 0.0001f && !m_isStopped && !m_rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY))
        {            
            Quaternion targetRotation = Quaternion.LookRotation(wishDir, Vector3.up);

            m_rigidbody.MoveRotation(
                Quaternion.Slerp(
                    m_rigidbody.rotation,
                    targetRotation,
                    m_rotationSpeed * Time.fixedDeltaTime
                )
            );
            
        }

        if (!m_isStopped &&
            m_canClimbThisFrame &&
            wishDir.sqrMagnitude > 0.0001f)
        {
            Vector3 vel = m_rigidbody.linearVelocity;

            float targetUp = m_climbSpeed * m_speedModifier;
            vel.y = Mathf.Max(vel.y, targetUp);

            m_rigidbody.linearVelocity = vel;

            ResetClimbFlags();
            return;
        }

        Vector3 targetVel = wishDir * m_walkSpeed * m_speedModifier;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * (m_acceleration * m_speedModifier), m_acceleration);

        m_rigidbody.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

        ResetClimbFlags();
    }

    /**
    @brief      Detect climbable wall during collision
    */
    private void OnCollisionStay(Collision _collision)
    {
        for (int i = 0; i < _collision.contactCount; i++)
        {
            Vector3 normal = _collision.GetContact(i).normal;

            if (normal.y <= m_wallNormalMaxY)
            {
                m_canClimbThisFrame = true;
                m_wallNormal = normal;
                return;
            }
        }
    }

    /**
    @brief      Reset climb flags
    */
    private void ResetClimbFlags()
    {
        m_canClimbThisFrame = false;
        m_wallNormal = Vector3.zero;
    }
}
