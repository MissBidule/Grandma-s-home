using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class TutoGhostController :  MonoBehaviour, TutoIInteractable
{
    [Header("Ghost references")]
    public bool m_isStopped = true;
    public bool m_beingRevived = false;
    public bool m_isReviving = false;

    [Header("Status Timers")]
    public float m_timerStop;
    private float m_currentTimerStop;

    [Header("Revive")]
    public float m_baseReviveTime = 5f;
    public float m_maxReviveTime = 30f;
    private int m_deathCount = 0;
    private TutoGhostController m_reviver = null;
    private float m_reviveTimer = 0f;
    public float m_reviveDuration = 0f;
    private bool m_isFocused = false;
    [SerializeField] private string m_promptMessage = "Hold E : Revive";


    //public Action<bool> OnDeathChange; // true for death | false for resurrection


    void Update()
    {
        if (m_beingRevived)
        {
            UpdateRevive();
            return;
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

    private void CancelRevive()
    {
        m_beingRevived = false;
        m_reviver.m_isReviving = false;
        m_reviveTimer = 0f;
        //m_reviver.UnfreezeReviverRpc();
        m_reviver = null;
    }

    private void CompleteRevive()
    {
        //ResNotification(m_reviver.m_username);
        //RequestReviveRpc();
        CancelRevive();
    }

    public void OnInteract(TutoInteract _who)
    {
        if (m_isReviving) return;
        if (m_isStopped)
        {
            m_reviver = _who.GetComponentInParent<TutoGhostController>();
            StartRevive(m_reviver);
        }
    }
    private void StartRevive(TutoGhostController _reviver)
    {
        m_reviver = _reviver;
        if (m_reviver.m_isStopped) return;
        m_reviveDuration = GetReviveTime();
        m_reviveTimer = 0f;
        m_beingRevived = true;
        m_reviver.RevivingBuddy(m_reviveDuration);
        //m_reviver.FreezeReviverRpc();
        if (InteractPromptUI.m_Instance != null) InteractPromptUI.m_Instance.Hide();
    }

    public void RevivingBuddy(float _duration)
    {
        m_reviveDuration = _duration;
        m_reviveTimer = 0f;
        m_isReviving = true;
    }


    public float GetReviveTime()
    {
        return Mathf.Min(m_baseReviveTime * (m_deathCount + 1), m_maxReviveTime);
    }

    //private void RequestReviveRpc()
    //{
      //  if (!m_isStopped) return;
        //ForceRevive();
        //if (!m_reviver.m_isStopped)
          //  m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    //}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void OnFocus(TutoInteract who)
    {
        Debug.Log("Found dead ghost");
        m_isFocused = true;
        InteractPromptUI.m_Instance.Show(m_promptMessage);
    }

    public void OnUnfocus(TutoInteract who)
    {
        Debug.Log("Lost focus on dead ghost");
        m_isFocused = false;
        InteractPromptUI.m_Instance.Hide();
    }

    public void OnStopInteract(TutoInteract who)
    {
        if (m_beingRevived)
        {
            CancelRevive();
        }
    }


}
