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
    // Camera Parameters
    [NonSerialized] public Vector3 m_cameraPosition;
    [NonSerialized] public Vector3 m_cameraForward;
    
    [Header("Weapon Switching")]
    private bool m_isranged;
    private float m_lastShot;
    private float m_switchingTime;
    [SerializeField] private float m_cdSwitch = 0.2f;
    
    [Header("CAC parameters")]
    private float m_attackRange = 0.5f;
    [SerializeField] private LayerMask m_GhostLayerMask;
    
    [Header("Shooting parameters")]
    [SerializeField] [Tooltip("In seconds")] private float m_cdGun = 0.2f;
    [SerializeField] private Transform m_bulletSpawnTransform;
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private float m_shootRange = 50f;
    
    [Header("Speed Modifiers")]
    [SerializeField][Tooltip("Duration of scared by ghost in seconds")] private float m_scaredDuration = 5.0f;

    protected override void OnSpawned()
    {
        base.OnSpawned();

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
        UpdateTimers();
    }
    
    

    void UpdateTimers()
    {
        m_lastShot += Time.deltaTime;
        m_switchingTime += Time.deltaTime;
    }

    /*
     * @brief function called when the child inputs the hit command
     * @return void
     */
    public void Attack()
    {
        if (!isServer) return;
        //if (m_isScared) return; // Return if the player is scared
        if (m_switchingTime < m_cdSwitch) return;
        if (m_isranged)
        {
            if (m_lastShot >= m_cdGun)
            {
                m_lastShot = 0;
                Vector3 aimTarget;
                if (Physics.Raycast(m_cameraPosition, m_cameraForward, out RaycastHit hit, 50f))
                    aimTarget = hit.point;
                else
                    aimTarget = m_cameraPosition + m_cameraForward * 50f;
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
        PurrLogger.Log($"Trigger Enter {_other.gameObject.name} {_other.gameObject.layer.ToString()}", this);
        if ((m_GhostLayerMask.value & (1 << _other.gameObject.layer)) == 0) return;
        PurrLogger.Log($"Trigger Good Layer", this);
        if (!_other.gameObject.TryGetComponent(out GhostController ghost))
        {
            foreach (var component in _other.GetComponents<Component>())
            {
                PurrLogger.LogWarning($"{_other.gameObject.name} Component: {component.GetType().Name}", this);
            }
            return;   
        }
        PurrLogger.Log($"Ghost Found", this);
        if (ghost.m_isStopped) return;
        PurrLogger.Log($"Ghost Not stopped", this);
        Debug.Log(ghost.m_canScareChild + " from ChildController OnTriggerEnter");
        if (!ghost.m_canScareChild) return;
        PurrLogger.Log($"Ghost Can Scare", this);
        PurrLogger.Log("Ghost", this);
        ghost.StartSpookyScary();
        //GhostTouch();
    }
    
    /**
    @brief      Apply scared effect from ghost
    */
    /*private void GhostTouch()
    {
        if (!isServer) return;
        m_isScared = true;
        PurrLogger.Log("Ghost Touch", this);
        UpdateScaredToAll(m_isScared);
        StartCoroutine(ScaredTimer(m_scaredDuration));
    }
    
    [ObserversRpc(runLocally:true)]
    public void UpdateScaredToAll(bool _isScared)
    {
        m_isScared = _isScared;
    }*/

    /*
     * @brief Timer for scared debuff
     */
    private IEnumerator ScaredTimer(float _scaredDuration)
    {
        yield return new WaitForSeconds(_scaredDuration);
        //m_isScared = false;
        //UpdateScaredToAll(m_isScared);
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
        m_isranged = !m_isranged;
        m_switchingTime = 0;
    }
}
