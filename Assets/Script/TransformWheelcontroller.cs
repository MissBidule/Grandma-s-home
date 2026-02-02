using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for TransformWheelcontroller
 * @details The TransformWheelcontroller class manages the transformation wheel UI and handles input to open/close it.
 */
public class TransformWheelcontroller : MonoBehaviour
{
    public static TransformWheelcontroller m_Instance;
    [SerializeField] private Animator m_anim;
    [SerializeField] private TransformPreviewGhost m_previewGhost;
    [SerializeField] private PlayerGhost m_playerGhost;
    [SerializeField] private float m_scanRange = 5f;
    [SerializeField] private List<TransformWheelButtonController> m_wheelButtons;

    [NonSerialized] public GameObject m_selectedPrefab;

    [NonSerialized] public bool m_isWaitingForSlotSelection = false;
    private GameObject m_pendingPrefabToAdd;
    private Sprite m_pendingIconToAdd;

    private MeshRenderer m_playerMeshRenderer;
    private Material[] m_playerOriginalMaterials;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Sets the instance and gets the animator if not assigned.
     * @return void
     */
    void Awake()
    {
        m_Instance = this;
        if (m_anim == null)
        {
            m_anim = GetComponent<Animator>();
        }
        m_playerMeshRenderer = m_playerGhost.GetComponent<MeshRenderer>();
        m_playerOriginalMaterials = m_playerMeshRenderer.sharedMaterials;
    }

    /*
     * @brief Update is called every frame
     * Checks for Tab key input to toggle the transformation wheel.
     * @return void
     */
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (m_isWaitingForSlotSelection)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Confined;
            bool open = !m_anim.GetBool("OpenTransformWheel");
            m_anim.SetBool("OpenTransformWheel", open);
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

        if (m_playerGhost == null)
        {
            return;
        }

        Transform playerTransform = m_playerGhost.transform;
        Camera mainCamera = Camera.main;

        Vector3 rayOrigin = playerTransform.position;
        Vector3 rayDirection = mainCamera.transform.forward;

        if (!Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, m_scanRange, -1))
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

        TryAddPrefabToWheel(scannedObject, scannableComponent.icon);
    }

    /*
     * @brief Tries to add a prefab to the transformation wheel
     * Finds the first empty slot or triggers slot selection if wheel is full.
     * @param _prefab: The prefab to add
     * @param _icon: The icon for the prefab (can be null)
     * @return void
     */
    private void TryAddPrefabToWheel(GameObject _prefab, Sprite _icon)
    {
        if (IsIconAlreadyInWheel(_icon))
        {
            Debug.Log($"Object already in the wheel: {_prefab.name}");
            SelectPrefab(_prefab);
            return;
        }

        TransformWheelButtonController emptySlot = FindFirstEmptySlot();

        if (emptySlot != null)
        {
            AddPrefabToSlot(emptySlot, _prefab, _icon);

            SelectPrefab(_prefab);
        }
        else
        {
            m_pendingPrefabToAdd = _prefab;
            m_pendingIconToAdd = _icon;
            m_isWaitingForSlotSelection = true;

            Cursor.lockState = CursorLockMode.Confined;
            m_anim.SetBool("OpenTransformWheel", true);

            Debug.Log("Wheel full");
        }
    }

    /*
     * @brief Checks if an icon is already present in the transformation wheel
     * Iterates through all wheel slots and compares the icon reference.
     * @param _icon: The icon Sprite to search for
     * @return True if the icon is already in the wheel, false otherwise
     */
    private bool IsIconAlreadyInWheel(Sprite _icon)
    {
        foreach (TransformWheelButtonController button in m_wheelButtons)
        {
            TransformOption option = button.GetTransformOption();
            if (option != null && option.icon == _icon)
            {
                return true;
            }
        }
        return false;
    }

    /*
     * @brief Finds the first empty slot in the transformation wheel
     * @return The first empty TransformWheelButtonController, or null if all slots are full
     */
    private TransformWheelButtonController FindFirstEmptySlot()
    {
        foreach (TransformWheelButtonController button in m_wheelButtons)
        {
            if (button.IsEmpty())
            {
                return button;
            }
        }
        return null;
    }

    /*
     * @brief Adds a prefab to a specific slot
     * @param _slot: The slot to add the prefab to
     * @param _prefab: The prefab to add
     * @param _icon: The icon for the prefab (can be null)
     * @return void
     */
    private void AddPrefabToSlot(TransformWheelButtonController _slot, GameObject _prefab, Sprite _icon)
    {
        TransformOption newOption = new TransformOption(_prefab, _icon);
        _slot.UpdateTransformOption(newOption);
        Debug.Log($"Prefab: {_prefab.name} added to the wheel");
    }

    /*
     * @brief Called when a slot is chosen for replacement (when wheel is full)
     * @param _chosenSlot: The slot that was chosen
     * @return void
     */
    public void OnSlotChosenForReplacement(TransformWheelButtonController _chosenSlot)
    {
        if (!m_isWaitingForSlotSelection)
        {
            return;
        }

        AddPrefabToSlot(_chosenSlot, m_pendingPrefabToAdd, m_pendingIconToAdd);

        SelectPrefab(m_pendingPrefabToAdd);

        m_anim.SetBool("OpenTransformWheel", false);

        m_isWaitingForSlotSelection = false;
        m_pendingPrefabToAdd = null;
        m_pendingIconToAdd = null;
    }

    /*
     * @brief Selects the prefab for transformation
     * Sets the selected prefab and activates the preview ghost.
     * @param _prefab: The prefab GameObject to select.
     * @return void
     */
    public void SelectPrefab(GameObject _prefab)
    {
        m_selectedPrefab = _prefab;
        m_previewGhost.SetPreview(_prefab);
        m_previewGhost.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        ApplyPlayerTransparency();
    }

    /*
     * @brief Applies a transparency effect to the player
     * Creates semi-transparent tinted materials from the player's original materials and applies them to the MeshRenderer.
     * @return void
     */
    private void ApplyPlayerTransparency()
    {
        Material[] mats = new Material[m_playerOriginalMaterials.Length];
        for (int i = 0; i < mats.Length; ++i)
        {
            mats[i] = new Material(m_playerOriginalMaterials[i]);
            mats[i].color = new Color(0.4f, 0.5f, 0.8f, 0.2f);
            mats[i].SetFloat("_Surface", 1);
            mats[i].SetInt("_SrcBlend", 10);
            mats[i].SetInt("_DstBlend", 10);
            mats[i].renderQueue = 3000;
        }
        m_playerMeshRenderer.materials = mats;
    }

    /*
     * @brief Removes the transparency effect from the player
     * Restores the original materials to the player's MeshRenderer.
     * @return void
     */
    private void RemovePlayerTransparency()
    {
        m_playerMeshRenderer.sharedMaterials = m_playerOriginalMaterials;
    }

    /*
     * @brief Clears the current selection
     * Resets the selected prefab and deactivates the preview ghost.
     * @return void
     */
    public void ClearSelection()
    {
        m_selectedPrefab = null;
        m_previewGhost.gameObject.SetActive(false);
        RemovePlayerTransparency();
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
