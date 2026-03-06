
using UnityEngine;

public interface IInteractable
{
    public void OnFocus(Interact who);
    public void OnUnfocus(Interact who);
    public void OnInteract(Interact who);
    public void OnStopInteract(Interact who);
}