using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class TransitionVersUI : MonoBehaviour
{
    [Header("Les éléments à relier")]
    public SceneMenuNavigator navigator; // Pour bouger la caméra
    public CinemachineVirtualCameraBase camToZoom; // La caméra gros plan
    public GameObject canvasToLaunch; // Ton menu 2D

    [Header("Réglages")]
    public float delaiAffichage = 1.5f; // Le temps que met la caméra pour zoomer

    // --- FONCTION POUR L'ALLER (Quand on clique sur l'objet 3D) ---
    public void LancerLaTransition()
    {
        if (navigator != null && camToZoom != null)
        {
            navigator.SwitchToCamera(camToZoom);
        }
        StartCoroutine(AfficherMenuApresDelai());
    }

    private IEnumerator AfficherMenuApresDelai()
    {
        yield return new WaitForSeconds(delaiAffichage);

        if (canvasToLaunch != null)
        {
            canvasToLaunch.SetActive(true);
        }
    }

    // --- NOUVELLE FONCTION POUR LE RETOUR (Quand on clique sur le bouton Exit 2D) ---
    public void FermerMenuEtRetourner(CinemachineVirtualCameraBase cam)
    {
        // 1. On éteint l'interface 2D instantanément
        if (canvasToLaunch != null)
        {
            canvasToLaunch.SetActive(false);
        }

        // 2. On utilise ton SceneMenuNavigator pour dire "Retourne à la caméra principale"
        if (navigator != null)
        {
            navigator.SwitchToCamera(cam);
        }
    }
}