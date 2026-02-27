using System;
using System.Diagnostics;
using PurrNet;
using UnityEngine;
using UnityEngine.UI;

/*
 * @brief  Contains class declaration for QteCircle
 * @details Script that handles a circular QTE with several difficulties
 */
public class QteCircle : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform m_circleTransform;
    [SerializeField] private RectTransform m_needlePivot;
    [SerializeField] private RectTransform m_zonePivot;

    [Header("Markers")]
    [SerializeField] private RectTransform m_needleMarker;
    [SerializeField] private RectTransform m_zoneMarkerLarge;
    [SerializeField] private RectTransform m_zoneMarkerMedium;
    [SerializeField] private RectTransform m_zoneMarkerSmall;

    [Header("Zone Visuals (placed in scene)")]
    [SerializeField] private GameObject m_zoneLarge;
    [SerializeField] private GameObject m_zoneMedium;
    [SerializeField] private GameObject m_zoneSmall;

    [Header("Timing")]
    [SerializeField] private float m_rotationSpeedDegPerSec = 240f;

    [Header("Phases")]
    [SerializeField] private float[] m_zoneToleranceByPhase = { 18f, 13f, 8f };

    private const float c_minVectorSqrMagnitude = 0.0001f;

    private int m_currentPhaseIndex;
    public bool m_isRunning;
    private Action<bool> m_onFinished;

    private void Start()
    {
        SetVisibility(false);
    }

    private void SetVisibility(bool _visible)
    {
        m_circleTransform.GetComponent<Image>().enabled = _visible;

        m_needleMarker.GetComponent<Image>().enabled = _visible;

        m_zoneMarkerLarge.GetComponent<Image>().enabled = _visible;
        m_zoneMarkerMedium.GetComponent<Image>().enabled = _visible;
        m_zoneMarkerSmall.GetComponent<Image>().enabled = _visible;

    }

    /**
    @brief      Starts the QTE
    @param      _onFinished: callback true if all phases are done
    @return     void
    */
    public void StartQte(Action<bool> _onFinished)
    {
        SetVisibility(true);
        enabled = true;
        
        
        m_onFinished = _onFinished;
        m_isRunning = true;
        m_currentPhaseIndex = 0;


        ResetNeedle();
        PlaceZoneRandomly();
        UpdateZoneVisual();
    }

    private void Update()
    {
        if (!m_isRunning) return;

        RotateNeedle();
    }

    /**
    @brief      Ends the QTE
    @param      _success: true if all phases are done
    @return     void
    */
    private void FinishQte(bool _success)
    {
        m_isRunning = false;

        SetVisibility(false);
        enabled = false;

        Action<bool> callback = m_onFinished;
        m_onFinished = null;
        callback?.Invoke(_success);
    }

    /**
    @brief      Makes the needle turn
    @return     void
    */
    private void RotateNeedle()
    {
        if (m_needlePivot == null) return;

        float delta = m_rotationSpeedDegPerSec * Time.deltaTime;
        m_needlePivot.Rotate(0f, 0f, -delta);
    }

    /**
    @brief      Replaces the needle at the start
    @return     void
    */
    private void ResetNeedle()
    {
        if (m_needlePivot != null)
            m_needlePivot.localEulerAngles = Vector3.zero;
    }

    /**
    @brief      Place the success zone at a random angle
    @return     void
    */
    private void PlaceZoneRandomly()
    {
        if (m_zonePivot == null) return;

        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        m_zonePivot.localEulerAngles = new Vector3(0f, 0f, randomAngle);
    }

    /**
    @brief      Activates only the current zone
    @return     void
    */
    private void UpdateZoneVisual()
    {
        if (m_zoneLarge != null) m_zoneLarge.SetActive(m_currentPhaseIndex == 0);
        if (m_zoneMedium != null) m_zoneMedium.SetActive(m_currentPhaseIndex == 1);
        if (m_zoneSmall != null) m_zoneSmall.SetActive(m_currentPhaseIndex == 2);
    }

    /**
    @brief     Retrieves the zone marker depending on the current phase
    @return     RectTransform of the active marker
    */
    private RectTransform GetCurrentZoneMarker()
    {
        if (m_currentPhaseIndex == 0) return m_zoneMarkerLarge;
        if (m_currentPhaseIndex == 1) return m_zoneMarkerMedium;
        return m_zoneMarkerSmall;
    }

    /**
    @brief      Checks if the needle is in the active zone marker
    @return     true if success
    */
    private bool IsNeedleInZone()
    {
        RectTransform zoneMarker = GetCurrentZoneMarker();
        if (m_needleMarker == null || zoneMarker == null || m_needlePivot == null) return false;

        Vector2 center = m_needlePivot.position;

        Vector2 needleVector = (Vector2)m_needleMarker.position - center;
        Vector2 zoneVector = (Vector2)zoneMarker.position - center;

        if (needleVector.sqrMagnitude < c_minVectorSqrMagnitude) return false;
        if (zoneVector.sqrMagnitude < c_minVectorSqrMagnitude) return false;

        Vector2 needleDir = needleVector.normalized;
        Vector2 zoneDir = zoneVector.normalized;

        float delta = Vector2.Angle(needleDir, zoneDir);
        float tolerance = GetToleranceForCurrentPhase();


        return delta <= tolerance;
    }

    /**
    @brief      Retrieve the actual tolerance
    @return     tolerance in degrees
    */
    private float GetToleranceForCurrentPhase()
    {
        if (m_zoneToleranceByPhase == null || m_zoneToleranceByPhase.Length == 0)
            return 0f;

        int phaseIndex = Mathf.Clamp(m_currentPhaseIndex, 0, m_zoneToleranceByPhase.Length - 1);
        return m_zoneToleranceByPhase[phaseIndex];
    }

    public void CheckSuccess()
    {
        bool success = IsNeedleInZone();

        if (!success)
        {
            FinishQte(false);
            return;
        }

        m_currentPhaseIndex++;

        if (m_currentPhaseIndex >= m_zoneToleranceByPhase.Length)
        {
            FinishQte(true);
        }
        else
        {
            ResetNeedle();
            PlaceZoneRandomly();
            UpdateZoneVisual();
        }
    }
}
