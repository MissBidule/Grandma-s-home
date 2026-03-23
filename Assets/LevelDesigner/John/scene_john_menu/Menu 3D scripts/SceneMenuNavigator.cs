using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SceneMenuNavigator : MonoBehaviour
{
    [System.Serializable]
    public struct MenuCamera
    {
        public string nomDuMenu;
        public CinemachineVirtualCameraBase camera;
        public Collider[] boutonsAssoicies;

        [Header("Animation des Textes")]
        [Tooltip("Glisse ici TOUS les textes TextMeshPro de ce menu qui doivent rebondir")]
        public EffetTextePlop[] textesTMPAssocies; // 🎯 NOUVEAU : C'est un tableau maintenant [] !

        [Header("Réglage Séquenceur")]
        public float tempsAttenteSequencer;
    }

    [Header("Caméra d'Introduction (Obligatoire)")]
    public CinemachineVirtualCameraBase sequencerCam;

    [Header("Configuration des Menus")]
    public MenuCamera[] configurationMenus;

    [Header("Réglages")]
    public float delaiCamera = 1.0f;

    [HideInInspector]
    public bool verrouillageAbsolu = false;

    private Coroutine transitionEnCours;
    private CinemachineVirtualCameraBase derniereCameraActive;

    private void Awake()
    {
        InitialiserPriorites();

        if (sequencerCam != null)
        {
            SwitchToCamera(sequencerCam);
        }
        else
        {
            Debug.LogError("⚠️ [SceneMenuNavigator] 'sequencerCam' n'est pas assignée !");
        }
    }

    private void InitialiserPriorites()
    {
        if (sequencerCam != null) sequencerCam.Priority = 10;

        foreach (var menu in configurationMenus)
        {
            if (menu.camera != null) menu.camera.Priority = 10;
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
        // 1. Désactiver tous les boutons
        ActiverTousLesGroupesBoutons(false);

        // 2. Cacher TOUS les textes de TOUS les menus
        foreach (var menu in configurationMenus)
        {
            if (menu.textesTMPAssocies != null)
            {
                foreach (var texte in menu.textesTMPAssocies)
                {
                    if (texte != null) texte.gameObject.SetActive(false);
                }
            }
        }

        yield return null;

        // 3. Attendre que Cinemachine termine son mouvement
        if (Camera.main != null)
        {
            CinemachineBrain cerveau = Camera.main.GetComponent<CinemachineBrain>();

            if (cerveau != null)
            {
                while (cerveau.IsBlending || verrouillageAbsolu)
                {
                    yield return null;
                }
            }
            else
            {
                float temps = 0f;
                while (temps < delaiCamera || verrouillageAbsolu)
                {
                    temps += Time.deltaTime;
                    yield return null;
                }
            }
        }
        else
        {
            yield return new WaitForSeconds(delaiCamera);
        }

        // ==========================================
        // LA CAMÉRA S'EST ARRÊTÉE !
        // ==========================================
        var configMenu = configurationMenus.FirstOrDefault(m => m.camera == targetCamera);

        if (configMenu.tempsAttenteSequencer > 0f)
        {
            yield return new WaitForSeconds(configMenu.tempsAttenteSequencer);
        }

        // 🎯 NOUVEAU : On fait apparaître TOUS les textes de ce menu précis !
        if (configMenu.textesTMPAssocies != null)
        {
            foreach (var texte in configMenu.textesTMPAssocies)
            {
                if (texte != null) texte.Apparaitre();
            }
        }

        // On allume les boutons du menu
        if (configMenu.boutonsAssoicies != null && configMenu.boutonsAssoicies.Length > 0)
        {
            ActiverGroupeBoutons(configMenu.boutonsAssoicies, true);
        }
    }

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