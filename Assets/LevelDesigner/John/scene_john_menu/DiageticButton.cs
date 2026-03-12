using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems; // 1. AJOUTE CETTE LIGNE EN HAUT !

public class DiegeticButton : MonoBehaviour
{
    public UnityEvent OnClick;
    private Outline[] outlineEffects;

    private void Start()
    {
        outlineEffects = GetComponentsInChildren<Outline>();
        foreach (Outline outline in outlineEffects) outline.enabled = false;
    }

    private void OnMouseEnter()
    {
        // 2. LA LIGNE MAGIQUE : Si on survole l'UI 2D, on ne fait rien !
        if (EventSystem.current.IsPointerOverGameObject()) return;

        foreach (Outline outline in outlineEffects) outline.enabled = true;
    }

    private void OnMouseExit()
    {
        foreach (Outline outline in outlineEffects) outline.enabled = false;
    }

    private void OnMouseDown()
    {
        // 3. LA MÊME LIGNE MAGIQUE : Si on clique sur l'UI 2D, le bouton 3D l'ignore !
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (OnClick != null) OnClick.Invoke();
    }
}