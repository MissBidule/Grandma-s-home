using UnityEngine;
using UnityEngine.Events;

public class DiegeticButton : MonoBehaviour
{
    public UnityEvent OnClick; //event Unity pour assigner des actions dans l'inspector
    private Outline[] outlineEffects; //stock les composants Outline pour les activer/desactiver au hover
    private Animator animator;

    private void Start()
    {
       
        outlineEffects = GetComponentsInChildren<Outline>();
        //desactive les outlines au debut pour ne pas les voir avant le hover
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
        animator = GetComponentInChildren<Animator>();

    }
    //active les outlines 
    private void OnMouseEnter()
    {
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = true;
        }
        if (animator != null)
        {
            animator.speed = 1;
            animator.SetBool("Hover", true);
        }
    }
    //desactive les outlines
    private void OnMouseExit()
    {
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
        if (animator != null)
        {
            animator.SetBool("Hover", false);
        }
    }
    //invoke l'event OnClick quand le bouton est clique
    private void OnMouseDown()
    {
        if (OnClick != null)
        {
            OnClick.Invoke();
        }
        if (animator != null)
        {
            animator.SetBool("Hover", false);
            animator.speed = 0;
        }
    }
}