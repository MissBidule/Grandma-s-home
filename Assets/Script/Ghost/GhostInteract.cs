using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * @brief  Contains class declaration for GhostInteract
 * @details The GhostInteract class handles interactions with sabotageable objects and downed ghosts for the Ghost player.
 */
public class GhostInteract : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float m_radius = 2.0f;
    [SerializeField] private LayerMask m_interactableMask;
    private SabotageObject m_focusedObject;
    private GhostController m_focusedGhost;
    private List<GameObject> m_colliders = new List<GameObject>();

    private void Update()
    {
        if (m_colliders.Count > 0)
        {
            GameObject closest = CheckClosest();
            if (closest != null)
            {
                var parent = closest.transform.parent;
                if (parent.GetComponent<SabotageObject>())
                {
                    SabotageObject resultObject = parent.GetComponent<SabotageObject>();

                    if (m_focusedObject != null && m_focusedObject != resultObject)
                    {
                        m_focusedObject.OnUnfocus();
                    }
                    resultObject.OnFocus();


                    m_focusedGhost = null;
                    m_focusedObject = resultObject;
                }
                else if (closest.GetComponent<GhostController>())
                {
                    GhostController resultGhost = parent.GetComponent<GhostController>();

                    if (m_focusedObject != null)
                    {
                        m_focusedObject.OnUnfocus();
                    }

                    m_focusedGhost = resultGhost;
                    m_focusedObject = null;
                }
                else
                {
                    Debug.Log("Error: Unknown Interactable");
                }
            }
        }
    }

    /*
    @brief      Check closest interactable object
    */
    private GameObject CheckClosest()
    {
        GameObject best = null;
        float bestSqrDistance = float.MaxValue;
        for (int i = 0; i < m_colliders.Count; i++)
        {
            GameObject interactable = m_colliders[i];
            if (interactable == null) continue;
            if (interactable.transform.parent.GetComponent<SabotageObject>() != null || interactable.gameObject.transform.GetComponent<GhostController>() != null && interactable.gameObject.transform.GetComponent<GhostController>().m_isStopped == true)
            {
                Vector3 closest = interactable.transform.position;
                float sqrDistance = (closest - transform.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = interactable;
                }
            }
        }
        return best;
    }

    /*
     * @brief Interact with the current target if available
     * @return void
     */
    public void Interact()
    {
        if (m_focusedGhost)
        {
            GhostController ghost = m_focusedGhost;
            if (ghost.m_isStopped == true)
            {
                ghost.m_isStopped = false;
                return;
            }
        }else if (m_focusedObject)
        {
            SabotageObject sabotageObject = m_focusedObject;
            sabotageObject.StartQte();
        }
    }

    /*
     * @brief OnTriggerEnter is called when another collider enters the trigger
     * @param _other: The other Collider that entered.
     * @return void
     */
    void OnTriggerEnter(Collider _other)
    {
        if (_other.gameObject.layer == 3 || _other.gameObject.layer == 9) // 9 = Ghost Layer 3 = Control
            m_colliders.Add(_other.gameObject);
    }

    /*
     * @brief OnTriggerExit is called when another collider exits the trigger
     * @param _other: The other Collider that exited.
     * @return void
     */
    void OnTriggerExit(Collider _other)
    {
        if (_other.gameObject.layer == 3 || _other.gameObject.layer == 9) // 9 = Ghost Layer 3 = Control
        {
            m_colliders.Remove(_other.gameObject);

            Transform parent = _other.gameObject.transform.parent;
            if (parent.GetComponent<SabotageObject>())
            {
                SabotageObject so = parent.GetComponent<SabotageObject>();
                if (so == m_focusedObject)
                {
                    m_focusedObject.OnUnfocus();
                    m_focusedObject = null;
                }
            }
            else
            {
                if (_other.gameObject.GetComponent<GhostController>() == m_focusedGhost)
                {
                    m_focusedGhost = null;
                }
            }
        }
    }
}
