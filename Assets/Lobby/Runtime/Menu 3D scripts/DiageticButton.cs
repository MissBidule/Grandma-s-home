using UnityEngine;
using UnityEngine.Events;

public class DiegeticButton : MonoBehaviour
{
    public UnityEvent OnClick; //event Unity pour assigner des actions dans l'inspector
    private Outline[] outlineEffects; //stock les composants Outline pour les activer/desactiver au hover
    public bool outlineAlwaysOn = false; //permet de garder les outlines actifs même sans hover 
    public Animator animator; //reference à l'animator pour jouer les animations du bouton

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
        if (!enabled) return;
        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool("Hover", true);
        }
        if (outlineAlwaysOn)
            return;
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = true;
        }
    }
    //desactive les outlines
    private void OnMouseExit()
    {
        if (animator != null)
        {
            animator.SetBool("Hover", false);
        }
        if (outlineAlwaysOn)
            return;
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
    }
    //invoke l'event OnClick quand le bouton est clique
    private void OnMouseDown()
    {
        if (!enabled) return;
        if (OnClick != null)
        {
            OnClick.Invoke();
        }
    }
}