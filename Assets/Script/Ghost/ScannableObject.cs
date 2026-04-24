using System;
using PurrNet;
using UnityEngine;
using UnityEngine.Serialization;

/*
 * @brief Contains class declaration for ScannableObject
 * @details The ScannableObject class marks an object as scannable and provide an icon.
 */
public class ScannableObject : NetworkBehaviour
{
    [FormerlySerializedAs("m_icon")]
    [SerializeField] private Sprite m_iconAsset;

    [NonSerialized] public bool m_isScannable = true;

    public Sprite m_icon => m_isScannable ? m_iconAsset : null;
}
