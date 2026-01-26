/**
 * @brief  This script allows you to fire a bullet at a speed
 * 
 * When the player clicks the left mouse button "Fire1" in (Edit/Project Settings/Input Manager/Fire1), it calls the Shoot() function.
 * 
 * @param  m_bulletSpawnTransform:  Bullet spawn point (it should not be too close to the weapon's collider)
 * @param  m_bulletPrefab:  Ball prefab (Ball's Rigidbody's "Use gravity" setting must be unchecked)
 */
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{

    [Header("Initial Setup")]
    [SerializeField] private Transform m_bulletSpawnTransform;
    [SerializeField] private GameObject m_bulletPrefab;

    private void Update()
    {
        
            if(Input.GetButtonDown("Fire1"))
            {
                Shoot();
            }

        
    }

/**
 * @brief  This function instantiates a ball prefab
 * 
 * We instantaneously transfer the ball and put the force into impulse mode.
 */

    void Shoot()
    {
        GameObject bullet = Instantiate(m_bulletPrefab, m_bulletSpawnTransform.position, Quaternion.identity, GameObject.FindGameObjectWithTag("WorldObjectHolder").transform);
        bullet.GetComponent<Rigidbody>().AddForce(m_bulletSpawnTransform.forward, ForceMode.Impulse);
    }

}
