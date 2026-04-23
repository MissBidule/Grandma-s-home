using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for WheelController
 * @details The WheelController class manages the transformation wheel UI and handles input to open/close it.
 */
public class WheelController : MonoBehaviour
{
    [SerializeField] private Animator m_anim;
    [NonSerialized] private GhostMorph m_ghostMorph;
    [NonSerialized] private GhostMorphPreview m_ghostMorphPreview;
    [SerializeField] public List<WheelButtonController> m_wheelButtons;
    [SerializeField] private float m_angleOffset = 114f;
    [SerializeField] private float m_minSelectDistance = 5f;

    [SerializeField] private string m_promptLabelReplace = "Replace transform slot";

    [NonSerialized] public GameObject m_selectedPrefab;
    [NonSerialized] public bool m_isWaitingForSlotSelection = false;

    private GameObject m_pendingPrefabToAdd;
    private Sprite m_pendingIconToAdd;
    private bool m_isOpen = false;
    private int m_highlightedIndex = -1;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Sets the instance and gets the animator if not assigned.
     * @return void
     */
    void Awake()
    {
        if (m_anim == null)
        {
            m_anim = GetComponent<Animator>();
        }
    }

    /*
     * @brief Update is called once per frame
     * Updates the highlighted wheel slice based on mouse direction from screen center
     * @return void
     */
    void Update()
    {
        if (!m_isOpen) return;

        // Gamepad: use right stick for highlight, A button to confirm
        if (InputDeviceTracker.IsGamepadActive)
        {
            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.rightStick.ReadValue();
                if (stick.sqrMagnitude > 0.3f)
                {
                    float stickAngle = Mathf.Atan2(stick.y, stick.x) * Mathf.Rad2Deg;
                    int bestIndex = GetButtonIndexFromAngle(stickAngle);
                    if (bestIndex != m_gamepadHighlightIndex && bestIndex >= 0)
                    {
                        m_gamepadHighlightIndex = bestIndex;
                        ApplyHighlight(bestIndex);
                        var es = UnityEngine.EventSystems.EventSystem.current;
                        if (es != null && m_wheelButtons[bestIndex] != null)
                            es.SetSelectedGameObject(m_wheelButtons[bestIndex].gameObject);
                    }
                }

                if (gp.buttonSouth.wasPressedThisFrame && m_gamepadHighlightIndex >= 0)
                {
                    var btn = m_wheelButtons[m_gamepadHighlightIndex];
                    if (btn != null)
                        btn.Select();
                    m_gamepadHighlightIndex = -1;
                }
            }
            return;
        }

        // Mouse: use cursor position for highlight
        if (Mouse.current == null) return;
        Vector2 dir = Mouse.current.position.ReadValue() - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (dir.sqrMagnitude < m_minSelectDistance * m_minSelectDistance) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float sliceAngle = 360f / m_wheelButtons.Count;
        int newIndex = Mathf.RoundToInt((angle - m_angleOffset + 360f * 10f) % 360f / sliceAngle) % m_wheelButtons.Count;

        if (newIndex != m_highlightedIndex)
        {
            ApplyHighlight(newIndex);
        }
    }

    /*
     * @brief Applies highlight to the button at the given index
     * @param index: The index to highlight
     * @return void
     */
    private void ApplyHighlight(int index)
    {
        for (int i = 0; i < m_wheelButtons.Count; i++)
        {
            bool canHighlight = m_isWaitingForSlotSelection || !m_wheelButtons[i].IsEmpty();
            m_wheelButtons[i].SetHighlight(i == index && canHighlight);
        }
        m_highlightedIndex = index;
    }

    public void LinkWithGhost(GhostClientController ghost)
    {
        m_ghostMorph = ghost.GetComponent<GhostMorph>();
        m_ghostMorphPreview = ghost.GetComponentInChildren<GhostMorphPreview>();

        m_ghostMorphPreview.m_wheel = this;
    }

    /*
     * @brief Toggle is called by the GhostInputController
     * Toggle the transformation wheel.
     * @return void
     */
    public void Toggle()
    {
        if (m_anim.GetBool("OpenWheel"))
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    /*
     * @brief Opens the transformation wheel
     * Hides cursor and enables directional slice selection
     * @return void
     */
    public void Open()
    {
        if (m_isWaitingForSlotSelection) return;
        if (m_ghostMorph != null && m_ghostMorph.m_isMorphed) return;

        m_isOpen = true;
        ApplyHighlight(-1);
        if (InputDeviceTracker.IsGamepadActive)
            Cursor.lockState = CursorLockMode.Locked; // Gamepad uses stick, no cursor needed
        else
            Cursor.lockState = CursorLockMode.Confined;
        InteractPromptUI.m_Instance.Hide();
        m_anim.SetBool("OpenWheel", true);
    }

    /*
     * @brief Closes the transformation wheel
     * Confirms the currently highlighted slice selection
     * @return void
     */
    public void Close()
    {
        m_isOpen = false;

        int confirmedIndex = m_highlightedIndex;
        ApplyHighlight(-1);

        if (m_isWaitingForSlotSelection)
        {
            if (confirmedIndex >= 0 && confirmedIndex < m_wheelButtons.Count)
            {
                OnSlotChosenForReplacement(m_wheelButtons[confirmedIndex]);
            }
        }
        else if (confirmedIndex >= 0 && confirmedIndex < m_wheelButtons.Count)
        {
            m_wheelButtons[confirmedIndex].Select();
        }

        Interact ghostInteract = m_ghostMorph.GetComponentInChildren<Interact>();
        if (ghostInteract != null)
        {
            ghostInteract.m_onFocus = null;
        }

        Cursor.lockState = CursorLockMode.Locked;
        m_anim.SetBool("OpenWheel", false);
    }

    private int m_gamepadHighlightIndex = -1;

    private int GetButtonIndexFromAngle(float _angle)
    {
        if (m_wheelButtons == null || m_wheelButtons.Count == 0) return -1;

        int count = m_wheelButtons.Count;
        float sliceSize = 360f / count;
        // Offset so first slot is at top (90°)
        float adjusted = (90f - _angle + 360f) % 360f;
        int index = Mathf.FloorToInt(adjusted / sliceSize) % count;
        return Mathf.Clamp(index, 0, count - 1);
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
            m_isOpen = true;
            ApplyHighlight(-1);

            Cursor.lockState = CursorLockMode.Confined;

            m_anim.SetBool("OpenWheel", true);
            InteractPromptUI.m_Instance.Show(InputBindingHelper.BuildPrompt("Ghost", "OpenProps", m_promptLabelReplace));
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
            if (button == null) continue;
            TransformOption option = button.GetTransformOption();
            if (option != null && option.m_icon == _icon)
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
        if (!m_isWaitingForSlotSelection) return;

        AddPrefabToSlot(_chosenSlot, m_pendingPrefabToAdd, m_pendingIconToAdd);
        SelectPrefab(m_pendingPrefabToAdd);

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
        // It will be better with a StateMachine I guess cause all the states variable will be centralized.
        if (!m_ghostMorphPreview.transform.parent.GetComponent<GhostMorph>().m_isMorphed) 
        {
            m_selectedPrefab = _prefab;
            m_ghostMorphPreview.SetPreview(_prefab);
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Debug.Log("Cannot select a new prefab while already transformed");
        }
            //InteractPromptUI.m_Instance.Hide();
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
