using UnityEngine;
using Unity.Cinemachine;

public class SceneMenuNavigator : MonoBehaviour
{
    [Header("Caméras")]
    public CinemachineVirtualCameraBase mainCam;
    public CinemachineVirtualCameraBase optionsCam;
    public CinemachineVirtualCameraBase playCam;

    [Header("Les Colliders des Boutons (pour bloquer les clics)")]
    public Collider[] boutonsMainMenu; // Glisse ici Quit, Play, Options
    public Collider[] boutonsOptions;  // Glisse ici le bouton Back des options
    public Collider[] boutonsPlay;     // Glisse ici les boutons de la chambre (si tu en as)

    private void Start()
    {
        // Au lancement du jeu, on s'assure d'être sur le menu principal
        // Ça va automatiquement activer les bons boutons et bloquer les autres !
        SwitchToCamera(mainCam);
    }

    public void SwitchToCamera(CinemachineVirtualCameraBase targetCamera)
    {
        // 1. On gère les priorités des caméras
        if (mainCam != null) mainCam.Priority = 10;
        if (optionsCam != null) optionsCam.Priority = 10;
        if (playCam != null) playCam.Priority = 10;

        if (targetCamera != null) targetCamera.Priority = 20;

        // 2. On DÉSACTIVE les clics de tous les boutons pour faire le ménage
        ActiverGroupeBoutons(boutonsMainMenu, false);
        ActiverGroupeBoutons(boutonsOptions, false);
        ActiverGroupeBoutons(boutonsPlay, false);

        // 3. On ACTIVE uniquement les clics du menu qu'on regarde
        if (targetCamera == mainCam)
            ActiverGroupeBoutons(boutonsMainMenu, true);
        else if (targetCamera == optionsCam)
            ActiverGroupeBoutons(boutonsOptions, true);
        else if (targetCamera == playCam)
            ActiverGroupeBoutons(boutonsPlay, true);
    }

    public void GoBackToMain()
    {
        SwitchToCamera(mainCam);
    }

    // Petite fonction secrète pour allumer/éteindre toute une liste d'un coup
    private void ActiverGroupeBoutons(Collider[] groupe, bool etat)
    {
        foreach (Collider col in groupe)
        {
            // On vérifie que la case n'est pas vide pour éviter les erreurs
            if (col != null)
            {
                col.enabled = etat;
            }
        }
    }
}