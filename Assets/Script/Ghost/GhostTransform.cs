using UnityEngine;
using UnityEngine.InputSystem;

public class GhostTransform : MonoBehaviour
{
    [SerializeField] private GameObject m_mesh;
    [SerializeField] private TransformPreviewGhost m_previewGhost;

    private bool m_isTransformed = false;
    private GameObject m_currentPrefab = null;
    private Collider m_playerCollider;
    private MeshRenderer[] m_renderers;
    private Material[][] m_originalMaterials;

    void Start()
    {
        m_playerCollider = GetComponent<BoxCollider>();
        // récupère tous les MeshRenderer enfants
        m_renderers = m_mesh.GetComponentsInChildren<MeshRenderer>();

        m_originalMaterials = new Material[m_renderers.Length][];
        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_originalMaterials[i] = m_renderers[i].sharedMaterials;
        }
    }

    /*
     * @brief Confirms the transformation based on input
     * Applies the selected prefab if the context is performed and transformation is allowed.
     * @param _context: The context of the input action.
     * @return void
     */
    public void ConfirmTransform(InputAction.CallbackContext _context)
    {
        if (!_context.performed || !m_previewGhost.m_CanTransform || !TransformWheelcontroller.m_Instance.m_selectedPrefab || m_isTransformed)
        {
            return;
        }
        GameObject prefab = TransformWheelcontroller.m_Instance.m_selectedPrefab;
        ApplyPrefab(prefab);
        TransformWheelcontroller.m_Instance.m_selectedPrefab = null;
        m_previewGhost.gameObject.SetActive(false);
        m_isTransformed = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.AnchorPlayer();
        }
    }

    /*
     * @brief Reverts the player to their original appearance
     * Restores the original mesh, materials, and collider.
     * @return void
     */
    public void RevertToOriginal()
    {
        if (!m_isTransformed)
        {
            return;
        }

        m_playerCollider.enabled = true;
        m_mesh.SetActive(true);
        Destroy(m_currentPrefab);
        m_currentPrefab = null;
        m_isTransformed = false;
        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_renderers[i].sharedMaterials = m_originalMaterials[i];
        }
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    /*
     * @brief Copies mesh, materials, and collider from the given prefab to the player
     * Applies the components from the prefab if they exist.
     * @param _prefab: The prefab GameObject to copy from.
     * @return void
     */
    void ApplyPrefab(GameObject _prefab)
    {
        MeshFilter targetFilter = _prefab.GetComponent<MeshFilter>();
        MeshRenderer targetRenderer = _prefab.GetComponent<MeshRenderer>();
        Collider targetCollider = _prefab.GetComponent<Collider>();
        if (!targetFilter || !targetRenderer || !targetCollider)
        {
            return;
        }

        m_playerCollider.enabled = false;
        m_mesh.SetActive(false);
        m_currentPrefab = Instantiate(_prefab, transform);
        m_currentPrefab.transform.localPosition = new Vector3(0, 0, 0);
    }
}
