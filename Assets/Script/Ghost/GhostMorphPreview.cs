using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/*
 * @brief Contains class declaration for TransformPreviewGhost
 * @details The TransformPreviewGhost class handles the preview of transformations, checking for collisions and updating materials accordingly.
 */
public class GhostMorphPreview : MonoBehaviour
{
    [SerializeField] private float m_scanRange = 10f;
    [SerializeField] private LayerMask m_scanLayerMask;
    [SerializeField] private GameObject m_mesh;
    [SerializeField] private Shader m_shader;

    private HashSet<Collider> m_colliders = new HashSet<Collider>();
    private MeshRenderer m_meshRenderer;
    public Collider m_previewCollider;
    public WheelController m_wheel;
    public bool m_canMorph => m_colliders.Count == 0;

    [NonSerialized] public GameObject m_currentPrefab = null;

    [SerializeField] private Color m_validColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color m_invalidColor = new Color(1f, 0f, 0f, 1f);

    [SerializeField] private Color m_highlightColor = Color.yellow;
    [SerializeField] private float m_pulseSpeed = 3f;
    [SerializeField] private float m_minIntensity = 0.2f;
    [SerializeField] private float m_maxIntensity = 0.6f;

    private GameObject m_currentHighlightedObject = null;
    private Coroutine m_pulseCoroutine = null;
    private MaterialPropertyBlock m_propertyBlock;

    [SerializeField] private Material m_ghostTransparentMaterial;

    private Transform m_cameraTransform;
    private PlayerControllerCore m_core;
    private Interact m_interact;
    private GhostClientController m_ghostClientController;
    private Material[] m_ghostOriginalMaterials;
    private Renderer m_ghostBodyRenderer;
    private bool m_rotateLeft = false;
    private bool m_rotateRight = false;

    [SerializeField] private string m_promptLabelSCAN = "SCAN";
    [SerializeField] private string m_promptLabelValid = "Confirm transform";
    [SerializeField] private float m_rotateSpeed = 120f;

    [SerializeField] private bool m_GhostPreviewOn;
    
    /*
     * Initializes the mesh renderer and collider, sets the collider as a trigger, and updates the material.
     * @return void
     */
    void Start()
    {
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_previewCollider = GetComponent<Collider>();
        m_meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        m_propertyBlock = new MaterialPropertyBlock();
        // Use PlayerControllerCore.m_playerCamera (Inspector-assigned, always valid)
        // instead of GhostClientController.m_playerCamera (lazy-initialized, may be null)
        m_core = transform.parent.GetComponent<PlayerControllerCore>();
        if (m_core != null && m_core.m_playerCamera != null)
            m_cameraTransform = m_core.m_playerCamera.transform;
        m_interact = transform.parent.GetComponentInChildren<Interact>();
        m_ghostClientController = transform.parent.GetComponent<GhostClientController>();
    }


    private void Update()
    {
        if (m_core == null || !m_core.isOwner) return;
        CheckForScannableObject();

        if (m_currentPrefab != null)
        {
            float rotDir = 0f;
            if (m_rotateLeft) rotDir -= 1f;
            if (m_rotateRight) rotDir += 1f;
            if (rotDir != 0f)
            {
                transform.Rotate(0f, rotDir * m_rotateSpeed * Time.deltaTime, 0f, Space.World);
                UpdateMaterial();
            }
        }

        if (m_GhostPreviewOn)
        {
            InteractPromptUI.m_Instance.ShowDynamic(() => InputBindingHelper.BuildPrompt("Ghost", "Interact", m_promptLabelValid));
        }
    }

