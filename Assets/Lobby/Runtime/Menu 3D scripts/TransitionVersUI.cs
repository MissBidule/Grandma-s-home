using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class TransitionVersUI : MonoBehaviour
{
    [Header("Transition")]
    public SceneMenuNavigator navigator; //ref du Menu Manager
    public CinemachineVirtualCameraBase camToZoom; //ref cam pour transition
    public GameObject canvasToLaunch; //ref pour l'UI a afficher

    [Header("Reglages")]
    public float delaiAffichage = 1.5f; //delai avant d'afficher l'UI apres le switch de cam

    [Header("Settings canvas (optional)")]
    public SettingsTab tabToOpen = SettingsTab.None; //si set, ouvre l'onglet correspondant sur le SettingsCanvasController

    //zoom cam puis affichage UI
    public void LancerLaTransition()
    {
        if (navigator != null && camToZoom != null)
        {
            navigator.SwitchToCamera(camToZoom);
        }
        StartCoroutine(AfficherMenuApresDelai());
    }
    //coroutine pour afficher l'UI apres un delai pour fluidifier la transition
    private IEnumerator AfficherMenuApresDelai()
    {
        yield return new WaitForSeconds(delaiAffichage);

        if (canvasToLaunch != null)
        {
            canvasToLaunch.SetActive(true);
            if (tabToOpen != SettingsTab.None)
                canvasToLaunch.BroadcastMessage("OpenOnTabInt", (int)tabToOpen, SendMessageOptions.DontRequireReceiver);
        }
    }

    //Pour les buttons des UI pour switch a une cam
    public void FermerMenuEtSwitch(CinemachineVirtualCameraBase cam)
    {
        if (canvasToLaunch != null)
        {
            canvasToLaunch.SetActive(false);
        }
        if (navigator != null)
        {
            navigator.SwitchToCamera(cam);
        }
    }
}