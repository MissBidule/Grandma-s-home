using PurrNet;
using UnityEngine;

/*
 * @brief Manages the application of physics and state modifications for the Ghost entity structure during standard timesteps and prediction reconciliations.
 * @details Calculates climbing scenarios, acceleration, rotations, and dashes consistently within identical predictive simulation ticks.
 */
public class GhostSimulateMovement : NetworkBehaviour, ISimulateMovement
{
    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;
    [SerializeField] private float m_slowAmplitude = 0.5f;
    [SerializeField] private float m_dashAmplitude = 1.5f;
    [SerializeField] private float m_sneakAmplitude = 0.5f;

    [Header("Rotation")]
    [SerializeField] private float m_rotationSpeed = 12f;

    [Header("Auto Climb")]
    [SerializeField] private float m_climbSpeed = 3.5f;
    [SerializeField] private float m_climbCheckDistance = 0.6f;
    [SerializeField] private float m_wallNormalMaxY = 0.4f; 
    [SerializeField] private float m_raycastHeightOffset = 0.5f;
    [SerializeField] private LayerMask m_climbableLayerMask = ~0;

    private Rigidbody m_rigidbody;
    private GhostController m_ghostController;
    private QteCircle m_qteCircle;
    
    private bool m_canClimbThisFrame;
    private Vector3 m_wallNormal;

    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_ghostController = GetComponent<GhostController>();
    }

    /*
     * @brief Simulates the movement and rotation of the ghost based on predictive inputs
     * @param _input The predictive input data
     * @return void
     */
    public void SimulateMovement(PredictiveInputData _input)
    {
        if (m_ghostController == null) return;
        if (m_ghostController.m_isStopped) return;

        Vector3 wishDir = _input.wishDirection;

        float speedModifier = GetSpeedModifier(_input);

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

        if (CheckForClimbableWall())
        {
            m_canClimbThisFrame = true;
        }

        if (m_canClimbThisFrame &&
            !m_ghostController.m_isReviving &&
            wishDir.sqrMagnitude > 0.0001f)
        {
            Vector3 vel = m_rigidbody.linearVelocity;

            float targetUp = m_climbSpeed * speedModifier;
            vel.y = Mathf.Max(vel.y, targetUp);

            m_rigidbody.linearVelocity = vel;

            ResetClimbFlags();
            return;
        }

        Vector3 targetVel = speedModifier * m_walkSpeed * wishDir;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * (m_acceleration * speedModifier), m_acceleration);

        // When physics runs in re-simulation, adding force instantly might not compute as expected immediately,
        // but since we sync transforms and preserve linear velocity, Euler velocity integration directly works best.
        m_rigidbody.linearVelocity += new Vector3(accel.x, 0f, accel.z) * Time.fixedDeltaTime;

        ResetClimbFlags();
    }

    /*
     * @brief Calculates the speed modifier for the ghost based on its current statuses
     * @param _input The predictive input data containing sneak status
     * @return float The computed speed modifier
     */
    float GetSpeedModifier(PredictiveInputData _input)
    {
        float speedModifier = 1f;
        if (m_ghostController.m_isSlowed) speedModifier *= m_slowAmplitude;
        if (m_ghostController.m_isDashing) speedModifier *= m_dashAmplitude; // isDashing is from GhostController tracking Server-side dashes
        if (_input.sneakPressed) speedModifier *= m_sneakAmplitude;
        
        if (m_ghostController.m_isStopped || m_ghostController.m_isReviving) speedModifier = 0f;

        return speedModifier;
    }

    /*
     * @brief Checks if there is a climbable wall in front of the ghost
     * @return bool True if a climbable wall is detected, false otherwise
     */
    private bool CheckForClimbableWall()
    {
        if (m_qteCircle == null) m_qteCircle = FindAnyObjectByType<QteCircle>();
        if (m_qteCircle != null && m_qteCircle.m_isRunning)
        {
            return false;
        }
        
        Vector3 rayOrigin = transform.position + Vector3.up * m_raycastHeightOffset;
        Vector3 rayDirection = transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, m_climbCheckDistance, m_climbableLayerMask))
        {
            if (hit.normal.y <= m_wallNormalMaxY || hit.transform.gameObject.layer == LayerMask.NameToLayer("Stairs"))
            {
                m_wallNormal = hit.normal;
                return true;
            }
        }
        return false;
    }

    /*
     * @brief Resets the climbing flags and normal vector
     * @return void
     */
    private void ResetClimbFlags()
    {
        m_canClimbThisFrame = false;
        m_wallNormal = Vector3.zero;
    }
}
