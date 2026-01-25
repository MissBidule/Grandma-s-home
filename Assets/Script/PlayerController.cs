using UnityEngine;


/*
 * @brief       Containts class declaration for PlayerController
 * @details     The PlayerController class handles player actions by reading input from the PlayerInputController component.
 *              Movements are camera-relative and physics-based using Rigidbody.
*/
public class PlayerController : MonoBehaviour
{
    public float m_speed = 5f;
    public float m_rotationSpeed = 10f;

    private PlayerInputController m_playerInputController;
    private Rigidbody m_rigidbody;

    private Vector3 m_moveDirection;

    void Awake()
    {
        m_playerInputController = GetComponent<PlayerInputController>();
        m_rigidbody = GetComponent<Rigidbody>();
    }

    /*
     * @brief   Reads player input and computes movement direction relative to the camera
     * @return  void
    */
    void Update()
    {
        Vector2 movementInput = m_playerInputController.m_movementInputVector;
        if (movementInput.sqrMagnitude < 0.01f)
        {
            m_moveDirection = Vector3.zero;
            return;
        }

        Transform cameraTransform = Camera.main.transform;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        m_moveDirection =
            cameraForward * movementInput.y +
            cameraRight * movementInput.x;
    }

    /*
     * @brief   Applies movement and rotation using Rigidbody physics
     * @return  void
    */
    void FixedUpdate()
    {
        if (m_moveDirection == Vector3.zero) return;

        m_rigidbody.MovePosition(
            m_rigidbody.position + m_moveDirection * m_speed * Time.fixedDeltaTime
        );

        Quaternion targetRot = Quaternion.LookRotation(m_moveDirection);
        m_rigidbody.MoveRotation(
            Quaternion.Slerp(
                m_rigidbody.rotation,
                targetRot,
                m_rotationSpeed * Time.fixedDeltaTime
            )
        );
    }
}
