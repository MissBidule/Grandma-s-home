using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief       Contains class declaration for PlayerCameraController
 * @details     Handles third-person orbital camera controlled by mouse input with collision handling.
 */
public class GhostCameraController : MonoBehaviour
{
    public float m_sensitivity = 120f;
    public float m_gamepadSensitivity = 180f;
    public float m_distance = 4f;
    public float m_minPitch = -40f;
    public float m_maxPitch = 70f;
    public float m_collisionOffset = 0.2f;
    public float m_collisionRadius = 0.2f;
    public float m_cameraSnapSpeed = 15f;
    public float m_cameraReturnSpeed = 4f;
    public LayerMask m_collisionMask;
    public Vector3 m_pivotOffset = new Vector3(0f, 1.6f, 0f); // approximate head height

    private float m_yaw;
    private float m_pitch;
    private float m_currentDistance;
    private const float m_gamepadRatio = 7f;

    private GhostInputController m_ghostInputController;
    private GhostClientController m_ghostClientController;
    private GhostController m_ghostController;
    private Transform m_target;
    
    /*
     * @brief   Initializes references and locks the cursor
     * @return  void
    */
    private void Awake()
    {
        m_ghostInputController = GetComponentInParent<GhostInputController>();
        m_ghostClientController = GetComponentInParent<GhostClientController>();
        m_ghostController = GetComponentInParent<GhostController>();
        m_target = transform.parent;

        m_sensitivity = PlayerPrefs.GetFloat("Settings_MouseSensitivity", PurrLobby.AccessibilitySettingsPanel.DefaultSensitivity);
        m_gamepadSensitivity = m_sensitivity * m_gamepadRatio;
        m_currentDistance = m_distance;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    /*
     * @brief Subscribes to sensitivity change events from both menu and pause settings
     * @return void
    */
    private void OnEnable()
    {
        PurrLobby.AccessibilitySettingsPanel.OnSensitivityChanged += OnSensitivityChanged;
        SettingsCanvasController.OnSensitivityChanged += OnSensitivityChanged;
    }

    /*
     * @brief Unsubscribes from sensitivity change events
     * @return void
    */
    private void OnDisable()
    {
        PurrLobby.AccessibilitySettingsPanel.OnSensitivityChanged -= OnSensitivityChanged;
        SettingsCanvasController.OnSensitivityChanged -= OnSensitivityChanged;
    }

    /*
     * @brief  Applies the new sensitivity to mouse and gamepad (gamepad scaled by m_gamepadRatio)
     * @params float v the new mouse sensitivity value from the slider
     * @return void
    */
    private void OnSensitivityChanged(float v)
    {
        m_sensitivity = v;
        m_gamepadSensitivity = v * m_gamepadRatio;
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

        bool blockLookInput = (m_ghostClientController.m_wheel != null && m_ghostClientController.m_wheel.IsWheelOpen())
            || (m_ghostController != null && m_ghostController.m_isReviving);
        if (!blockLookInput)
        {
            Vector2 lookInput = m_ghostInputController.m_lookInputVector;
            float activeSensitivity = InputDeviceTracker.IsGamepadActive ? m_gamepadSensitivity : m_sensitivity;
            m_yaw += lookInput.x * activeSensitivity * Time.deltaTime;
            m_pitch -= lookInput.y * activeSensitivity * Time.deltaTime;
            m_pitch = Mathf.Clamp(m_pitch, m_minPitch, m_maxPitch);
        }

        rotation = Quaternion.Euler(m_pitch, m_yaw, 0f);
        desiredOffset = rotation * Vector3.back * m_distance;

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
