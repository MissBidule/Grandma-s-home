using PurrNet;
using UnityEngine;

/*
 * @brief Manages the application of physics and state modifications for the Child entity layout during standard timesteps and prediction rollbacks.
 * @details Replaces standard continuous native FixedUpdate movements. Exposes a SimulateMovement that processes one structural tick of input natively via rigidbody parameters.
 */
public class ChildSimulateMovement : NetworkBehaviour, ISimulateMovement
{
    [SerializeField] public float m_speed = 5f;
    [SerializeField] private float m_acceleration = 25f;
    [SerializeField] private float m_jumpImpulse = 6.0f;
    public bool m_isScared = false;
    [SerializeField] private float m_scaredAmplitude = 0.5f;
    [SerializeField] private float m_sneakAmplitude = 0.5f;

    private Rigidbody m_rigidbody;
    private PredictiveMovement m_predictiveMovement;

    private ChildController m_childController;
    private NetworkAnimator m_animator;

    private JumpTriggerScript m_jumpTriggerScript;
    private bool m_isJumping = false;
    private bool m_jumpAppliedThisFrame = false;

    /*
     * @brief Initializes component references
     * @return void
     */
    void Start()
    {
        m_childController = GetComponent<ChildController>();
        m_animator = GetComponentInChildren<NetworkAnimator>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_predictiveMovement = GetComponent<PredictiveMovement>();
        m_jumpTriggerScript = GetComponentInChildren<JumpTriggerScript>();
    }

    /*
     * @brief Simulates the movement and rotation of the child based on predictive inputs
     * @param _input The predictive input data
     * @return void
     */
    public void SimulateMovement(PredictiveInputData _input)
    {
        // Rotation
        if (_input.cameraYaw != -1000)  // -1000 is the default value, meaning no input received
            transform.rotation = Quaternion.Euler(0, _input.cameraYaw, 0);

        // Movement with acceleration
        var speedModifier = GetSpeedModifier(_input.sneakPressed);
        Vector3 wishDir = _input.wishDirection;

        Vector3 targetVel = speedModifier * m_speed * wishDir;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * (m_acceleration * speedModifier), m_acceleration);

        m_rigidbody.linearVelocity += new Vector3(accel.x, 0f, accel.z) * Time.fixedDeltaTime;

        m_jumpAppliedThisFrame = false;
        if (_input.jumpPressed) 
        {
            Jump();
            m_jumpAppliedThisFrame = true;
        }
        
        // Clamp the player to the ground to prevent glitching through the floor during prediction errors
        ClampToGround();
    }

    /*
     * @brief Calculates the speed modifier for the child based on its statuses
     * @param _sneak The current sneak status
     * @return float The computed speed modifier
     */
    float GetSpeedModifier(bool _sneak)
    {
        var speedModifier = 1f;
        m_isScared = m_childController.m_isScared;
        if (_sneak) speedModifier *= m_sneakAmplitude;
        if (m_isScared) speedModifier *= m_scaredAmplitude;

        return speedModifier;
    }

    /*
    * @brief   Makes the child jump by applying an impulse force upwards
     * @return  void
     */
    public void Jump()
    {
        if (!IsGrounded()) return;
        if (m_isJumping) return;
        m_rigidbody.AddForce(Vector3.up * m_jumpImpulse, ForceMode.Impulse);
        m_childController.callChangeFace(new Vector2(0.66f, 0.66f));
        m_animator.SetTrigger("OnJump");
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
     * @brief Clamps the child's position to prevent glitching through the ground due to prediction errors
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
