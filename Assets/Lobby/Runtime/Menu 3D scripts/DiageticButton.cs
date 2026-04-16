using UnityEngine;
using UnityEngine.Events;

public class DiegeticButton : MonoBehaviour
{
    public UnityEvent OnClick; //event Unity pour assigner des actions dans l'inspector
    private Outline[] outlineEffects; //stock les composants Outline pour les activer/desactiver au hover
    public bool outlineAlwaysOn = false; //permet de garder les outlines actifs même sans hover 

    private void Start()
    {
       
        outlineEffects = GetComponentsInChildren<Outline>();
        //desactive les outlines au debut pour ne pas les voir avant le hover
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
    }
    //active les outlines 
    private void OnMouseEnter()
    {
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
        if (OnClick != null)
        {
            OnClick.Invoke();
        }
    }
}