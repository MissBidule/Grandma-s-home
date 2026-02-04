using UnityEngine;
using UnityEngine.InputSystem;

public class GhostMorph : MonoBehaviour
{
    [SerializeField] private GameObject m_mesh;
    [SerializeField] private GhostMorphPreview m_previewGhost;
    [SerializeField] private float m_scanRange = 5f;
    [SerializeField] private LayerMask m_scanLayerMask;
    private WheelController m_wheel;

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

        m_wheel = WheelController.m_Instance;

        m_originalMaterials = new Material[m_renderers.Length][];
        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_originalMaterials[i] = m_renderers[i].sharedMaterials;
        }
    }

    public void SetPreview(GameObject _prefab)
    {
        if (m_isTransformed)
        {
            return;
        }
        ApplyTransparency();
        m_previewGhost.SetPreview(_prefab);
    }

    public void ClearPreview()
    {
        RemovePlayerTransparency();
        transform.localPosition = new Vector3(0f, 0f, 0f);
    }

    /*
     * @brief Applies a transparency effect to the player
     * Applies transparency and color to the preview materials.
     * @return void
     */
    private void ApplyTransparency()
    {
        foreach (MeshRenderer renderer in m_renderers)
        {
            Material[] mats = renderer.materials;
            foreach (Material mat in mats)
            {
                Color color = mat.color;
                color.a = 0.2f;
                mat.color = color;
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }
    }

    /*
     * @brief Removes the transparency effect from the player
     * Restores the original materials to the player's MeshRenderer.
     * @return void
     */
    private void RemovePlayerTransparency()
    {
        foreach (MeshRenderer renderer in m_renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = 1.0f;
                mat.color = color;
                mat.SetFloat("_Surface", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.renderQueue = -1;
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
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
        if (!_context.performed || !m_previewGhost.m_CanTransform || !m_wheel.m_selectedPrefab || m_isTransformed)
        {
            return;
        }
        GameObject prefab = m_wheel.m_selectedPrefab;
        ApplyPrefab(prefab);
        m_wheel.m_selectedPrefab = null;
        m_previewGhost.GetComponent<MeshRenderer>().enabled = false;
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
        m_currentPrefab.transform.localPosition = m_previewGhost.transform.localPosition;
    }


    /*
     * @brief Scans for a scannable prefab in front of the player
     * If found and there's a free slot, adds it to the wheel. If wheel is full, opens the wheel for slot selection.
     * @return void
     */
    public void ScanForPrefab()
    {
        Debug.Log("Scan");
        Camera mainCamera = Camera.main;

        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;

        if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, m_scanRange, m_scanLayerMask))
        {
            Debug.Log("No objects detected by the raycast");
            return;
        }

        GameObject scannedObject = hit.collider.gameObject;
        Debug.Log($"Object detected: {scannedObject.name}");

        ScannableObject scannableComponent = scannedObject.GetComponent<ScannableObject>();
        if (scannableComponent == null)
        {
            Debug.Log($"Object detected but not scannable: {scannedObject.name}");
            return;
        }

        Debug.Log($"Scannable object found: {scannedObject.name}");

        if (scannableComponent.icon == null)
        {
            Debug.Log($"No icon for the scanned object: {scannedObject.name}");
            return;
        }

        m_wheel.TryAddPrefabToWheel(scannedObject, scannableComponent.icon);
    }
}
