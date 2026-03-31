
using UnityEngine;

public interface DTutoIInteractable
{
    public void OnFocus(DTutoInteract who);
    public void OnUnfocus(DTutoInteract who);
    public void OnInteract(DTutoInteract who);
    public void OnStopInteract(DTutoInteract who);
}