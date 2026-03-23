using System;
using System.Collections;
using PurrNet;
using PurrNet.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/*
 * @brief Contains class declaration for ChildController
 * @details The ChildController class handles child actions by reading input from the ChildInputController component.
 *          Movements are camera-relative and physics-based using Rigidbody (may change).
 */
public class ChildController : PlayerControllerCore
{
    private Rigidbody m_rigidbody;

    public Vector3 m_wishDir;
    
    // Camera Parameters
    public float m_cameraYaw;
    [NonSerialized] public Vector3 m_cameraPosition;
    [NonSerialized] public Vector3 m_cameraForward;
    
    [Header("Weapon Switching")]
    public bool m_isRanged;
    public float m_lastShot;
    public float m_switchingTime;
    [SerializeField] public float m_cdSwitch = 0.2f;
    
    [Header("CAC parameters")]
    private float m_attackRange = 0.5f;
    [SerializeField] private LayerMask m_GhostLayerMask;
    
    [Header("Shooting parameters")]
    [SerializeField] [Tooltip("In seconds")] private float m_cdGun = 1.0f;
    [SerializeField] private Transform m_bulletSpawnTransform;
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private float m_shootRange = 50f;
    
    [Header("Speed Modifiers")]
    [SerializeField] private float m_speed = 5f;
    [SerializeField] private float m_jumpImpulse = 6.0f;
    public bool m_isScared = false;
    [SerializeField] private float m_scaredAmplitude = 0.5f;
    [SerializeField] [Tooltip("Duration of scared by ghost in seconds")] private float m_scaredDuration = 5.0f;
    public bool m_isSneaking = false;
    [SerializeField] private float m_sneakAmplitude = 0.5f;
    private float m_speedModifier = 1.0f; // Default speed modifier

    [Header("Animation")]
    [SerializeField] private NetworkAnimator m_animator;
    public bool m_shootAnimRunning = false;
    public MaterialInstance m_faceMat;




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
    private void Update()
    {
        if (!isServer) return;

        PingServer();
     
        UpdateTimers();
        
        transform.rotation = Quaternion.Euler(0, m_cameraYaw, 0);

        SetSpeedModifier();

        m_rigidbody.MovePosition(
            m_rigidbody.position + m_wishDir * (m_speed * Time.deltaTime * m_speedModifier)
        );


    }
    
    void SetSpeedModifier()
    {
        m_speedModifier = 1f;
        if (m_isSneaking) m_speedModifier *= m_sneakAmplitude;
        if (m_isScared) m_speedModifier *= m_scaredAmplitude;
    }

    void UpdateTimers()
    {
        m_lastShot += Time.deltaTime;
        m_switchingTime += Time.deltaTime;
        if (m_shootAnimRunning && m_switchingTime > m_cdSwitch)
        {
            changeAttackAnimStatusServer();
            changeAttackAnimStatusClient();
        }
    }

    /*
     * @brief   Makes the child jump by applying an impulse force upwards
     * @return  void
     */
    public void Jump()
    {
        if (!isServer) return;
        changeFaceMat(new Vector2(0.66f,0.66f));
        if (!IsGrounded() || m_rigidbody.linearVelocity.y > 0.1f) return;
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
        if (!isServer || m_isScared) return; // Return if the player is scared
        if (m_switchingTime < m_cdSwitch) return;
        changeFaceMat(new Vector2(0,0.33f));
        if (m_isRanged)
        {
            if (m_lastShot >= m_cdGun)
            {
                m_lastShot = 0;
                Debug.Log("shoot");
                Vector3 aimTarget;
                if (Physics.Raycast(m_cameraPosition, m_cameraForward, out RaycastHit hit, m_shootRange))
                    aimTarget = hit.point;
                else
                    aimTarget = m_cameraPosition + m_cameraForward * m_shootRange;
                Vector3 shootDir = (aimTarget - m_bulletSpawnTransform.position).normalized;
                ShootForAll(Quaternion.LookRotation(shootDir));
            }
        }
        else
        {
            Cac();
            Debug.Log("cac");
        }

    }
    
    
    
