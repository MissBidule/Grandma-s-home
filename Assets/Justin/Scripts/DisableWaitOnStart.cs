using PurrNet.StateMachine;
using System;
using System.Collections;
using Unity.Services.Multiplayer;
using UnityEngine;

public class DisableWaitOnStart : MonoBehaviour
{
    [SerializeField] private StateMachine m_stateMachine;
    [SerializeField] private GameObject m_waitCamera;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private float m_fadeDuration = 0.5f;

    private void Awake()
    {
        m_stateMachine.onStateChanged += OnStateChange;
    }

    private void OnDestroy()
    {
        m_stateMachine.onStateChanged -= OnStateChange;
    }

    private void OnStateChange(StateNode _previousState, StateNode _currentState)
    {
        if (_currentState is PlayerSpawningState)
        {
            // fade out
            StartCoroutine(FadeOut());
            
            // Disable UI camera
            m_waitCamera.SetActive(false);
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = m_canvasGroup.alpha;
        
        while (elapsed < m_fadeDuration)
        {
            elapsed += Time.deltaTime;
            m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / m_fadeDuration);
            yield return null;
        }
        
        m_canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
