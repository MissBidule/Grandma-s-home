using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/*
 * @brief       Contains class declaration for TransformWheelcontroller
 * @details     The TransformWheelcontroller class manages the transformation wheel UI and handles input to open/close it.
 */
public class TransformWheelcontroller : MonoBehaviour
{
    public Animator m_anim;
    private bool m_transformWheelSelected = false;
    public Image m_selectedTransform;
    public Sprite m_noImage;
    public static int m_transformID;

    void Awake()
    {
        if (m_anim == null)
            m_anim = GetComponent<Animator>();
    }

    /* 
     * @brief Update triggers every frame
     * Checks for Tab key input to toggle the transformation wheel and logs the selected transformation option.
     * @return void
     */
    void Update()
    {
        // New Input System: read Tab key
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            bool open = !m_anim.GetBool("OpenTransformWheel");
            m_anim.SetBool("OpenTransformWheel", open);
        }
        switch (m_transformID)
        {
            case 0:
                m_selectedTransform.sprite = m_noImage;
                break;
            case 1:
                Debug.Log("Chaise/carré");
                break;
            case 2:
                Debug.Log("Armoire/Cylindre");
                break;
            case 3:
                Debug.Log("machin");
                break;
            case 4:
                Debug.Log("bidule");
                break;
            case 5:
                Debug.Log("chose");
                break;
            case 6:
                Debug.Log("objet");
                break;
            case 7:
                Debug.Log("item");
                break;
            case 8:
                Debug.Log("accessoire");
                break;
            case 9:
                Debug.Log("Amoir");
                break;

        }

    }
}
