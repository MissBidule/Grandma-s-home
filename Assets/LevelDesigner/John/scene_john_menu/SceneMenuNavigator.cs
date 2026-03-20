using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Nécessaire pour l'astuce de recherche

public class SceneMenuNavigator : MonoBehaviour
{
    // --- NOUVELLE STRUCTURE ---
    [System.Serializable]
    public struct MenuCamera
    {
        public string nomDuMenu; // Juste pour l'organisation dans l'Inspector (ex: "Options")
        public CinemachineVirtualCameraBase camera;
        public Collider[] boutonsAssoicies;
    }
    // ----------------------------

    [Header("Caméra d'Introduction (Obligatoire)")]
    [Tooltip("La caméra qui se lance automatiquement au début.")]
    public CinemachineVirtualCameraBase sequencerCam;

    [Header("Configuration des Menus")]
    [Tooltip("Ajoute un élément pour chaque menu (Principal, Options, etc.). Glisse la caméra et ses boutons.")]
    public MenuCamera[] configurationMenus;

    [Header("Réglages")]
    [Tooltip("Temps d'attente avant d'activer les boutons après une transition.")]
    public float delaiCamera = 1.0f;

    // Références internes
    private Coroutine transitionEnCours;
    private CinemachineVirtualCameraBase derniereCameraActive;

    private void Awake()
    {
        // 1. Initialisation : On met TOUTES les caméras à 10
        InitialiserPriorites();

        // 2. On lance la séquence d'intro
        if (sequencerCam != null)
        {
            SwitchToCamera(sequencerCam);
        }
        else
        {
            Debug.LogError("⚠️ [SceneMenuNavigator] 'sequencerCam' n'est pas assignée dans l'Inspector !");
        }
    }

    /// <summary>
    /// Met toutes les caméras (Intro + Menus) à la priorité par défaut (10).
    /// </summary>
    private void InitialiserPriorites()
    {
        // Caméra d'intro
        if (sequencerCam != null) sequencerCam.Priority = 10;

        // Toutes les caméras de menus configurées
        foreach (var menu in configurationMenus)
        {
            if (menu.camera != null) menu.camera.Priority = 10;
        }
    }

    /// <summary>
    /// Change la caméra active en gérant les priorités et les boutons.
    /// </summary>
    public void SwitchToCamera(CinemachineVirtualCameraBase targetCamera)
    {
        if (targetCamera == null) return;

        // Arrêter la transition précédente si elle n'est pas finie
        if (transitionEnCours != null) StopCoroutine(transitionEnCours);

        // 1. Désactiver proprement l'ancienne caméra (Priorité 10)
        if (derniereCameraActive != null) derniereCameraActive.Priority = 10;

        // 2. Activer la nouvelle (Priorité 20)
        targetCamera.Priority = 20;
        derniereCameraActive = targetCamera;

        // 3. Gérer les boutons avec délai
        transitionEnCours = StartCoroutine(GererBoutonsAvecDelai(targetCamera));
    }

    private IEnumerator GererBoutonsAvecDelai(CinemachineVirtualCameraBase targetCamera)
    {
        // ÉTAPE 1 : On désactive TOUS les boutons instantanément
        ActiverTousLesGroupesBoutons(false);

        // ÉTAPE 2 : On attend 1 micro-seconde pour que Cinemachine démarre son mouvement
        yield return null;

        // ÉTAPE 3 : LA MAGIE 🎯 On récupère le Cerveau de la caméra principale
        CinemachineBrain cerveau = Camera.main.GetComponent<CinemachineBrain>();

        if (cerveau != null)
        {
            // Tant que le Cerveau est en train de faire une transition (IsBlending)...
            while (cerveau.IsBlending)
            {
                // ... on met le script en pause !
                yield return null;
            }
        }
        else
        {
            // (Sécurité au cas où le cerveau n'est pas trouvé)
            yield return new WaitForSeconds(delaiCamera);
        }

        // ÉTAPE 4 : Le mouvement est 100% terminé ! On allume les boutons du bon menu.
        var configMenu = configurationMenus.FirstOrDefault(m => m.camera == targetCamera);

        if (configMenu.boutonsAssoicies != null && configMenu.boutonsAssoicies.Length > 0)
        {
            ActiverGroupeBoutons(configMenu.boutonsAssoicies, true);
        }
    }

    // --- Fonctions d'aide (Helpers) ---

    private void ActiverTousLesGroupesBoutons(bool etat)
    {
        foreach (var menu in configurationMenus)
        {
            ActiverGroupeBoutons(menu.boutonsAssoicies, etat);
        }
    }

    private void ActiverGroupeBoutons(Collider[] groupe, bool etat)
    {
        if (groupe == null) return;
        foreach (Collider col in groupe)
        {
            if (col != null) col.enabled = etat;
        }
    }

    public void QuitterLeJeu()
    {
        Debug.Log("Quitter le jeu...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}