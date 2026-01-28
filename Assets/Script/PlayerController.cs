using UnityEngine;

/*
 * @brief Contains class declaration for PlayerController
 * @details The PlayerController class handles player actions by reading input from the PlayerInputController component.
 *          Movements are camera-relative and physics-based using Rigidbody.
 */
public class PlayerController : MonoBehaviour
{
    public float m_speed = 5f;
    public float m_rotationSpeed = 10f;

    private PlayerInputController m_playerInputController;
    private Rigidbody m_rigidbody;
    private PlayerGhost m_playerGhost;
    private bool m_waitingForInputRelease = false;

    private Vector3 m_moveDirection;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the PlayerInputController and PlayerGhost components.
     * @return void
     */
    void Awake()
    {
        m_playerInputController = GetComponent<PlayerInputController>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_playerGhost = GetComponent<PlayerGhost>();
    }

    /*
     * @brief   Reads player input and computes movement direction relative to the camera
     * @return  void
     */
    void Update()
    {
        Vector2 movementInput = m_playerInputController.m_movementInputVector;

        if (m_waitingForInputRelease)
        {
            if (movementInput == Vector2.zero)
            {
                m_waitingForInputRelease = false;
            }
            m_moveDirection = Vector3.zero;
            return;
        }

        if (m_playerGhost != null
            && m_playerGhost.m_isTransformed
            && movementInput != Vector2.zero)
        {
            m_playerGhost.RevertToOriginal();
        }

        if (m_playerGhost != null && m_playerGhost.m_isTransformed)
        {
            m_moveDirection = Vector3.zero;
            return;
        }

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

    /*
     * @brief Anchors the player in place
     * Called when the player confirms a transformation. Resets input and waits for release.
     * @return void
     */
    public void AnchorPlayer()
    {
        m_playerInputController.ResetMovementInput();
        m_waitingForInputRelease = true;
        m_moveDirection = Vector3.zero;
    }

    /*
     * @brief Unanchors the player
     * Called when reverting to original form. Allows movement again.
     * @return void
     */
    public void UnanchorPlayer()
    {
        m_waitingForInputRelease = false;
    }
}
