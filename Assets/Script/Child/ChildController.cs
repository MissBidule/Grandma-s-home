using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * @brief Contains class declaration for ChildController
 * @details The ChildController class handles child actions by reading input from the ChildInputController component.
 *          Movements are camera-relative and physics-based using Rigidbody (may change).
 */
public class ChildController : PlayerControllerCore
{
    private ChildInputController m_childInputController;
    private Rigidbody m_rigidbody;
    private bool m_waitingForInputRelease = false;

    private Vector3 m_moveDirection;
    private BoxCollider m_boxCollider;
    private float m_cleanRange = 2f;
    private float m_attackRange = 0.5f;

    private bool m_isranged;
    [SerializeField] private float m_cdGun = 0.2f;
    private float m_lastShot;
    private float m_yaw;


    [SerializeField] private float m_speed = 5f;
    [SerializeField] private Transform m_bulletSpawnTransform;
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private float m_jumpImpulse = 6.0f;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;
    }
    
    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the ChildInputController component.
     * @return void
     */
    void Awake()
    {
        m_childInputController = GetComponent<ChildInputController>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_lastShot = m_cdGun;
    }

    /*
     * @brief   Reads child input and computes movement direction relative to the camera
     * @return  void
     */
    void Update()
    {
        m_lastShot += Time.deltaTime;
        Vector2 movementInput = m_childInputController.m_movementInputVector;
        if (m_waitingForInputRelease)
        {
            if (movementInput == Vector2.zero)
            {
                m_waitingForInputRelease = false;
            }
            m_moveDirection = Vector3.zero;
            return;
        }

        if (movementInput.sqrMagnitude < 0.01f)
        {
            m_moveDirection = Vector3.zero;
            return;
        }

        Transform cameraTransform = m_playerCamera.transform;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        m_moveDirection =
            cameraForward * movementInput.y +
            cameraRight * movementInput.x;
    }

    /*
     * @brief   Applies movement using Rigidbody physics
     * @return  void
     */
    void FixedUpdate()
    {
        if (m_moveDirection == Vector3.zero) return;
        m_rigidbody.MovePosition(
            m_rigidbody.position + m_moveDirection * m_speed * Time.fixedDeltaTime
        );
    }

    /*
     * @brief   Applies rotation using the mouse movement
     * @return  void
     */
    void LateUpdate()
    {
        Vector2 lookInput = m_childInputController.m_lookInputVector;
        m_yaw += lookInput.x * m_playerCamera.GetComponent<ChildCameraController>().m_sensitivity * Time.deltaTime;
        Quaternion targetRot = Quaternion.Euler(0f, m_yaw, 0f);
        float epsilon = 0.01f;
        if (Mathf.Abs(lookInput.x) > epsilon)
        {
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            m_rigidbody.transform.rotation = targetRot;
        }
        else
        {

            m_rigidbody.freezeRotation = true;
        }
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
        if (m_rigidbody == null) return false;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.0f))
        {
            return true;
        }
        return false;
    }

    /*
     * @brief function called when the child inputs the hit command
     * @return void
     */
    public void Attacks()
    {
        if (m_isranged) 
        {
            if (m_lastShot >= m_cdGun) 
            {
                m_lastShot = 0;
                Debug.Log("shoot");
                Shoot();
            }
        }
        else
        {
            Cac();
            Debug.Log("cac");
        }

    }

    //logic to implement
    /*
     * @brief Enables the attack collider to detect hits, asked on the server
     * @return void
     */
    [ServerRpc]
    private void Cac()
    {
        Collider[] hits = Physics.OverlapSphere(m_bulletSpawnTransform.position, m_attackRange);

        foreach (Collider col in hits)
        {
            var ghost = col.GetComponent<GhostStatus>();
            if (ghost != null)
            {
                HitOpponent();
                ghost.GotHitByCac();
            }
        }
    }

    /*
     * @brief Logic executed when hitting an opponent.
     * TODO: Implement actual hit logic
     * @return void
     */
    private void HitOpponent()
    {
        //anim only
    }


    /*
     * @brief  This function instantiates a ball prefab
     * We instantaneously transfer the ball and put the force into impulse mode.
     * @return void
     */
    void Shoot()
    {
        GameObject bullet = Instantiate(m_bulletPrefab, m_bulletSpawnTransform.position, transform.rotation);
    }

    /*
     * @brief  This function allows you to clean the slime
     * When the child is close to a distance of m_cleanRange and there is a gameObject with the tag "Slime", they destroy the gameObject.
     * @return void
     */
    public void Clean()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, m_cleanRange);

        foreach (Collider col in hits)
        {
            if (col.CompareTag("Slime"))
            {
                Destroy(col.gameObject);
                break;
            }
        }
    }

    /*
     * @brief  This function allows you to switch between melee and ranged attack modes.
     * @return void
     */
    public void SwitchAttackType()
    {
        m_isranged = !m_isranged;
    }
}
