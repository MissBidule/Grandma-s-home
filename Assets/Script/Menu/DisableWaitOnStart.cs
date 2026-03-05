using PurrNet;
using PurrNet.Logging;
using PurrNet.StateMachine;
using System;
using System.Collections;
using Unity.Services.Multiplayer;
using UnityEngine;

public class DisableWaitOnStart : NetworkBehaviour
{
    [SerializeField] private StateMachine m_stateMachine;
    [SerializeField] private GameObject m_waitCamera;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private float m_fadeDuration = 1f;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    override protected void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<DisableWaitOnStart>();
    }

    [ObserversRpc(runLocally: true, bufferLast: true)]
    public void DisableWaitInterface()
    {
        PurrLogger.Log("DisableWaitInterface", this);
        
        // fade out
        StartCoroutine(FadeOut());
        m_waitCamera.SetActive(false);
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
        // Disable UI camera
        enabled = false;
    }
}
