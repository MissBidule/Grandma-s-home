using UnityEngine;
using Unity.Cinemachine;
using System.Collections; // INDISPENSABLE pour utiliser les Coroutines (les délais)

public class SceneMenuNavigator : MonoBehaviour
{
    [Header("Caméras")]
    public CinemachineVirtualCameraBase mainCam;
    public CinemachineVirtualCameraBase optionsCam;
    public CinemachineVirtualCameraBase playCam;
    public CinemachineVirtualCameraBase quitCam;
    public CinemachineVirtualCameraBase TVCam;
    public CinemachineVirtualCameraBase PosterCam;
    public CinemachineVirtualCameraBase SkinsCam;


    [Header("Réglages de Transition")]
    public float delaiCamera = 1.5f; // Le temps que met ta caméra à bouger (en secondes)

    [Header("Les Colliders des Boutons (pour bloquer les clics)")]
    public Collider[] boutonsMainMenu;
    public Collider[] boutonsOptions;
    public Collider[] boutonsPlay;
    public Collider[] boutonsSkins;

    // Variable secrète pour mémoriser si une transition est en cours
    private Coroutine transitionEnCours;

    private void Start()
    {
        // Au lancement, on va sur le menu principal (les boutons s'activeront après le délai)
        SwitchToCamera(mainCam);
    }

    public void SwitchToCamera(CinemachineVirtualCameraBase targetCamera)
    {
        // Si on reclique pendant que la caméra bougeait déjà, on annule l'ancien timer
        if (transitionEnCours != null)
        {
            StopCoroutine(transitionEnCours);
        }

        // 1. On gère les priorités des caméras (J'ai corrigé quitCam et ajouté TVCam !)
        if (mainCam != null) mainCam.Priority = 10;
        if (optionsCam != null) optionsCam.Priority = 10;
        if (playCam != null) playCam.Priority = 10;
        if (quitCam != null) quitCam.Priority = 10;
        if (TVCam != null) TVCam.Priority = 10;
        if (PosterCam != null) PosterCam.Priority = 10;
        if (SkinsCam != null) SkinsCam.Priority = 10;

        if (targetCamera != null) targetCamera.Priority = 20;

        // 2. On lance notre fonction temporelle (la Coroutine)
        transitionEnCours = StartCoroutine(GererBoutonsAvecDelai(targetCamera));
    }

    // --- LA COROUTINE QUI GÈRE LE TEMPS ---
    private IEnumerator GererBoutonsAvecDelai(CinemachineVirtualCameraBase targetCamera)
    {
        // ÉTAPE 1 : On DÉSACTIVE TOUT immédiatement au moment du clic
        // Comme ça, pendant que la caméra vole, RIEN n'est cliquable.
        ActiverGroupeBoutons(boutonsMainMenu, false);
        ActiverGroupeBoutons(boutonsOptions, false);
        ActiverGroupeBoutons(boutonsPlay, false);
        ActiverGroupeBoutons(boutonsSkins, false);

        // ÉTAPE 2 : LA PAUSE
        // Le script s'arrête ici et attend le temps indiqué dans l'Inspecteur
        yield return new WaitForSeconds(delaiCamera);

        // ÉTAPE 3 : La caméra est arrivée ! On ACTIVE uniquement les bons boutons.
        if (targetCamera == mainCam)
            ActiverGroupeBoutons(boutonsMainMenu, true);
        else if (targetCamera == optionsCam)
            ActiverGroupeBoutons(boutonsOptions, true);
        else if (targetCamera == playCam)
            ActiverGroupeBoutons(boutonsPlay, true);
        else if (targetCamera == SkinsCam)
            ActiverGroupeBoutons(boutonsSkins, true);
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


    // --- FONCTION POUR QUITTER LE JEU ---
    public void QuitterLeJeu()
    {
        // 1. Affiche un message dans la console pour prouver que ça marche
        Debug.Log("Le joueur a quitté le jeu !");

        // 2. Ferme le vrai jeu (une fois exporté/buildé)
        Application.Quit();

        // 3. (Bonus de Pro) Arrête le mode "Play" dans l'éditeur Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}