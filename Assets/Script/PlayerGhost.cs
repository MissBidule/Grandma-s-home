using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for PlayerGhost
 * @details The PlayerGhost class allows the player to interact with items to sabotage them and transform into different objects.
 */
public class PlayerGhost : MonoBehaviour
{
    private MeshFilter m_meshFilter;
    private MeshRenderer m_meshRenderer;
    private Collider m_currentCollider;
    [SerializeField] private TransformPreviewGhost m_previewGhost;

    [System.NonSerialized] public bool m_isTransformed = false;

    private Mesh m_originalMesh;
    private Material[] m_originalMaterials;
    private System.Type m_originalColliderType;
    private Vector3 m_originalColliderCenter;
    private Vector3 m_originalColliderSize;
    private float m_originalColliderRadius;
    private float m_originalColliderHeight;
    private int m_originalColliderDirection;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Initializes the mesh filter, renderer, and collider. Saves the original appearance.
     * @return void
     */
    void Awake()
    {
        m_meshFilter = GetComponent<MeshFilter>();
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_currentCollider = GetComponent<Collider>();

        SaveOriginalAppearance();
    }

    /*
     * @brief Saves the original appearance of the player
     * Stores mesh, materials, and collider properties for later restoration.
     * @return void
     */
    void SaveOriginalAppearance()
    {
        m_originalMesh = m_meshFilter.sharedMesh;
        m_originalMaterials = m_meshRenderer.sharedMaterials;

        if (m_currentCollider != null)
        {
            m_originalColliderType = m_currentCollider.GetType();

            if (m_currentCollider is BoxCollider box)
            {
                m_originalColliderCenter = box.center;
                m_originalColliderSize = box.size;
            }
            else if (m_currentCollider is SphereCollider sphere)
            {
                m_originalColliderCenter = sphere.center;
                m_originalColliderRadius = sphere.radius;
            }
            else if (m_currentCollider is CapsuleCollider capsule)
            {
                m_originalColliderCenter = capsule.center;
                m_originalColliderRadius = capsule.radius;
                m_originalColliderHeight = capsule.height;
                m_originalColliderDirection = capsule.direction;
            }
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

        m_meshFilter.mesh = m_originalMesh;
        m_meshRenderer.materials = m_originalMaterials;

        if (m_currentCollider != null)
        {
            Destroy(m_currentCollider);
        }

        m_currentCollider = gameObject.AddComponent(m_originalColliderType) as Collider;

        if (m_currentCollider is BoxCollider box)
        {
            box.center = m_originalColliderCenter;
            box.size = m_originalColliderSize;
        }
        else if (m_currentCollider is SphereCollider sphere)
        {
            sphere.center = m_originalColliderCenter;
            sphere.radius = m_originalColliderRadius;
        }
        else if (m_currentCollider is CapsuleCollider capsule)
        {
            capsule.center = m_originalColliderCenter;
            capsule.radius = m_originalColliderRadius;
            capsule.height = m_originalColliderHeight;
            capsule.direction = m_originalColliderDirection;
        }

        m_isTransformed = false;

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.UnanchorPlayer();
        }

        if (TransformWheelcontroller.m_Instance != null)
        {
            TransformWheelcontroller.m_Instance.ClearSelection();
        }
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
        if (!targetFilter
            ||
            !targetRenderer || !targetCollider)
        {
            return;
        }
        m_meshFilter.mesh = targetFilter.sharedMesh;
        m_meshRenderer.materials = targetRenderer.sharedMaterials;
        ReplaceCollider(targetCollider);
        transform.localScale = _prefab.transform.localScale;
        transform.localRotation = _prefab.transform.localRotation;
    }

    /*
     * @brief Replaces the current collider with a new one based on the target collider
     * Copies relevant properties from the target collider to the new collider.
     * @param _target: The target Collider to copy from.
     * @return void
     */
    void ReplaceCollider(Collider _target)
    {
        if (m_currentCollider != null)
        {
            Destroy(m_currentCollider);
        }
        System.Type type = _target.GetType();
        m_currentCollider = gameObject.AddComponent(type) as Collider;
        if (m_currentCollider is BoxCollider box &&
            _target is BoxCollider tBox)
        {
            box.center = tBox.center;
            box.size = tBox.size;
        }
        else if (m_currentCollider is SphereCollider sphere &&
                 _target is SphereCollider tSphere)
        {
            sphere.center = tSphere.center;
            sphere.radius = tSphere.radius;
        }
        else if (m_currentCollider is CapsuleCollider capsule &&
                 _target is CapsuleCollider tCapsule)
        {
            capsule.center = tCapsule.center;
            capsule.radius = tCapsule.radius;
            capsule.height = tCapsule.height;
            capsule.direction = tCapsule.direction;
        }
    }
}
