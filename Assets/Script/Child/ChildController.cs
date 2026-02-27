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
    private Rigidbody m_rigidbody;

    public Vector3 m_wishDir;
    public float m_cameraYaw; 

    private float m_attackRange = 0.5f;

    private bool m_isranged;
    [SerializeField] private float m_cdGun = 0.2f;
    private float m_lastShot;
    [SerializeField] private float m_cdSwitch = 0.2f;
    private float m_switchingTime;


    [SerializeField] private float m_speed = 5f;
    [SerializeField] private Transform m_bulletSpawnTransform;
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private float m_jumpImpulse = 6.0f;
    [SerializeField] private float m_shootRange = 50f;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        m_rigidbody = GetComponent<Rigidbody>();
        if (!isServer) return;
        m_lastShot = m_cdGun;
        m_switchingTime = m_cdSwitch;

    }

    /*
     * @brief   Reads child input and computes movement direction relative to the camera
     * @return  void
     */
    void Update()
    {
        if (!isServer) return;
        m_lastShot += Time.deltaTime;
        m_switchingTime += Time.deltaTime;
        

        transform.rotation = Quaternion.Euler(0, m_cameraYaw, 0);




        m_rigidbody.MovePosition(
            m_rigidbody.position + m_wishDir * m_speed * Time.deltaTime
        );


    }

    /*
     * @brief   Makes the child jump by applying an impulse force upwards
     * @return  void
     */
    public void Jump()
    {
        if (!isServer) return;
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

    /*
     * @brief function called when the child inputs the hit command
     * @return void
     */
    public void Attack()
    {
        if (!isServer) return;
        if (m_switchingTime < m_cdSwitch) return;
        if (m_isranged)
        {
            if (m_lastShot >= m_cdGun)
            {
                m_lastShot = 0;
                Debug.Log("shoot");
                ShootForAll();
            }
        }
        else
        {
            Cac();
            Debug.Log("cac");
        }

    }

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
            var ghost = col.GetComponent<GhostController>();
            if (ghost != null)
            {
                ghost.HitCac();
            }
            if (col.transform.parent) 
            {
                if (col.transform.parent.gameObject.layer == LayerMask.NameToLayer("Ghost"))
                {
                    var ghostMorph = col.transform.parent.gameObject.GetComponent<GhostMorph>();
                    if (ghostMorph != null)
                        {
                            ghostMorph.RevertToOriginal();
                        }
                }
            }
        }
    }


    /*
     * @brief  Instantiates a bullet from the character aimed at where the camera points
     * @return void
     */
    [ObserversRpc(runLocally:true)]
    void ShootForAll()
    {
        Vector3 camOrigin = m_playerCamera.transform.position;
        Vector3 camDirection = m_playerCamera.transform.forward;

        // Find aim target via camera raycast
        Vector3 aimTarget;
        if (Physics.Raycast(camOrigin, camDirection, out RaycastHit hit, m_shootRange))
            aimTarget = hit.point;
        else
            aimTarget = camOrigin + camDirection * m_shootRange;

        // Orient bullet from spawn point toward aim target
        Vector3 shootDirection = (aimTarget - m_bulletSpawnTransform.position).normalized;
        GameObject bullet = UnityProxy.InstantiateDirectly(m_bulletPrefab, m_bulletSpawnTransform.position, Quaternion.LookRotation(shootDirection));
        if (isServer)
        {
            Bullet bScript = bullet.GetComponent<Bullet>();
            bScript.m_amIServerSide = true;
        }
    }

    /*
     * @brief  This function allows you to switch between melee and ranged attack modes.
     * @return void
     */
    public void SwitchAttackType()
    {
        if (!isServer) return;
        m_isranged = !m_isranged;
        m_switchingTime = 0;
    }
}
