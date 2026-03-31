using UnityEngine;
using UnityEngine.Events;

public class DiegeticButton : MonoBehaviour
{
    public UnityEvent OnClick; //event Unity pour assigner des actions dans l'inspector
    private Outline[] outlineEffects; //stock les composants Outline pour les activer/désactiver au hover

    private void Start()
    {
       
        outlineEffects = GetComponentsInChildren<Outline>();
        //désactive les outlines au début pour ne pas les voir avant le hover
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
    }
    //active les outlines 
    private void OnMouseEnter()
    {
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = true;
        }
    }
    //désactive les outlines
    private void OnMouseExit()
    {
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
    }
    //invoke l'event OnClick quand le bouton est cliqué
    private void OnMouseDown()
    {
        if (OnClick != null)
        {
            OnClick.Invoke();
        }
    }
}