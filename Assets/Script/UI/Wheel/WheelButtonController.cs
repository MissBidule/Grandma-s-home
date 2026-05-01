using UnityEngine;
using UnityEngine.UI;

/*
 * @brief Contains class declaration for WheelButtonController
 * @details The WheelButtonController class manages the behavior of buttons in the transformation wheel UI, using TransformOption for data.
 */
public class WheelButtonController : MonoBehaviour
{
    [SerializeField] private TransformOption m_transformOption;
    private Image m_iconImage;
    private Button m_button;
    private Vector3 m_originalScale;
    private AudioClip m_buttonClickSound;

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
        m_originalScale = transform.localScale;
        m_buttonClickSound = Resources.Load<AudioClip>("Audio/MenuButtonSFX");

        m_wheelController = GetComponentInParent<WheelController>();

        m_button.onClick.AddListener(PlayButtonClickSound);

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

    }

    /*
     * @brief Selects this transformation option
     * Selects the prefab in the WheelController.
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
     * @brief Sets the visual highlight state of this button using its Color Tint colors.
     * Empty slots are never highlighted.
     * @param active: True to apply highlighted color, false to restore normal color.
     * @return void
     */
    public void SetHighlight(bool active)
    {
        ColorBlock colors = m_button.colors;
        Color target;
        if (active)
            target = colors.highlightedColor;
        else if (IsEmpty())
            target = colors.disabledColor;
        else
            target = colors.normalColor;
        m_button.targetGraphic.CrossFadeColor(target, colors.fadeDuration, true, true);

        transform.localScale = active ? m_originalScale * 1.05f : m_originalScale;
    }

    /*
     * @brief Called when this button is selected for replacement
     * Notifies the WheelController that this slot was chosen for replacement.
     * @return void
     */
    public void OnSlotSelectedForReplacement()
    {
        m_wheelController.OnSlotChosenForReplacement(this);
    }

    private void PlayButtonClickSound()
    {
        if (m_buttonClickSound == null) return;
        GameObject go = new GameObject("TempAudioSource");
        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.clip = m_buttonClickSound;
        audioSource.volume = 0.5f;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
        Destroy(go, m_buttonClickSound.length);
    }
}
