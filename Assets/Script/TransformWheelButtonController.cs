using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * @brief       Contains class declaration for TransformWheelButtonController
 * @details     The TransformWheelButtonController class manages the behavior of buttons in the transformation wheel UI.
 */
public class TransformWheelButtonController : MonoBehaviour
{
    public int m_ID;
    private Animator m_anim;
    public string m_transformName;
    public TextMeshProUGUI m_transformText;
    public Image m_selectedTransform;
    private bool m_selected = false;
    public Sprite m_icon;


    void Start()
    {
        m_anim = GetComponent<Animator>();
    }

    
    void Update()
    {
        if(m_selected)
        {
            m_selectedTransform.sprite = m_icon;
        }

    }

    /* 
     * @brief Selects this transformation option.
     * Sets the selected flag to true and updates the TransformWheelcontroller's transformID.
     * @return void
     */
    public void Select()
    {
        m_selected = true;
        TransformWheelcontroller.m_transformID = m_ID;
    }

    /* 
     * @brief Deselects this transformation option.
     * Sets the selected flag to false and resets the TransformWheelcontroller's transformID.
     * @return void
     */
    public void Deselect()
    {
        m_selected = false;
        TransformWheelcontroller.m_transformID = 0;
    }

    /* 
     * @brief Handles hover enter event.
     * Sets the hover animation state and updates the transform text.
     * @return void
     */
    public void HoverEnter()
    {
        m_anim.SetBool("Hover", true);
        m_transformText.text = m_transformName;
    }

    /* 
     * @brief Handles hover exit event.
     * Resets the hover animation state and clears the transform text.
     * @return void
     */
    public void HoverExit()
    {
        m_anim.SetBool("Hover", false);
        m_transformText.text = "";
    }
}
