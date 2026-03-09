using UnityEngine;
using UnityEngine.InputSystem;

public class GhostMorph : MonoBehaviour
{
    public bool m_isMorphed = false;

    [SerializeField] private GameObject m_mesh;
    [SerializeField] private GhostMorphPreview m_previewGhost;
    [SerializeField] private float m_scanRange = 10f;
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
        m_renderers = m_mesh.GetComponentsInChildren<MeshRenderer>();

    void Start()
    {
        m_playerCollider = GetComponent<BoxCollider>();


        m_renderers = m_mesh.GetComponentsInChildren<MeshRenderer>();

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
        m_previewGhost.SetPreview(_prefab);
    }

    [ObserversRpc(requireServer: true, runLocally: true)]
    public void InstantiateForAll(GameObject _prefab, Vector3 _position)
    {
        m_playerCollider.enabled = false;
        m_mesh.SetActive(false);
        InteractPromptUI.m_Instance.Hide();
        m_currentPrefab = UnityProxy.InstantiateDirectly(_prefab, transform);
        m_currentPrefab.transform.localPosition = _position;
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
        InteractPromptUI.m_Instance.Hide();
        m_isMorphed = false;
        DestroyForAll();
    }

    [ObserversRpc(requireServer: true, runLocally: true)]
    public void DestroyForAll()
    {
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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        m_wheel.ClearSelection();
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

        Vector3 rayOrigin = mainCamera.transform.position;
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
