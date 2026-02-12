using PurrNet;
using UnityEngine;

/*
 * @brief Contains class declaration for ScannableObject
 * @details The ScannableObject class marks an object as scannable and provide an icon.
 */
public class ScannableObject : NetworkBehaviour
{
    [Tooltip("Icon to display in the transformation wheel (optional)")]
    public Sprite m_icon;
}
