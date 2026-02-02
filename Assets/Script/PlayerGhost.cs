using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for PlayerGhost
 * @details The PlayerGhost class allows the player to interact with items to sabotage them and transform into different objects.
 */
public class PlayerGhost : MonoBehaviour
{
    private MeshRenderer m_meshRenderer;
    private Collider m_playerCollider;
    [SerializeField] private TransformPreviewGhost m_previewGhost;
    private GameObject m_currentPrefab = null;
    [System.NonSerialized] public bool m_isTransformed = false;
    private Material[] m_originalMaterials;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Initializes the mesh filter, renderer, and collider. Saves the original appearance.
     * @return void
     */
    void Awake()
    {
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_playerCollider = GetComponent<CapsuleCollider>();
        m_originalMaterials = m_meshRenderer.sharedMaterials;
    }

    /*
     * @brief Confirms the transformation based on input
     * Applies the selected prefab if the context is performed and transformation is allowed.
     * @param _context: The context of the input action.
     * @return void
     */
    public void ConfirmTransform(InputAction.CallbackContext _context)
    {
        if (!_context.performed || !m_previewGhost.m_CanTransform || !TransformWheelcontroller.m_Instance.m_selectedPrefab)
        {
            return;
        }
        GameObject prefab = TransformWheelcontroller.m_Instance.m_selectedPrefab;
        ApplyPrefab(prefab);
        m_previewGhost.gameObject.SetActive(false);
        m_isTransformed = true;

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
        m_meshRenderer.enabled = true;
        Destroy(m_currentPrefab);
        m_currentPrefab = null;
        m_isTransformed = false;
        m_meshRenderer.sharedMaterials = m_originalMaterials;
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
        m_meshRenderer.enabled = false;
        m_currentPrefab = Instantiate(_prefab, transform);
        m_currentPrefab.transform.localPosition = new Vector3(0,0,0);
    }
}