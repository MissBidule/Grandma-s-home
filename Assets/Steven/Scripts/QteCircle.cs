using System;
using UnityEngine;

/**
@brief       Script du QTE cercle
@details     La classe \c QteCircle affiche un QTE circulaire : une aiguille tourne et une zone de succès
             est placée aléatoirement sur le cercle. Le joueur valide avec une touche.
*/
public class QteCircle : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject m_root;
    [SerializeField] private RectTransform m_needlePivot;
    [SerializeField] private RectTransform m_zonePivot;

    [Header("Timing")]
    [SerializeField] private float m_rotationSpeedDegPerSec = 180f;

    [Header("Zone")]
    [SerializeField] private float m_zoneToleranceDeg = 15f;

    [Header("Input")]
    [SerializeField] private KeyCode m_validateKey = KeyCode.Space;

    private bool m_isRunning;
    private Action<bool> m_onFinished;

    /**
    @brief      Lance le QTE et place la zone de succès au hasard.
    @param      _onFinished: callback appelée avec true si réussite, sinon false
    @return     void
    */
    public void StartQte(Action<bool> _onFinished)
    {
        m_onFinished = _onFinished;
        m_isRunning = true;

        if (m_root != null) m_root.SetActive(true);

        if (m_needlePivot != null)
            m_needlePivot.localEulerAngles = Vector3.zero;

        if (m_zonePivot != null)
        {
            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            m_zonePivot.localEulerAngles = new Vector3(0f, 0f, randomAngle);
        }
    }

    private void Update()
    {
        if (!m_isRunning) return;
        if (m_needlePivot == null) return;

        float delta = m_rotationSpeedDegPerSec * Time.deltaTime;
        m_needlePivot.Rotate(0f, 0f, -delta);

        if (Input.GetKeyDown(m_validateKey))
        {
            bool success = IsNeedleInZone();
            FinishQte(success);
        }
    }

    /**
    @brief      Termine le QTE et masque l'UI.
    @param      _success: résultat du QTE
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
    @brief      Vérifie si l'aiguille est dans la zone de succès.
    @return     true si réussite, false sinon
    */
    private bool IsNeedleInZone()
    {
        if (m_zonePivot == null) return false;

        float needleDeg = NormalizeAngle360(m_needlePivot.localEulerAngles.z);
        float zoneDeg = NormalizeAngle360(m_zonePivot.localEulerAngles.z);

        float delta = Mathf.Abs(Mathf.DeltaAngle(needleDeg, zoneDeg));
        return delta <= m_zoneToleranceDeg;
    }

    /**
    @brief      Normalise un angle en degrés dans [0, 360).
    @param      _deg: angle en degrés
    @return     angle normalisé
    */
    private float NormalizeAngle360(float _deg)
    {
        float deg = _deg % 360f;
        if (deg < 0f) deg += 360f;
        return deg;
    }
}
