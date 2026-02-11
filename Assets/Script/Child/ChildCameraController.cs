using UnityEngine;
using UnityEngine.UIElements;

/*
 * @brief       Contains class declaration for PlayerCameraController
 * @details     Handles third-person orbital camera controlled by mouse input with collision handling.
 */
public class ChildCameraController : MonoBehaviour
{
    public float m_sensitivity = 120f;
    public float m_distance = 4f;
    public float m_minPitch = -40f;
    public float m_maxPitch = 70f;
    public float m_collisionOffset = 0.2f;
    public LayerMask m_collisionMask;
    public Vector3 m_pivotOffset = new Vector3(0f, 1.6f, 0f); // approx head height

    private float m_yaw;
    private float m_pitch;

    private ChildInputController m_childInputController;
    private Transform m_target;
    private Rigidbody m_rigidbody;

    /*
     * @brief   Initializes references and locks the cursor
     * @return  void
    */
    private void Awake()
    {
        m_childInputController = GetComponentInParent<ChildInputController>();
        m_target = transform.parent;
        m_rigidbody = GetComponentInParent<Rigidbody>();

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    /*
     * @brief   Updates camera rotation and position after player movement
     * @return  void
    */
    private void LateUpdate()
    {
        Vector3 pivot = m_target.position + m_pivotOffset;
        Quaternion rotation;
        Vector3 desiredOffset;
        float finalDistance = m_distance;

        Vector2 lookInput = m_childInputController.m_lookInputVector;
        m_yaw += lookInput.x * m_sensitivity * Time.deltaTime;
        m_pitch -= lookInput.y * m_sensitivity * Time.deltaTime;
        m_pitch = Mathf.Clamp(m_pitch, m_minPitch, m_maxPitch);

        rotation = Quaternion.Euler(m_pitch, m_yaw, 0f);
        desiredOffset = rotation * Vector3.back * m_distance;
        finalDistance = m_distance;

        if (Physics.Raycast(
            pivot,
            desiredOffset.normalized,
            out RaycastHit hit2,
            m_distance,
            m_collisionMask))
        {
            finalDistance = hit2.distance - m_collisionOffset;
        }
        Vector3 finalOffset2 = rotation * Vector3.back * finalDistance;
        transform.position = pivot + finalOffset2;
        transform.LookAt(pivot);
    }
}
