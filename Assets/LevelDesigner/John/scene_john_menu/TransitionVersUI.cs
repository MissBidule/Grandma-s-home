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

    // C'est cette fonction qu'on mettra sur le bouton de la télé
    public void LancerLaTransition()
    {
        // 1. On lance le mouvement de caméra 
        if (navigator != null && camToZoom != null)
        {
            navigator.SwitchToCamera(camToZoom);
        }

        // 2. On attend que la caméra arrive, puis on allume l'UI
        StartCoroutine(AfficherMenuApresDelai());
    }

    private IEnumerator AfficherMenuApresDelai()
    {
        // On attend (le même temps que la transition Cinemachine)
        yield return new WaitForSeconds(delaiAffichage);

        // On allume le Canvas !
        if (canvasToLaunch != null)
        {
            canvasToLaunch.SetActive(true);
        }
    }
}