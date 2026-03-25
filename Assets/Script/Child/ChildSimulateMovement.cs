using PurrNet;
using UnityEngine;

public class ChildSimulateMovement : NetworkBehaviour, ISimulateMovement
{
    [SerializeField] private float m_speed = 5f;
    [SerializeField] private float m_jumpImpulse = 6.0f;
    public bool m_isScared = false;
    [SerializeField] private float m_scaredAmplitude = 0.5f;
    [SerializeField] private float m_sneakAmplitude = 0.5f;

    private Rigidbody m_rigidbody;
    private PredictiveMovement m_predictiveMovement;


    void Start()
    {
        m_childController = GetComponent<ChildController>();
        m_animator = GetComponent<NetworkAnimator>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_predictiveMovement = GetComponent<PredictiveMovement>();
    }

    public void SimulateMovement(PredictiveInputData _input)
    {

        // Rotation
        if (_input.cameraYaw != -1000)  // -1000 is the default value, meaning no input received
            transform.rotation = Quaternion.Euler(0, _input.cameraYaw, 0);

        // Movement
        var speedModifier = GetSpeedModifier(_input.sneakPressed);
        Vector3 movement = _input.wishDirection * (m_speed * Time.fixedDeltaTime * speedModifier);

        m_rigidbody.position += movement;

        if (_input.jumpPressed) Jump();
    }

    float GetSpeedModifier(bool _sneak)
    {
        var speedModifier = 1f;
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
        if(m_isJumping) return;
        if (!IsGrounded()) return;
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
        if (Physics.Raycast(transform.position, Vector3.down, out _, 1.0f) || (m_jumpTriggerScript.m_colliders.Count > 0 && m_rigidbody.linearVelocity.y == 0f))
        {
            if (m_rigidbody.linearVelocity.y < 1.0E-07f && m_rigidbody.linearVelocity.y > -1.0E-07f)
            {
                m_isJumping = false;
            }
            return true;
        }
        else return false;
    }





}
