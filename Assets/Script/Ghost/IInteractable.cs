
using UnityEngine;

public interface IInteractable
{
    public void OnFocus();
    public void OnUnfocus();
    public void OnInteract(GhostInteract who);
    public void OnStopInteract(GhostInteract who);
}