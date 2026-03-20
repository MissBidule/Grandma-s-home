using UnityEngine;
using UnityEngine.Events;

public class DiegeticButton : MonoBehaviour
{
    public UnityEvent OnClick;
    private Outline[] outlineEffects;

    private void Start()
    {
        // On trouve tous les contours sur l'objet et ses enfants
        outlineEffects = GetComponentsInChildren<Outline>();

        // On les éteint au démarrage
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
    }

    private void OnMouseEnter()
    {
        // On allume tout
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = true;
        }
    }

    private void OnMouseExit()
    {
        // On éteint tout
        foreach (Outline outline in outlineEffects)
        {
            outline.enabled = false;
        }
    }

    private void OnMouseDown()
    {
        // On déclenche le clic (changement de caméra, etc.)
        if (OnClick != null)
        {
            OnClick.Invoke();
        }
    }
}