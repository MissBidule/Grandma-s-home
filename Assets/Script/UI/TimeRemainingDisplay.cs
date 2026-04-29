using PurrNet;
using Script.States;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeRemainingDisplay : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI m_latencyText;
    [SerializeField] private GameObject m_timerRoot;

    private Graphic[] m_graphics;

    RoundRunningState m_roundRunningState;
    PanicState m_panicState;

    bool m_isPanic = false;

    void Awake()
    {
        if (m_timerRoot == null && transform.parent != null)
            m_timerRoot = transform.parent.gameObject;

        if (m_timerRoot != null)
            m_graphics = m_timerRoot.GetComponentsInChildren<Graphic>(true);
    }

    void Start()
    {
        SetVisible(false);
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        m_roundRunningState = FindAnyObjectByType<RoundRunningState>();
    }

    void Update()
    {
        if (m_roundRunningState == null) {
            SetVisible(false);
            return;
        }


        float remaining = m_isPanic && m_panicState != null
            ? m_panicState.m_remainingTime
            : m_roundRunningState.m_remainingTime;

        bool shouldShow = remaining > 0f;
        SetVisible(shouldShow);

        if (!shouldShow) return;

        m_latencyText.text = $"{SecondsToDisplay((int)remaining)}";

        if (m_isPanic != m_roundRunningState.m_isPanic)
        {
            m_isPanic = m_roundRunningState.m_isPanic;
            m_latencyText.color = Color.red;
            m_isPanic = true;
            m_panicState = FindAnyObjectByType<PanicState>();
        }
    }

    private void SetVisible(bool _visible)
    {
        if (m_graphics != null && m_graphics.Length > 0)
        {
            foreach (var g in m_graphics)
            {
                if (g != null && g.enabled != _visible)
                    g.enabled = _visible;
            }
        }
        else if (m_latencyText != null && m_latencyText.enabled != _visible)
        {
            m_latencyText.enabled = _visible;
        }
    }

    string SecondsToDisplay(int seconds)
    {
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        return $"{minutes:D2}:{remainingSeconds:D2}";
    }
}
