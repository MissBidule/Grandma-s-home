
using UnityEngine;

public interface IInteractable
{
    public void OnFocus(GhostInteract _who);
    public void OnUnfocus(GhostInteract _who);
    public void OnInteract(GhostInteract _who);
    public void OnStopInteract(GhostInteract _who);
}