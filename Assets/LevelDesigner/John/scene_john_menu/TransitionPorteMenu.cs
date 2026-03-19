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
    public CinemachineVirtualCameraBase camDepart;    // La caméra devant la porte
    public CinemachineVirtualCameraBase camSequencer; // <-- TA CAMÉRA SÉQUENCEUR !

    [Header("4. Réglages")]
    public float tempsDansLeNoir = 1f; // Le temps d'attendre que la 1ère cam du séquenceur soit bien placée

    private bool sequenceEnCours = false;

    public void LancerLaSequence()
    {
        if (sequenceEnCours) return;
        StartCoroutine(SequenceComplete());
    }

    private IEnumerator SequenceComplete()
    {
        sequenceEnCours = true;

        // ACTE 1 : On s'assure d'être sur la caméra de départ
        if (navigator != null && camDepart != null) navigator.SwitchToCamera(camDepart);

        Quaternion rotationDepartGauche = pivotPorteGauche.rotation;
        Quaternion rotationDepartDroite = pivotPorteDroite.rotation;
        Quaternion rotationFinGauche = pivotPorteGauche.rotation * Quaternion.Euler(angleOuvertureGauche); 
        Quaternion rotationFinDroite = pivotPorteDroite.rotation * Quaternion.Euler(angleOuvertureDroite); 

        // ACTE 2 : Portes + Fondu au noir en même temps
        float tempsMax = Mathf.Max(tempsOuverture, tempsFade);
        float temps = 0f;

        while (temps < tempsMax)
        {
            temps += Time.deltaTime;
            float progressionPorte = Mathf.Clamp01(temps / tempsOuverture);
            pivotPorteGauche.rotation = Quaternion.Slerp(rotationDepartGauche, rotationFinGauche, progressionPorte);
            pivotPorteDroite.rotation = Quaternion.Slerp(rotationDepartDroite, rotationFinDroite, progressionPorte);

            float progressionFade = Mathf.Clamp01(temps / tempsFade);
            ecranNoir.alpha = Mathf.Lerp(0f, 1f, progressionFade);
            yield return null;
        }

        pivotPorteGauche.rotation = rotationFinGauche;
        pivotPorteDroite.rotation = rotationFinDroite;
        ecranNoir.alpha = 1f; // Il fait tout noir

        // ==========================================
        // ACTE 3 : ON LANCE LE SÉQUENCEUR !
        // ==========================================
        // En donnant la priorité à ta Sequencer Camera, elle va automatiquement
        // jouer sa 1ère caméra enfant, puis sa 2ème, selon ses propres réglages !
        if (navigator != null && camSequencer != null)
        {
            navigator.SwitchToCamera(camSequencer);
        }

        // On patiente un petit peu dans le noir pour que la 1ère caméra du séquenceur soit bien affichée
        yield return new WaitForSeconds(tempsDansLeNoir);

        // ACTE 4 : Retour à la lumière (Le séquenceur est déjà en train de faire son travail)
        temps = 0f;
        while (temps < tempsFade)
        {
            temps += Time.deltaTime;
            ecranNoir.alpha = Mathf.Lerp(1f, 0f, temps / tempsFade); 
            yield return null;
        }
        ecranNoir.alpha = 0f; 

        sequenceEnCours = false;
    }
}