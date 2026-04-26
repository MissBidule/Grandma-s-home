using Script.States;
using TMPro;
using UnityEngine;

public class TimeRemainingDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_latencyText;
    RoundRunningState m_roundRunningState;

    void Start()
    {
        m_roundRunningState = FindAnyObjectByType<RoundRunningState>();
        //Vector2 pos = m_latencyText.rectTransform.anchoredPosition;
        //pos.x = 0f;
        //pos.y = -50f;
        //m_latencyText.rectTransform.anchoredPosition = pos;
    }

    void Update()
    {
        m_latencyText.text = $"{SecondsToDisplay((int)m_roundRunningState.m_remainingTime)}";
    }

    string SecondsToDisplay(int seconds)
    {
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        return $"{minutes:D2}:{remainingSeconds:D2}";
    }
}
