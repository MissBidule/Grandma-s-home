using PurrNet;
using PurrNet.Logging;
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
    public bool m_isSneaking = false;
    public bool m_morphInputReleased = true;
    public bool m_beingRevived = false;
    public bool m_isReviving = false;
    public bool m_canDash = true;


    [Header("Ghost references")]
    private GhostMorph m_ghostMorph;
    private GhostDeathIndicator m_deathIndicator;

    [Header("Status Timers")]
    [SerializeField] private float m_timerSlowed;
    public float m_timerStop;
    private float m_currentTimerSlowed;
    private float m_currentTimerStop;
    
    [Header("Scary Parameters")]
    [SerializeField] [Tooltip("In seconds")] private float m_cdChildScare = 10f;
    public bool m_canScareChild = true;

    [Header("Revive")]
    public float m_baseReviveTime = 5f;
    public float m_maxReviveTime = 30f;
    private int m_deathCount = 0;
    private GhostController m_reviver = null;
    private float m_reviveTimer = 0f;
    public float m_reviveDuration = 0f;

    [Header("Movement")]
    [SerializeField] private float m_walkSpeed = 4f;
    [SerializeField] private float m_acceleration = 25f;
    [SerializeField] private float m_slowAmplitude = 0.5f;
    [SerializeField] private float m_dashAmplitude = 1.5f;
    [SerializeField] [Tooltip("In seconds")] private float m_dashDuration = 2.5f;
    // [SerializeField] [Tooltip("In seconds")] private float m_dashCooldown = 30.0f;
    [SerializeField] private float m_sneakAmplitude = 0.5f;
    

    [Header("Rotation")]
    [SerializeField] private float m_rotationSpeed = 12f;

    [Header("Auto Climb")]
    [SerializeField] private float m_climbSpeed = 3.5f;
    [SerializeField] private float m_climbCheckDistance = 0.6f;
    [SerializeField] private float m_wallNormalMaxY = 0.4f; 
    [SerializeField] private float m_raycastHeightOffset = 0.5f;
    [SerializeField] private LayerMask m_climbableLayerMask = ~0;
    private QteCircle m_qteCircle;

    private Rigidbody m_rigidbody;

    private bool m_canClimbThisFrame;
    private Vector3 m_wallNormal;

    private float m_speedModifier = 1f;
    
    public Action<bool, PlayerID> OnDeathChange; // true for death | false for resurrection

    // -------------------------------------------
    // --- Everything Down Here is Server-Side ---
    // ---  And should be checked by isServer  ---
    // -------------------------------------------

    protected override void OnSpawned()
    {
        base.OnSpawned();

        m_deathIndicator = GetComponent<GhostDeathIndicator>();
        m_rigidbody = GetComponent<Rigidbody>();

        if (!isServer) return;

        m_ghostMorph = GetComponent<GhostMorph>();

    }

    private void Update()
    {
        if (!isServer) return;
     
        UpdateTimers();

        SetSpeedModifier();

        if (m_beingRevived)
        {
            UpdateRevive();
            return;
        }

        // Casting to Vector2 to ignore falling movement
        if (m_morphInputReleased && (Vector2)m_wishDir != Vector2.zero)
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
            && !m_isReviving
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
            !m_isReviving &&
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
            // m_currentTimerStop -= Time.deltaTime;
            // if (m_currentTimerStop <= 0f)
            // {
            //     m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            //     RemoveStopToAll();
            // }
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

    private void UpdateRevive()
    {
        if (!m_isStopped || m_reviver == null || m_reviver.m_isStopped)
        {
            CancelRevive();
            return;
        }
        m_reviveTimer += Time.deltaTime;
        if (m_reviveTimer >= m_reviveDuration)
            CompleteRevive();
    }

    void SetSpeedModifier()
    {
        m_speedModifier = 1f;
        if (m_isSlowed) m_speedModifier *= m_slowAmplitude;
        if (m_isDashing) m_speedModifier *= m_dashAmplitude;
        if (m_isSneaking) m_speedModifier *= m_sneakAmplitude;
        // Place between those lines the speedModifier change for when the player will "dash" / "sprint"
        if (m_isStopped || m_isReviving) m_speedModifier = 0f;
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
        if (m_qteCircle == null) m_qteCircle = FindAnyObjectByType<QteCircle>();
        if (m_qteCircle != null && m_qteCircle.m_isRunning)
        {
            return false;
        }
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
    @brief Apply stop effect from close combat hit
    */
    public void HitCac()
    {
        if (!isServer) return;
        if (!owner.HasValue)
            return;
        PurrLogger.LogWarning("Ghost Died", this);
        OnDeathChange?.Invoke(true, owner.Value); // True because he dies
        ApplyStopToAll();
        m_currentTimerStop = m_timerStop;
    }

    [ObserversRpc(runLocally:true)]
    public void ApplyStopToAll()
    {
        m_isStopped = true;
        m_deathIndicator?.OnGhostDied();
    }

    public float GetReviveTime()
    {
        return Mathf.Min(m_baseReviveTime * (m_deathCount + 1), m_maxReviveTime);
    }

    public void ForceRevive()
    {
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        RemoveStopToAll();
    }

    [ObserversRpc(runLocally:true)]
    public void RemoveStopToAll()
    {
        m_isStopped = false;
        m_deathCount++;
    }

    [ObserversRpc(runLocally:true)]
    private void StartRevive(GhostController _reviver)
    {
        m_reviver = _reviver;
        if (m_reviver.m_isStopped) return;
        m_reviveDuration = GetReviveTime();
        m_reviveTimer = 0f;
        m_beingRevived = true;
        m_reviver.RevivingBuddy(m_reviveDuration);
        m_reviver.FreezeReviverRpc();
        if (InteractPromptUI.m_Instance != null) InteractPromptUI.m_Instance.Hide();
    }

    public void RevivingBuddy(float _duration)
    {
        m_reviveDuration = _duration;
        m_reviveTimer = 0f;
        m_isReviving = true;
    }

    
    [ObserversRpc(runLocally:true)]
    private void CancelRevive()
    {
        m_beingRevived = false;
        m_reviver.m_isReviving = false;
        m_reviveTimer = 0f;
        m_reviver.UnfreezeReviverRpc();
        m_reviver = null;
    }

    /**
    @brief      Tell the server to freeze the reviver's rigidbody
    */
    public void FreezeReviverRpc()
    {
        m_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    /**
    @brief      Tell the server to unfreeze the reviver's rigidbody (only if not downed)
    */
    private void UnfreezeReviverRpc()
    {
        if (!m_isStopped)
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }


    /**
   @brief      Tell the server to revive the target ghost
   */
    private void RequestReviveRpc()
    {
        if (!m_isStopped) return;
        if (!owner.HasValue)
            return;
        PurrLogger.LogWarning("Ghost Revive", this);
        OnDeathChange?.Invoke(false, owner.Value); // False because he undies
        ForceRevive();
        if (!m_reviver.m_isStopped)
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void CompleteRevive()
    {
        RequestReviveRpc();
        CancelRevive();
    }

    [ObserversRpc(runLocally:true)]
    public void ApplyDashToAll(bool _isDashing, bool _canDash)
    {
        m_isDashing = _isDashing;
        m_canDash = _canDash;
    }

    public void OnInteract(Interact _who)
    {
        if (!isServer) return;
        if (m_isReviving) return;
        if (m_isStopped)
        {
            m_reviver = _who.GetComponentInParent<GhostController>();
            StartRevive(m_reviver);
        }
    }

    public void OnStopInteract(Interact _who)
    {
        if (!isServer) return;
        if (m_beingRevived)
        {
            CancelRevive();
        }
    }

    [ServerRpc]
    private void ResetStoppedRPC()
    {
        m_isStopped = false;
        m_currentTimerStop = 0f;
    }

    public void OnFocus(Interact who)
    {
        print("Found dead ghost");
    }

    public void OnUnfocus(Interact who)
    {
        print("Lost focus on dead ghost");
    }
    
    
    public void StartDash()
    {
        if (!m_canDash)
        {
            // case when can't dash
            return;
        }
        
        ApplyDashToAll(true, false);
        
        StartCoroutine(DashDuration(m_dashDuration));
    }
    
    private IEnumerator DashDuration(float _duration)
    {
        yield return new WaitForSeconds(_duration);
        ApplyDashToAll(false, false);
    }

    public void StartSpookyScary()
    {
        m_canScareChild = false;
        ApplyScaryToAll(m_canScareChild);
        StartCoroutine(ScaryCooldown(m_cdChildScare));
    }
    
    private IEnumerator ScaryCooldown(float _duration)
    {
        yield return new WaitForSeconds(_duration);
        m_canScareChild = true;
        ApplyScaryToAll(m_canScareChild);
    }
    
    [ObserversRpc(runLocally:true)]
    public void ApplyScaryToAll(bool _canScare)
    {
        m_canScareChild = _canScare;
        Debug.Log(m_canScareChild + " from ApplyScaryToAll in GhostController");
    }

    public float GetScaryCooldownDuration()
    {
        return m_cdChildScare;
    }
}