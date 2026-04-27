using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/*
 * @brief  Contains class declaration for QteCircle
 * @details Script that handles a circular QTE with several difficulties
 */
public class QteCircle : MonoBehaviour
{
    [Header("3D Transforms")]
    [SerializeField] private Transform m_needlePivot;
    [SerializeField] private Transform m_zonePivot;
    [SerializeField] private GameObject m_visualRoot;

    [Header("Render Texture")]
    [SerializeField] private Camera m_qteCamera;
    [SerializeField] private RawImage m_rawImage;

    [Header("Zone Visuals")]
    [SerializeField] private GameObject m_zoneLarge;
    [SerializeField] private GameObject m_zoneMedium;
    [SerializeField] private GameObject m_zoneSmall;
    [SerializeField] private Volume m_volume;
    private OutlineVolumeComponent m_outline;
    private Color defaultOutline1;
    private Color transpOutline1;
    private Color defaultOutline2;
    private Color transpOutline2;

    [Header("Debug")]
    [SerializeField] private Renderer m_needleRenderer;
    [SerializeField] private bool m_debugNeedleColor = false;

    [Header("Speed")]
    [SerializeField] private float m_rotationSpeedDegPerSec = 240f;

    [Header("Phases")]
    [SerializeField] private float[] m_zoneToleranceByPhase = { 52f, 43f, 20f };

    private int m_currentPhaseIndex;
    public bool m_isRunning;
    private Action<bool> m_onFinished;

    private void Start()
    {
        //set up the volume
        VolumeProfile volumeProfile = m_volume?.profile;
        if(!volumeProfile) throw new System.NullReferenceException(nameof(UnityEngine.Rendering.VolumeProfile));

        if(!volumeProfile.TryGet(out m_outline)) throw new System.NullReferenceException(nameof(m_outline));

        SetVisibility(false);
        defaultOutline1 = m_outline.color1.value;
        transpOutline1 = m_outline.color1.value;
        transpOutline1.a = 0;
        defaultOutline2 = m_outline.color2.value;
        transpOutline2 = defaultOutline2;
        transpOutline2.a = 0;
    }

    private void SetVisibility(bool _visible)
    {
        m_visualRoot?.SetActive(_visible);
        if (m_rawImage != null) m_rawImage.enabled = _visible;
    }

    /**
    @brief      Starts the QTE
    @param      _onFinished: callback true if all phases are done
    @return     void
    */
    public void StartQte(Action<bool> _onFinished)
    {
        Debug.Log("should change");
        m_outline.color1.value = transpOutline1;
        m_outline.color2.value = transpOutline2;
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
        UpdateNeedleDebugColor();
    }

    /**
    @brief      Ends the QTE
    @param      _success: true if all phases are done
    @return     void
    */
    private void FinishQte(bool _success)
    {
        m_outline.color1.value = defaultOutline1;
        m_outline.color2.value = defaultOutline2;
        m_isRunning = false;

        SetVisibility(false);
        enabled = false;

        Action<bool> callback = m_onFinished;
        m_onFinished = null;
        callback?.Invoke(_success);
    }

    private void UpdateNeedleDebugColor()
    {
        if (!m_debugNeedleColor || m_needleRenderer == null) return;
        m_needleRenderer.material.color = IsNeedleInZone() ? Color.white : Color.black;
    }

    /**
    @brief      Makes the needle turn
    @return     void
    */
    private void RotateNeedle()
    {
        if (m_needlePivot == null) return;

        float direction = (m_currentPhaseIndex % 2 == 0) ? 1f : -1f;
        float delta = direction * m_rotationSpeedDegPerSec * Time.deltaTime;
        m_needlePivot.Rotate(0f, 0f, delta, Space.Self);
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
    @brief      Checks if the needle is aligned with the active zone using rotation angles
    @return     true if success
    */
    private bool IsNeedleInZone()
    {
        if (m_needlePivot == null || m_zonePivot == null) return false;

        float needleAngle = m_needlePivot.localEulerAngles.z;
        float zoneAngle   = m_zonePivot.localEulerAngles.z;
        float delta       = Mathf.Abs(Mathf.DeltaAngle(needleAngle, zoneAngle));

        return delta <= GetToleranceForCurrentPhase();
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

    public void CancelQte()
    {
        if (m_isRunning)
            FinishQte(false);
    }

    public bool CheckSuccess()
    {
        bool success = IsNeedleInZone();

        if (!success)
        {
            FinishQte(false);
            return false;
        }

        m_currentPhaseIndex++;

        if (m_currentPhaseIndex >= m_zoneToleranceByPhase.Length)
        {
            FinishQte(true);
            return true;
        }
        else
        {
            PlaceZoneRandomly();
            UpdateZoneVisual();
        }
        return false;
    }
}
