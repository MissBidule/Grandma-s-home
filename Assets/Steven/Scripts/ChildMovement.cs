using UnityEngine;

/**
@brief       Déplacement enfant (ZQSD standard Unity) + course + saut
@details     Déplacement
*/
public class ChildMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_runSpeed = 7f;
    [SerializeField] private float m_acceleration = 25f;

    [Header("Jump")]
    [SerializeField] private float m_jumpImpulse = 6f;
    [SerializeField] private LayerMask m_groundMask;
    [SerializeField] private float m_groundCheckDistance = 0.25f;

    [Header("Input")]
    [SerializeField] private KeyCode m_runKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode m_jumpKey = KeyCode.Space;

    private Rigidbody m_rigidbody;

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (m_rigidbody == null) return;

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));

        Vector3 wishDir = new Vector3(input.x, 0f, input.y);
        if (wishDir.sqrMagnitude > 1f)
            wishDir.Normalize();

        wishDir = transform.TransformDirection(wishDir);

        float targetSpeed = Input.GetKey(m_runKey) ? m_runSpeed : m_walkSpeed;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 targetHorizontal = wishDir * targetSpeed;

        Vector3 delta = targetHorizontal - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * m_acceleration, m_acceleration);

        m_rigidbody.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);
    }

    private void Update()
    {
        if (m_rigidbody == null) return;

        if (Input.GetKeyDown(m_jumpKey) && IsGrounded())
        {
            Vector3 vel = m_rigidbody.linearVelocity;
            vel.y = 0f;
            m_rigidbody.linearVelocity = vel;

            m_rigidbody.AddForce(Vector3.up * m_jumpImpulse, ForceMode.Impulse);
        }
    }

    /**
    @brief      Vérifie si le joueur est au sol
    @return     true si un sol est détecté
    */
    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        return Physics.Raycast(
            origin,
            Vector3.down,
            m_groundCheckDistance + 0.05f,
            m_groundMask,
            QueryTriggerInteraction.Ignore
        );
    }
}
