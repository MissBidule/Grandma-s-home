using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class SceneMenuNavigator : MonoBehaviour
{
    [Header("Caméras d'Introduction (La Porte)")]
    public CinemachineVirtualCameraBase startCam;      // La TOUTE PREMIÈRE caméra (Dehors)
    public CinemachineVirtualCameraBase start1Cam;     // Devant la porte
    public CinemachineVirtualCameraBase main1Cam;      // Dedans (dans le noir)
    public CinemachineVirtualCameraBase mainAutoCam;   // Menu final auto

    [Header("Caméras des Menus")]
    public CinemachineVirtualCameraBase mainCam;
    public CinemachineVirtualCameraBase optionsCam;
    public CinemachineVirtualCameraBase playCam;
    public CinemachineVirtualCameraBase quitCam;
    public CinemachineVirtualCameraBase TVCam;
    public CinemachineVirtualCameraBase PosterCam;
    public CinemachineVirtualCameraBase SkinsCam;

    [Header("Réglages de Transition")]
    public float delaiCamera = 1.5f;

    [Header("Les Colliders des Boutons")]
    // (J'ai ajouté un groupe pour les boutons de démarrage dehors !)
    public Collider[] boutonsStart;
    public Collider[] boutonsMainMenu;
    public Collider[] boutonsOptions;
    public Collider[] boutonsPlay;
    public Collider[] boutonsSkins;

    private Coroutine transitionEnCours;

    private void Start()
    {
        // 🎯 LA CORRECTION EST ICI : On commence dehors !
        SwitchToCamera(startCam);
    }

    public void SwitchToCamera(CinemachineVirtualCameraBase targetCamera)
    {
        if (transitionEnCours != null)
        {
            StopCoroutine(transitionEnCours);
        }

        // 1. ON REMET ABSOLUMENT TOUTES LES CAMÉRAS À 10 (Le grand ménage)
        if (startCam != null) startCam.Priority = 10;
        if (start1Cam != null) start1Cam.Priority = 10;
        if (main1Cam != null) main1Cam.Priority = 10;
        if (mainAutoCam != null) mainAutoCam.Priority = 10;

        if (mainCam != null) mainCam.Priority = 10;
        if (optionsCam != null) optionsCam.Priority = 10;
        if (playCam != null) playCam.Priority = 10;
        if (quitCam != null) quitCam.Priority = 10;
        if (TVCam != null) TVCam.Priority = 10;
        if (PosterCam != null) PosterCam.Priority = 10;
        if (SkinsCam != null) SkinsCam.Priority = 10;

        // 2. On allume uniquement la caméra ciblée à 20
        if (targetCamera != null) targetCamera.Priority = 20;

        // 3. On lance les boutons
        transitionEnCours = StartCoroutine(GererBoutonsAvecDelai(targetCamera));
    }

    private IEnumerator GererBoutonsAvecDelai(CinemachineVirtualCameraBase targetCamera)
    {
        // ÉTAPE 1 : On désactive TOUS les boutons
        ActiverGroupeBoutons(boutonsStart, false);
        ActiverGroupeBoutons(boutonsMainMenu, false);
        ActiverGroupeBoutons(boutonsOptions, false);
        ActiverGroupeBoutons(boutonsPlay, false);
        ActiverGroupeBoutons(boutonsSkins, false);

        // ÉTAPE 2 : Pause
        yield return new WaitForSeconds(delaiCamera);

        // ÉTAPE 3 : On réactive les boutons selon la pièce
        if (targetCamera == startCam) ActiverGroupeBoutons(boutonsStart, true);
        else if (targetCamera == mainCam || targetCamera == mainAutoCam) ActiverGroupeBoutons(boutonsMainMenu, true);
        else if (targetCamera == optionsCam) ActiverGroupeBoutons(boutonsOptions, true);
        else if (targetCamera == playCam) ActiverGroupeBoutons(boutonsPlay, true);
        else if (targetCamera == SkinsCam) ActiverGroupeBoutons(boutonsSkins, true);
    }

    public void GoBackToMain()
    {
        SwitchToCamera(mainCam);
    }

    private void ActiverGroupeBoutons(Collider[] groupe, bool etat)
    {
        foreach (Collider col in groupe)
        {
            if (col != null) col.enabled = etat;
        }
    }

    public void QuitterLeJeu()
    {
        Debug.Log("Le joueur a quitté le jeu !");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}