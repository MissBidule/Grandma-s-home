using PurrNet;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/**
@brief       Controller for the Ghost character
@details     Handles movement, rotation and wall climbing
*/
public class GhostController : PlayerControllerCore, IInteractable
{
    // Network Variables
    [NonSerialized] public Vector3 m_wishDir;
    public bool m_isSlowed = false;
    public bool m_isDashing = false;
    public bool m_isStopped = false;
    
    private bool m_CanDash = true;
    public int m_dashCounters = 1;
    public bool m_dashDisabled = false;


    [Header("Ghost references")]
    private GhostMorph m_ghostMorph;

    [Header("Status Timers")]
    [SerializeField] private float m_timerSlowed;
    [SerializeField] private float m_timerStop;
    private float m_currentTimerSlowed;
    private float m_currentTimerStop;

    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;
    [SerializeField] private float m_slowAmplitude = 0.5f;
    [SerializeField] private float m_dashAmplitude = 1.5f;
    [SerializeField] [Tooltip("In seconds")] private float m_dashDuration = 2.5f;
    [SerializeField] [Tooltip("In seconds")] private float m_dashCooldown = 30.0f;
    

    [Header("Rotation")]
    [SerializeField] private float m_rotationSpeed = 12f;

    [Header("Auto Climb")]
    [SerializeField] private float m_climbSpeed = 3.5f;
    [SerializeField] private float m_climbCheckDistance = 0.6f;
    [SerializeField] private float m_wallNormalMaxY = 0.4f; 
    [SerializeField] private float m_raycastHeightOffset = 0.5f;
    [SerializeField] private LayerMask m_climbableLayerMask = ~0;

    private Rigidbody m_rigidbody;

    private bool m_canClimbThisFrame;
    private Vector3 m_wallNormal;
    

    private float m_speedModifier = 1f;


    // -------------------------------------------
    // --- Everything Down Here is Server-Side ---
    // ---  And should be checked by isServer  ---
    // -------------------------------------------

    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (!isServer) return;
        m_rigidbody = GetComponent<Rigidbody>();
        m_ghostMorph = GetComponent<GhostMorph>();

    }

    private void Update()
    {
        if (!isServer) return;
     
        UpdateTimers();

        SetSpeedModifier();

        // Casting to Vector2 to ignore falling movement
        if ((Vector2)m_wishDir != Vector2.zero)
        {
            m_ghostMorph.RevertToOriginal();
        }
    }

    /**
    @brief      Physics update handling movement and climbing
    @details    Applies camera-relative movement, rotation and automatic climbing when facing a wall
    */
    private void FixedUpdate()
    {
        if (!isServer) return;
        if (m_isStopped) return;

        if (m_wishDir.sqrMagnitude > 0.0001f
            && !m_isStopped
            && !m_rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY)
        )
        {
            Quaternion targetRotation = Quaternion.LookRotation(m_wishDir, Vector3.up);

            m_rigidbody.MoveRotation(
                Quaternion.Slerp(
                    m_rigidbody.rotation,
                    targetRotation,
                    m_rotationSpeed * Time.fixedDeltaTime
                )
            );
        }

        if (CheckForClimbableWall())
        {
            m_canClimbThisFrame = true;
        }

        if (m_canClimbThisFrame &&
            m_wishDir.sqrMagnitude > 0.0001f)
        {
            Vector3 vel = m_rigidbody.linearVelocity;

            float targetUp = m_climbSpeed * m_speedModifier;
            vel.y = Mathf.Max(vel.y, targetUp);

            m_rigidbody.linearVelocity = vel;

            ResetClimbFlags();
            return;
        }

        Vector3 targetVel = m_speedModifier * m_walkSpeed * m_wishDir;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * (m_acceleration * m_speedModifier), m_acceleration);

        m_rigidbody.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

        ResetClimbFlags();
    }

    void UpdateTimers()
    {
        if (m_isStopped)
        {
            m_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            m_currentTimerStop -= Time.deltaTime;
            if (m_currentTimerStop <= 0f)
            {
                m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                RemoveStopToAll();
            }
        }
        
        if (m_isSlowed)
        {
            m_currentTimerSlowed -= Time.deltaTime;
            if (m_currentTimerSlowed <= 0f)
            {
                RemoveSlowToAll();
            }
        }
    }

    void SetSpeedModifier()
    {
        m_speedModifier = 1f;
        if (m_isSlowed) m_speedModifier *= m_slowAmplitude;
        if (m_isDashing) m_speedModifier *= m_dashAmplitude; 
        // Place between those lines the speedModifier change for when the player will "dash" / "sprint"
        if (m_isStopped) m_speedModifier = 0f;
    }

    /**
    @brief      Check if the ghost is grounded
    @return     True if a surface is detected below the character
    */
    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, out _, 1.0f);
    }

    /**
    @brief      Check for climbable wall in front of the player using raycast
    @return     True if a climbable wall is detected
    */
    private bool CheckForClimbableWall()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * m_raycastHeightOffset;
        Vector3 rayDirection = transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, m_climbCheckDistance, m_climbableLayerMask))
        {
            if (hit.normal.y <= m_wallNormalMaxY)
            {
                m_wallNormal = hit.normal;
                return true;
            }
        }
        return false;
    }

    

    /**
    @brief      Reset climb flags
    */
    private void ResetClimbFlags()
    {
        m_canClimbThisFrame = false;
        m_wallNormal = Vector3.zero;
    }

    /**
    @brief      Apply slow effect from projectile hit
    */
    public void HitRanged()
    {
        if (!isServer) return;
        ApplySlowToAll();
        m_currentTimerSlowed = m_timerSlowed;
    }

    [ObserversRpc(runLocally:true)]
    public void ApplySlowToAll()
    {
        m_isSlowed = true;
    }

    [ObserversRpc(runLocally:true)]
    public void RemoveSlowToAll()
    {
        m_isSlowed = false;
    }

    /**
    @brief      Apply stop effect from close combat hit
    */
    public void HitCac()
    {
        if (!isServer) return;
        ApplyStopToAll();
        m_currentTimerStop = m_timerStop;
    }

    [ObserversRpc(runLocally:true)]
    public void ApplyStopToAll()
    {
        m_isStopped = true;
    }

    [ObserversRpc(runLocally:true)]
    public void RemoveStopToAll()
    {
        m_isStopped = false;
    }


    public void OnInteract(GhostInteract _who)
    {
        if (!isServer) return;
        ResetStoppedRPC();
    }

    public void OnStopInteract(GhostInteract _who)
    {
        // Do nothing
    }

    [ServerRpc]
    private void ResetStoppedRPC()
    {
        m_isStopped = false;
        m_currentTimerStop = 0f;
    }

    public void OnFocus(GhostInteract who)
    {
        throw new NotImplementedException();
    }

    public void OnUnfocus(GhostInteract who)
    {
        throw new NotImplementedException();
    }
    
    
    public void StartDash()
    {
        if (!m_CanDash && !m_dashDisabled)
        {
            // case when can't dash
            return;
        }
        m_isDashing = true;
        m_CanDash = false;
        
        StartCoroutine(DashDuration(m_dashDuration));
    }
    
    private IEnumerator DashDuration(float _duration)
    {
        yield return new WaitForSeconds(_duration);
        m_isDashing = false;
        m_dashDisabled = true;
    }
}