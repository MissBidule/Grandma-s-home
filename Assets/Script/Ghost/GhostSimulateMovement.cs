using PurrNet;
using TMPEffects.Components;
using UnityEngine;

/*
 * @brief Manages the application of physics and state modifications for the Ghost entity structure during standard timesteps and prediction reconciliations.
 * @details Calculates climbing scenarios, acceleration, rotations, and dashes consistently within identical predictive simulation ticks.
 */
public class GhostSimulateMovement : NetworkBehaviour, ISimulateMovement
{
    [Header("Movement")]
    [SerializeField] public float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;
    [SerializeField] private float m_slowAmplitude = 0.5f;
    [SerializeField] private float m_dashAmplitude = 2f;
    [SerializeField] private float m_sneakAmplitude = 0.5f;
    [SerializeField] private float m_jumpImpulse = 6.0f;

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
    public bool m_isClimbing { get; private set; }
    private Vector3 m_wallNormal;

    private JumpTriggerScript m_jumpTriggerScript;
    private bool m_isJumping = false;
    private bool m_jumpAppliedThisFrame = false;

    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_jumpTriggerScript = GetComponentInChildren<JumpTriggerScript>();
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

        m_jumpAppliedThisFrame = false;
        if (_input.jumpPressed) 
        {
            Jump();
            m_jumpAppliedThisFrame = true;
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

            m_isClimbing = true;
            ResetClimbFlags();
            return;
        }

        m_isClimbing = false;


        Vector3 targetVel = speedModifier * m_walkSpeed * wishDir;
        
        m_ghostController.m_soundEffects?.SetWalkingSpeed(_input.sneakPressed ? 0 : (_input.wishDirection * (m_walkSpeed * speedModifier)).magnitude);

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * (m_acceleration * speedModifier), m_acceleration);

        // When physics runs in re-simulation, adding force instantly might not compute as expected immediately,
        // but since we sync transforms and preserve linear velocity, Euler velocity integration directly works best.
        m_rigidbody.linearVelocity += new Vector3(accel.x, 0f, accel.z) * Time.fixedDeltaTime;

        ResetClimbFlags();
        
        // Clamp the ghost to the ground to prevent glitching through the floor during prediction errors
        ClampToGround();
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

        if (Physics.SphereCast(rayOrigin, 0.2f, rayDirection, out RaycastHit hit, m_climbCheckDistance, m_climbableLayerMask))
        {
            if (hit.normal.y <= m_wallNormalMaxY)
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

    /*
    * @brief   Makes the child jump by applying an impulse force upwards
     * @return  void
     */
    public void Jump()
    {
        if (!IsGrounded()) return;
        if (m_isJumping) return;
        m_ghostController.m_soundEffects?.PlayJumpAudio();
        m_rigidbody.AddForce(Vector3.up * m_jumpImpulse, ForceMode.Impulse);
        m_isJumping = true;
    }


    /*
     * @brief   Checks if the child is grounded by casting a ray downwards
     * @return  bool True if grounded, false otherwise
     */
    public bool IsGrounded()
    {
        bool onGround = Physics.Raycast(transform.position, Vector3.down, out _, 1.0f)
                        || m_jumpTriggerScript.m_colliders.Count > 0;

        if (onGround && m_isJumping && Mathf.Abs(m_rigidbody.linearVelocity.y) < 0.2f)
            m_isJumping = false;

        return onGround && !m_isJumping;
    }

    /*
     * @brief Clamps the ghost's position to prevent glitching through the ground due to prediction errors
     * @return void
     */
    private void ClampToGround()
    {
        // Don't clamp if we just applied a jump impulse this frame, as the force hasn't been integrated yet
        // Also don't clamp if moving upward (already jumping or in mid-air)
        if (m_jumpAppliedThisFrame || m_rigidbody.linearVelocity.y > 0) return;
        
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2.0f))
        {
            float groundY = hit.point.y;
            Vector3 currentPos = m_rigidbody.position;
            
            if (currentPos.y < groundY)
            {
                m_rigidbody.position = new Vector3(currentPos.x, groundY, currentPos.z);
                // Stop downward velocity to prevent further sinking
                if (m_rigidbody.linearVelocity.y < 0)
                {
                    Vector3 vel = m_rigidbody.linearVelocity;
                    vel.y = 0;
                    m_rigidbody.linearVelocity = vel;
                }
            }
        }
    }
}
