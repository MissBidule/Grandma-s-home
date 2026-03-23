using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPEffects.Components;

public class SceneMenuNavigator : MonoBehaviour
{
    [System.Serializable]
    public struct MenuCamera
    {
        public string nomDuMenu;
        public CinemachineVirtualCameraBase camera;
        public Collider[] boutonsAssoicies;

        [Header("Animation des Textes (TMPEffects)")]
        public TMPWriter[] textesTMPWriters;

    }

    [Header("Caméra d'Introduction (Obligatoire)")]
    public CinemachineVirtualCameraBase sequencerCam;

    [Header("Configuration des Menus")]
    public MenuCamera[] configurationMenus;

    [Header("Raccourci Options")]
    public CinemachineVirtualCameraBase optionsSequencerCam;

    [Header("Réglages")]
    public float delaiCamera = 1.0f;

    [HideInInspector]
    public bool verrouillageAbsolu = false;

    private Coroutine transitionEnCours;
    private CinemachineVirtualCameraBase derniereCameraActive;

    private void Awake()
    {
        InitialiserPriorites();
        NettoyerTousLesTextes(); // 🎯 Force tout à disparaître au démarrage

        if (sequencerCam != null)
            SwitchToCamera(sequencerCam);
    }

    private void InitialiserPriorites()
    {
        if (sequencerCam != null) sequencerCam.Priority = 10;
        foreach (var menu in configurationMenus)
        {
            if (menu.camera != null) menu.camera.Priority = 10;
        }
    }

    public void AllerAuxOptions()
    {
        if (optionsSequencerCam != null) SwitchToCamera(optionsSequencerCam);
    }

    private void NettoyerTousLesTextes()
    {
        foreach (var menu in configurationMenus)
        {
            if (menu.textesTMPWriters != null)
            {
                foreach (var writer in menu.textesTMPWriters)
                {
                    if (writer != null)
                    {
                        writer.StopWriter();  // 🛑 Arrête l'animation en cours
                        writer.ResetWriter(); // ⏪ Rembobine
                        writer.gameObject.SetActive(false); // 🙈 Cache l'objet
                    }
                }
            }
        }
    }

    public void SwitchToCamera(CinemachineVirtualCameraBase targetCamera)
    {
        if (targetCamera == null) return;
        if (transitionEnCours != null) StopCoroutine(transitionEnCours);

        if (derniereCameraActive != null) derniereCameraActive.Priority = 10;
        targetCamera.Priority = 20;
        derniereCameraActive = targetCamera;

        transitionEnCours = StartCoroutine(GererBoutonsAvecDelai(targetCamera));
    }

    private IEnumerator GererBoutonsAvecDelai(CinemachineVirtualCameraBase targetCamera)
    {
        ActiverTousLesGroupesBoutons(false);
        NettoyerTousLesTextes(); // 🎯 On nettoie à nouveau au début de chaque switch

        // 🛑 PETITE PAUSE DE SÉCURITÉ (0.1s)
        // Indispensable pour que Cinemachine ait le temps de lancer le "Blending"
        yield return new WaitForSeconds(0.1f);

        if (Camera.main != null)
        {
            CinemachineBrain cerveau = Camera.main.GetComponent<CinemachineBrain>();
            if (cerveau != null)
            {
                while (cerveau.IsBlending || verrouillageAbsolu)
                    yield return null;
            }
        }

        var configMenu = configurationMenus.FirstOrDefault(m => m.camera == targetCamera);

       

        // ÉTAPE 4 : Apparition
        if (configMenu.textesTMPWriters != null)
        {
            foreach (var writer in configMenu.textesTMPWriters)
            {
                if (writer != null)
                {
                    writer.gameObject.SetActive(true);
                    writer.ResetWriter(); // Sécurité
                    writer.StartWriter(); // 🎬 Play !
                }
            }
        }

        if (configMenu.boutonsAssoicies != null && configMenu.boutonsAssoicies.Length > 0)
        {
            ActiverGroupeBoutons(configMenu.boutonsAssoicies, true);
        }
    }

    private void ActiverTousLesGroupesBoutons(bool etat)
    {
        foreach (var menu in configurationMenus) ActiverGroupeBoutons(menu.boutonsAssoicies, etat);
    }

    private void ActiverGroupeBoutons(Collider[] groupe, bool etat)
    {
        if (groupe == null) return;
        foreach (Collider col in groupe) if (col != null) col.enabled = etat;
    }
}