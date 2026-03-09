using UnityEngine;
using UnityEngine.SceneManagement;

/**
@brief       Controller for the Ghost character
@details     Handles movement, rotation and wall climbing
*/
public class GhostController : MonoBehaviour
{
    // Network Variables
    [NonSerialized] public Vector3 m_wishDir;
    public bool m_isSlowed = false;
    public bool m_isStopped = false;
    public bool m_beingRevived = false;
    public bool m_isReviving = false;

    [Header("Ghost references")]
    private GhostMorph m_ghostMorph;
    private GhostDeathIndicator m_deathIndicator;

    [Header("Status Timers")]
    [SerializeField] private float m_timerSlowed;
    public float m_timerStop;
    private float m_currentTimerSlowed;
    private float m_currentTimerStop;

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

    [Header("Rotation")]
    [SerializeField] private float m_rotationSpeed = 12f;

    [Header("Auto Climb")]
    [SerializeField] private float m_climbSpeed = 3.5f;
    [SerializeField] private float m_wallNormalMaxY = 0.4f;

    [Header("Canva")]
    [SerializeField] public GameObject m_stopped;
    [SerializeField] public GameObject m_slowed;

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

        m_deathIndicator = GetComponent<GhostDeathIndicator>();
        m_rigidbody = GetComponent<Rigidbody>();

        if (!isServer) return;

        m_ghostMorph = GetComponent<GhostMorph>();


    }

    /**
    @brief      Update timers and movement modifiers
    */
    private void Update()
    {
        if (m_isSlowed)
        {
            m_currentTimerSlowed -= Time.deltaTime;
            if (m_currentTimerSlowed <= 0f)
            {
                m_isSlowed = false;
                m_slowed.SetActive(false);
                m_currentTimerSlowed = m_timerSlowed;
            }
        }

        if (m_isStopped)
        {
            m_currentTimerStop -= Time.deltaTime;
            if (m_currentTimerStop <= 0f)
            {
                m_isStopped = false;
                m_stopped.SetActive(false);
                m_currentTimerStop = m_timerStop;
            }
        }

        if (m_beingRevived)
        {
            UpdateRevive();
            return;
        }

        // Casting to Vector2 to ignore falling movement
        if ((Vector2)m_wishDir != Vector2.zero)
        {
            m_ghostMorph.RevertToOriginal();
        }
    }

    /**
    @brief      Check if the ghost is grounded
    @return     True if a surface is detected below the character
    */
    public bool IsGrounded()
    {
        if (m_rigidbody == null) return false;

        if (Physics.Raycast(transform.position, Vector3.down, out _, 1.0f))
            return true;

        return false;
    }

    /**
    @brief      Physics update handling movement and climbing
    @details    Applies camera-relative movement, rotation and automatic climbing
                when colliding with a wal
    */
    private void FixedUpdate()
    {
        if (m_rigidbody == null) return;
        if (m_ghostInputController == null) return;
        if (Camera.main == null) return;

        if (m_wishDir.sqrMagnitude > 0.0001f
            && !m_isStopped
            && !m_isReviving
            && !m_rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY)
        )
        {
            Quaternion targetRotation = Quaternion.LookRotation(m_wishDir, Vector3.up);

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector2 movementInput = m_ghostInputController.m_movementInputVector;

        Vector3 wishDir = Vector3.zero;
        if (movementInput.sqrMagnitude > 0.0001f)
            wishDir = (forward * movementInput.y + right * movementInput.x).normalized;

        if (wishDir.sqrMagnitude > 0.0001f && !m_isStopped && !m_rigidbody.constraints.HasFlag(RigidbodyConstraints.FreezeRotationY))
        {            
            Quaternion targetRotation = Quaternion.LookRotation(wishDir, Vector3.up);

            m_rigidbody.MoveRotation(
                Quaternion.Slerp(
                    m_rigidbody.rotation,
                    targetRotation,
                    m_rotationSpeed * Time.fixedDeltaTime
                )
            );
            
        }

        if (!m_isStopped &&
            m_canClimbThisFrame &&
            wishDir.sqrMagnitude > 0.0001f)
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

        Vector3 targetVel = wishDir * m_walkSpeed * m_speedModifier;

        Vector3 currentVel = m_rigidbody.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        Vector3 delta = targetVel - currentHorizontal;
        Vector3 accel = Vector3.ClampMagnitude(delta * (m_acceleration * m_speedModifier), m_acceleration);

        m_rigidbody.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

        ResetClimbFlags();
    }

    /**
    @brief      Apply slow effect from projectile hit
    */
    public void GotHitByProjectile()
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
        if (m_isSlowed) m_speedModifier *= 0.5f;
        // Place between those lines the speedModifier change for when the player will "dash" / "sprint"
        if (m_isStopped || m_isReviving) m_speedModifier = 0f;
    }

    /**
    @brief      Apply stop effect from close combat hit
    */
    public void GotHitByCac()
    {
        m_isStopped = true;
        m_stopped.SetActive(true);
        m_currentTimerStop = m_timerStop;
    }

    /**
    @brief      Detect climbable wall during collision
    */
    private void OnCollisionStay(Collision _collision)
    {
        for (int i = 0; i < _collision.contactCount; i++)
        {
            Vector3 normal = _collision.GetContact(i).normal;

            if (normal.y <= m_wallNormalMaxY)
            {
                m_canClimbThisFrame = true;
                m_wallNormal = normal;
                return;
            }
        }
    }

    /**
    @brief      Reset climb flags
    */
    private void ResetClimbFlags()
    {
        m_canClimbThisFrame = false;
        m_wallNormal = Vector3.zero;
    }

    /*
     * @brief  This function allows you to switch to the SampleScene. (DEBUG PURPOSES ONLY)
     * @return void
     */
    public void SwitchScene()
    {
        SceneManager.LoadScene("Scene_Child_Test");
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

    public void RevivingBuddy(float duration)
    {
        m_reviveDuration = duration;
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
        ForceRevive();
        if (!m_reviver.m_isStopped)
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void CompleteRevive()
    {
        RequestReviveRpc();
        CancelRevive();
    }

    public void OnInteract(GhostInteract _who)
    {
        if (!isServer) return;
        if (m_isReviving) return;
        if (m_isStopped)
        {
            m_reviver = _who.GetComponentInParent<GhostController>();
            StartRevive(m_reviver);
        }
    }

    public void OnStopInteract(GhostInteract _who)
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

    public void OnFocus(GhostInteract who)
    {
        print("Found dead ghost");
    }

    public void OnUnfocus(GhostInteract who)
    {
        print("Lost focus on dead ghost");
    }
}
