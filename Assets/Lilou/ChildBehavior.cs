using UnityEngine;

/**
 * @brief determines what is a child
 */
public class ChildBehavior : MonoBehaviour
{
    private Transform m_playerTransform;
    private Rigidbody m_playerRigidBody;

    private void Awake()
    {
        m_playerTransform = GetComponent<Transform>();
        m_playerRigidBody = GetComponent<Rigidbody>();
    }

    public void Setup()
    {
        if (m_playerRigidBody != null)
            m_playerRigidBody.useGravity = true;

        if (m_playerTransform != null)
            m_playerTransform.localScale = Vector3.one * 1f;
    }
}
