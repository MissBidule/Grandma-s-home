using System;
using System.Collections;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Logging;
using TMPEffects.Components;
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
    [SerializeField] public GhostSoundEffects m_soundEffects;

    [Header("Status Timers")]
    [SerializeField] private float m_timerSlowed;
    public float m_timerStop;
    private float m_currentTimerSlowed;
    private float m_currentTimerStop;
    
    [Header("Scary Parameters")]
    [SerializeField] [Tooltip("In seconds")] private float m_cdChildScare = 10f;
    public bool m_canScareChild = true;

    [Header("Revive")]
    public float m_baseReviveTime = 3f;
    public float m_maxReviveTime = 20f;
    private int m_deathCount = 0;
    private GhostController m_reviver = null;
    private float m_reviveTimer = 0f;
    public float m_reviveDuration = 0f;
    private bool m_isFocused = false;
    [SerializeField] private string m_promptLabelRevive = "Revive";

    [Header("Highlight")]
    [SerializeField] private List<Renderer> m_highlightRenderers = new List<Renderer>();
    [SerializeField] private Color m_highlightColor = Color.green;
    [SerializeField] private float m_pulseSpeed = 3f;
    [SerializeField] private float m_minIntensity = 0.2f;
    [SerializeField] private float m_maxIntensity = 1f;
    private Coroutine m_pulseCoroutine;
    private MaterialPropertyBlock m_propertyBlock;


    [Header("Abilities Parameters")]
    [SerializeField] [Tooltip("In seconds")] private float m_dashDuration = 2.5f;
    [SerializeField] [Tooltip("In seconds")] private float m_dashCooldown = 20f;
    private float m_currentDashCooldown = 0f;

    private Rigidbody m_rigidbody;

    public Action<bool, PlayerID> OnDeathChange; // true for death | false for resurrection


    [Header("Animation")]
    [SerializeField] private NetworkAnimator m_animator;

    // -------------------------------------------
    // --- Everything Down Here is Server-Side ---
    // ---  And should be checked by isServer  ---
    // -------------------------------------------

    protected override void OnSpawned()
    {
        base.OnSpawned();

    }

    public void Start()
    {

        m_deathIndicator = GetComponent<GhostDeathIndicator>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_propertyBlock = new MaterialPropertyBlock();

        if (m_highlightRenderers.Count > 0)
        {
            foreach (Renderer r in m_highlightRenderers)
            {
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat != null)
                    {
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
        SetHighlight(false);

        if (!isServer) return;

        m_ghostMorph = GetComponent<GhostMorph>();
    }

    void Update()
    {
        if (!isServer) return;

        PingServer();
     
        UpdateTimers();

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
                m_animator.SetBool("GotShot", false);
            }
        }
        
        if (!m_canDash && m_currentDashCooldown > 0f)
        {
            m_currentDashCooldown -= Time.deltaTime;
            if (m_currentDashCooldown <= 0f)
            {
                m_currentDashCooldown = 0f;
                ApplyDashToAll(false, true);
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

    /**
    @brief      Check if the ghost is grounded
    @return     True if a surface is detected below the character
    */
    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, out _, 1.0f);
    }

    /**
    @brief      Apply slow effect from projectile hit
    */
    public void HitRanged()
    {
        if (!isServer) return;
        ApplySlowToAll();
        m_currentTimerSlowed = m_timerSlowed;
        callAnimationTrigger("OnHit");
        m_animator.SetBool("GotShot", true);
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
        m_soundEffects?.PlayDeathAudio();
        ApplyStopToAll();
        m_currentTimerStop = m_timerStop;
        m_animator.SetBool("GotShot", false);
        callAnimationTrigger("OnHit");
        StopQTE();
    }

    [ObserversRpc(runLocally:true)]
    private void StopQTE()
    {
        if(!isOwner) return;
        QteCircle qteCircle = FindAnyObjectByType<QteCircle>();
        if (qteCircle == null) return;
        if (!qteCircle.m_isRunning) return;
        qteCircle.CancelQte();
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
        m_soundEffects?.PlayRevivingAudio();
        if (_reviver.isOwner && InteractPromptUI.m_Instance != null) InteractPromptUI.m_Instance.Hide();
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
        m_soundEffects?.StopRevivingAudio();
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
        m_soundEffects?.PlayReviveAudio();
        if (!m_reviver.m_isStopped)
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void CompleteRevive()
    {
        ResNotification(m_reviver.m_username);
        RequestReviveRpc();
        CancelRevive();
        callAnimationTrigger("Revived");
    }

    [ObserversRpc (requireServer: false)]
    public void ResNotification(string _reviverName)
    {
        InteractPromptUI.m_Instance.ShowRes(_reviverName, m_username);
    }

    [ObserversRpc(runLocally:true)]
    public void ApplyDashToAll(bool _isDashing, bool _canDash)
    {
        m_isDashing = _isDashing;
        m_canDash = _canDash;
        if (_canDash)
        {
            m_currentDashCooldown = 0f;
        }
    }
    
    /**
    @brief      Reset the dash cooldown when sabotaging (morphing)
    */
    public void ResetDashCooldown()
    {
        ApplyDashToAll(false, true);
        m_currentDashCooldown = 0f;
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
        print("Stop Interact with dead ghost " + isServer + m_beingRevived);
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

    public void OnFocus(Interact _who)
    {
        print("Found dead ghost");
        m_isFocused = true;
        InteractPromptUI.m_Instance.Show(InputBindingHelper.BuildPrompt("Ghost", "Interact", m_promptLabelRevive));
        SetHighlight(true);
    }

    public void OnUnfocus(Interact _who)
    {
        print("Lost focus on dead ghost");
        m_isFocused = false;
        InteractPromptUI.m_Instance.Hide();
        SetHighlight(false);
    }
    
    public void StartDash()
    {
        if (!m_canDash)
        {
            // case when can't dash
            return;
        }
        
        m_soundEffects?.PlayDashAudio();
        
        ApplyDashToAll(true, false);
        m_currentDashCooldown = m_dashCooldown;
        
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
        m_soundEffects?.PlayScarringAudio();
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

    /*
     * @brief Starts or stops the pulsing highlight coroutine on the highlight renderer
     * Resets emission to black when disabled
     * @param _enabled: Whether the highlight should be active
     * @return void
     */
    private void SetHighlight(bool _enabled)
    {
        if (m_highlightRenderers.Count == 0)
        {
            return;
        }

        if (_enabled)
        {
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
            }
            m_pulseCoroutine = StartCoroutine(PulseHighlight());
        }
        else
        {
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
                m_pulseCoroutine = null;
            }


            foreach (Renderer r in m_highlightRenderers)
            {
                r.GetPropertyBlock(m_propertyBlock);
                m_propertyBlock.SetColor("_EmissionColor", new Color(0, 0, 0, 0));
                r.SetPropertyBlock(m_propertyBlock);
            }
        }
    }

    /*
     * @brief Animates the highlight renderer with a pulsing emission effect
     * @return IEnumerator for coroutine
     */
    private IEnumerator PulseHighlight()
    {
        float time = 0f;

        while (true)
        {
            float pulse = Mathf.Lerp(m_minIntensity, m_maxIntensity,
                                     (Mathf.Sin(time * m_pulseSpeed) + 1f) * 0.5f);

            foreach (Renderer r in m_highlightRenderers)
            {
                r.GetPropertyBlock(m_propertyBlock);
                m_propertyBlock.SetColor("_EmissionColor", m_highlightColor * pulse);
                r.SetPropertyBlock(m_propertyBlock);
            }

            time += Time.deltaTime;
            yield return null;
        }
    }

    [ServerRpc]
    public void callAnimationTrigger(string _triggerName)
    {
        m_animator.SetTrigger(_triggerName);
    }

    [ServerRpc]
    public void callAnimationCrossFade(string _animationName, float _transitionDuration)
    {
        m_animator.CrossFadeInFixedTime(_animationName, _transitionDuration, 0);
    }

    [ServerRpc]
    public void callAnimationSetBool(string _parameterName, bool _value)
    {
        m_animator.SetBool(_parameterName, _value);
    }
}