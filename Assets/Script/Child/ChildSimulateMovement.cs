using PurrNet;
using UnityEngine;

public class ChildSimulateMovement : NetworkBehaviour
{
    private float tickRate = 1f / 60f;
    [SerializeField] private float m_speed = 5f;
    [SerializeField] private float m_jumpImpulse = 6.0f;
    public bool m_isScared = false;
    [SerializeField] private float m_scaredAmplitude = 0.5f;
    [SerializeField] private float m_sneakAmplitude = 0.5f;

    private Rigidbody m_rigidbody;

    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    public void SimulateMovement(ChildInputData _input)
    {
        // Rotation
        transform.rotation = Quaternion.Euler(0, _input.cameraYaw, 0);

        // Movement
        var speedModifier = GetSpeedModifier(_input.sneakPressed);
        Vector3 movement = _input.wishDirection * (m_speed * tickRate * speedModifier);

        m_rigidbody.MovePosition(
            m_rigidbody.position + movement
        );

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
        if (!IsGrounded()) return;
        m_rigidbody.AddForce(Vector3.up * m_jumpImpulse, ForceMode.Impulse);
    }

    /*
     * @brief   Checks if the child is grounded by casting a ray downwards
     * @return  bool True if grounded, false otherwise
     */
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, out _, 1.0f);
    }


}
