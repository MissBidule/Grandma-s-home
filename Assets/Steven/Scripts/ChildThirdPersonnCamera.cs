using UnityEngine;

/**
@brief       Caméra 3ème personne enfant (yaw player + pitch caméra)
@details     Souris X fait tourner le joueur, souris Y incline la caméra
*/
public class ChildThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform m_playerTransform;
    [SerializeField] private Transform m_cameraTransform;

    [Header("Settings")]
    [SerializeField] private float m_sensitivity = 120f;
    [SerializeField] private float m_minPitch = -25f;
    [SerializeField] private float m_maxPitch = 45f;

    private float m_pitch;

    private void Awake()
    {
        if (m_playerTransform == null)
            m_playerTransform = transform.parent;

        if (m_cameraTransform == null && transform.childCount > 0)
            m_cameraTransform = transform.GetChild(0);
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (m_playerTransform != null)
        {
            m_playerTransform.Rotate(
                0f,
                mouseX * m_sensitivity * Time.deltaTime,
                0f
            );
        }
        m_pitch -= mouseY * m_sensitivity * Time.deltaTime;
        m_pitch = Mathf.Clamp(m_pitch, m_minPitch, m_maxPitch);

        if (m_cameraTransform != null)
        {
            m_cameraTransform.localEulerAngles = new Vector3(
                m_pitch,
                0f,
                0f
            );
        }
    }
}
