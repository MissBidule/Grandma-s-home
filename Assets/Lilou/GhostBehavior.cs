using UnityEngine;

/**
 * @brief       determines what is a ghost
 */
public class GhostBehavior : MonoBehaviour
{
    private Transform m_playerTransform;
    private Rigidbody m_playerRigidBody;

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
        m_playerRigidBody.useGravity = false;
        m_playerTransform.localScale = new Vector3(.5f, .5f, .5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
