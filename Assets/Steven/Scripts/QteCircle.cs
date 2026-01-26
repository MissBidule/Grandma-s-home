using System;
using System.Diagnostics;
using UnityEngine;

/**
@brief       Script du QTE cercle à plusieurs phases
@details     La classe \c QteCircle gère un QTE circulaire en plusieurs niveaux de difficulté
             avec 3 zones visuelles (grande/moyenne/petite)
*/
public class QteCircle : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject m_root;
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

    [Header("Input")]
    [SerializeField] private KeyCode m_validateKey = KeyCode.Space;


    private const float c_minVectorSqrMagnitude = 0.0001f;

    private int m_currentPhaseIndex;
    private bool m_isRunning;
    private Action<bool> m_onFinished;

    /**
    @brief      Lance le QTE multi-phase
    @param      _onFinished: callback true si toutes les phases sont réussies
    @return     void
    */
    public void StartQte(Action<bool> _onFinished)
    {
        m_onFinished = _onFinished;
        m_isRunning = true;
        m_currentPhaseIndex = 0;

        if (m_root != null)
            m_root.SetActive(true);

        ResetNeedle();
        PlaceZoneRandomly();
        UpdateZoneVisual();
    }

    private void Update()
    {
        if (!m_isRunning) return;

        RotateNeedle();

        if (Input.GetKeyDown(m_validateKey))
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

    /**
    @brief      Termine le QTE
    @param      _success: true si toutes les phases sont réussie
    @return     void
    */
    private void FinishQte(bool _success)
    {
        m_isRunning = false;

        if (m_root != null)
            m_root.SetActive(false);

        Action<bool> callback = m_onFinished;
        m_onFinished = null;
        callback?.Invoke(_success);
    }

    /**
    @brief      Fait tourner l'aiguille
    @return     void
    */
    private void RotateNeedle()
    {
        if (m_needlePivot == null) return;

        float delta = m_rotationSpeedDegPerSec * Time.deltaTime;
        m_needlePivot.Rotate(0f, 0f, -delta);
    }

    /**
    @brief      Replace l'aiguille au point de départ 
    @return     void
    */
    private void ResetNeedle()
    {
        if (m_needlePivot != null)
            m_needlePivot.localEulerAngles = Vector3.zero;
    }

    /**
    @brief      Place la zone de succès à un angle aléatoire
    @return     void
    */
    private void PlaceZoneRandomly()
    {
        if (m_zonePivot == null) return;

        float randomAngle = UnityEngine.Random.Range(0f, 360f);
        m_zonePivot.localEulerAngles = new Vector3(0f, 0f, randomAngle);
    }

    /**
    @brief      Active seulement la zone correspondant à la phase courante
    @return     void
    */
    private void UpdateZoneVisual()
    {
        if (m_zoneLarge != null) m_zoneLarge.SetActive(m_currentPhaseIndex == 0);
        if (m_zoneMedium != null) m_zoneMedium.SetActive(m_currentPhaseIndex == 1);
        if (m_zoneSmall != null) m_zoneSmall.SetActive(m_currentPhaseIndex == 2);
    }

    /**
    @brief      Récupère le marker de zone selon la phase courante
    @return     RectTransform du marker actif
    */
    private RectTransform GetCurrentZoneMarker()
    {
        if (m_currentPhaseIndex == 0) return m_zoneMarkerLarge;
        if (m_currentPhaseIndex == 1) return m_zoneMarkerMedium;
        return m_zoneMarkerSmall;
    }

    /**
    @brief      Vérifie si l'aiguille est dans la zone de succès de la phase courante
    @return     true si réussite
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
    @brief      Récupère la tolérance de la phase courante
    @return     tolérance en degrés
    */
    private float GetToleranceForCurrentPhase()
    {
        if (m_zoneToleranceByPhase == null || m_zoneToleranceByPhase.Length == 0)
            return 0f;

        int phaseIndex = Mathf.Clamp(m_currentPhaseIndex, 0, m_zoneToleranceByPhase.Length - 1);
        return m_zoneToleranceByPhase[phaseIndex];
    }
}
