using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for WheelController
 * @details The WheelController class manages the transformation wheel UI and handles input to open/close it.
 */
public class WheelController : MonoBehaviour
{
    public static WheelController m_Instance;

    [SerializeField] private Animator m_anim;
    [SerializeField] private GhostMorph m_ghostTransform;
    [SerializeField] private List<WheelButtonController> m_wheelButtons;

    [NonSerialized] public GameObject m_selectedPrefab;
    [NonSerialized] public bool m_isWaitingForSlotSelection = false;

    private GameObject m_pendingPrefabToAdd;
    private Sprite m_pendingIconToAdd;

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

        // Will break in multi I guess, cause there will be multiple instances of <<GhostMorph>> ?
        // We would need an other way to get reference to it
        m_ghostTransform = FindAnyObjectByType<GhostMorph>();
    }

    /*
     * @brief Toggle is called by the GhostInputController
     * Toggle the transformation wheel.
     * @return void
     */
    public void Toggle()
    {
        if (m_isWaitingForSlotSelection)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Confined;
        bool toggle = !m_anim.GetBool("OpenWheel");
        m_anim.SetBool("OpenWheel", toggle);
    }

    /*
     * @brief Check if the wheel is currently open
     * @return True if the wheel is open, false otherwise
     */
    public bool IsWheelOpen()
    {
        if (m_anim == null)
        {
            return false;
        }
        return m_anim.GetBool("OpenWheel");
    }

    /*
     * @brief Tries to add a prefab to the transformation wheel
     * Finds the first empty slot or triggers slot selection if wheel is full.
     * @param _prefab: The prefab to add
     * @param _icon: The icon for the prefab (can be null)
     * @return void
     */
    public void TryAddPrefabToWheel(GameObject _prefab, Sprite _icon)
    {
        if (IsIconAlreadyInWheel(_icon))
        {
            Debug.Log($"Object already in the wheel: {_prefab.name}");
            SelectPrefab(_prefab);
            return;
        }

        WheelButtonController emptySlot = FindFirstEmptySlot();

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
            m_anim.SetBool("OpenWheel", true);

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
        foreach (WheelButtonController button in m_wheelButtons)
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
    private WheelButtonController FindFirstEmptySlot()
    {
        foreach (WheelButtonController button in m_wheelButtons)
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
    private void AddPrefabToSlot(WheelButtonController _slot, GameObject _prefab, Sprite _icon)
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
    public void OnSlotChosenForReplacement(WheelButtonController _chosenSlot)
    {
        if (!m_isWaitingForSlotSelection)
        {
            return;
        }

        AddPrefabToSlot(_chosenSlot, m_pendingPrefabToAdd, m_pendingIconToAdd);

        SelectPrefab(m_pendingPrefabToAdd);

        m_anim.SetBool("OpenWheel", false);

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
        m_ghostTransform.SetPreview(_prefab);
        Cursor.lockState = CursorLockMode.Locked;

        m_anim.SetBool("OpenWheel", false);
    }

    /*
     * @brief Clears the current selection
     * Resets the selected prefab and deactivates the preview ghost.
     * @return void
     */
    public void ClearSelection()
    {
        m_selectedPrefab = null;
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
