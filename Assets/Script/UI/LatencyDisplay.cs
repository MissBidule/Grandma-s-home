using UnityEngine;
using TMPro;
using PurrNet;
using System.Collections;

/*
 * @brief Simple UI component to display network latency on screen
 * @details Attaches to a TextMeshProUGUI component and continuously displays the current latency
 */
public class LatencyDisplay : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI m_latencyText;
    public PlayerControllerCore m_localPlayer;
    private bool m_started = false;

    float m_startTime;

    float m_pingInterval = 2f;

    private void Start()
    {
    }

    private void Update()
    {
        if (m_started) return;
        if (!m_localPlayer) return;
        Vector2 pos = m_latencyText.rectTransform.anchoredPosition;
        pos.x = 0f;
        pos.y = 0f;
        m_latencyText.rectTransform.anchoredPosition = pos;
        StartCoroutine(PingRoutine());
        m_started = true;
    }


    private IEnumerator PingRoutine()
    {
        while (true)
        {
            SendPing();
            yield return new WaitForSeconds(m_pingInterval);
        }
    }

    void SendPing()
    {
        m_startTime = Time.time;
        m_localPlayer.PingServer(m_startTime);
    }

    

    public void ReceivePong(float _sentTime)
    {
        float latency = (Time.time - _sentTime) * 1000f / 2f;
        m_latencyText.text = $"Ping: {latency:F1}ms";
    }
}
