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

public class Bullet : NetworkBehaviour
{
    [Header("Balle")]
    [SerializeField] private float m_lifeTime = 3f;
    [SerializeField] private float m_currentLife;
    [SerializeField] private float m_speed = 10f; 

    [Header("Slime")]      
    [SerializeField] private float m_offsetFromSurface = 0.01f;
    [SerializeField] private GameObject m_slimePrefab;
    
    [SerializeField] float m_impactTimeBeforeDespawn = 1f;

    public bool m_amIServerSide = false; 

    void Start()
    {
        Destroy(gameObject, m_lifeTime);
    }

    void Update()
    {
        transform.position += m_speed * Time.deltaTime * transform.forward;
    }

    /**
    * @brief  This function allows you to show or hide a Decal and define for how long.
    * 
    * If the ball hits, The Slime ppears for the duration "timeSlimeWall" before disappearing.
    */
    private void OnTriggerEnter(Collider _other)
    {
        if (_other.transform.parent) 
        {
            if (_other.transform.parent.gameObject.layer == LayerMask.NameToLayer("Ghost"))
            {
                var ghost = _other.transform.parent.gameObject.GetComponent<GhostMorph>();
                if (ghost != null)
                {   
                    ghost.RevertToOriginal();
                }
            }
        }


        // We check if the collider is a ghost player by checking if it has the GhostController component
        GameObject gameobject = _other.gameObject;
        if (gameObject != null) {
            if (_other.CompareTag("Player") && gameobject.layer == LayerMask.NameToLayer("Child"))
            {
                return;
            }

            if (gameobject.layer == LayerMask.NameToLayer("Ghost"))
            {
                var ghost = gameobject.GetComponent<GhostController>();
                if (ghost != null)
                {
                    if (m_amIServerSide) // Only calling HitRanged on the server side
                    {
                        print("hit ranged");
                        ghost.HitRanged();
                    }
                }
            }
            else
            {
                SpawnSlimePrefab(_other);
            }
        }
        Destroy(gameObject);
    }

    /**
    * @brief  This code snippet allows you to instantiate a Slime Decal
    * 
    * A decal is instantiated at the point closest to the impact with an offset "m_offsetFromSurface"
    * 
    * @param  slime: The Decal instance
    *
    * @return Returns the instance
    */
    void SpawnSlimePrefab(Collider _target)
    {
        Vector3 spawnPos = _target.ClosestPoint(transform.position);
        spawnPos.z -= 0.3f;
        spawnPos.y += m_offsetFromSurface;
        SpawnForAll(spawnPos);
    }

    [ObserversRpc(runLocally:true)]
    void SpawnForAll(Vector3 _spawnPos)
    {
        GameObject slime = UnityProxy.InstantiateDirectly(m_slimePrefab, _spawnPos, Quaternion.Euler(0, 0, 1));
        Destroy(slime, m_impactTimeBeforeDespawn);
    }
}
