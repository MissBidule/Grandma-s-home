using UnityEngine;

/*
 * @brief Contains class declaration for TransformPreviewGhost
 * @details The TransformPreviewGhost class handles the preview of transformations, checking for collisions and updating materials accordingly.
 */
public class TransformPreviewGhost : MonoBehaviour
{
    private MeshRenderer m_meshRenderer;
    private Collider m_previewCollider;
    private uint m_collisionCount = 0;
    public bool m_CanTransform => m_collisionCount == 0;

    [SerializeField] private Material m_validMat;
    [SerializeField] private Material m_invalidMat;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Initializes the mesh renderer and collider, sets the collider as a trigger, and updates the material.
     * @return void
     */
    void Awake()
    {
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_previewCollider = GetComponent<Collider>();
        m_previewCollider.isTrigger = true;
        UpdateMaterial();
    }

    /*
     * @brief Sets the preview based on the given prefab
     * Copies the mesh and collider from the prefab to the preview ghost.
     * @param _prefab: The prefab GameObject to preview.
     * @return void
     */
    public void SetPreview(GameObject _prefab)
    {
        MeshFilter meshFilter = _prefab.GetComponentInChildren<MeshFilter>();
        Collider collider = _prefab.GetComponentInChildren<Collider>();
        GetComponent<MeshFilter>().mesh = meshFilter.sharedMesh;
        m_collisionCount = 0;
        ReplaceCollider(collider);
        transform.localScale = _prefab.transform.localScale;
        transform.localRotation = _prefab.transform.localRotation;
        UpdateMaterial();
    }

    /*
     * @brief Replaces the current collider with a new one based on the target collider
     * Destroys the old collider and adds a new one of the same type, copying properties if it's a BoxCollider.
     * @param _target: The target Collider to copy from.
     * @return void
     */
    void ReplaceCollider(Collider _target)
    {
        Destroy(m_previewCollider);
        m_previewCollider = gameObject.AddComponent(_target.GetType()) as Collider;
        m_previewCollider.isTrigger = true;
        if (m_previewCollider is BoxCollider box &&
            _target is BoxCollider tBox)
        {
            box.center = tBox.center;
            box.size = tBox.size;
        }
    }

    /*
     * @brief OnTriggerEnter is called when another collider enters the trigger
     * Increments the collision count if not the ground, and updates the material.
     * @param _other: The other Collider that entered.
     * @return void
     */
    void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Ground"))
        {
            return;
        }
        m_collisionCount++;
        UpdateMaterial();
    }

    /*
     * @brief OnTriggerExit is called when another collider exits the trigger
     * Decrements the collision count if not the ground, and updates the material.
     * @param _other: The other Collider that exited.
     * @return void
     */
    void OnTriggerExit(Collider _other)
    {
        if (_other.CompareTag("Ground"))
        {
            return;
        }
        m_collisionCount--;
        UpdateMaterial();
    }

    /*
     * @brief Updates the material based on the collision count
     * Sets the valid material if no collisions, otherwise sets the invalid material.
     * @return void
     */
    void UpdateMaterial()
    {
        if (m_collisionCount == 0)
        {
            m_meshRenderer.material = m_validMat;
        }
        else
        {
            m_meshRenderer.material = m_invalidMat;
        }
    }
}