    /*
     * @brief   Called when the child collides with a ghost to apply the scared debuff, the collider is quite small to prevent from triggering while trying to hit a ghost with the bat
     * @return  void
     */
    public void CollideWithObject(Collider _other)
    {
        //PurrLogger.Log($"Trigger Enter {_other.gameObject.name} {_other.gameObject.layer.ToString()}", this);
        if ((m_GhostLayerMask.value & (1 << _other.gameObject.layer)) == 0) return;
        //PurrLogger.Log($"Trigger Good Layer", this);
        if (!_other.gameObject.TryGetComponent(out GhostController ghost))
        {
            //foreach (var component in _other.GetComponents<Component>())
            //{
            //    PurrLogger.LogWarning($"{_other.gameObject.name} Component: {component.GetType().Name}", this);
            //}
            return;   
        }
        //PurrLogger.Log($"Ghost Found", this);
        if (ghost.m_isStopped) return;
        //PurrLogger.Log($"Ghost Not stopped", this);
        //Debug.Log(ghost.m_canScareChild + " from ChildController OnTriggerEnter");
        if (!ghost.m_canScareChild) return;
        //PurrLogger.Log($"Ghost Can Scare", this);
        //PurrLogger.Log("Ghost", this);
        ghost.StartSpookyScary();
        GhostTouch();
    }
    
    /**
    @brief      Apply scared effect from ghost
    */
    private void GhostTouch()
    {
        if (!isServer) return;
        m_isScared = true;
        //PurrLogger.Log("Ghost Touch", this);
        UpdateScaredToAll(m_isScared);
        StartCoroutine(ScaredTimer(m_scaredDuration));
        changeFaceMat(new Vector2(0.33f,0.33f));
    }
    
    [ObserversRpc(runLocally:true)]
    public void UpdateScaredToAll(bool _isScared)
    {
        if (_isScared)
        {
            m_animator.SetTrigger("OnScared");
        }
        m_isScared = _isScared;
    }

    /*
     * @brief Timer for scared debuff
     */
    private IEnumerator ScaredTimer(float _scaredDuration)
    {
        yield return new WaitForSeconds(_scaredDuration);
        m_isScared = false;
        UpdateScaredToAll(m_isScared);
    }

    public float GetScaredDuration()
    {
        return m_scaredDuration;
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
                CacNotification(ghost);
            }
            if (col.transform.parent) 
            {
                if (col.transform.parent.gameObject.GetComponent<BrokeDecor>())
                {
                    var brokeDecor = col.transform.parent.gameObject.GetComponent<BrokeDecor>();
                    if(brokeDecor != null)
                    {
                        brokeDecor.Broke();
                    }
                }
            
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

    [ObserversRpc]
    private void CacNotification (GhostController _ghost)
    {
        InteractPromptUI.m_Instance.ShowKill(m_username, _ghost.m_username);
    }

    /*
     * @brief  Instantiates a bullet aimed at the camera's target point
     * @return void
     */
    [ObserversRpc(runLocally:true)]
    void ShootForAll(Quaternion rotation)
    {
        GameObject bullet = UnityProxy.InstantiateDirectly(m_bulletPrefab, m_bulletSpawnTransform.position, rotation);
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
        changeAttackAnimStatusServer();
        m_isRanged = !m_isRanged;
        m_switchingTime = 0;
        changeAttackAnimStatusClient();
    }

    /*
     * @brief  This function allows you to change the attack animation based on the current attack type.
     *         It is called when switching attack types to update the animation accordingly.
     * @return void
     */
    [ObserversRpc(runLocally:true)]
    public void changeAttackAnimStatusClient()
    {
        if (!isOwner) return;
        m_isRanged = !m_isRanged;
        m_shootAnimRunning = !m_shootAnimRunning;
    }

    /*
     * @brief  This function allows you to change the attack animation based on the current attack type.
     *         It is called when switching attack types to update the animation accordingly.
     * @return void
     */
    public void changeAttackAnimStatusServer()
    {
        if (!isOwner)
        {
            m_shootAnimRunning = !m_shootAnimRunning;
        }
    }

    /*
     * @brief  This function allows you to change the face material offset based on the current action (or lack thereof).
     *         It is called to get the server side of the action
     * @return void
     */
    [ServerRpc]
    public void callChangeFace(Vector2 _surfaceOffset)
    {
        changeFaceMat(_surfaceOffset);
    }

    /*
     * @brief  This function allows you to change the face material offset based on the current action (or lack thereof).
     * @return void
     */
    [ObserversRpc(runLocally:true)]
    public void changeFaceMat(Vector2 _surfaceOffset)
    {
        m_faceMat.surfaceOffset = _surfaceOffset;
    }
}
