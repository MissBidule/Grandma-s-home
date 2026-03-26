
using UnityEngine;

public interface TutoIInteractable
{
    public void OnFocus(TutoInteract who);
    public void OnUnfocus(TutoInteract who);
    public void OnInteract(TutoInteract who);
    public void OnStopInteract(TutoInteract who);
}