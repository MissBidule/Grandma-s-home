/**
 * @brief  This function allows a Slime Decal to appear upon impact before the ball is destroyed.
 * 
 * When the player fires a ball, a Slime Decal appears at the point of impact under certain conditions (only one Slime Decal is allowed at a time).
 * 
 * @param  m_lifeTime:  lifespan of the ball before disappearance
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
    [SerializeField] private float m_speed = 10f; 

    [Header("Slime")]      
    [SerializeField] private float m_offsetFromSurface = 0.01f;
    [SerializeField] private GameObject m_slimePrefab;

    
    private static Dictionary<Collider, GameObject> m_slimeOnCollider = new Dictionary<Collider, GameObject>();

    protected override void OnSpawned()
    {
        base.OnSpawned();

        enabled = isOwner;
    }

    void Update()
    {
        transform.position += transform.forward * m_speed * Time.deltaTime;

        m_lifeTime -= Time.deltaTime;
        if (m_lifeTime <= 0f)
            Destroy(gameObject);
    }

    /**
    * @brief  This function allows you to show or hide a Decal and define for how long.
    * 
    * If the ball hits a Collider that has a "Wall" tag, The Slime Decal appears for the duration "timeSlimeWall" before disappearing., then the ball.
    * If the ball hits an existing Collider that already contains a Decal, then the ball is destroyed and nothing else happens.
    * Otherwise, a Slime Decal of size "size" appears at the point of impact (using the "SpawnSlimePrefab" function), it is stored in the "m_slimeOnCollider" dictionary, and the ball is destroyed.
    * 
    * @param  size:  size of the Decal created
    * @param  timeSlimeWall:  the duration of slime appearance on the walls
    */

    private void OnTriggerEnter(Collider _other)
    {
        float size = 1.5f;
        float timeSlimeWall = 1f;


        // We check if the collider is a ghost player by checking if it has the GhostMovement component
        GameObject gameobject = _other.gameObject;
        if (gameObject != null) {
            var ghost = gameobject.GetComponent<GhostController>();
            if (ghost != null)
            {
                GameObject slime2 = SpawnSlimePrefab(_other, size);
                Destroy(slime2, timeSlimeWall);
                ghost.GotHitByProjectile();
                Destroy(gameObject);
                return;
            }
        }

        /*if (_other.CompareTag("Wall"))
        {
            GameObject slime2 = SpawnSlimePrefab(_other, size);
            Destroy(slime2, timeSlimeWall);
            Destroy(gameObject);
            return;
        }*/
        if (_other.CompareTag("Slime"))
        {
            return;
        }
        if (m_slimeOnCollider.ContainsKey(_other) && m_slimeOnCollider[_other] != null)
        {
            Destroy(gameObject);
            return;
        }
        
        GameObject slime = SpawnSlimePrefab(_other, size);
        Destroy(slime, timeSlimeWall);


        m_slimeOnCollider[_other] = slime;

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

    GameObject SpawnSlimePrefab(Collider _target, float _size)
    {
        Vector3 spawnPos = _target.ClosestPoint(transform.position);

        spawnPos.z -= 0.3f;
        spawnPos.y += m_offsetFromSurface;

        GameObject slime = Instantiate(m_slimePrefab, spawnPos, Quaternion.identity);

        slime.transform.localScale = Vector3.one * _size;

        slime.transform.SetParent(_target.transform);
        return slime;
    }

}
