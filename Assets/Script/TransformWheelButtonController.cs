using UnityEngine;
using UnityEngine.UI;

/*
 * @brief Contains class declaration for TransformWheelButtonController
 * @details The TransformWheelButtonController class manages the behavior of buttons in the transformation wheel UI, using TransformOption for data.
 */
public class TransformWheelButtonController : MonoBehaviour
{
    [SerializeField] private TransformOption m_transformOption;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Finds the icon transform and sets its sprite from the TransformOption.
     * @return void
     */
    void Awake()
    {
        Transform iconTransform = transform.Find("icone");
        Image iconImage = iconTransform.GetComponent<Image>();
        iconImage.sprite = m_transformOption.icon;
    }

    /*
     * @brief Selects this transformation option
     * Selects the prefab in the TransformWheelcontroller.
     * @return void
     */
    public void Select()
    {
        if (m_transformOption.prefab == null)
        {
            TransformWheelcontroller.m_Instance.ClearSelection();
            return;
        }
        TransformWheelcontroller.m_Instance.SelectPrefab(m_transformOption.prefab);
    }
}
