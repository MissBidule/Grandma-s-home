using UnityEngine;
using System.Collections.Generic;

/*
 * @brief Contains class declaration for TransformPreviewGhost
 * @details The TransformPreviewGhost class handles the preview of transformations, checking for collisions and updating materials accordingly.
 */
public class GhostMorphPreview : MonoBehaviour
{
    [SerializeField] private GameObject m_mesh;

    private HashSet<Collider> m_colliders = new HashSet<Collider>();

    private MeshRenderer m_meshRenderer;
    private Collider m_previewCollider;
    public bool m_CanTransform => m_colliders.Count == 0;

    [SerializeField] private Color m_validColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color m_invalidColor = new Color(1f, 0f, 0f, 0.3f);

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
        MeshRenderer prefabRenderer = _prefab.GetComponentInChildren<MeshRenderer>();

        GetComponent<MeshFilter>().mesh = meshFilter.sharedMesh;

        if (prefabRenderer != null)
        {
            m_meshRenderer.sharedMaterials = prefabRenderer.sharedMaterials;
        }

        ReplaceCollider(collider);
        transform.localScale = _prefab.transform.localScale;
        transform.localRotation = _prefab.transform.localRotation;

        /*
         Maths to place the preview correctly on the player
         */
        Renderer[] playerRenders = m_mesh.GetComponentsInChildren<Renderer>();

        Bounds playerBounds = playerRenders[0].bounds;
        for (int i = 1; i < playerRenders.Length; i++)
            playerBounds.Encapsulate(playerRenders[i].bounds);

        // utiliser le renderer DU GHOST (déjà en scène)
        Renderer previewRender = GetComponentInChildren<Renderer>();
        Bounds previewBounds = previewRender.bounds;

        float offsetY = playerBounds.min.y - previewBounds.min.y;

        transform.localPosition = new Vector3(0f, offsetY+0.01f, 0f);

        UpdateMaterial();
    }

    /*
     * @brief Modify the current collider to fit the size of the new prefab
     * @param _target: The target Collider to copy from.
     * @return void
     */
    void ReplaceCollider(Collider _target)
    {
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
        if (_other.CompareTag("Ground") || _other.gameObject.layer == 9)
            return;

        m_colliders.Add(_other);
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
        if (_other.CompareTag("Ground") || _other.gameObject.layer == 9)
            return;

        m_colliders.Remove(_other);
        UpdateMaterial();
    }

    /*
     * @brief Updates the material colors based on the collision count
     * Applies transparency and color to the preview materials.
     * @return void
     */
    void UpdateMaterial()
    {
        Material[] mats = m_meshRenderer.materials;
        Color targetColor = m_CanTransform ? m_validColor : m_invalidColor;

        foreach (Material mat in mats)
        {
            mat.color = targetColor;

            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
