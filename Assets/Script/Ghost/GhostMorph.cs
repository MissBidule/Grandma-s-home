using PurrNet;
using PurrNet.Logging;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GhostMorph : NetworkBehaviour
{
    [SerializeField] private GameObject m_mesh;
    [SerializeField] private GhostMorphPreview m_previewGhost;
    [SerializeField] private float m_scanRange = 10f;
    [SerializeField] private LayerMask m_scanLayerMask;
    private WheelController m_wheel;

    public bool m_isTransformed = false;
    private GameObject m_currentPrefab = null;
    private Collider m_playerCollider;
    private MeshRenderer[] m_renderers;
    private Material[][] m_originalMaterials;

    public PlayerID m_localPlayer;

    [SerializeField] private Color m_highlightColor = Color.yellow;
    [SerializeField] private float m_pulseSpeed = 3f;
    [SerializeField] private float m_minIntensity = 0.2f;
    [SerializeField] private float m_maxIntensity = 0.6f;

    private GameObject m_currentHighlightedObject = null;
    private Coroutine m_pulseCoroutine = null;
    private MaterialPropertyBlock m_propertyBlock;

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

        m_propertyBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        // Code moved to OnSpawned
    }

    void Update()
    {
        CheckForScannableObject();
    }

    /*
     * @brief Checks for scannable objects in view and highlights them
     * @return void
     */
    private void CheckForScannableObject()
    {
        CinemachineCamera mainCamera = GetComponent<PlayerControllerCore>().m_playerCamera;
        if (mainCamera == null)
        {
            ClearHighlight();
            return;
        }

        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, m_scanRange, m_scanLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (IsPartOfPlayer(hitObject))
            {
                ClearHighlight();
                return;
            }

            ScannableObject scannableComponent = hitObject.GetComponent<ScannableObject>();

            if (scannableComponent != null && scannableComponent.m_icon != null)
            {
                if (m_currentHighlightedObject != hitObject)
                {
                    ClearHighlight();
                    HighlightObject(hitObject);
                }
            }
            else
            {
                ClearHighlight();
            }
        }
        else
        {
            ClearHighlight();
        }
    }

    /*
     * @brief Checks if a GameObject is part of the player hierarchy
     * @param _obj: The GameObject to check
     * @return True if the object is the player or a child of the player
     */
    private bool IsPartOfPlayer(GameObject _obj)
    {
        Transform current = _obj.transform;
        while (current != null)
        {
            if (current == transform)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    /*
     * @brief Highlights a scannable object with pulsing emission
     * @param _object: The GameObject to highlight
     * @return void
     */
    private void HighlightObject(GameObject _object)
    {
        m_currentHighlightedObject = _object;

        Renderer[] objectRenderers = _object.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in objectRenderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null)
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        if (m_pulseCoroutine != null)
        {
            StopCoroutine(m_pulseCoroutine);
        }
        m_pulseCoroutine = StartCoroutine(PulseHighlight());
    }

    /*
     * @brief Animates the highlight with a pulsing effect
     * @return IEnumerator for coroutine
     */
    private IEnumerator PulseHighlight()
    {
        float time = 0;

        Renderer[] renderers = m_currentHighlightedObject.GetComponentsInChildren<Renderer>();

        while (m_currentHighlightedObject != null)
        {
            float pulse = Mathf.Lerp(m_minIntensity, m_maxIntensity,
                                    (Mathf.Sin(time * m_pulseSpeed) + 1f) * 0.5f);

            foreach (Renderer r in renderers)
            {
                r.GetPropertyBlock(m_propertyBlock);
                m_propertyBlock.SetColor("_EmissionColor", m_highlightColor * pulse);
                r.SetPropertyBlock(m_propertyBlock);
            }

            time += Time.deltaTime;
            yield return null;
        }
    }

    /*
     * @brief Clears the current highlight by restoring the original materials
     * @return void
     */
    private void ClearHighlight()
    {
        if (m_currentHighlightedObject != null)
        {
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
                m_pulseCoroutine = null;
            }

            Renderer[] objectRenderers = m_currentHighlightedObject.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in objectRenderers)
            {
                r.SetPropertyBlock(null);
            }

            m_currentHighlightedObject = null;
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
        ApplyPrefab(prefab, m_previewGhost.transform.localPosition);
        m_wheel.m_selectedPrefab = null;
        m_previewGhost.GetComponent<MeshRenderer>().enabled = false;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Reset the input to prevent immediate detransformation
        GhostInputController ghostInput = GetComponent<GhostInputController>();
        if (ghostInput != null)
        {
            ghostInput.ResetMovementInput();
        }

        if (m_wheel.IsWheelOpen())
        {
            m_wheel.Toggle();
        }

        ClearHighlight();
    }

    /*
     * @brief Reverts the player to their original appearance
     * Restores the original mesh, materials, and collider.
     * @return void
     */
    //Called by everyone
    [ObserversRpc(runLocally: true)]
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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        m_wheel.ClearSelection();
    }

    /*
     * @brief Copies mesh, materials, and collider from the given prefab to the player
     * Applies the components from the prefab if they exist.
     * @param _prefab: The prefab GameObject to copy from.
     * @param _position: The local position to place the instantiated prefab.
     * @return void
     */
    //Called by everyone
    [ObserversRpc(runLocally: true)]
    void ApplyPrefab(GameObject _prefab, Vector3 _position)
    {
        print("Applying prefab: " + _prefab.name);
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
        //correct position for observers
        m_currentPrefab.transform.localPosition = _position;
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
