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

    [Header("3. Caméras")]
    public SceneMenuNavigator navigator;
    public CinemachineVirtualCameraBase camDepart;
    public CinemachineVirtualCameraBase camSequencer;
    public CinemachineVirtualCameraBase camMainFinale; // <--- NOUVEAU : La caméra du menu final

    [Header("4. Réglages")]
    public float tempsDansLeNoir = 1f;
    public float dureeZoomSequencer = 3.0f; // <--- NOUVEAU : Le temps du zoom avant de passer au menu

    private bool sequenceEnCours = false;

    public void LancerLaSequence()
    {
        if (sequenceEnCours) return;
        StartCoroutine(SequenceComplete());
    }

    private IEnumerator SequenceComplete()
    {
        sequenceEnCours = true;

        // ACTE 1 : Caméra de départ
        if (navigator != null && camDepart != null) navigator.SwitchToCamera(camDepart);

        Quaternion rotationDepartGauche = pivotPorteGauche.rotation;
        Quaternion rotationDepartDroite = pivotPorteDroite.rotation;
        Quaternion rotationFinGauche = pivotPorteGauche.rotation * Quaternion.Euler(angleOuvertureGauche);
        Quaternion rotationFinDroite = pivotPorteDroite.rotation * Quaternion.Euler(angleOuvertureDroite);

        // ACTE 2 : Portes + Fondu
        float tempsMax = Mathf.Max(tempsOuverture, tempsFade);
        float temps = 0f;

        while (temps < tempsMax)
        {
            temps += Time.deltaTime;
            float progressionPorte = Mathf.Clamp01(temps / tempsOuverture);
            pivotPorteGauche.rotation = Quaternion.Slerp(rotationDepartGauche, rotationFinGauche, progressionPorte);
            pivotPorteDroite.rotation = Quaternion.Slerp(rotationDepartDroite, rotationFinDroite, progressionPorte);

            float progressionFade = Mathf.Clamp01(temps / tempsFade);
            if (ecranNoir != null) ecranNoir.alpha = Mathf.Lerp(0f, 1f, progressionFade);
            yield return null;
        }

        pivotPorteGauche.rotation = rotationFinGauche;
        pivotPorteDroite.rotation = rotationFinDroite;
        if (ecranNoir != null) ecranNoir.alpha = 1f;

        // ACTE 3 : Lancement du séquenceur (Le Zoom)
        if (navigator != null && camSequencer != null)
        {
            navigator.SwitchToCamera(camSequencer);
        }

        yield return new WaitForSeconds(tempsDansLeNoir);

        // ACTE 4 : Retour à la lumière
        temps = 0f;
        while (temps < tempsFade)
        {
            temps += Time.deltaTime;
            if (ecranNoir != null) ecranNoir.alpha = Mathf.Lerp(1f, 0f, temps / tempsFade);
            yield return null;
        }
        if (ecranNoir != null) ecranNoir.alpha = 0f;

        // ==========================================
        // ACTE 5 : ATTENTE DU ZOOM PUIS MAIN CAM
        // ==========================================
        // On attend que le séquenceur finisse son mouvement de zoom
        yield return new WaitForSeconds(dureeZoomSequencer);

        // On bascule automatiquement sur la caméra du menu principal
        if (navigator != null && camMainFinale != null)
        {
            navigator.SwitchToCamera(camMainFinale);
        }

        sequenceEnCours = false;
    }
}