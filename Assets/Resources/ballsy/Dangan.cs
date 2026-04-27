/**
 * @brief  This function allows a Slime Decal to appear upon impact before the ball is destroyed.
 * 
 * When the player fires a ball, a Slime Decal appears at the point of impact under certain conditions (only one Slime Decal is allowed at a time).
 * 
 * @param  m_lifeTime:  lifespan of the ball before disappearance
 * @param  m_currentLife:  current life left
 * @param  m_speed:  ball speed
 * @param  m_offsetFromSurface:  offset added
 * @param  m_slimePrefab:  Decal Prefab
 * @param  m_slimeOnCollider:  A dictionary that stores whether a collider already contains a Decal
 */
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using PurrNet;

public class Dangan : Bullet
{
    [Header("Sound")]
    public AudioClip m_audioClip = null;
}
