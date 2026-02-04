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

    private GameObject m_current;

    private void Update()
    {
        CheckClosest();
    }

    /*
    @brief      Met à jour la cible : prend l'interactible autorisé le plus proche
    @return     void
    */
    private void CheckClosest()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, m_radius, m_interactableMask);
        GameObject best = null;
        float bestSqrDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject interactable = hits[i].gameObject;
            if (interactable == null) continue;
            if (interactable.GetComponent<SabotageObject>() != null || interactable.GetComponent<GhostController>() != null && interactable.GetComponent<GhostController>().m_isStopped == true)
            {
                Vector3 closest = hits[i].ClosestPoint(transform.position);
                float sqrDistance = (closest - transform.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = interactable;
                }
            }
        }
        if (best == m_current) return;
        /*
        // Unfocus ancien
        if (m_current != null)
            //m_current.OnUnfocus(playerType);

          

        // Focus nouveau
        if (m_current != null)
            //m_current.OnFocus(playerType);
        */
        m_current = best;
    }

    /*
     * @brief Interact with the current target if available
     * @return void
     */
    public void Interact()
    {
        print("GhostInteract: Interact called");
        if (m_current == null) return;
        if (m_current.GetComponent<GhostController>() != null)
        {
            GhostController ghost = m_current.GetComponent<GhostController>();
            if (ghost.m_isStopped == true)
            {
                ghost.m_isStopped = false;
                return;
            }
        }
        else if (m_current.GetComponent<SabotageObject>() != null)
        {
            SabotageObject sabotageObject = m_current.GetComponent<SabotageObject>();
            sabotageObject.StartQte();
        }
    }
}
