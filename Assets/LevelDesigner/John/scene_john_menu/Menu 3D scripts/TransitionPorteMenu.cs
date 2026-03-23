using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class TransitionDoublePorteMenu : MonoBehaviour
{
    [Header("1. La Double Porte")]
    public Transform pivotPorteGauche;
    public Transform pivotPorteDroite;
    public Vector3 angleOuvertureGauche = new Vector3(0f, 90f, 0f);
    public Vector3 angleOuvertureDroite = new Vector3(0f, -90f, 0f);
    public float tempsOuverture = 1.5f;

    [Header("2. Le Fondu (Fade)")]
    public CanvasGroup ecranNoir;
    public float tempsFade = 1.5f;
    public float tempsFadeIn = 0.5f;

    [Header("3. Caméras")]
    public SceneMenuNavigator navigator;
    public CinemachineVirtualCameraBase camDepart;
    public CinemachineVirtualCameraBase camSequencer;
    public CinemachineVirtualCameraBase camMainFinale;

    [Header("4. Réglages")]
    public float tempsDansLeNoir = 1f;
    public float dureeZoomSequencer = 3.0f;

    [Header("5. Sécurité Démarrage")]
    [Tooltip("Temps en secondes pendant lequel le joueur ne peut pas cliquer au tout début du jeu")]
    public float delaiAvantAutoriserClic = 3.0f;
    private float chronometreDemarrage = 0f;

    private bool sequenceEnCours = false;
    private bool introDejaJouee = false;

    private void Update()
    {
        // ==========================================
        // PRE-ÉTAPE : SÉCURITÉ ET ATTENTE D'INPUT
        // ==========================================

        // 1. On fait tourner le chronomètre depuis le lancement du jeu
        chronometreDemarrage += Time.deltaTime;

        // 2. Si la caméra d'intro n'a pas fini d'arriver, on bloque les clics !
        if (chronometreDemarrage < delaiAvantAutoriserClic)
        {
            return;
        }

        // 3. La caméra est en place, on attend que le joueur appuie sur une touche
        if (!introDejaJouee && Input.anyKeyDown)
        {
            introDejaJouee = true;
            LancerLaSequence();
        }
    }

    public void LancerLaSequence()
    {
        if (sequenceEnCours) return;
        StartCoroutine(SequenceComplete());
    }

    private IEnumerator SequenceComplete()
    {
        sequenceEnCours = true;

        // ==========================================
        // ÉTAPE 1 : PLACEMENT DE LA CAMÉRA DE DÉPART
        // ==========================================
        if (navigator != null && camDepart != null) navigator.SwitchToCamera(camDepart);

        // On sauvegarde les angles de base pour calculer l'ouverture des portes
        Quaternion rotationDepartGauche = pivotPorteGauche.rotation;
        Quaternion rotationDepartDroite = pivotPorteDroite.rotation;
        Quaternion rotationFinGauche = pivotPorteGauche.rotation * Quaternion.Euler(angleOuvertureGauche);
        Quaternion rotationFinDroite = pivotPorteDroite.rotation * Quaternion.Euler(angleOuvertureDroite);

        float tempsMax = Mathf.Max(tempsOuverture, tempsFade);
        float temps = 0f;

        // ==========================================
        // ÉTAPE 2 : ANIMATION PORTES + FONDU AU NOIR
        // ==========================================
        while (temps < tempsMax)
        {
            temps += Time.deltaTime;

            // On ouvre la porte progressivement
            float progressionPorte = Mathf.Clamp01(temps / tempsOuverture);
            pivotPorteGauche.rotation = Quaternion.Slerp(rotationDepartGauche, rotationFinGauche, progressionPorte);
            pivotPorteDroite.rotation = Quaternion.Slerp(rotationDepartDroite, rotationFinDroite, progressionPorte);

            // On obscurcit l'écran en même temps
            float progressionFade = Mathf.Clamp01(temps / tempsFade);
            if (ecranNoir != null) ecranNoir.alpha = Mathf.Lerp(0f, 1f, progressionFade);

            yield return null;
        }

        // Sécurité : on s'assure que tout est parfaitement en place (Portes grandes ouvertes, écran 100% noir)
        pivotPorteGauche.rotation = rotationFinGauche;
        pivotPorteDroite.rotation = rotationFinDroite;
        if (ecranNoir != null) ecranNoir.alpha = 1f;

        // ==========================================
        // ÉTAPE 3 : LANCEMENT DU SÉQUENCEUR (DANS LE NOIR)
        // ==========================================
        if (navigator != null && camSequencer != null)
        {
            navigator.SwitchToCamera(camSequencer);
        }

        // On attend un peu que Cinemachine place sa nouvelle caméra avant de rallumer
        yield return new WaitForSeconds(tempsDansLeNoir);

        // ==========================================
        // ÉTAPE 4 : RETOUR DE LA LUMIÈRE (FADE IN RAPIDE)
        // ==========================================
        temps = 0f;
        while (temps < tempsFadeIn)
        {
            temps += Time.deltaTime;
            if (ecranNoir != null) ecranNoir.alpha = Mathf.Lerp(1f, 0f, temps / tempsFadeIn);
            yield return null;
        }
        if (ecranNoir != null) ecranNoir.alpha = 0f; // La vue est dégagée !

        // ==========================================
        // ÉTAPE 5 : ATTENTE DU ZOOM SÉQUENCEUR ET MENU FINAL
        // ==========================================

        // On laisse le séquenceur faire son mouvement d'approche vers le salon
        yield return new WaitForSeconds(dureeZoomSequencer);

        // Le zoom est fini, on donne enfin le contrôle de la caméra principale !
        if (navigator != null && camMainFinale != null)
        {
            navigator.SwitchToCamera(camMainFinale);
        }

        sequenceEnCours = false;
    }
}