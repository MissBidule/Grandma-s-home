using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class TransitionVersUI : MonoBehaviour
{
    [Header("Les éléments à relier")]
    public SceneMenuNavigator navigator; // Pour bouger la caméra
    public CinemachineVirtualCameraBase camTV; // La caméra gros plan
    public GameObject canvasLobby; // Ton menu 2D

    [Header("Réglages")]
    public float delaiAffichage = 1.5f; // Le temps que met la caméra pour zoomer

    // C'est cette fonction qu'on mettra sur le bouton de la télé
    public void LancerLaTransition()
    {
        // 1. On lance le mouvement de caméra vers la télé
        if (navigator != null && camTV != null)
        {
            navigator.SwitchToCamera(camTV);
        }

        // 2. On attend que la caméra arrive, puis on allume l'UI
        StartCoroutine(AfficherMenuApresDelai());
    }

    private IEnumerator AfficherMenuApresDelai()
    {
        // On attend (le même temps que la transition Cinemachine)
        yield return new WaitForSeconds(delaiAffichage);

        // On allume le Canvas !
        if (canvasLobby != null)
        {
            canvasLobby.SetActive(true);
        }
    }
}