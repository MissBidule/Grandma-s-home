using UnityEngine;

/**
 * @brief       determines what is a child
 */
public class ChildBehavior : MonoBehaviour
{
    private Transform m_playerTransform;
    private Rigidbody m_playerRigidBody;
    private CapsuleCollider m_playerCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_playerTransform = GetComponent<Transform>();
        m_playerRigidBody = GetComponent<Rigidbody>();
    }

    /*
     * @brief Setup is called when changing the player type
     * @return void
     */
    public void Setup()
    {
        m_playerTransform.localScale = new Vector3(1, 1, 1);
        m_playerRigidBody.useGravity = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
