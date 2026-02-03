using UnityEngine;

/*
 * @brief Contains class declaration for ChildController
 * @details The ChildController class handles child actions by reading input from the ChildInputController component.
 *          Movements are camera-relative and physics-based using Rigidbody (may change).
 */
public class ChildController : MonoBehaviour
{
    private ChildInputController m_childInputController;
    private Rigidbody m_rigidbody;
    private bool m_waitingForInputRelease = false;

    private Vector3 m_moveDirection;
    private BoxCollider m_boxCollider;
    private float m_cleanRange = 2f;
    private float m_attackRange = 0.5f;

    public bool m_isranged;


    [SerializeField] private float m_speed = 5f;
    [SerializeField] private float m_rotationSpeed = 10f;
    [SerializeField] private Transform m_bulletSpawnTransform;
    [SerializeField] private GameObject m_bulletPrefab;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Gets the ChildInputController component.
     * @return void
     */
    void Awake()
    {
        m_childInputController = GetComponent<ChildInputController>();
        m_rigidbody = GetComponent<Rigidbody>();
    }

    /*
     * @brief   Reads child input and computes movement direction relative to the camera
     * @return  void
     */
    void Update()
    {
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

        Transform cameraTransform = Camera.main.transform;

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
     * @brief   Applies movement and rotation using Rigidbody physics
     * @return  void
     */
    void FixedUpdate()
    {
        if (m_moveDirection == Vector3.zero) return;


        // GOOD WAY TO MOVE, BUT NOT SEPARATED BETWEEN CHILD AND GHOST SO COMMENTED UNTIL FIX.
        // GOOD WAY TO MOVE, BUT NOT SEPARATED BETWEEN CHILD AND GHOST SO COMMENTED UNTIL FIX.
        // GOOD WAY TO MOVE, BUT NOT SEPARATED BETWEEN CHILD AND GHOST SO COMMENTED UNTIL FIX.
        m_rigidbody.MovePosition(
            m_rigidbody.position + m_moveDirection * m_speed * Time.fixedDeltaTime
        );

        Quaternion targetRot = Quaternion.LookRotation(m_moveDirection);
        m_rigidbody.MoveRotation(
            Quaternion.Slerp(
                m_rigidbody.rotation,
                targetRot,
                m_rotationSpeed * Time.fixedDeltaTime
            )
        );
    }



    /*
     * @brief function called when the child inputs the hit command
     * @return void
     */
    public void Attacks()
    {
        if (m_isranged)
        {
            Shoot();
        }
        else
        {
            Cac();
        }

    }

    /*
     * @brief Enables the attack collider to detect hits.
     * @return void
     */
    private void Cac()
    {
        Collider[] hits = Physics.OverlapSphere(m_bulletSpawnTransform.position, m_attackRange);

        foreach (Collider col in hits)
        {
            var ghost = col.GetComponent<GhostController>();
            if (ghost != null)
            {
                HitOpponent(ghost);
            }
        }
    }

    /*
     * @brief Logic executed when hitting an opponent.
     * TODO: Implement actual hit logic
     * @return void
     */
    private void HitOpponent(GhostController _ghost)
    {
        print("tape un fantôme");
        _ghost.GotHitByCac();
    }


    /*
     * @brief  This function instantiates a ball prefab
     * We instantaneously transfer the ball and put the force into impulse mode.
     * @return void
     */

    void Shoot()
    {
        print("shoot");
        GameObject bullet = Instantiate(m_bulletPrefab, m_bulletSpawnTransform.position, transform.rotation);
        bullet.GetComponent<Rigidbody>().AddForce(m_bulletSpawnTransform.forward, ForceMode.Impulse);
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
}
