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

    public SyncVar<Vector3> m_wishDir; // I dont think SyncVar is needed for this.
    public SyncVar<Vector3> m_lookDir; // Same
    private Vector3 lastLookDir;

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
        
        if (lastLookDir != m_lookDir)
        {
            //UpdateRotateForEveryone(m_lookDir);
            lastLookDir = m_lookDir;
        }
    }

    /*
     * @brief   Applies movement using Rigidbody physics
     * @return  void
     */
    void FixedUpdate()
    {
        if (!isServer) return;

        m_rigidbody.rotation = Quaternion.LookRotation(m_lookDir, Vector3.up);

        if (m_wishDir == Vector3.zero) return;


        m_rigidbody.MovePosition(
            m_rigidbody.position + m_wishDir.value * m_speed * Time.fixedDeltaTime
        );
    }

    // Called to bypass the latency from the server and have the child rotate immediately on the client side
    public void LocalRotation(Vector3 _lookDir)
    {
        m_rigidbody.rotation = Quaternion.LookRotation(_lookDir, Vector3.up);
    }

    // Called when lookDir changes to update the rotation of the child on all clients except the one that initiated the change (since it already updated locally)
    // I think this is awful.
    // So I didn't put it to try, so I think rotation only work for local player and server, but not other clients.
    /*[ObserversRpc(runLocally:true)]
    public void UpdateRotateForEveryone(Vector3 _lookDir)
    {
        if (isOwner || isServer) return;
        m_rigidbody.rotation = Quaternion.LookRotation(_lookDir, Vector3.up);
    }*/

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
    private void Cac()
    {
        Collider[] hits = Physics.OverlapSphere(m_bulletSpawnTransform.position, m_attackRange);

        foreach (Collider col in hits)
        {
            var ghost = col.GetComponent<GhostController>();
            if (ghost != null)
            {
                HitOpponent();
                ghost.HitCac();
            }
            if (col.transform.parent) 
            {
                Debug.Log(col.transform.parent.gameObject.layer);
                if (col.transform.parent.gameObject.layer == LayerMask.NameToLayer("Ghost"))
                {
                    var ghost2 = col.transform.parent.gameObject.GetComponent<GhostMorph>();
                    if (ghost2 != null)
                        {   
                            ghost2.RevertToOriginal();
                        }
                }
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
    [ObserversRpc(runLocally:true)]
    void ShootForAll()
    {
        GameObject bullet = UnityProxy.InstantiateDirectly(m_bulletPrefab, m_bulletSpawnTransform.position, transform.rotation);
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
