/**
 * @brief  Cette fonction permet de faire apparaitre un Decal de Slime à l'impacte avant de détruire la balle 
 * 
 * Quand le player tire une balle un Decal de Slime apparait au point d'impacte selon des conditions (un seul Decal de slime est autorisé à la fois)
 * 
 * @param  m_lifeTime:  temps de vie de la balle avant disparision
 * @param  m_speed:  vitesse de la balle
 * @param  m_offsetFromSurface:  offset rajouté
 * @param  m_slimePrefab:  Prefab du Decal
 * @param  m_slimeOnCollider:  Un disctionnaire qui stock si un collider contient déja un Decal
 */
using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    [Header("Balle")]
    [SerializeField] private float m_lifeTime = 3f;
    [SerializeField] private float m_speed = 10f; 

    [Header("Slime")]      
    [SerializeField] private float m_offsetFromSurface = 0.01f;
    [SerializeField] private GameObject m_slimePrefab;

    
    private static Dictionary<Collider, GameObject> m_slimeOnCollider = new Dictionary<Collider, GameObject>();

    void Update()
    {
        
        transform.position += transform.forward * m_speed * Time.deltaTime;

        m_lifeTime -= Time.deltaTime;
        if (m_lifeTime <= 0f)
            Destroy(gameObject);
    }

    /**
    * @brief  Cette fonction permet de faire ou non apparaitre un Decal et de définir pour combien de temps
    * 
    * si la balle touche un Collider qui a un Tag "Wall" alors la balle se détruit et rien d'autre ne se passe
    * si la balle touche un Collider qui existe et qui contient déjà un Decal alors  la balle se détruit et rien d'autre ne se passe
    * sinon un Decal de Slime de taille "size" apparait au point d'impacte (appele fonction "SpawnSlimePrefab"), il est enregistré dans le dictionnaire "m_slimeOnCollider" et la balle est détruite
    * 
    * @param  size:  taille du Decal crée
    */

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        // Si on a déjà un slime sur ce collider ET qu'il existe encore -> on ne fait rien
        if (m_slimeOnCollider.ContainsKey(_other) && m_slimeOnCollider[_other] != null)
        {
            Destroy(gameObject);
            return;
        }

        // Sinon on peut en poser un nouveau
        float size = 1.5f;

        GameObject slime = SpawnSlimePrefab(_other, size);

        // On enregistre le slime posé sur ce collider
        m_slimeOnCollider[_other] = slime;

        Destroy(gameObject);
    }


    /**
    * @brief  Cette partie de code permet d'instantier un Decal de Slime
    * 
    * Un Decal est instantier au point le plus proche de l'impacte avec un offset "m_offsetFromSurface"
    * 
    * @param  slime: L'instance du Decal
    *
    * @return Renvoie l'instance
    */

    GameObject SpawnSlimePrefab(Collider target, float size)
    {
        Vector3 spawnPos = target.ClosestPoint(transform.position);

        spawnPos.z -= 0.3f;
        spawnPos.y += m_offsetFromSurface;

        GameObject slime = Instantiate(m_slimePrefab, spawnPos, Quaternion.identity);

        slime.transform.localScale = Vector3.one * size;

        slime.transform.SetParent(target.transform);
        return slime;
    }

}
