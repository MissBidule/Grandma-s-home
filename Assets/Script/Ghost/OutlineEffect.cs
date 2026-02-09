using UnityEngine;

/*
 * @brief Contains class declaration for OutlineEffect
 * @details The OutlineEffect class renders an outline around an object using a scaled duplicate mesh technique.
 */
public class OutlineEffect : MonoBehaviour
{
    [SerializeField] private Color m_outlineColor = Color.cyan;
    [SerializeField] private float m_outlineWidth = 0.03f;

    private GameObject m_outlineObject;
    private Material m_outlineMaterial;

    /*
     * @brief Called when the component is enabled
     * Creates the outline effect.
     * @return void
     */
    void OnEnable()
    {
        CreateOutline();
    }

    /*
     * @brief Called when the component is disabled
     * Removes the outline effect.
     * @return void
     */
    void OnDisable()
    {
        DestroyOutline();
    }

    /*
     * @brief Sets the outline color and width
     * @param _color: The color of the outline
     * @param _width: The width of the outline
     * @return void
     */
    public void SetOutline(Color _color, float _width)
    {
        m_outlineColor = _color;
        m_outlineWidth = _width;

        if (enabled)
        {
            DestroyOutline();
            CreateOutline();
        }
    }

    /*
     * @brief Creates the outline by duplicating the mesh
     * @return void
     */
    private void CreateOutline()
    {
        // Get all mesh renderers (including children)
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0) return;

        // Create parent for all outlines
        m_outlineObject = new GameObject("OutlineGroup");
        m_outlineObject.transform.SetParent(transform);
        m_outlineObject.transform.localPosition = Vector3.zero;
        m_outlineObject.transform.localRotation = Quaternion.identity;
        m_outlineObject.transform.localScale = Vector3.one;

        // Create outline material with proper settings
        Shader outlineShader = Shader.Find("Custom/OutlineShader");
        if (outlineShader == null)
        {
            // Fallback to Unlit shader if custom shader not found
            outlineShader = Shader.Find("Unlit/Color");
        }

        m_outlineMaterial = new Material(outlineShader);
        m_outlineMaterial.SetColor("_Color", m_outlineColor);
        m_outlineMaterial.SetColor("_OutlineColor", m_outlineColor);
        m_outlineMaterial.SetFloat("_OutlineWidth", m_outlineWidth);

        // Create outline for each mesh
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null) continue;

            GameObject outlinePart = new GameObject("OutlinePart");
            outlinePart.transform.SetParent(m_outlineObject.transform);
            outlinePart.transform.position = meshFilter.transform.position;
            outlinePart.transform.rotation = meshFilter.transform.rotation;

            // Scale slightly larger
            Vector3 worldScale = meshFilter.transform.lossyScale;
            float scaleMultiplier = 1.0f + m_outlineWidth;
            outlinePart.transform.localScale = new Vector3(
                worldScale.x * scaleMultiplier / m_outlineObject.transform.lossyScale.x,
                worldScale.y * scaleMultiplier / m_outlineObject.transform.lossyScale.y,
                worldScale.z * scaleMultiplier / m_outlineObject.transform.lossyScale.z
            );

            MeshFilter outlineFilter = outlinePart.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = meshFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlinePart.AddComponent<MeshRenderer>();
            outlineRenderer.material = m_outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;

            outlinePart.layer = gameObject.layer;
        }
    }

    /*
     * @brief Destroys the outline
     * @return void
     */
    private void DestroyOutline()
    {
        if (m_outlineObject != null)
        {
            Destroy(m_outlineObject);
            m_outlineObject = null;
        }

        if (m_outlineMaterial != null)
        {
            Destroy(m_outlineMaterial);
            m_outlineMaterial = null;
        }
    }

    /*
     * @brief Called when the object is destroyed
     * Cleans up the outline.
     * @return void
     */
    void OnDestroy()
    {
        DestroyOutline();
    }
}