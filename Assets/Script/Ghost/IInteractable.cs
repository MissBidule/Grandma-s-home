
using UnityEngine;

public interface IInteractable
{
    public void OnFocus(GhostInteract who);
    public void OnUnfocus(GhostInteract who);
    public void OnInteract(GhostInteract who);
    public void OnStopInteract(GhostInteract who);
}