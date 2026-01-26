/**
 * @brief  A Script that allows you to clean the slime
 * 
 * When the player is close to a distance of m_range and there is a gameObject with the tag "Slime", they click on m_key and destroy the gameObject.
 * 
 */
using UnityEngine;

public class SlimeCleaner : MonoBehaviour
{
    private float m_range = 2f;
    private KeyCode m_key = KeyCode.E;

    void Update()
    {
        if(Input.GetKeyDown(m_key))
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, m_range);

            foreach (Collider col in hits)
            {
                if(col.CompareTag("Slime"))
                {
                    Destroy(col.gameObject);
                    break;
                }
            }
        }
    }
}
