using UnityEngine;
using UnityEngine.Events;

// Ce script demande à Unity d'ajouter automatiquement l'effet de contour
// si l'objet n'en a pas déjà un. Pratique !
[RequireComponent(typeof(Outline))]
public class DiegeticButton : MonoBehaviour
{
    [Header("Ce qui se passe quand on clique")]
    public UnityEvent OnClick;

    // Référence secrète vers le composant Outline
    private Outline outlineEffect;

    private void Start()
    {
        // Au lancement, le script va chercher le composant Outline sur l'objet
        outlineEffect = GetComponent<Outline>();

        // Et on s'assure qu'il est éteint au départ
        outlineEffect.enabled = false;
    }

    // --- Gestion du Survol (Hover) ---

    private void OnMouseEnter()
    {
        // La souris entre : on allume le contour blanc
        if (outlineEffect != null)
        {
            outlineEffect.enabled = true;
        }
    }

    private void OnMouseExit()
    {
        // La souris part : on éteint le contour blanc
        if (outlineEffect != null)
        {
            outlineEffect.enabled = false;
        }
    }

    // --- Gestion du Clic ---

    private void OnMouseDown()
    {
        // On clique, ça déclenche l'événement Unity (pour changer de caméra)
        OnClick.Invoke();
    }
}