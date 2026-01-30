using UnityEngine;

/**
@brief       Contrôleur d'interaction du joueur
@details     La classe \c PlayerInteractionController sélectionne l'interactible le plus proche autorisé,
             affiche le prompt et déclenche l'interaction.
*/
public class PlayerInteractionController : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float m_radius = 2.0f;
    [SerializeField] private LayerMask m_interactableMask;
    [SerializeField] private Transform m_origin;

    [Header("Input")]
    [SerializeField] private KeyCode m_interactKey = KeyCode.E;

    private PlayerBehavior m_playerBehavior;
    private PlayerInteractable m_current;

    private void Awake()
    {
        m_playerBehavior = GetComponent<PlayerBehavior>();
        if (m_origin == null) m_origin = transform;
    }

    private void Update()
    {
        UpdateTarget();

        if (Input.GetKeyDown(m_interactKey))
            TryInteract();
    }

    /**
    @brief      Met à jour la cible : prend l'interactible autorisé le plus proche
    @return     void
    */
    private void UpdateTarget()
    {
        if (m_playerBehavior == null) return;

        PlayerType playerType = m_playerBehavior.m_playerType;

        Collider[] hits = Physics.OverlapSphere(m_origin.position, m_radius, m_interactableMask);

        PlayerInteractable best = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerInteractable interactable = hits[i].GetComponentInParent<PlayerInteractable>();
            if (interactable == null) continue;

            if (!interactable.CanInteract(playerType)) continue;

            Vector3 closest = hits[i].ClosestPoint(m_origin.position);
            float sqrDistance = (closest - m_origin.position).sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = interactable;
            }
        }

        if (best == m_current) return;

        // Unfocus ancien
        if (m_current != null)
            m_current.OnUnfocus(playerType);

        m_current = best;

        // Focus nouveau
        if (m_current != null)
            m_current.OnFocus(playerType);
        else if (InteractPromptUI.Instance != null)
            InteractPromptUI.Instance.Hide();
    }

    /**
    @brief      Déclenche l'interaction sur la cible courante
    @return     void
    */
    private void TryInteract()
    {
        if (m_playerBehavior == null) return;
        if (m_current == null) return;

        PlayerType playerType = m_playerBehavior.m_playerType;

        if (!m_current.CanInteract(playerType))
            return;

        m_current.Interact(transform, playerType);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = m_origin != null ? m_origin : transform;
        Gizmos.DrawWireSphere(origin.position, m_radius);
    }
}
