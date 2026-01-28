using UnityEngine;

/**
@brief       Déplacement fantôme + auto-climb limité
@details     Le fantôme se déplace relativement à la caméra et grimpe automatiquement
             en poussant n'importe quel mur (sans mask), avec une durée limitée.
*/
public class GhostMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;

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

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (m_rigidbody == null) return;

        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        Vector3 wishDir = Vector3.zero;

        if (input.sqrMagnitude > 0.0001f)
        {
            Transform cam = Camera.main.transform;

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            wishDir = (forward * input.y + right * input.x).normalized;
        }

        if (wishDir.sqrMagnitude > 0.0001f)
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
            vel.y = Mathf.Max(vel.y, m_climbSpeed);
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
                    vel.y = Mathf.Max(vel.y, targetUp);
                    m_rigidbody.linearVelocity = vel;

                    ResetClimbFlags();
                    return;
                }
            }
        }

        Vector3 targetVel = wishDir * m_walkSpeed;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * m_acceleration, m_acceleration);

        m_rigidbody.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

        ResetClimbFlags();
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
