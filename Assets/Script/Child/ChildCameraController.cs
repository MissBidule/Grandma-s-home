using PurrNet;
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
    public float m_collisionRadius = 0.2f;
    public float m_cameraSnapSpeed = 15f;
    public float m_cameraReturnSpeed = 4f;
    public LayerMask m_collisionMask;
    public Vector3 m_pivotOffset = new Vector3(0f, 1.6f, 0f); // approx head height

    public float m_yaw;
    private float m_pitch;
    private float m_currentDistance;
    [SerializeField] private float m_xOffset;

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

        m_sensitivity = PlayerPrefs.GetFloat("Settings_MouseSensitivity", PurrLobby.AccessibilitySettingsPanel.DefaultSensitivity);
        m_currentDistance = m_distance;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()  => PurrLobby.AccessibilitySettingsPanel.OnSensitivityChanged += OnSensitivityChanged;
    private void OnDisable() => PurrLobby.AccessibilitySettingsPanel.OnSensitivityChanged -= OnSensitivityChanged;
    private void OnSensitivityChanged(float v) => m_sensitivity = v;

    /*
     * @brief   Updates camera rotation and position after player movement
     * @return  void
    */
    private void LateUpdate()
    {
        Vector3 pivot = m_target.position + m_target.right * m_xOffset + m_pivotOffset;
        Quaternion rotation;
        Vector3 desiredOffset;

        Vector2 lookInput = m_childInputController.m_lookInputVector;
        m_yaw += lookInput.x * m_sensitivity * Time.deltaTime;
        m_pitch -= lookInput.y * m_sensitivity * Time.deltaTime;
        m_pitch = Mathf.Clamp(m_pitch, m_minPitch, m_maxPitch);

        rotation = Quaternion.Euler(m_pitch, m_yaw, 0f);
        desiredOffset = rotation * Vector3.back * m_distance;
        float finalDistance = m_distance;

        if (Physics.SphereCast(
            pivot,
            m_collisionRadius,
            desiredOffset.normalized,
            out RaycastHit hit,
            m_distance,
            m_collisionMask))
        {
            finalDistance = Mathf.Max(0f, hit.distance - m_collisionOffset);
        }

        float speed = finalDistance < m_currentDistance ? m_cameraSnapSpeed : m_cameraReturnSpeed;
        m_currentDistance = Mathf.Lerp(m_currentDistance, finalDistance, speed * Time.deltaTime);

        Vector3 finalOffset = rotation * Vector3.back * m_currentDistance;
        transform.position = pivot + finalOffset;
        transform.LookAt(pivot);
    }
}
