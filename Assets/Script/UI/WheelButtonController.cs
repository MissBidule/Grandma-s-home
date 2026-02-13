using PurrNet;
using UnityEngine;
using UnityEngine.UI;

/*
 * @brief Contains class declaration for WheelButtonController
 * @details The WheelButtonController class manages the behavior of buttons in the transformation wheel UI, using TransformOption for data.
 */
public class WheelButtonController : NetworkBehaviour
{
    [SerializeField] private TransformOption m_transformOption;
    private Image m_iconImage;
    private Button m_button;

    private WheelController m_wheelController;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Finds the icon transform and sets its sprite from the TransformOption.
     * @return void
     */
    void Awake()
    {
        Transform iconTransform = transform.Find("icone");
        m_iconImage = iconTransform.GetComponent<Image>();
        m_button = GetComponent<Button>();
        
        
        m_wheelController = GetComponentInParent<WheelController>();
        
        
        UpdateIcon();
    }

    /*
     * @brief Updates the icon display based on current transform option
     * @return void
     */
    private void UpdateIcon()
    {
        if (m_iconImage != null)
        {
            if (m_transformOption != null && m_transformOption.m_icon != null)
            {
                m_iconImage.sprite = m_transformOption.m_icon;
                m_iconImage.enabled = true;
            }
            else
            {
                m_iconImage.enabled = false;
            }
        }

        if (m_button != null)
        {
            m_button.interactable = !IsEmpty();
        }
    }

    /*
     * @brief Selects this transformation option
     * Selects the prefab in the TransformWheelcontroller.
     * @return void
     */
    public void Select()
    {
        if (IsEmpty() && !m_wheelController.m_isWaitingForSlotSelection)
        {
            return;
        }

        if (m_wheelController.m_isWaitingForSlotSelection)  
        {
            OnSlotSelectedForReplacement();
            return;
        }

        if (m_transformOption == null || m_transformOption.m_prefab == null)
        {
            m_wheelController.ClearSelection();
            return;
        }
        m_wheelController.SelectPrefab(m_transformOption.m_prefab);
    }

    /*
     * @brief Updates the transform option for this button
     * @param _newOption: The new TransformOption to assign
     * @return void
     */
    public void UpdateTransformOption(TransformOption _newOption)
    {
        m_transformOption = _newOption;
        UpdateIcon();
    }

    /*
     * @brief Gets the current transform option
     * @return The current TransformOption
     */
    public TransformOption GetTransformOption()
    {
        return m_transformOption;
    }

    /*
     * @brief Checks if this slot is empty
     * @return True if the slot is empty, false otherwise
     */
    public bool IsEmpty()
    {
        return m_transformOption == null || m_transformOption.IsEmpty();
    }

    /*
     * @brief Called when this button is selected for replacement
     * Notifies the TransformWheelcontroller that this slot was chosen for replacement.
     * @return void
     */
    public void OnSlotSelectedForReplacement()
    {
        m_wheelController.OnSlotChosenForReplacement(this);
    }
}
