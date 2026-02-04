using UnityEngine;

/**
@brief       Controller for the Ghost character
@details     Ghost can move and climb walls automatically when colliding with them.
*/
public class GhostController : MonoBehaviour
{
    [Header("References")]
    private GhostInputController m_ghostInputController;
    private GhostMorph m_ghostMorph;

    [SerializeField] private bool m_isSlowed = false;
    [SerializeField] public bool m_isStopped = false;
    [SerializeField] private float m_timerSlowed = 5f;
    [SerializeField] private float m_timerStop = 5f;
    [SerializeField] private float m_currentTimerSlowed = 5f;
    [SerializeField] private float m_currentTimerStop = 5f;

    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;
    private Vector2 m_moveDirection;

    [Header("Rotation")]
    [SerializeField] private float m_rotationSpeed = 12f;

    [Header("Auto Climb")]
    [SerializeField] private float m_climbSpeed = 3.5f;
    [SerializeField] private float m_wallNormalMaxY = 0.4f;
    [SerializeField] private float m_minPushDot = 0.2f;
    [SerializeField] private float m_maxClimbDuration = 0.20f;
    [SerializeField] private float m_minVelYToAllowNewClimb = 0.05f;

    private Rigidbody m_rigidbody;

    private bool m_canClimbThisFrame;
    private Vector3 m_wallNormal;
    private float m_climbTimer;

    private float speedModifier = 1f;


    private void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_ghostInputController = GetComponent<GhostInputController>();
        m_ghostMorph = GetComponent<GhostMorph>();
    }

    private void Update()
    {
        if (m_isSlowed)
        {
            m_currentTimerSlowed -= Time.deltaTime;
            if (m_currentTimerSlowed <= 0f)
            {
                m_isSlowed = false;
                m_currentTimerSlowed = m_timerSlowed;
            }
        }
        if (m_isStopped)
        {
            m_currentTimerStop -= Time.deltaTime;
            if (m_currentTimerStop <= 0f)
            {
                m_isStopped = false;
                m_currentTimerStop = m_timerStop;
            }
        }

        speedModifier = m_isSlowed ? 0.5f : 1f;
        speedModifier = m_isStopped ? 0f : speedModifier;

        if (m_ghostInputController.m_movementInputVector != Vector2.zero)
        {
            m_ghostMorph.RevertToOriginal();
        }
    }

    public bool IsGrounded()
    {
        if (m_rigidbody == null) return false;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.0f))
        {
            return true;
        }
        return false;
    }

    private void FixedUpdate()
    {
        Transform cam = Camera.main.transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector2 movementInput = m_ghostInputController.m_movementInputVector;
        Vector3 wishDir = (forward * movementInput.y + right * movementInput.x).normalized;
        

        if (wishDir.sqrMagnitude > 0.0001f && !m_isStopped)
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

        if (m_climbTimer > 0f)
        {
            m_climbTimer -= Time.fixedDeltaTime;

            Vector3 vel = m_rigidbody.linearVelocity;
            vel.y = Mathf.Max(vel.y, m_climbSpeed) * speedModifier;
            m_rigidbody.linearVelocity = vel;

            ResetClimbFlags();
            return;
        }

        if (m_canClimbThisFrame && wishDir.sqrMagnitude > 0.0001f)
        {
            float pushDot = Vector3.Dot(wishDir, -m_wallNormal);

            if (pushDot > m_minPushDot)
            {
                Vector3 vel = m_rigidbody.linearVelocity;

                if (vel.y <= m_minVelYToAllowNewClimb)
                {
                    m_climbTimer = m_maxClimbDuration;

                    float targetUp = m_climbSpeed * pushDot;
                    vel.y = Mathf.Max(vel.y, targetUp) * speedModifier;
                    m_rigidbody.linearVelocity = vel;

                    ResetClimbFlags();
                    return;
                }
            }
        }

        Vector3 targetVel = wishDir * m_walkSpeed * speedModifier;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * m_acceleration * speedModifier, m_acceleration);

        m_rigidbody.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

        ResetClimbFlags();
    }

    public void GotHitByProjectile()
    {
        m_isSlowed = true;
        m_currentTimerSlowed = m_timerSlowed;
    }

    public void GotHitByCac()
    {
        m_isStopped = true;
        m_currentTimerStop = m_timerStop;
    }

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
    @brief      Reset des flags de climb
    @return     void
    */
    private void ResetClimbFlags()
    {
        m_canClimbThisFrame = false;
        m_wallNormal = Vector3.zero;
    }
}
