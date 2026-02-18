using System.Collections;
using PurrNet;
using PurrNet.Logging;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;


//MISSING COMM
public class GhostMorph : NetworkBehaviour
{
    [SerializeField] private GameObject m_mesh;
    [SerializeField] private GhostMorphPreview m_previewGhost;
    [SerializeField] private float m_scanRange = 10f;
    [SerializeField] private LayerMask m_scanLayerMask;
    private WheelController m_wheel;

    public bool m_isTransformed = false;
    public bool m_isLocked = false;
    private GameObject m_currentPrefab = null;
    private Collider m_playerCollider;
    private MeshRenderer[] m_renderers;
    private Material[][] m_originalMaterials;

    public PlayerID m_localPlayer;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        
        // Move Start code to OnSpawned for proper network Initialisation
        
        m_playerCollider = GetComponent<BoxCollider>();
        m_renderers = m_mesh.GetComponentsInChildren<MeshRenderer>();

        m_wheel = GetComponent<GhostInputController>().m_wheelController;
        m_wheel.m_localPlayer = (PlayerID)localPlayer;

        m_originalMaterials = new Material[m_renderers.Length][];
        for (int i = 0; i < m_renderers.Length; i++)
        {
            m_originalMaterials[i] = m_renderers[i].sharedMaterials;
        }
    }

    void Start()
    {
        // Code moved to OnSpawned
    }

    public void SetPreview(GameObject _prefab)
    {
        if (m_isTransformed)
        {
            return;
        }
        m_previewGhost.SetPreview(_prefab);
    }

    /*
     * @brief Confirms the transformation based on input
     * Applies the selected prefab if the context is performed and transformation is allowed.
     * @return void
     */
    public void TryToConfirmTransform()
    {
        ConfirmTransform(m_previewGhost, m_wheel.m_selectedPrefab);
    }

    /*
     * @brief Checks that we can transform from the server side
     * @return void
     */
    [ServerRpc]
    private void ConfirmTransform(GhostMorphPreview _previewGhost, GameObject _selectedPrefab)
    {
        if (!_previewGhost.m_CanTransform || !_selectedPrefab || m_isTransformed)
        {
            return;
        }
        GameObject prefab = _selectedPrefab;
        
        ApplyPrefab(prefab, _previewGhost.transform.localPosition);
    }

    /*
     * @brief Reverts the player to their original appearance
     * Restores the original mesh, materials, and collider.
     * @return void
     */
     //Called by everyone
    [ObserversRpc]
    public void RevertToOriginal()
    {
        if (!m_isTransformed || m_isLocked)
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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        m_wheel.ClearSelection();
    }

    /*
     * @brief Copies mesh, materials, and collider from the given prefab to the player
     * Applies the components from the prefab if they exist.
     * @param _prefab: The prefab GameObject to copy from.
     * @return void
     */
     //Called by everyone
    [ObserversRpc(runLocally:true)]
    void ApplyPrefab(GameObject _prefab, Vector3 _position)
    {        
        // Reset the input to prevent immediate detransformation on all sides
        GhostInputController ghostInput = GetComponent<GhostInputController>();
        if (ghostInput != null)
        {
            StopCoroutine(LockTransform());
            ghostInput.ResetMovementInput();
            m_isLocked = true;
            StartCoroutine(LockTransform());
        }
        ApplyPrefabLocally(_prefab, _position);
    }

    /*
     * @brief Spawn the transform on each client
     * @return void
     */
    void ApplyPrefabLocally(GameObject _prefab, Vector3 _position)
    {
        m_wheel.m_selectedPrefab = null;
        m_previewGhost.GetComponent<MeshRenderer>().enabled = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (m_wheel.IsWheelOpen())
        {
            m_wheel.Toggle();
        }

        m_isTransformed = true;
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
        Spawn(_prefab);
        //correct position for observers
        m_currentPrefab.transform.localPosition = _position;
    }

    IEnumerator LockTransform()
    {
        yield return new WaitForSeconds(.5f);
        UnlockGhost();
    }
    
    [ObserversRpc]
    private void UnlockGhost()
    {
        m_isLocked = false;
    }

    /*
     * @brief Scans for a scannable prefab in front of the player
     * If found and there's a free slot, adds it to the wheel. If wheel is full, opens the wheel for slot selection.
     * @return void
     */
    public void ScanForPrefab()
    {
        Debug.Log("Scan");
        CinemachineCamera mainCamera = GetComponent<PlayerControllerCore>().m_playerCamera;

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

        if (scannableComponent.m_icon == null)
        {
            Debug.Log($"No icon for the scanned object: {scannedObject.name}");
            return;
        }

        m_wheel.TryAddPrefabToWheel(scannedObject, scannableComponent.m_icon);
    }
}
