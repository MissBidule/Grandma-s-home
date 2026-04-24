using System;
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPEffects.Components;
using UnityEngine.UI;

public class SceneMenuNavigator : MonoBehaviour
{
    [System.Serializable]
    public struct MenuCamera //structure pour config menu dans l'inspector
    {
        public string nomDuMenu; //pour orga
        public CinemachineVirtualCameraBase camera; //ref cam pour ce menu
        public Collider[] boutonsAssoicies; //ref colliders des boutons à activer pour ce menu

        [Header("Animation des Textes (TMPWriters)")]
        public TMPWriter[] textesTMPWriters; //ref des TMPWriter pour les textes à animer dans ce menu

    }
    private static bool AlreadyStarted = false; 

    [Header("Caméra d'Introduction Start")]
    public CinemachineVirtualCameraBase sequencerCam; //ref cam Sequencer Camera Start

    [Header("Caméra de retour au lobby")]
    public CinemachineVirtualCameraBase PlayCam; //ref cam Retour play Camera
    public TransitionVersUI LobbyCam; //ref cam Retour Lobby Camera
    public GameObject LobbyCanvas; //ref canvas du lobby pour l'activer au bon moment

    [Header("Configuration des Menus")]
    public MenuCamera[] configurationMenus; //pour orga et config dans l'inspector


    [Header("Réglages")]
    public float delaiCamera = 1.0f; //délai pour activer les boutons après le switch de cam


    private Coroutine transitionEnCours; //pour stock la coroutine en cours et éviter que le joueur switch rapidement de cam
    private CinemachineVirtualCameraBase derniereCameraActive; //stock la dernière cam active pour gérer les priorités

    //init des prio des cam et desactiver les textes
    private void Awake()
    {
        InitialiserPriorites();

        if (sequencerCam != null && !AlreadyStarted) {
            SwitchToCamera(sequencerCam);
            AlreadyStarted = true;
        }
        else
        {
            SwitchToCamera(PlayCam);
        }
    }

    private void Start()
    {
        NettoyerTousLesTextes(); 
    }

    //met les prio des cam à 10 pour que la cam du sequencer start soit prio au début 
    private void InitialiserPriorites()
    {
        if (sequencerCam != null) sequencerCam.Priority = 10;
        foreach (var menu in configurationMenus)
        {
            if (menu.camera != null) menu.camera.Priority = 10;
        }
    }

    //desactive les TMPWriter de tous les menus pour éviter de les voir avant le switch de cam
    private void NettoyerTousLesTextes()
    {
        foreach (MenuCamera menu in configurationMenus)
        {
            if (menu.textesTMPWriters == null)
                continue;
            
            foreach (TMPWriter writer in menu.textesTMPWriters)
            {
                if (writer == null)
                    continue;
                
                // Removed the call to ResetWriter() here to prevent it from resetting before the writer is activated
                // Just useless and clutters the console with warnings about missing text components when the writer is disabled
                writer.gameObject.SetActive(false);
            }
        }
    }

    public void BackToLobby()
    {
        LobbyCam.LancerLaTransition();
        LobbyCanvas.SetActive(true);
    }

    //switch de cam avec gestion des prio et activation des boutons associés
    public void SwitchToCamera(CinemachineVirtualCameraBase targetCamera)
    {
        if (targetCamera == null) return;
        if (transitionEnCours != null) StopCoroutine(transitionEnCours);

        if (derniereCameraActive != null) derniereCameraActive.Priority = 10; //remet la prio de la dernière cam plus basse pour etre inactive
        targetCamera.Priority = 20; //met la cam cible à une prio plus haute pour qu'elle devienne active
        derniereCameraActive = targetCamera;

        transitionEnCours = StartCoroutine(GererBoutonsAvecDelai(targetCamera));
    }
    //coroutine pour gérer l'activation des boutons associés et tmpwriter à la cam avec delai
    private IEnumerator GererBoutonsAvecDelai(CinemachineVirtualCameraBase targetCamera)
    {
        ActiverTousLesGroupesBoutons(false);
        NettoyerTousLesTextes(); 

        yield return new WaitForSeconds(0.1f);

        //attend la fin du blend de cam avant pour éviter les clicks pendant le switch
        if (Camera.main != null)
        {
            CinemachineBrain cerveau = Camera.main.GetComponent<CinemachineBrain>();
            if (cerveau != null)
            {
                while (cerveau.IsBlending)
                    yield return null;
            }
        }

        var configMenu = configurationMenus.FirstOrDefault(m => m.camera == targetCamera);

        //active les textes tmpwriter du menu associé à la cam
        if (configMenu.textesTMPWriters != null)
        {
            foreach (var writer in configMenu.textesTMPWriters)
            {
                if (writer != null)
                {
                    writer.gameObject.SetActive(true);
                    writer.ResetWriter(); 
                    writer.StartWriter(); 
                }
            }
        }
        //active les boutons associés à la cam
        if (configMenu.boutonsAssoicies != null && configMenu.boutonsAssoicies.Length > 0)
        {
            ActiverGroupeBoutons(configMenu.boutonsAssoicies, true);
        }
    }
    //désactive les boutons pour éviter les clicks pendant le switch de cam
    private void ActiverTousLesGroupesBoutons(bool etat)
    {
        foreach (var menu in configurationMenus) ActiverGroupeBoutons(menu.boutonsAssoicies, etat);
    }
    //active ou désactive grp boutons 
    private void ActiverGroupeBoutons(Collider[] groupe, bool etat)
    {
        if (groupe == null) return;
        foreach (Collider col in groupe) if (col != null) col.enabled = etat;
    }
    //quit le jeu et stop le play mode
    public void QuitterLeJeu()
    {
        Debug.Log("Quitter le jeu...");

       
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}