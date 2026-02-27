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
public class GhostMorphPreview : NetworkBehaviour
{
    [SerializeField] private float m_scanRange = 10f;
    [SerializeField] private LayerMask m_scanLayerMask;
    [SerializeField] private GameObject m_mesh;

    private HashSet<Collider> m_colliders = new HashSet<Collider>();
    private MeshRenderer m_meshRenderer;
    private Collider m_previewCollider;
    public WheelController m_wheel;
    public bool m_canMorph => m_colliders.Count == 0;

    [NonSerialized] public GameObject m_currentPrefab = null;

    [SerializeField] private Color m_validColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color m_invalidColor = new Color(1f, 0f, 0f, 0f);

    [SerializeField] private Color m_highlightColor = Color.yellow;
    [SerializeField] private float m_pulseSpeed = 3f;
    [SerializeField] private float m_minIntensity = 0.2f;
    [SerializeField] private float m_maxIntensity = 0.6f;

    private GameObject m_currentHighlightedObject = null;
    private Coroutine m_pulseCoroutine = null;
    private MaterialPropertyBlock m_propertyBlock;

    private Transform m_cameraTransform;

    /*
     * Initializes the mesh renderer and collider, sets the collider as a trigger, and updates the material.
     * @return void
     */
    void Start()
    {
        if (!isOwner) return;
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_previewCollider = GetComponent<Collider>();
        m_meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        m_propertyBlock = new MaterialPropertyBlock();
        m_cameraTransform = transform.parent.GetComponent<GhostClientController>().m_playerCamera.transform;
    }

    private void Update()
    {
        if (!isOwner) return;
        CheckForScannableObject();
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

    /*
     * @brief Sets the preview based on the given prefab
     * Copies the mesh and collider from the prefab to the preview ghost.
     * @param _prefab: The prefab GameObject to preview.
     * @return void
     */
    public void SetPreview(GameObject _prefab)
    {
        m_currentPrefab = _prefab;

        MeshFilter meshFilter = _prefab.GetComponentInChildren<MeshFilter>();
        BoxCollider collider = _prefab.GetComponentInChildren<BoxCollider>();
        MeshRenderer prefabRenderer = _prefab.GetComponentInChildren<MeshRenderer>();

        m_meshRenderer.enabled = true;
        GetComponent<MeshFilter>().mesh = meshFilter.sharedMesh;

        if (prefabRenderer != null)
        {
            m_meshRenderer.sharedMaterials = prefabRenderer.sharedMaterials;
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

        transform.localPosition = new Vector3(0f, offsetY+0.01f, 0f);

        UpdateMaterial();
    }

    public void HidePreview()
    {
        m_meshRenderer.enabled = false;
        m_currentPrefab = null;
    }

    /*
     * @brief Modify the current collider to fit the size of the new prefab
     * @param _target: The target Collider to copy from.
     * @return void
     */
    void ReplaceCollider(BoxCollider _target)
    {
        if (m_previewCollider != null)
        {
            Destroy(m_previewCollider);
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = _target.center;
        box.size = _target.size;
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
        if (!isOwner) return;
        Material[] mats = m_meshRenderer.materials;
        Color targetColor = m_canMorph ? m_validColor : m_invalidColor;
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

    /*
     * @brief Checks for scannable objects in view and highlights them
     * @return void
     */
    private void CheckForScannableObject()
    {
        if (!isOwner) return;
        if (m_cameraTransform == null)
        {
            ClearHighlight();
            return;
        }

        Vector3 rayOrigin = m_cameraTransform.transform.position;
        Vector3 rayDirection = m_cameraTransform.transform.forward;

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
    public void ClearHighlight()
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
}