    /*
     * @brief Scans for a scannable prefab in front of the player
     * If found and there's a free slot, adds it to the wheel. If wheel is full, opens the wheel for slot selection.
     * @return void
     */
    public void ScanForPrefab()
    {
        Debug.Log("Scan");

        Vector3 rayOrigin = m_cameraTransform.transform.position;
        Vector3 rayDirection = m_cameraTransform.transform.forward;

        if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, m_scanRange, m_scanLayerMask))
        {
            Debug.Log("No objects detected by the raycast");
            if (m_GhostPreviewOn && !(m_ghostClientController?.m_cancelPreviewBlocked ?? false)) { HidePreview(); InteractPromptUI.m_Instance.Hide(); }
            return;
        }

        GameObject scannedObject = hit.collider.gameObject;
        Debug.Log($"Object detected: {scannedObject.name}");

        if (IsPartOfPlayer(scannedObject))
        {
            Debug.Log("Cannot scan yourself");
            return;
        }

        ScannableObject scannableComponent = scannedObject.GetComponent<ScannableObject>();
        if (scannableComponent == null)
        {
            Debug.Log($"Object detected but not scannable: {scannedObject.name}");
            if (m_GhostPreviewOn && !(m_ghostClientController?.m_cancelPreviewBlocked ?? false)) { HidePreview(); InteractPromptUI.m_Instance.Hide(); }
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

    /*
     * @brief Sets the preview based on the given prefab
     * Copies the mesh and collider from the prefab to the preview ghost.
     * @param _prefab: The prefab GameObject to preview.
     * @return void
     */
    public void SetPreview(GameObject _prefab, RPCInfo _info = default)
    {
        m_currentPrefab = _prefab;

        MeshFilter meshFilter = _prefab.GetComponentInChildren<MeshFilter>();
        MeshCollider collider = _prefab.GetComponentInChildren<MeshCollider>();
        MeshRenderer prefabRenderer = _prefab.GetComponentInChildren<MeshRenderer>();

        m_meshRenderer.enabled = true;
        GetComponent<MeshFilter>().mesh = meshFilter.sharedMesh;

        SwapGhostMaterial(true);

        if (prefabRenderer != null)
        {
            m_meshRenderer.sharedMaterials = prefabRenderer.sharedMaterials;
            //This one prevents unwanted visuals
            UpdateMaterial();

            m_GhostPreviewOn =true;
        }
        m_colliders.Clear();
        ReplaceCollider(collider);
        transform.localScale = _prefab.transform.localScale;
        transform.localRotation = _prefab.transform.localRotation;

        transform.localPosition = new Vector3(0, 0f, 0f);

        // Maths to place the preview correctly on the player
        Renderer[] playerRenders = m_mesh.GetComponentsInChildren<Renderer>();

        Bounds playerBounds = playerRenders[0].bounds;
        for (int i = 1; i < playerRenders.Length; i++)
        {
            playerBounds.Encapsulate(playerRenders[i].bounds);
        }

        Renderer previewRender = GetComponentInChildren<Renderer>();
        Bounds previewBounds = previewRender.bounds;

        float offsetY = playerBounds.min.y - previewBounds.min.y;

        transform.localPosition = new Vector3(0f, offsetY+0.1f, 0f);

        UpdateMaterial();
    }

    public void HidePreview()
    {
        m_meshRenderer.enabled = false;
        m_GhostPreviewOn = false;
        m_currentPrefab = null;

        SwapGhostMaterial(false);
    }

    /*
     * @brief Modify the current collider to fit the size of the new prefab
     * @param _target: The target Collider to copy from.
     * @return void
     */
    void ReplaceCollider(MeshCollider _target)
    {
        if (m_previewCollider != null)
        {
            Destroy(m_previewCollider);
        }

        MeshCollider box = gameObject.AddComponent<MeshCollider>();
        box.sharedMesh = _target.sharedMesh;
        box.convex = true;
        box.isTrigger = true;
        m_previewCollider = box;
    }

    /*
     * @brief OnTriggerEnter is called when another collider enters the trigger
     * Increments the collision count if not the ground, and updates the material.
     * @param _other: The other Collider that entered.
     * @return void
     */
    void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Ground") || _other.gameObject.layer == 9
            || _other.gameObject.layer == LayerMask.NameToLayer("Control"))
        {
            return;
        }

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
        if (_other.CompareTag("Ground") || _other.gameObject.layer == 9
        || _other.gameObject.layer == LayerMask.NameToLayer("Control"))
        {
            return;
        }

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
        if (m_meshRenderer == null) return;
        Material[] mats = m_meshRenderer.materials;
        Color targetColor = m_canMorph ? m_validColor : m_invalidColor;
        foreach (Material mat in mats)
        {
            mat.shader = m_shader;
            mat.color = targetColor;
        }
    }

    /*
     * @brief Checks for scannable objects in view and highlights them
     * @return void
     */
    private void CheckForScannableObject()
    {
        if (m_cameraTransform == null || GetComponentInParent<GhostMorph>().m_isMorphed)
        {
            ClearHighlight();
            return;
        }
        if (m_interact != null && m_interact.m_onFocus != null) return;
        if (m_wheel != null && m_wheel.IsWheelOpen()) return;

        Vector3 rayOrigin = m_cameraTransform.transform.position;
        Vector3 rayDirection = m_cameraTransform.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, m_scanRange, m_scanLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (IsPartOfPlayer(hitObject))
            {
                InteractPromptUI.m_Instance.Hide();

                ClearHighlight();
                return;
            }

            ScannableObject scannableComponent = hitObject.GetComponent<ScannableObject>();

            if (scannableComponent != null && scannableComponent.m_icon != null)
            {
                if (m_currentHighlightedObject != hitObject)
                {
                    if(!GetComponentInParent<GhostMorph>().m_isMorphed)
                    {
                       // There is a clone for few seconds...
                    InteractPromptUI.m_Instance.ShowDynamic(() => InputBindingHelper.BuildPrompt("Ghost", "Scan", m_promptLabelSCAN));
                    }
                    ClearHighlight();
                    HighlightObject(hitObject);
                }
            }
            else
            {
                ClearHighlight();
                InteractPromptUI.m_Instance.Hide();
            }
        }
        else
        {
             
            ClearHighlight();
            InteractPromptUI.m_Instance.Hide();
        }
    }

    /*
     * @brief Checks if a GameObject is part of the player hierarchy
     * @param _obj: The GameObject to check
     * @return True if the object is the player or a child of the player
     */
    private bool IsPartOfPlayer(GameObject _obj)
    {
        return _obj.GetComponentInParent<PlayerControllerCore>() != null;
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

    public void SetRotateLeft(bool active) => m_rotateLeft = active;
    public void SetRotateRight(bool active) => m_rotateRight = active;

    public bool IsLookingAtScannable()
    {
        if (m_cameraTransform == null) return false;
        if (!Physics.Raycast(m_cameraTransform.position, m_cameraTransform.forward, out RaycastHit hit, m_scanRange, m_scanLayerMask))
            return false;
        if (IsPartOfPlayer(hit.collider.gameObject)) return false;
        ScannableObject s = hit.collider.GetComponent<ScannableObject>();
        return s != null && s.m_icon != null;
    }

    private void SwapGhostMaterial(bool _transparent)
    {
        if (_transparent)
        {
            m_ghostBodyRenderer = m_mesh.GetComponentInChildren<Renderer>();
            if (m_ghostOriginalMaterials == null)
                m_ghostOriginalMaterials = m_ghostBodyRenderer.sharedMaterials;
            var mats = m_ghostBodyRenderer.sharedMaterials;
            mats[0] = m_ghostTransparentMaterial;
            m_ghostBodyRenderer.sharedMaterials = mats;
        }
        else if (m_ghostBodyRenderer != null && m_ghostOriginalMaterials != null)
        {
            m_ghostBodyRenderer.sharedMaterials = m_ghostOriginalMaterials;
            m_ghostBodyRenderer = null;
            m_ghostOriginalMaterials = null;
        }
    }
}
