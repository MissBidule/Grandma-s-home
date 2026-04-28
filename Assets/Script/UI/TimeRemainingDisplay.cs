using Script.States;
using TMPro;
using UnityEngine;

public class TimeRemainingDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_latencyText;
    RoundRunningState m_roundRunningState;
    PanicState m_panicState;

    bool m_isPanic = false;

    void Start()
    {
        m_roundRunningState = FindAnyObjectByType<RoundRunningState>();
        Vector2 pos = m_latencyText.rectTransform.anchoredPosition;
        pos.x = 0f;
        pos.y = -50f;
        m_latencyText.rectTransform.anchoredPosition = pos;
    }

    void Update()
    {
        m_latencyText.text = $"Time Remaining: {SecondsToDisplay((int)m_roundRunningState.m_remainingTime)}";

        if (m_isPanic)

        {

            m_latencyText.text = $"Time Remaining: {SecondsToDisplay((int)m_panicState.m_remainingTime)}";

        }

        else

        {

            m_latencyText.text = $"Time Remaining: {SecondsToDisplay((int)m_roundRunningState.m_remainingTime)}";

        }

        if (m_isPanic != m_roundRunningState.m_isPanic)

        {

            m_isPanic = m_roundRunningState.m_isPanic;

            m_latencyText.color = Color.red;

            m_isPanic = true;

            m_panicState = FindAnyObjectByType<PanicState>();

        }
    }

    string SecondsToDisplay(int seconds)
    {
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        return $"{minutes:D2}:{remainingSeconds:D2}";
    }
}
