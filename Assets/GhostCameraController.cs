using UnityEngine;

/**
@brief       Caméra orbitale du Ghost
@details     Caméra 3ème personne contrôlée par la souris, indépendante du Child. La direction de déplacement du Ghost est alignée avec la caméra
*/
public class GhostCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform m_target;

    [Header("Camera Settings")]
    [SerializeField] private float m_distance = 4f;
    [SerializeField] private float m_sensitivity = 120f;
    [SerializeField] private float m_minPitch = -40f;
    [SerializeField] private float m_maxPitch = 70f;

    [Header("Collision")]
    [SerializeField] private LayerMask m_collisionMask;
    [SerializeField] private float m_collisionOffset = 0.2f;

    private float m_yaw;
    private float m_pitch;

    /**
    @brief      Initialise les références et verrouille le curseur
    @return     void
    */
    private void Awake()
    {
        if (m_target == null)
            m_target = transform.parent;

        Cursor.lockState = CursorLockMode.Locked;
    }

    /**
    @brief      Met à jour la position et la rotation de la caméra
    @return     void
    */
    private void LateUpdate()
    {
        if (m_target == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        m_yaw += mouseX * m_sensitivity * Time.deltaTime;
        m_pitch -= mouseY * m_sensitivity * Time.deltaTime;
        m_pitch = Mathf.Clamp(m_pitch, m_minPitch, m_maxPitch);

        Quaternion rotation = Quaternion.Euler(m_pitch, m_yaw, 0f);
        Vector3 desiredOffset = rotation * Vector3.back * m_distance;

        float finalDistance = m_distance;

        if (Physics.Raycast(
            m_target.position,
            desiredOffset.normalized,
            out RaycastHit hit,
            m_distance,
            m_collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            finalDistance = Mathf.Max(0.1f, hit.distance - m_collisionOffset);
        }

        Vector3 finalOffset = rotation * Vector3.back * finalDistance;
        transform.position = m_target.position + finalOffset;
        transform.LookAt(m_target.position);
    }

    /**
    @brief      Retourne la direction forward de la caméra
    @return     direction normalisée sur le plan horizontal
    */
    public Vector3 GetCameraForwardFlat()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.normalized;
    }
}
