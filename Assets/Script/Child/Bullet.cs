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

    
    //private static Dictionary<Collider, GameObject> m_slimeOnCollider = new Dictionary<Collider, GameObject>();

    protected override void OnSpawned()
    {
        //to init those var that keep bugging
        m_currentLife = m_lifeTime;
        base.OnSpawned();
    }

    void Awake()
    {
        //for the pool made by purrnet
        m_currentLife = m_lifeTime;
    }

    void Update()
    {
        transform.position += transform.forward * m_speed * Time.deltaTime;

        m_currentLife -= Time.deltaTime;
        if (m_currentLife <= 0f)
        {
            Debug.Log("dead");
            Destroy(gameObject);
        }
    }

    /**
    * @brief  This function allows you to show or hide a Decal and define for how long.
    * 
    * If the ball hits a Collider that has a "Wall" tag, The Slime Decal appears for the duration "timeSlimeWall" before disappearing., then the ball.
    */
    private void OnTriggerEnter(Collider _other)
    {
        float size = 1.5f;
        float timeSlimeWall = 1f;


        // We check if the collider is a ghost player by checking if it has the GhostMovement component
        GameObject gameobject = _other.gameObject;
        if (gameObject != null) {
            if (_other.CompareTag("Player") && gameobject.layer == LayerMask.NameToLayer("Child"))
            {
                return;
            }

            if (gameobject.layer == LayerMask.NameToLayer("Ghost"))
            {
                var ghost = gameobject.GetComponent<GhostStatus>();
                if (ghost != null)
                {   
                    if (isServer) ghost.GotHitByProjectile();
                }
            }
            //Use if needed, to check that we only have one slime
            //m_slimeOnCollider[_other] = slime;
            GameObject slime = SpawnSlimePrefab(_other, size);
            //prevents from skipping the hit on other clients
            Destroy(slime, timeSlimeWall);
        }
        gameObject.SetActive(false);
        Destroy(gameObject, timeSlimeWall);
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

        GameObject slime = Instantiate(m_slimePrefab, spawnPos, Quaternion.Euler(0, 0, 1));

        slime.transform.localScale = Vector3.one * _size;

        slime.transform.SetParent(_target.transform);
        return slime;
    }
}
